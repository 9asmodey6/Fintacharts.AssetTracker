namespace Fintacharts.AssetTracker.Features.GetPrices;

public record GetPricesResponse(
    GetPricesResponseItem[] Prices);

public record GetPricesResponseItem(
    string InstrumentId,
    decimal Bid,
    decimal Ask,
    decimal Last,
    DateTime UpdatedAt);