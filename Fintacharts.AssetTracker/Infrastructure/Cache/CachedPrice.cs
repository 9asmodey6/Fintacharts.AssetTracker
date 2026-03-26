namespace Fintacharts.AssetTracker.Infrastructure.Cache;

public record CachedPrice(
    decimal Bid,
    decimal Ask,
    decimal Last,
    DateTime UpdatedAt);