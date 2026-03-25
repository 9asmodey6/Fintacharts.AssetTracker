namespace Fintacharts.AssetTracker.BackgroundServices;

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
using Shared.Events;
using Shared.Interfaces;

public class PriceUpdateWorker : BackgroundService
{
    private readonly FintachartsTokenManager _tokenManager;
    private readonly PriceCache _cache;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<FintachartsOptions> _options;
    private readonly ILogger<PriceUpdateWorker> _logger;
    private readonly IEventBus _eventBus;

    private volatile List<string> _instrumentIds = new();

    private CancellationTokenSource? _sessionCts;

    public PriceUpdateWorker(
        FintachartsTokenManager tokenManager,
        PriceCache cache,
        IServiceScopeFactory scopeFactory,
        IOptions<FintachartsOptions> options,
        ILogger<PriceUpdateWorker> logger,
        IEventBus eventBus)
    {
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
        var isChanged = evt.InstrumentIds.Count != _instrumentIds.Count ||
                        evt.InstrumentIds.Except(_instrumentIds).Any();

        if (isChanged)
        {
            _logger.LogInformation("IDs changed. Reconnecting...");
            _instrumentIds = evt.InstrumentIds.ToList();
            _sessionCts?.Cancel();
            _logger.LogInformation("Instrument list changed. Reconnecting. New count: {Count}",
                evt.InstrumentIds.Count);
        }
        else
        {
            _logger.LogInformation("Instruments list hasn't changed. Keeping current socket.");
        }
    }


    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PriceUpdateWorker starting...");

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

            _logger.LogInformation(
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

            await SavePriceAsync(message.InstrumentId!, price, ct);
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
}

internal record WsMessage(
    [property: JsonPropertyName("type")] string? Type,
    [property: JsonPropertyName("instrumentId")]
    string? InstrumentId,
    [property: JsonPropertyName("ask")] PriceData? Ask,
    [property: JsonPropertyName("bid")] PriceData? Bid,
    [property: JsonPropertyName("last")] PriceData? Last);

internal record PriceData(
    [property: JsonPropertyName("price")] decimal Price,
    [property: JsonPropertyName("timestamp")]
    DateTime Timestamp);