namespace Fintacharts.AssetTracker.Infrastructure.Cache;

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

public class PriceCache
{
    private readonly ConcurrentDictionary<string, CachedPrice> _prices = new();

    public void Set(string instrumentId, CachedPrice price)
    {
        _prices[instrumentId] = price;
    }

    public CachedPrice? Get(string instrumentId)
    {
        return _prices.GetValueOrDefault(instrumentId);
    }

    public IReadOnlyDictionary<string, CachedPrice> GetAll()
    {
        return _prices;
    }

    public bool TryGet(string instrumentId, [NotNullWhen(true)] out CachedPrice? price)
    {
        return _prices.TryGetValue(instrumentId, out price);
    }
}

