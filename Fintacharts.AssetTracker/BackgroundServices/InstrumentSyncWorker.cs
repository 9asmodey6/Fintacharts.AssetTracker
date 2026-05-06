namespace Fintacharts.AssetTracker.BackgroundServices;

using Infrastructure.Fintacharts;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Consts;
using Shared.Services;

public class InstrumentSyncWorker(
    FintachartsRestClient restClient,
    IServiceScopeFactory scopeFactory,
    InstrumentSyncNotifier notifier,
    ILogger<InstrumentSyncWorker> logger) : BackgroundService
{
    private static readonly TimeSpan SyncInterval = TimeSpan.FromMinutes(1);

    private HashSet<string> _currentInstrumentIds = [];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("InstrumentSyncWorker starting...");

        // Sync immediately on startup
        await SyncInstrumentsAsync(stoppingToken);

        // Then sync periodically
        using var timer = new PeriodicTimer(SyncInterval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await SyncInstrumentsAsync(stoppingToken);
        }
    }

    private async Task SyncInstrumentsAsync(CancellationToken ct)
    {
        try
        {
            logger.LogInformation("Syncing instruments from Fintacharts...");

            var instruments = await restClient.GetInstrumentsAsync(ct: ct);

            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            foreach (var i in instruments)
            {
                await db.Database.ExecuteSqlInterpolatedAsync($"""
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

            logger.LogInformation(
                "Instruments synced successfully (Upserted {Count} items)", instruments.Count);

            var newIds = instruments.Select(x => x.Id).ToHashSet();

            if (!newIds.SetEquals(_currentInstrumentIds))
            {
                logger.LogInformation(
                    "Instrument set changed ({Old} → {New}). Notifying PriceUpdateWorker...",
                    _currentInstrumentIds.Count,
                    newIds.Count);

                _currentInstrumentIds = newIds;
                notifier.NotifyInstrumentsChanged();
            }
        }
        catch (OperationCanceledException)
        {
            throw; // Let the host handle graceful shutdown
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to sync instruments. Will retry on next tick.");
        }
    }
}
