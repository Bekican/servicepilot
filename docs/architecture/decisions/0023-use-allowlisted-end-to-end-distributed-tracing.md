# ADR 0023: Use Allowlisted End-to-End Distributed Tracing

- Status: Accepted
- Date: 2026-08-05

## Context

ADR 0022 established provider-neutral correlated observability through W3C
Trace Context, OpenTelemetry and a lightweight Collector. ServicePilot now
emits API request traces and independent operation traces for Worker and
Migrator, but it does not yet provide one complete and consistently sanitized
trace model across Next.js Web, API, PostgreSQL, Worker and Migrator.

Web currently forwards a correlation ID but does not create and propagate a
W3C `traceparent`. PostgreSQL work is not represented by controlled database
spans. Background and migration operations have no incoming request parent,
and therefore require an explicit root-trace lifecycle. The implementation
must add this diagnostic value without copying SQL, request data, exception
details or customer information into the telemetry backend.

## Considered Options

### Keep Correlation IDs Without Distributed Traces

Correlation IDs are useful support references, but they do not describe
parent-child timing across Web, API and PostgreSQL. Operators would still need
to reconstruct an operation manually from separate records.

### Instrument Only API and PostgreSQL

This would expose backend latency but leave the Next.js BFF boundary outside
the trace. Time spent before the API request could not be distinguished from
API processing time.

### Capture Full Automatic Telemetry

Automatic instrumentation is easy to enable, but its default attributes can
include raw URLs, query strings, database statements, exception messages and
high-cardinality values. That is incompatible with ServicePilot's telemetry
data-minimization rules.

Native Npgsql tracing was also verified against a controlled database failure.
Its failure activity contained exception events with an exception message and
stack trace. Removing those fields only at the Collector would allow sensitive
data to leave the application process before sanitization, so Collector-side
redaction alone is not an acceptable boundary.

### Use W3C Propagation with an Explicit Attribute Allowlist

Each runtime uses standard OpenTelemetry instrumentation and W3C Trace Context,
while application processors and configuration retain only approved,
low-cardinality diagnostic fields.

## Decision

ServicePilot will implement end-to-end distributed tracing across Next.js Web,
ASP.NET Core API and PostgreSQL. Worker cycles and Migrator execution will use
the same trace and sanitization conventions, but will start independent root
traces because they have no causal incoming HTTP request.

### Trace Topology

The synchronous request topology is:

```text
Next.js server request
  -> ASP.NET Core route
     -> PostgreSQL operation
```

Next.js is the initial server-side trace boundary. Browser Real User
Monitoring is outside the initial scope. Web will inject a valid W3C
`traceparent` into its server-to-server API requests. API will continue that
context and database spans will be children of the active API span.

The asynchronous and startup topologies are:

```text
reminder.process_due
  -> PostgreSQL operation

retention.run
  -> PostgreSQL operation

database.migrate
  -> PostgreSQL operation
```

Each Worker cycle and each Migrator execution starts a new root trace. A future
message queue may carry `traceparent` in its message envelope, but no synthetic
parent relationship will be invented before such a causal message exists.
Root creation explicitly discards unrelated ambient activity context. Worker
sleep intervals are outside the root span, so span duration represents only the
actual processing cycle rather than its scheduling interval.

### Trace and Correlation Roles

Trace ID is the canonical machine-to-machine relationship used by the
telemetry system. Correlation ID remains the stable support-facing lookup code.
Web will propagate both `traceparent` and `X-Correlation-ID`; API will keep
returning the correlation ID and include both identifiers in safe Problem
Details responses.

Browser-supplied `traceparent`, `tracestate` and `baggage` are not trusted. The
Next.js boundary removes them and starts server-owned context. Web propagates
that context only on marked ServicePilot API fetches. API accepts this private
server-to-server context. Baggage is never extracted or propagated, because it
can become a path for user data and unbounded cardinality.

### Stable Span Names

Span names will describe operations using stable route templates or controlled
operation names. Examples include:

- `GET api/customers/{customerId}`
- `POST api/appointments`
- `reminder.process_due`
- `retention.run`
- `database.migrate`

Raw identifiers, URLs and query values will not appear in span names.

### Controlled Database Tracing

PostgreSQL spans will be created by a custom EF Core `DbCommandInterceptor`
using the `ServicePilot.Database` activity source. Native Npgsql activity
export will remain disabled.

The interceptor observes only the command lifecycle: start, successful
completion, cancellation or failure, and elapsed time. It does not inspect or
export command text, parameters, result rows, connection strings or exception
messages. Database health checks are outside this interceptor's responsibility.

