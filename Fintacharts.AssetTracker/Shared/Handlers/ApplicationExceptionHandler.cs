namespace Fintacharts.AssetTracker.Shared.Handlers;

using Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

public class ApplicationExceptionHandler(
    ILogger<ApplicationExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var details = exception switch
        {
            IncompleteBatchException ex =>
                HandleIncompleteBatch(ex),

            HttpRequestException ex =>
                HandleRequestException(ex),

            DbUpdateException ex =>
                HandleDbUpdateException(ex),

            _ =>
                HandleDefaultException(exception)
        };

        logger.LogError(exception,
            "Unhandled {ExceptionType} for {Method} {Path}",
            exception.GetType().Name,
            context.Request.Method,
            context.Request.Path);

        context.Response.StatusCode = details.Status!.Value;
        
        await context.Response.WriteAsJsonAsync(details, cancellationToken);
        
        return true;
    }

    private ProblemDetails HandleIncompleteBatch(IncompleteBatchException exception)
    {
        var result = new ProblemDetails
        {
            Type = "https://developer.mozilla.org/en-US/docs/Web/HTTP/Status/400",
            Title = "Incomplete batch",
            Status = StatusCodes.Status400BadRequest,
            Detail = exception.Message,
            Extensions =
            {
                ["missingItems"] = exception.MissingIds,
            },
        };

        return result;
    }

    private ProblemDetails HandleRequestException(HttpRequestException exception)
    {
        var result = new ProblemDetails
        {
            Type = "https://developer.mozilla.org/en-US/docs/Web/HTTP/Reference/Status/502",
            Title = "Fintacharts service unavailable",
            Status = StatusCodes.Status502BadGateway,
            Detail = exception.Message,
        };

        return result;
    }

    private ProblemDetails HandleDbUpdateException(DbUpdateException exception)
    {
        var result = new ProblemDetails
        {
            Type = "https://developer.mozilla.org/en-US/docs/Web/HTTP/Reference/Status/503",
            Title = "Database error",
            Status = StatusCodes.Status503ServiceUnavailable,
            Detail = exception.Message,
        };

        return result;
    }

    private ProblemDetails HandleDefaultException(Exception exception)
    {
        var result = new ProblemDetails
        {
            Type = "https://developer.mozilla.org/en-US/docs/Web/HTTP/Status/500",
            Title = "Unhandled error",
            Status = StatusCodes.Status500InternalServerError,
            Detail = exception.Message,
        };

        return result;
    }
}