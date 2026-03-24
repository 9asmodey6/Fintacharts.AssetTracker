namespace Fintacharts.AssetTracker.Features.GetPrices;

using Infrastructure.Cache;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

public class GetPricesHandler(PriceCache cache, AppDbContext db)
{
    public async Task<GetPricesResponse> HandleAsync(string[]? ids, CancellationToken ct = default)
    {
        var items = ids != null && ids.Length > 0
            ? await GetSpecificPricesAsync(ids, ct)
            : await GetAllPricesAsync(ct);

        return new GetPricesResponse(items.ToArray());
    }

    private async Task<List<GetPricesResponseItem>> GetSpecificPricesAsync(string[] ids, CancellationToken ct)
    {
        var result = new List<GetPricesResponseItem>();
        var missingInCache = new List<string>();

        foreach (var id in ids)
        {
            if (cache.TryGet(id, out var cached))
            {
                result.Add(MapToResponse(id, cached));
            }
            else
            {
                missingInCache.Add(id);
            }
        }

        if (missingInCache.Count > 0)
        {
            var dbPrices = await db.AssetPrices
                .Where(p => missingInCache.Contains(p.InstrumentId))
                .ToListAsync(ct);

            result.AddRange(dbPrices.Select(p => MapToResponse(p.InstrumentId, p)));
        }

        return result;
    }

    private async Task<List<GetPricesResponseItem>> GetAllPricesAsync(CancellationToken ct)
    {
        var result = cache.GetAll()
            .Select(kvp => MapToResponse(kvp.Key, kvp.Value))
            .ToList();

        if (result.Count == 0)
        {
            var dbPrices = await db.AssetPrices.ToListAsync(ct);
            result = dbPrices.Select(p => MapToResponse(p.InstrumentId, p)).ToList();
        }

        return result;
    }

    private static GetPricesResponseItem MapToResponse(string id, CachedPrice price) =>
        new(id,
            price.Bid,
            price.Ask,
            price.Last,
            price.UpdatedAt);
    
    private static GetPricesResponseItem MapToResponse(string id, AssetPrice price) =>
        new(id,
            price.Bid,
            price.Ask,
            price.Last,
            price.UpdatedAt);
}