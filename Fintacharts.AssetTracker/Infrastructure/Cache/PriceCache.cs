namespace Fintacharts.AssetTracker.Infrastructure.Cache;

using System.Collections.Concurrent;

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
}

public record CachedPrice(
    decimal Bid,
    decimal Ask,
    decimal Last,
    DateTime UpdatedAt);