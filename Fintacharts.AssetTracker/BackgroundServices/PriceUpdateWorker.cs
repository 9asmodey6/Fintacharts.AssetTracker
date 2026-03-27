namespace Fintacharts.AssetTracker.BackgroundServices;

using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Infrastructure.Cache;
using Infrastructure.Fintacharts;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Models;
using Shared.Consts;
using Shared.Events;
using Shared.Interfaces;

public class PriceUpdateWorker : BackgroundService
{
    private readonly FintachartsRestClient _restClient;
    private readonly FintachartsTokenManager _tokenManager;
    private readonly PriceCache _cache;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<FintachartsOptions> _options;
    private readonly ILogger<PriceUpdateWorker> _logger;
    private readonly IEventBus _eventBus;

    private volatile List<string> _instrumentIds = new();

    private readonly ConcurrentDictionary<string, CachedPrice> _dbBuffer = new();
    
    private CancellationTokenSource? _sessionCts;

    public PriceUpdateWorker(
        FintachartsRestClient restClient,
        FintachartsTokenManager tokenManager,
        PriceCache cache,
        IServiceScopeFactory scopeFactory,
        IOptions<FintachartsOptions> options,
        ILogger<PriceUpdateWorker> logger,
        IEventBus eventBus)
    {
        _restClient = restClient;
        _tokenManager = tokenManager;
        _cache = cache;
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
        _eventBus = eventBus;

        _eventBus.Subscribe<InstrumentsSyncedEvent>(OnInstrumentsSynced);
    }

