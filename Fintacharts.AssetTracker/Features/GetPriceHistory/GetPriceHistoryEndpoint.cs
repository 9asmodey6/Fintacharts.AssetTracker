namespace Fintacharts.AssetTracker.Features.GetPriceHistory;

using Microsoft.AspNetCore.Http.HttpResults;
using Shared.Consts;
using Shared.Extensions;
using Shared.Interfaces;

public class GetPriceHistoryEndpoint : IEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/assets/{id}/history", HandleAsync)
            .WithValidation<GetPriceHistoryRequest>()
            .WithTags(EndpointTags.PricesTag)
            .WithSummary("Get historical prices for asset");
    }

    private static async Task<Results<Ok<GetPriceHistoryResponse>, NotFound<string>, ValidationProblem>> HandleAsync(
        [AsParameters] GetPriceHistoryRequest request,
        GetPriceHistoryHandler handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(request.id, request.barsCount, ct);

        if (result.History.Length == 0)
        {
            return TypedResults.NotFound($"No history found for asset: {request.id}");
        }

        return TypedResults.Ok(result);
    }
}