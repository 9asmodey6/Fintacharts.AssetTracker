namespace Fintacharts.AssetTracker.BackgroundServices;

using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Infrastructure.Cache;
using Infrastructure.Fintacharts;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Models;
using Shared.Services;

public class PriceUpdateWorker(
    FintachartsTokenManager tokenManager,
    PriceCache cache,
    IServiceScopeFactory scopeFactory,
    IOptions<FintachartsOptions> options,
    InstrumentSyncNotifier notifier,
    ILogger<PriceUpdateWorker> logger) : BackgroundService
{
    private readonly ConcurrentDictionary<string, CachedPrice> _dbBuffer = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("PriceUpdateWorker starting...");

        _ = Task.Run(() => StartDatabaseFlushingAsync(stoppingToken), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(
                stoppingToken, notifier.ReconnectToken);

            try
            {
                await ConnectAndListenAsync(combinedCts.Token);
            }
            catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogInformation("Reconnecting due to new instrument data...");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "WebSocket error. Reconnecting in 5s...");
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
        var instrumentIds = await GetInstrumentIdsAsync(ct);

        if (instrumentIds.Count == 0)
        {
            logger.LogWarning("No instruments to subscribe. Waiting for sync...");
            await Task.Delay(Timeout.Infinite, ct);
            return;
        }

        var token = await tokenManager.GetAccessTokenAsync(ct);
        var url = $"{options.Value.WssUrl}/api/streaming/ws/v1/realtime?token={token}";

        using var ws = new ClientWebSocket();
        await ws.ConnectAsync(new Uri(url), ct);
        logger.LogInformation("WebSocket connected. Subscribing to {Count} instruments", instrumentIds.Count);

        foreach (var instrumentId in instrumentIds)
        {
            await SubscribeAsync(ws, instrumentId, ct);
        }

        await ReceiveLoopAsync(ws, ct);
    }

    private async Task<List<string>> GetInstrumentIdsAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
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
                    logger.LogWarning("WebSocket closed by server");
                    return;
                }

                ms.Write(buffer, 0, result.Count);
            } while (!result.EndOfMessage);

            var json = Encoding.UTF8.GetString(ms.ToArray());
            ms.SetLength(0);

            ProcessMessage(json);
        }
    }

    private void ProcessMessage(string json)
    {
        try
        {
            var message = JsonSerializer.Deserialize<WsMessage>(json);

            if (message?.Type != "l1-update") return;

            logger.LogDebug(
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

            cache.Set(message.InstrumentId!, price);

            _dbBuffer[message.InstrumentId!] = price;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process WebSocket message: {Json}", json);
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

                var itemsToSave = new List<KeyValuePair<string, CachedPrice>>();
                foreach (var key in _dbBuffer.Keys)
                {
                    if (_dbBuffer.TryRemove(key, out var price))
                        itemsToSave.Add(new KeyValuePair<string, CachedPrice>(key, price));
                }

                logger.LogInformation("Flushing {Count} prices to database...", itemsToSave.Count);

                using var scope = scopeFactory.CreateScope();
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
        catch (OperationCanceledException) {}
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during database flushing");
        }
    }
}