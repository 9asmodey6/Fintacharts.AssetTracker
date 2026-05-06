namespace Fintacharts.AssetTracker.Features.GetAssets;

using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public class GetAssetsHandler(
    AppDbContext db,
    ILogger<GetAssetsHandler> logger)
{
    public async Task<GetAssetsResponse> HandleAsync(CancellationToken ct = default)
    {
        var items = await db.Instruments
            .Select(i => new GetAssetsResponseItem(
                i.Id,
                i.Symbol,
                i.Description,
                i.Kind,
                i.Provider))
            .ToArrayAsync(ct);

        return new GetAssetsResponse(items);
    }
}