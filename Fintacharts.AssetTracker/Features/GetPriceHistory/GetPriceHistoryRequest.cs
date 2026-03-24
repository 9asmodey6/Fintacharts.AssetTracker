namespace Fintacharts.AssetTracker.Features.GetPriceHistory;

public record GetPriceHistoryRequest(
    string id, 
    int barsCount);