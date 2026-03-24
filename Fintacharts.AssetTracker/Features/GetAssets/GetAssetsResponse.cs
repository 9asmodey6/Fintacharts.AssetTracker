namespace Fintacharts.AssetTracker.Features.GetAssets;

public record GetAssetsResponse(
    GetAssetsResponseItem[] Assets);

public record GetAssetsResponseItem(
    string Id,
    string Symbol,
    string? Description,
    string Kind,
    string Provider);