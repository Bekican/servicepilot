using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.Logging;

using ServicePilot.Api.Errors;
using ServicePilot.Api.Observability;

namespace ServicePilot.ArchitectureTests;

public sealed class CorrelationLogContractTests
{
    [Fact]
    public async Task CompletedRequest_ShouldLog_OnlySafeStableFields()
    {
        CapturingLogger<CorrelationIdMiddleware> logger = new();
        CorrelationIdMiddleware middleware = new(
            context =>
            {
                context.Response.StatusCode =
                    StatusCodes.Status409Conflict;
                context.Items[
                    ApiProblemDetailsFactory
                        .ProblemCodeItemName] =
                    "Customer.EmailAlreadyExists";
                return Task.CompletedTask;
            },
            logger);
        DefaultHttpContext context = new();
        context.Request.Method = HttpMethods.Post;
        context.Request.Path =
            "/api/customers/private-customer-id";
        context.SetEndpoint(
            new RouteEndpoint(
                _ => Task.CompletedTask,
                RoutePatternFactory.Parse(
                    "api/customers/{id:guid}"),
                0,
                EndpointMetadataCollection.Empty,
                "customer"));

        await middleware.InvokeAsync(context);

        CapturedLog log = Assert.Single(logger.Logs);
        Assert.Equal(LogLevel.Information, log.Level);
        Assert.Equal("ApiRequestCompleted", log.EventId.Name);
        Assert.Equal(
            "api/customers/{id:guid}",
            log.Properties["Route"]);
        Assert.Equal(
            "Customer.EmailAlreadyExists",
            log.Properties["ProblemCode"]);
        Assert.DoesNotContain(
            "private-customer-id",
            log.Message,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "RequestPath",
            log.Properties.Keys);
    }

    [Fact]
    public async Task HealthRequest_ShouldNotBeLogged()
    {
        CapturingLogger<CorrelationIdMiddleware> logger = new();
        CorrelationIdMiddleware middleware = new(
            _ => Task.CompletedTask,
            logger);
        DefaultHttpContext context = new();
        context.Request.Path = "/health/ready";

        await middleware.InvokeAsync(context);

        Assert.Empty(logger.Logs);
    }

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<CapturedLog> Logs { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Dictionary<string, object?> properties =
                state is IEnumerable<
                    KeyValuePair<string, object?>> values
                    ? values.ToDictionary(
                        pair => pair.Key,
                        pair => pair.Value,
                        StringComparer.Ordinal)
                    : new Dictionary<string, object?>(
                        StringComparer.Ordinal);
            Logs.Add(
                new CapturedLog(
                    logLevel,
                    eventId,
                    formatter(state, exception),
                    properties));
        }
    }

    private sealed record CapturedLog(
        LogLevel Level,
        EventId EventId,
        string Message,
        IReadOnlyDictionary<string, object?> Properties);
}
