using System.Diagnostics;

using ServicePilot.Api.Errors;

namespace ServicePilot.Api.Observability;

public sealed class CorrelationIdMiddleware(
    RequestDelegate next,
    ILogger<CorrelationIdMiddleware> logger)
{
    private static readonly EventId RequestCompletedEvent =
        new(1000, "ApiRequestCompleted");

    public const string HeaderName = "X-Correlation-ID";
    public const string ItemName = "ServicePilot.CorrelationId";

    public async Task InvokeAsync(HttpContext context)
    {
        long startedAt = Stopwatch.GetTimestamp();
        string correlationId = Resolve(context);
        context.Items[ItemName] = correlationId;
        context.Request.Headers[HeaderName] = correlationId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] =
                correlationId;
            return Task.CompletedTask;
        });
        Activity.Current?.SetTag(
            "servicepilot.correlation_id",
            correlationId);

        using IDisposable? scope = logger.BeginScope(
            new Dictionary<string, object>
            {
                ["CorrelationId"] = correlationId
            });

        try
        {
            await next(context);
        }
        finally
        {
            if (!context.Request.Path.StartsWithSegments(
                    "/health"))
            {
                string route = (
                    context.GetEndpoint()
                        as Microsoft.AspNetCore.Routing
                            .RouteEndpoint)?
                    .RoutePattern.RawText
                    ?? "unmatched";
                string problemCode =
                    context.Items[
                        ApiProblemDetailsFactory
                            .ProblemCodeItemName]
                        as string
                    ?? string.Empty;
                string traceId = Activity.Current?
                    .TraceId.ToHexString()
                    ?? string.Empty;
                string spanId = Activity.Current?
                    .SpanId.ToHexString()
                    ?? string.Empty;
                LogLevel level =
                    context.Response.StatusCode >= 500
                        ? LogLevel.Error
                        : context.Response.StatusCode
                            == StatusCodes
                                .Status429TooManyRequests
                            ? LogLevel.Warning
                            : LogLevel.Information;

                logger.Log(
                    level,
                    RequestCompletedEvent,
                    "HTTP {RequestMethod} {Route} completed "
                    + "with {StatusCode} in {DurationMs} ms "
                    + "as {ProblemCode} "
                    + "({TraceId}, {SpanId}, {CorrelationId})",
                    context.Request.Method,
                    route,
                    context.Response.StatusCode,
                    Stopwatch.GetElapsedTime(startedAt)
                        .TotalMilliseconds,
                    problemCode,
                    traceId,
                    spanId,
                    correlationId);
            }
        }
    }

    public static bool IsValid(string? value)
    {
        if (value is null
            || value.Length is < 8 or > 64
            || !IsAlphaNumeric(value[0]))
        {
            return false;
        }

        foreach (char character in value)
        {
            if (!IsAlphaNumeric(character)
                && character is not '.' and not '_' and not '-')
            {
                return false;
            }
        }

        return true;
    }

    private static string Resolve(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(
                HeaderName,
                out Microsoft.Extensions.Primitives.StringValues values)
            && values.Count == 1
            && IsValid(values[0]))
        {
            return values[0]!;
        }

        return Guid.NewGuid().ToString("N");
    }

    private static bool IsAlphaNumeric(char character) =>
        character is >= 'a' and <= 'z'
        or >= 'A' and <= 'Z'
        or >= '0' and <= '9';
}
