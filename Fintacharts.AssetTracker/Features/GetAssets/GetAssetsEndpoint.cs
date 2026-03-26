namespace Fintacharts.AssetTracker.Features.GetAssets;

using Microsoft.AspNetCore.Http.HttpResults;
using Shared.Consts;
using Shared.Interfaces;

public class GetAssetsEndpoint : IEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/assets",
                async (GetAssetsHandler handler, CancellationToken ct)
                    => TypedResults.Ok(await handler.HandleAsync(ct)))
            .WithTags(EndpointTags.AssetTag)
            .WithSummary("Get all Assets");
    }
}