Database spans use the stable name `postgresql.command`. They may contain only
the database fields in the allowlist below. A PostgreSQL SQLSTATE may be
retained as `db.response.status_code` only when it matches the controlled
five-character SQLSTATE format. No exception event is added to the activity.

### Attribute Allowlist

Telemetry emitted to the Collector may contain the following fields when they
are applicable:

- `service.name`
- `service.version`
- `deployment.environment.name`
- `http.request.method`
- `http.route`
- `http.response.status_code`
- `db.system.name`
- `db.operation.name`
- `db.response.status_code`
- `exception.type`
- `problem.code`
- `servicepilot.correlation_id`
- `servicepilot.reminder.processed_count`
- `servicepilot.retention.deleted_invitation_count`
- `servicepilot.retention.anonymized_audit_count`
- `servicepilot.migration.count`

Instrumentation-specific identifiers required by the OpenTelemetry protocol,
such as trace ID, span ID, parent span ID, timestamps, duration, span kind and
status, are also retained.

The following data is prohibited:

- Raw path, full URL, query string and HTTP target
- Request or response bodies
- Headers, cookies, JWTs and credentials
- SQL command text, parameters and connection strings
- Customer, tenant and user identifiers or contact data
- Migration names
- Exception messages, stack traces and inner exceptions

Filtering will follow an allowlist model. Adding a new business or
high-cardinality attribute requires a separate security review.

### Error Semantics

Expected client outcomes such as validation, authentication, authorization and
not-found responses will retain their HTTP status and safe problem code, but
will not record exception details. Unexpected server and database failures will
set the owning span status to `ERROR` and may record only the sanitized
exception type.

Worker shutdown cancellation is an expected lifecycle event and will not mark
the cycle span as an error. Unexpected Worker failures mark the cycle span as
`ERROR` before the Worker logs the safe exception type and continues with a
later cycle. Migrator failures follow the same trace policy and retain their
non-zero process exit code.

Staging and production exporters will not emit exception message, stack trace
or inner-exception data. Local development may display additional diagnostic
detail outside the Collector pipeline.

### Sampling and Failure Behavior

Initial staging and production deployments will export all traces. This is
appropriate for the expected launch traffic and avoids missing evidence during
early incident diagnosis. Sampling will be reconsidered when telemetry volume
or backend cost becomes material. Any future policy must preserve errors and
slow operations preferentially.

Export remains asynchronous, bounded and fail-open. Collector or backend
unavailability must not fail an HTTP request, stop Worker processing or mark a
successful migration as failed. Telemetry may be dropped after bounded queues
and retries are exhausted; business operations may not be blocked.

### Verification

Automated and local-staging verification will prove that:

- Web and API share a trace and have the correct parent-child relationship.
- PostgreSQL spans are children of the active API, Worker or Migrator span.
- Database failure spans contain no activity events, exception messages or
  stack traces.
- Worker cycles and Migrator executions start independent root traces.
- Browser-controlled trace context and baggage are ignored safely.
- Health endpoints do not create routine request traces.
- Correlation ID has the same value in the relevant log and trace.
- Raw URLs, queries, SQL, secrets, PII, exception messages and stack traces do
  not reach the Collector output.
- API, Worker and Migrator continue operating when the Collector is unavailable.

The local staging observability drill will remain an executable acceptance
test and will be expanded as each trace boundary is implemented.

## Positive Consequences

- A request can be timed across Web, API and PostgreSQL as one trace.
- Background and migration database work follows the same diagnostic model.
- W3C and OpenTelemetry standards preserve provider neutrality.
- Explicit allowlisting reduces sensitive-data exposure and telemetry cost.
- Stable names and attributes avoid uncontrolled cardinality.
- Telemetry failure remains isolated from business availability.

## Negative Consequences

- Next.js tracing and custom database tracing add runtime configuration and
  maintenance responsibility.
- Strict sanitization requires processors, tests and ongoing review.
- Removing raw SQL and exception details can make some investigations slower.
- Exporting every initial trace consumes more backend capacity.
- Web tracing adds another runtime whose instrumentation must be kept current.

## Relationship to ADR 0022

This decision specializes ADR 0022. It replaces ADR 0022's deferral of Node.js
Web tracing: server-side Next.js tracing is now required for the synchronous
Web-to-API boundary. ADR 0022 remains authoritative for the provider-neutral
Collector boundary, structured logging, operational ownership and the broader
observability strategy.

## Reconsideration Triggers

Revisit this decision when:

- Trace volume or hosted-backend cost requires sampling
- Browser-side performance requires Real User Monitoring
- A queue introduces causal trace propagation into Worker processing
- Approved pseudonymous tenant correlation becomes operationally necessary
- OpenTelemetry semantic conventions require allowlist changes
- Sanitization materially prevents resolution of recurring incidents
