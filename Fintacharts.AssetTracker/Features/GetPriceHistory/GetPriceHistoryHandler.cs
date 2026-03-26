namespace Fintacharts.AssetTracker.Features.GetPriceHistory;

using Infrastructure.Fintacharts;

public class GetPriceHistoryHandler(FintachartsRestClient restClient)
{
    public async Task<GetPriceHistoryResponse> HandleAsync(Guid id, int limit, CancellationToken ct)
    {
        var bars = await restClient.GetPriceHistoryAsync(id.ToString(), limit, ct);
        
        var items = bars
            .Select(b => new GetPriceHistoryItem(b.Timestamp, b.Close))
            .ToArray();
            
        return new GetPriceHistoryResponse(items);
    }
}