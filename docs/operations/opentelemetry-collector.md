# OpenTelemetry Collector Operations

ServicePilot API, Worker and Migrator send logs and traces over OTLP/gRPC to
the Collector on the private `backend` Docker network. Application containers
do not receive hosted telemetry credentials.

## Local Drill

Start Docker Desktop, create `.env.staging.local` as documented for local
staging, then run:

```powershell
./scripts/verify-observability-local.ps1
```

The local Collector uses the `debug` exporter. The drill proves that:

- API, Worker and Migrator telemetry reaches the Collector
- API correlation ID is attached to its request trace
- Reminder, retention and migration activities produce spans
- SQL command text and raw HTTP path/query fields are absent
- The API remains ready while the Collector is stopped

The Collector is restarted before the script exits. View its local output
with:

```powershell
docker compose --env-file .env.staging.local `
  -f compose.staging.local.yaml logs -f otel-collector
```

## Real Staging

Set the feature gate and these values only in the restricted runtime
environment file:

```dotenv
OBSERVABILITY_ENABLED=true
OTEL_COLLECTOR_IMAGE=otel/opentelemetry-collector:0.157.0@sha256:<real-digest>
OTEL_BACKEND_ENDPOINT=https://provider-otlp-endpoint.example
OTEL_BACKEND_AUTHORIZATION=Bearer replace-with-provider-token
```

The production-style Collector reads these values and exports with OTLP/HTTP.
Applications still send only to `http://otel-collector:4317`. The bounded
queue, batch processor and retry window absorb short backend outages; after
the bound is reached telemetry can be dropped without blocking application
work.

Before enabling a real provider, confirm its OTLP/HTTP base endpoint and exact
`Authorization` header format. Never place that token in API, Worker or
Migrator environment variables. When the gate is false, the Collector overlay
is not loaded and all application OTLP exporters remain disabled.
