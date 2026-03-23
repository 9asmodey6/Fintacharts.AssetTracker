namespace Fintacharts.AssetTracker.Features.GetAssets;

public record GetAssetsResponse(
    string Id,
    string Symbol,
    string? Description,
    string Kind,
    string Provider);