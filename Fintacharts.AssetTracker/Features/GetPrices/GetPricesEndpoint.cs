namespace Fintacharts.AssetTracker.Features.GetPrices;

using Microsoft.AspNetCore.Http.HttpResults;
using Shared.Consts;
using Shared.Extensions;
using Shared.Interfaces;

public class GetPricesEndpoint : IEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/prices", HandleAsync)
            .WithValidation<GetPricesRequest>()
            .WithTags(EndpointTags.PricesTag)
            .WithSummary("Get current prices")
            .WithDescription(
                "Returns current bid/ask/last prices for specific assets or all available assets if no IDs provided.")
            .WithParameterDescription("ids",
                "Array of unique asset identifiers (GUIDs). Leave empty to get all prices.");
    }

    public static async Task<Results<Ok<GetPricesResponse>, ValidationProblem, BadRequest>> HandleAsync(
        [AsParameters] GetPricesRequest request,
        GetPricesHandler handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(request.ids, ct);

        return TypedResults.Ok(result);
    }
}