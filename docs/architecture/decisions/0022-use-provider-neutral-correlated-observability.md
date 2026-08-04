# ADR 0022: Use Provider-Neutral Correlated Observability

- Status: Accepted
- Date: 2026-08-04

## Context

ADR 0020 requires structured and sanitized logs, correlated requests,
OpenTelemetry and external operational alerts. ServicePilot currently writes
basic console logs and records limited HTTP request information, but it cannot
reliably connect a browser operation to its Web, API and background-processing
events.

The first VPS must remain operationally small. ServicePilot also has no chosen
telemetry provider yet, so application code must not depend on a vendor SDK or
proprietary data model. Observability must help incident diagnosis without
turning authentication secrets or customer data into a second sensitive data
store.

## Considered Options

### Keep Independent Console Logs

This has no additional infrastructure cost, but operators must manually match
timestamps across processes. Requests, failures and background work cannot be
followed reliably across service boundaries.

### Integrate a Vendor SDK Directly into Every Process

This provides a quick hosted dashboard, but spreads provider credentials and
vendor-specific instrumentation across Web, API and Worker. Changing provider
would require application changes and repeated secret configuration.

### Self-Host a Full Logging and Metrics Platform

Running Prometheus, Grafana, Loki and a tracing backend provides control, but
adds too much resource use and operational responsibility to the first VPS.

### Emit OpenTelemetry Through a Lightweight Collector

Applications use standard telemetry APIs and send data to one internal
collector. The collector owns the external endpoint and credentials and can
change exporter without changing domain or application code.

## Decision

ServicePilot will use structured JSON logs, W3C trace context and OpenTelemetry
with a lightweight OpenTelemetry Collector. The first deployment will not
self-host a full observability storage and dashboard stack.

### Correlation and Trace Context

Web will propagate W3C `traceparent` when available and will carry an
`X-Correlation-ID` for operator-facing request lookup. A missing correlation
identifier will be generated. An incoming value will be accepted only when it
passes strict length and character validation; otherwise it will be replaced.

API will return `X-Correlation-ID` in its response and include `traceId` and
`correlationId` in safe error responses. Technical exception details and stack
traces will never be returned to the client.

Trace context is the canonical machine-to-machine relationship. Correlation ID
is the stable value an operator or customer-support interaction can quote.

### Structured Logging

API, Worker, Migrator and Web will emit single-line JSON logs to standard
output in staging and production. Development may retain a readable console
formatter. Log records will use stable property names and include, when
applicable:

- Timestamp and severity
- Service name, version and deployment environment
- Event name
- Trace ID, span ID and correlation ID
- HTTP method, route template, status code and duration
- Background operation outcome, duration and aggregate item count
- Sanitized exception type

Request and response bodies will not be logged. Query strings and raw route
values will not be logged by default. `Authorization`, `Cookie`, `Set-Cookie`,
JWTs, passwords, invitation tokens, SMTP credentials, database credentials,
S3 credentials and row-level customer PII are prohibited from logs and
telemetry.

User and tenant identifiers will not be emitted by default. A future incident
workflow may introduce a separately reviewed pseudonymous identifier, but raw
identifiers will not be metric labels.

### Telemetry and Collector Boundary

.NET host projects will share a small `ServicePilot.Observability` project for
resource metadata, JSON logging, OpenTelemetry registration, `ActivitySource`
and `Meter` definitions. Domain and Application projects will not depend on
OpenTelemetry packages.

API, Worker and Migrator will send traces, metrics and logs to an internal
OpenTelemetry Collector over OTLP. The Collector will join the private
application network and a restricted outbound operations network. Application
containers will not contain external telemetry credentials.

The Collector will export to an environment-configured OTLP-compatible hosted
backend. Export is asynchronous and fail-open: telemetry unavailability must
not make registration, scheduling, reminders or other application operations
fail. Backpressure will be bounded so a prolonged exporter outage cannot cause
unbounded memory or disk growth.

Web must emit structured logs and propagate correlation context to API. Node.js
OpenTelemetry export may be added when it provides sufficient diagnostic value;
it is not required before the Web correlation boundary is implemented.

### Initial Instrumentation

The initial trace scope will cover:

- ASP.NET Core requests
- Outbound HTTP requests
- Worker reminder-processing cycles
- Retention cycles
- Migrator execution

Database command text, SQL parameters and email contents will not be captured.
Database spans may contain only low-cardinality system and duration metadata.

The initial metrics will cover:

- HTTP request count, duration and status class
- Unhandled API failures
- Reminder cycle duration and processed/failed aggregate counts
- Retention duration and aggregate result counts
- Process/runtime CPU, memory and garbage collection
- Telemetry exporter failures and dropped batches where available

Metrics will not use tenant ID, user ID, customer ID, appointment ID, reminder
ID, URL query values or raw exception messages as labels.

Health-check routes will be excluded from normal request traces and routine
request logs to control noise. External uptime monitoring will still check the
public Web path and controlled API readiness route.

### Alerts and Ownership

Application telemetry will support alerts for sustained 5xx rate, latency and
Worker failures. External mechanisms remain responsible for public uptime,
host CPU/memory/disk pressure, container restarts, deployment readiness and
backup heartbeat failures. Backup, deployment and application-availability
alerts remain separate because they require different operator actions.

Every production alert must identify an owner, severity, evaluation window and
first response action before it is enabled.

### Verification

Automated tests will verify correlation generation, validation, propagation and
response headers. Integration tests will verify safe Problem Details trace
fields and assert that representative JWTs, passwords, invitation tokens,
cookies, emails and phone numbers do not appear in captured logs or telemetry.

Local staging will use a Collector test configuration or debug exporter to
prove end-to-end export without purchasing a telemetry service. A real hosted
backend remains a staging-readiness gate before production.

## Positive Consequences

- One request can be followed across Web, API and background operations.
- JSON logs are machine-searchable without a vendor-specific logging API.
- Telemetry providers can change at the Collector boundary.
- External credentials remain outside application containers.
- A telemetry outage does not become an application outage.
- Explicit field and cardinality rules reduce cost and sensitive-data risk.
- The first VPS avoids operating a full observability storage platform.

## Negative Consequences

- The Collector adds another runtime component to configure and monitor.
- Correlation propagation requires coordinated Web and API changes.
- Sanitization tests and stable event schemas add engineering work.
- Fail-open export can create temporary telemetry gaps during an outage.
- Excluding raw identifiers and payloads can make some investigations slower.
- A hosted telemetry backend will add cost before production.

## Reconsideration Triggers

Revisit this decision when:

- Telemetry volume makes hosted storage uneconomical
- Compliance requires a specific region, retention period or immutable logs
- Incident response requires approved pseudonymous tenant correlation
- Collector availability or backpressure affects application resources
- Web tracing becomes necessary for diagnosing BFF performance
- Multiple VPS instances require a dedicated Collector tier
- A managed platform provides a materially simpler standards-compatible path
