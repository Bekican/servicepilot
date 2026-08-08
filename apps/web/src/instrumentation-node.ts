import { OTLPTraceExporter } from "@opentelemetry/exporter-trace-otlp-proto";
import { BatchSpanProcessor } from "@opentelemetry/sdk-trace-base";
import { registerOTel } from "@vercel/otel";

import { OutboundOnlyTraceContextPropagator } from "./lib/observability/outbound-only-trace-context-propagator";
import { SanitizingSpanExporter } from "./lib/observability/sanitizing-span-exporter";

export function registerNodeTelemetry() {
  const exporter = new SanitizingSpanExporter(new OTLPTraceExporter());

  registerOTel({
    serviceName: "ServicePilot.Web",

    attributes: {
      "service.version": process.env.SERVICEPILOT_VERSION ?? "unknown",
      "deployment.environment.name":
        process.env.SERVICEPILOT_ENVIRONMENT ??
        process.env.NODE_ENV ??
        "unknown",
    },

    autoDetectResources: false,

    instrumentations: ["fetch"],

    instrumentationConfig: {
      fetch: {
        propagateContextUrls: [],
      },
    },

    propagators: [new OutboundOnlyTraceContextPropagator()],

    traceSampler: "always_on",

    spanProcessors: [
      new BatchSpanProcessor(exporter, {
        maxQueueSize: 512,
        maxExportBatchSize: 128,
        scheduledDelayMillis: 5_000,
        exportTimeoutMillis: 10_000,
      }),
    ],
  });
}
