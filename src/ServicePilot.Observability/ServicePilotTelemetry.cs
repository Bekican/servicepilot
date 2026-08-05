using System.Diagnostics;
using System.Reflection;
using System.Text.Json;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace ServicePilot.Observability;

public static class ServicePilotTelemetry
{
    public const string ActivitySourceName =
        "ServicePilot.Operations";

    private static readonly ActivitySource Source =
        new(ActivitySourceName);

    public static Activity? StartActivity(
        string operationName,
        ActivityKind kind = ActivityKind.Internal) =>
        Source.StartActivity(operationName, kind);

    public static IHostApplicationBuilder
        AddServicePilotObservability(
            this IHostApplicationBuilder builder,
            string serviceName,
            Action<TracerProviderBuilder>? configureTracing = null)
    {
        Sdk.SetDefaultTextMapPropagator(
            new TraceContextPropagator());

        if (builder.Environment.IsStaging()
            || builder.Environment.IsProduction())
        {
            builder.Logging.ClearProviders();
            builder.Logging.AddJsonConsole(options =>
            {
                options.IncludeScopes = false;
                options.TimestampFormat =
                    "yyyy-MM-dd'T'HH:mm:ss.fff'Z'";
                options.UseUtcTimestamp = true;
                options.JsonWriterOptions =
                    new JsonWriterOptions
                    {
                        Indented = false
                    };
            });
            builder.Logging.AddFilter(
                "Microsoft.EntityFrameworkCore.Database.Command",
                LogLevel.Warning);
        }

        if (!IsEnabled(builder))
        {
            return builder;
        }

        ResourceBuilder resourceBuilder =
            ResourceBuilder.CreateDefault()
                .AddService(
                    serviceName,
                    serviceVersion: GetServiceVersion())
                .AddAttributes(
                    new Dictionary<string, object>
                    {
                        ["deployment.environment.name"] =
                            builder.Environment.EnvironmentName
                    });

        builder.Logging.AddOpenTelemetry(options =>
        {
            options.IncludeFormattedMessage = true;
            options.IncludeScopes = false;
            options.ParseStateValues = true;
            options.SetResourceBuilder(resourceBuilder);
            options.AddOtlpExporter();
        });
        builder.Logging.AddFilter<OpenTelemetryLoggerProvider>(
            "Microsoft.EntityFrameworkCore.Database.Command",
            LogLevel.Warning);

        builder.Services
            .AddOpenTelemetry()
            .ConfigureResource(resource =>
                resource
                    .AddService(
                        serviceName,
                        serviceVersion: GetServiceVersion())
                    .AddAttributes(
                        new Dictionary<string, object>
                        {
                            ["deployment.environment.name"] =
                                builder.Environment.EnvironmentName
                        }))
            .WithTracing(tracing =>
            {
                tracing.SetSampler(
                    new ParentBasedSampler(
                        new AlwaysOnSampler()));
                tracing.AddSource(ActivitySourceName);
                tracing.AddHttpClientInstrumentation();
                configureTracing?.Invoke(tracing);
                tracing.AddProcessor(
                    new AllowedTraceTagProcessor());
                tracing.AddOtlpExporter();
            });

        return builder;
    }

    private static bool IsEnabled(
        IHostApplicationBuilder builder) =>
        bool.TryParse(
            builder.Configuration[
                "Observability:OtlpEnabled"],
            out bool enabled)
        && enabled;

    private static string GetServiceVersion() =>
        Assembly.GetEntryAssembly()?
            .GetCustomAttribute<
                AssemblyInformationalVersionAttribute>()?
            .InformationalVersion
        ?? "unknown";
}
