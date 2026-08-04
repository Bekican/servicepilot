using Microsoft.AspNetCore.Diagnostics;

namespace ServicePilot.Api.Errors;

public sealed class GlobalExceptionHandler(
    ApiProblemDetailsFactory problemFactory,
    ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    private static readonly EventId UnhandledExceptionEvent =
        new(1001, "ApiUnhandledException");

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(
            UnhandledExceptionEvent,
            "Unhandled API exception of type "
            + "{ExceptionType} ({CorrelationId})",
            exception.GetType().Name,
            httpContext.Items[
                Observability.CorrelationIdMiddleware
                    .ItemName]);

        await problemFactory.WriteAsync(
            httpContext,
            ApiProblemCodes.Unexpected,
            StatusCodes.Status500InternalServerError,
            "An unexpected error occurred.",
            cancellationToken);
        return true;
    }
}
