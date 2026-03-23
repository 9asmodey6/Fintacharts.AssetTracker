namespace Fintacharts.AssetTracker.Features.GetAssets;

using Microsoft.AspNetCore.Http.HttpResults;
using Shared.Consts;
using Shared.Interfaces;

public class GetAssetsEndpoint : IEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/assets", HandleAsync)
            .WithTags(EndpointTags.AssetTag)
            .WithName("Get Assets");
    }

    private static async Task<Ok<List<GetAssetsResponse>>> HandleAsync(
        GetAssetsHandler handler,
        CancellationToken ct)
    {
        var assets = await handler.HandleAsync(ct);
        return TypedResults.Ok(assets);
    }
}