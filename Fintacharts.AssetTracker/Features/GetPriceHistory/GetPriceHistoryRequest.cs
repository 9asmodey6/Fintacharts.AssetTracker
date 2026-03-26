namespace Fintacharts.AssetTracker.Features.GetPriceHistory;

public record GetPriceHistoryRequest(
    Guid id, 
    int barsCount);