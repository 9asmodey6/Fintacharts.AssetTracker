namespace Fintacharts.AssetTracker.Shared.Interfaces;

public interface IEndpoint
{
    static abstract void MapEndpoint(IEndpointRouteBuilder app);
}