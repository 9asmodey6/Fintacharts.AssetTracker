namespace Fintacharts.AssetTracker.Features.GetAssets;

using Infrastructure.Fintacharts;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Consts;
using Shared.Events;
using Shared.Interfaces;

public class GetAssetsHandler(
    FintachartsRestClient restClient,
    AppDbContext db,
    ILogger<GetAssetsHandler> logger,
    IEventBus eventBus)
{
    public async Task<GetAssetsResponse> HandleAsync(CancellationToken ct = default)
    {
        logger.LogInformation("Syncing instruments from Fintacharts...");

        var instruments = await restClient.GetInstrumentsAsync(ct: ct);
        var instrumentIds = instruments.Select(x => x.Id).ToList();

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

        logger.LogInformation("Instruments synced successfully (Upserted {Count} items)", instruments.Count);

        var items = await db.Instruments
            .Select(i => new GetAssetsResponseItem(
                i.Id,
                i.Symbol,
                i.Description,
                i.Kind,
                i.Provider))
            .ToArrayAsync(ct);

        eventBus.Publish(new InstrumentsSyncedEvent(instrumentIds));

        return new GetAssetsResponse(items);
    }
}