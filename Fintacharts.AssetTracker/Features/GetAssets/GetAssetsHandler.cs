namespace Fintacharts.AssetTracker.Features.GetAssets;

using Infrastructure.Fintacharts;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Shared.Consts;

public class GetAssetsHandler(
    FintachartsRestClient restClient,
    AppDbContext db,
    ILogger<GetAssetsHandler> logger)
{
    public async Task<List<GetAssetsResponse>> HandleAsync(CancellationToken ct = default)
    {
        logger.LogInformation("Syncing instruments from Fintacharts...");

        var instruments = await restClient.GetInstrumentsAsync(ct: ct);

        var entities = instruments.Select(i => new Instrument
        {
            Id = i.Id,
            Symbol = i.Symbol,
            Description = i.Description,
            Kind = i.Kind,
            Provider = FintachartsConstants.DefaultProvider,
        }).ToList();

        var list = await db.Instruments.Select(x => x.Id).ToListAsync(ct);
        var existingIds = new HashSet<string>(list);

        var newEntities = entities
            .Where(e => !existingIds.Contains(e.Id))
            .ToList();

        if (newEntities.Count > 0)
        {
            db.Instruments.AddRange(newEntities);
            await db.SaveChangesAsync(ct);

            logger.LogInformation(
                "Added {Count} new instruments to DB", newEntities.Count);
        }

        return await db.Instruments
            .Select(i => new GetAssetsResponse(
                i.Id,
                i.Symbol,
                i.Description,
                i.Kind,
                i.Provider))
            .ToListAsync(ct);
    }

    private static GetAssetsResponse MapToResponse(Instrument i) => new(
        i.Id,
        i.Symbol,
        i.Description,
        i.Kind,
        i.Provider);
}