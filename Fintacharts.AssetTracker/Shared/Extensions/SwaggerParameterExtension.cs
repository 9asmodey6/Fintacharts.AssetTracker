namespace Fintacharts.AssetTracker.Shared.Extensions;

public static class SwaggerParameterExtension
{
    public static RouteHandlerBuilder WithParameterDescription(
        this RouteHandlerBuilder builder,
        string parameterName,
        string description)
    {
        return builder.WithOpenApi(operation =>
        {
            var param = operation.Parameters.FirstOrDefault(p => p.Name == parameterName);
            if (param != null) param.Description = description;
            return operation;
        });
    }
}