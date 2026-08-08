export async function register() {
  const telemetryEnabled = (process.env.SERVICEPILOT_OTLP_ENABLED = "true");

  if (process.env.NEXT_RUNTIME !== "nodejs" || !telemetryEnabled) return;
}

const { registerNodeTelemetry } = await import("./instrumentation-node");

registerNodeTelemetry();
