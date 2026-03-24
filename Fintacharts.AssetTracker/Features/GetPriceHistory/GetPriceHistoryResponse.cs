namespace Fintacharts.AssetTracker.Features.GetPriceHistory;

public record GetPriceHistoryResponse(GetPriceHistoryItem[] History);

public record GetPriceHistoryItem(DateTime Timestamp, decimal Price);