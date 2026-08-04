using System.Diagnostics;
using System.Reflection;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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
                tracing.AddSource(ActivitySourceName);
                tracing.AddHttpClientInstrumentation();
                configureTracing?.Invoke(tracing);
                tracing.AddProcessor(
                    new SensitiveHttpTagProcessor());
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
