namespace Fintacharts.AssetTracker.Shared.Extensions;

using Bootstrap.Filters;

public static class ValidationExtension
{
    public static RouteHandlerBuilder WithValidation<TRequest>(this RouteHandlerBuilder builder)
        where TRequest : class
    {
        return builder.AddEndpointFilter<ValidationFilter<TRequest>>();
    }
}