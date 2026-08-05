using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

using OpenTelemetry;
using OpenTelemetry.Context.Propagation;

using ServicePilot.Observability;

namespace ServicePilot.ArchitectureTests;

public sealed class TracePropagationContractTests
{
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
