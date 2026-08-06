using System.Diagnostics;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

using OpenTelemetry;
using OpenTelemetry.Context.Propagation;

using ServicePilot.Observability;

namespace ServicePilot.ArchitectureTests;

public sealed class TracePropagationContractTests
{
    [Fact]
    public void StartRootActivity_ShouldIgnoreCurrentActivity()
    {
        using ActivityListener listener = new()
        {
            ShouldListenTo = source =>
                source.Name.Equals(
                    ServicePilotTelemetry.ActivitySourceName,
                    StringComparison.Ordinal),
            Sample = static (ref ActivityCreationOptions<
                    ActivityContext> _) =>
                ActivitySamplingResult.AllDataAndRecorded
        };
        ActivitySource.AddActivityListener(listener);

        using Activity parent = new("existing.parent");
        parent.SetIdFormat(ActivityIdFormat.W3C);
        parent.Start();

        using Activity? root =
            ServicePilotTelemetry.StartRootActivity(
                "test.root");

        Assert.NotNull(root);
        Assert.Equal(default, root.ParentSpanId);
        Assert.NotEqual(parent.TraceId, root.TraceId);
    }

    [Fact]
    public void RecordFailure_ShouldKeepOnlySafeExceptionType()
    {
        const string secretMarker =
            "secret-exception-message";
        using ActivityListener listener = new()
        {
            ShouldListenTo = source =>
                source.Name.Equals(
                    ServicePilotTelemetry.ActivitySourceName,
                    StringComparison.Ordinal),
            Sample = static (ref ActivityCreationOptions<
                    ActivityContext> _) =>
                ActivitySamplingResult.AllDataAndRecorded
        };
        ActivitySource.AddActivityListener(listener);

        using Activity? activity =
            ServicePilotTelemetry.StartRootActivity(
                "test.failure");
        InvalidOperationException exception =
            new(secretMarker);

        ServicePilotTelemetry.RecordFailure(
            activity,
            exception);

        Assert.NotNull(activity);
        Assert.Equal(
            ActivityStatusCode.Error,
            activity.Status);
        Assert.Null(activity.StatusDescription);
        Assert.Equal(
            typeof(InvalidOperationException).FullName,
            activity.GetTagItem("exception.type"));
        Assert.Empty(activity.Events);
        Assert.DoesNotContain(
            secretMarker,
            string.Join('|', activity.TagObjects),
            StringComparison.Ordinal);
    }

    [Fact]
    public void Registration_ShouldUseTraceContext_WithoutBaggage()
    {
        TextMapPropagator previous =
            Propagators.DefaultTextMapPropagator;

        try
        {
            HostApplicationBuilder builder =
                Host.CreateApplicationBuilder();
            builder.Configuration[
                "Observability:OtlpEnabled"] = "false";

            builder.AddServicePilotObservability(
                "ServicePilot.Test");

            TextMapPropagator propagator =
                Propagators.DefaultTextMapPropagator;
            Assert.IsType<TraceContextPropagator>(
                propagator);
            Assert.NotNull(propagator.Fields);
            Assert.Contains(
                "traceparent",
                propagator.Fields!);
            Assert.Contains(
                "tracestate",
                propagator.Fields!);
            Assert.DoesNotContain(
                "baggage",
                propagator.Fields!);
        }
        finally
        {
            Sdk.SetDefaultTextMapPropagator(
                previous);
        }
    }
}
