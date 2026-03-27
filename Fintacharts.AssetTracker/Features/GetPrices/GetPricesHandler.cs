namespace Fintacharts.AssetTracker.Features.GetPrices;

using Infrastructure.Cache;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Shared.Exceptions;

public class GetPricesHandler(PriceCache cache, AppDbContext db)
{
    public async Task<GetPricesResponse> HandleAsync(Guid[]? ids, CancellationToken ct = default)
    {
        var items = ids != null && ids.Length > 0
            ? await GetSpecificPricesAsync(ids, ct)
            : await GetAllPricesAsync(ct);

        return new GetPricesResponse(items.ToArray());
    }

    private async Task<List<GetPricesResponseItem>> GetSpecificPricesAsync(Guid[] ids, CancellationToken ct)
    {
        var result = new List<GetPricesResponseItem>();

        var missingInCache = new Dictionary<string, Guid>();

        foreach (var id in ids)
        {
            var stringId = id.ToString();
            if (cache.TryGet(stringId, out var cached))
            {
                result.Add(MapToResponse(id, cached));
            }
            else
            {
                missingInCache.Add(stringId, id);
            }
        }

        if (missingInCache.Count > 0)
        {
            var stringIds = missingInCache.Keys.ToList();

            var dbPrices = await db.AssetPrices
                .Where(p => stringIds.Contains(p.InstrumentId))
                .ToListAsync(ct);

            foreach (var p in dbPrices)
            {
                var originalGuid = missingInCache[p.InstrumentId];
                result.Add(MapToResponse(originalGuid, p));
            }
        }

        if (result.Count < ids.Length)
        {
            var foundIds = result.Select(r => r.InstrumentId).ToHashSet();
            var missingIds = ids.Where(id => !foundIds.Contains(id)).ToList();
            throw new IncompleteBatchException("Unable to obtain a complete price list.", missingIds);
        }

        return result;
    }

    private async Task<List<GetPricesResponseItem>> GetAllPricesAsync(CancellationToken ct)
    {
        var cached = cache.GetAll();
        var cachedIds = cached.Keys.ToHashSet();

        var result = cached
            .Select(kvp => MapToResponse(Guid.Parse(kvp.Key), kvp.Value))
            .ToList();

        var dbPrices = await db.AssetPrices
            .Where(p => !cachedIds.Contains(p.InstrumentId))
            .ToListAsync(ct);

        result.AddRange(dbPrices.Select(p =>
            MapToResponse(Guid.Parse(p.InstrumentId), p)));

        return result;
    }

    private static GetPricesResponseItem MapToResponse(Guid id, CachedPrice price) =>
        new(id,
            price.Bid,
            price.Ask,
            price.Last,
            price.UpdatedAt);

    private static GetPricesResponseItem MapToResponse(Guid id, AssetPrice price) =>
        new(id,
            price.Bid,
            price.Ask,
            price.Last,
            price.UpdatedAt);
}