    private void OnInstrumentsSynced(InstrumentsSyncedEvent evt)
    {
        var incoming = new HashSet<string>(evt.InstrumentIds);
        var current = new HashSet<string>(_instrumentIds);

        if (incoming.SetEquals(current))
        {
            _logger.LogInformation("Instruments unchanged. Keeping current socket.");
            return;
        }

        _logger.LogInformation(
            "Instrument list changed. Reconnecting. New count: {Count}", evt.InstrumentIds.Count);
        _instrumentIds = evt.InstrumentIds.ToList();
        _sessionCts?.Cancel();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PriceUpdateWorker starting...");

        await SeedInstrumentsAsync(stoppingToken);
        
        _ = Task.Run(() => StartDatabaseFlushingAsync(stoppingToken), stoppingToken);
        
        while (!stoppingToken.IsCancellationRequested)
        {
            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            _sessionCts = combinedCts;

            try
            {
                await ConnectAndListenAsync(_sessionCts.Token);
            }
            catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Reconnecting due to new instrument data...");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "WebSocket error. Reconnecting in 5s...");
                try
                {
                    await Task.Delay(5000, stoppingToken);
                }
                catch
                {
                    break;
                }
            }
        }
    }

    private async Task ConnectAndListenAsync(CancellationToken ct)
    {
        var instrumentIds = _instrumentIds.Count > 0
            ? _instrumentIds
            : await GetInstrumentIdsAsync(ct);

        if (instrumentIds.Count == 0)
        {
            _logger.LogWarning("No instruments to subscribe. Waiting for event...");

            await Task.Delay(Timeout.Infinite, ct);
            return;
        }

        var token = await _tokenManager.GetAccessTokenAsync(ct);
        var url = $"{_options.Value.WssUrl}/api/streaming/ws/v1/realtime?token={token}";

        using var ws = new ClientWebSocket();
        await ws.ConnectAsync(new Uri(url), ct);
        _logger.LogInformation("WebSocket connected. Subscribing to {Count} instruments", instrumentIds.Count);

        foreach (var instrumentId in instrumentIds)
        {
            await SubscribeAsync(ws, instrumentId, ct);
        }

        await ReceiveLoopAsync(ws, ct);
    }

    private async Task<List<string>> GetInstrumentIdsAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.Instruments.Select(x => x.Id).ToListAsync(ct);
    }

    private async Task SubscribeAsync(
        ClientWebSocket ws,
        string instrumentId,
        CancellationToken ct)
    {
        var message = new
        {
            type = "l1-subscription",
            id = instrumentId,
            instrumentId = instrumentId,
            provider = "oanda",
            subscribe = true,
            kinds = new[] { "ask", "bid", "last" }
        };

        var json = JsonSerializer.Serialize(message);
        var bytes = Encoding.UTF8.GetBytes(json);

        await ws.SendAsync(
            new ArraySegment<byte>(bytes),
            WebSocketMessageType.Text,
            endOfMessage: true,
            ct);
    }


    private async Task ReceiveLoopAsync(ClientWebSocket ws, CancellationToken ct)
    {
        var buffer = new byte[4096];
        using var ms = new MemoryStream();

        while (ws.State == WebSocketState.Open && !ct.IsCancellationRequested)
        {
            WebSocketReceiveResult result;

            do
            {
                result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), ct);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    _logger.LogWarning("WebSocket closed by server");
                    return;
                }

                ms.Write(buffer, 0, result.Count);
            } while (!result.EndOfMessage);

            var json = Encoding.UTF8.GetString(ms.ToArray());
            ms.SetLength(0);

            await ProcessMessageAsync(json, ct);
        }
    }

    private async Task ProcessMessageAsync(string json, CancellationToken ct)
    {
        try
        {
            var message = JsonSerializer.Deserialize<WsMessage>(json);

            if (message?.Type != "l1-update") return;

            _logger.LogDebug(
                "Tick: {Symbol} | Bid: {Bid:0.####} | Ask: {Ask:0.####} | Last: {Last:0.####} | Time: {Time:HH:mm:ss.fff}",
                message.InstrumentId,
                message.Bid?.Price ?? 0,
                message.Ask?.Price ?? 0,
                message.Last?.Price ?? 0,
                DateTime.Now);

            var price = new CachedPrice(
                Bid: message.Bid?.Price ?? 0,
                Ask: message.Ask?.Price ?? 0,
                Last: message.Last?.Price ?? 0,
                UpdatedAt: message.Bid?.Timestamp
                           ?? message.Ask?.Timestamp
                           ?? message.Last?.Timestamp
                           ?? DateTime.UtcNow);

            _cache.Set(message.InstrumentId!, price);

            _dbBuffer[message.InstrumentId!] = price;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process WebSocket message: {Json}", json);
        }
    }

    private async Task SavePriceAsync(
        string instrumentId,
        CachedPrice price,
        CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var updatedAtUtc = price.UpdatedAt.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(price.UpdatedAt, DateTimeKind.Utc)
            : price.UpdatedAt.ToUniversalTime();

        await db.Database.ExecuteSqlInterpolatedAsync($"""
                                                       INSERT INTO asset_prices (instrument_id, bid, ask, last, updated_at)
                                                       VALUES ({instrumentId}, {price.Bid}, {price.Ask}, {price.Last}, {updatedAtUtc})
                                                       ON CONFLICT (instrument_id)
                                                       DO UPDATE SET
                                                           bid = EXCLUDED.bid,
                                                           ask = EXCLUDED.ask,
                                                           last = EXCLUDED.last,
                                                           updated_at = EXCLUDED.updated_at
                                                       """, ct);
    }

    private async Task SeedInstrumentsAsync(
        CancellationToken ct)
    {
        using (var scope = _scopeFactory.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            _logger.LogInformation("Syncing instruments from Fintacharts...");

            var instruments = await _restClient.GetInstrumentsAsync(ct: ct);
            var instrumentIds = instruments.Select(x => x.Id).ToList();

            foreach (var i in instruments)
            {
                await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
                                                                      INSERT INTO instruments (id, symbol, description, kind, provider)
                                                                      VALUES ({i.Id}, {i.Symbol}, {i.Description}, {i.Kind}, {FintachartsConstants.DefaultProvider})
                                                                      ON CONFLICT (id) 
                                                                      DO UPDATE SET
                                                                          symbol = EXCLUDED.symbol,
                                                                          description = EXCLUDED.description,
                                                                          kind = EXCLUDED.kind,
                                                                          provider = EXCLUDED.provider
                                                                      """, ct);
            }

            _logger.LogInformation("Instruments synced successfully (Upserted {Count} items)", instruments.Count);
        }
    }

    private async Task StartDatabaseFlushingAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));

    try
    {
        while (await timer.WaitForNextTickAsync(ct))
        {
            if (_dbBuffer.IsEmpty) continue;
            
            var itemsToSave = _dbBuffer.ToArray();
            _dbBuffer.Clear();

            _logger.LogInformation("Flushing {Count} prices to database...", itemsToSave.Length);
            
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            foreach (var item in itemsToSave)
            {
                var instrumentId = item.Key;
                var price = item.Value;
                
                var updatedAtUtc = price.UpdatedAt.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(price.UpdatedAt, DateTimeKind.Utc)
                    : price.UpdatedAt.ToUniversalTime();
                
                await db.Database.ExecuteSqlInterpolatedAsync($"""
                    INSERT INTO asset_prices (instrument_id, bid, ask, last, updated_at)
                    VALUES ({instrumentId}, {price.Bid}, {price.Ask}, {price.Last}, {updatedAtUtc})
                    ON CONFLICT (instrument_id)
                    DO UPDATE SET
                        bid = EXCLUDED.bid,
                        ask = EXCLUDED.ask,
                        last = EXCLUDED.last,
                        updated_at = EXCLUDED.updated_at
                    """, ct);
            }
        }
    }
    catch (OperationCanceledException) { /* Это нормально при выключении */ }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error during database flushing");
    }
    }
}