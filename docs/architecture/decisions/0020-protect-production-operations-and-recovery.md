# ADR 0020: Protect Production Operations and Recovery

- Status: Accepted
- Date: 2026-08-01

## Context

The local demo uses development configuration, Mailpit, local Docker volumes
and console logging. A production deployment must protect credentials and
customer data, detect failures, recover from data loss and deliver real email
without turning the first VPS into a large observability platform.

Production safety must cover the host, application network, backups, logs,
telemetry and alerting. These controls must remain provider-neutral where
practical so that moving from the first VPS does not require changes to domain
code.

## Considered Options

### Keep Demo Operations and Rely on the Docker Volume

This is inexpensive but provides no off-host recovery, weak failure visibility
and unsafe development services in a public environment.

### Self-Host a Full Operations Stack

Running a complete logging, metrics, tracing, dashboarding and alerting stack
on the same VPS provides control but consumes resources and adds several new
systems that must themselves be maintained and backed up.

### Minimal Secure Runtime with Provider-Neutral Telemetry

The host enforces a small public surface, backups leave the VPS, applications
emit structured and sanitized telemetry, and external services provide uptime,
error and backup alerts. OpenTelemetry keeps the application independent from
the final telemetry backend.

## Decision

The VPS will use SSH key authentication, disable password and direct root SSH
login, and provide a dedicated restricted deployment user. The firewall allows
only SSH, HTTP and HTTPS, with SSH source restriction when a stable
administration address is available. The operating system receives security
updates, and application containers continue to run as non-root users.

Caddy manages TLS and HTTP-to-HTTPS redirects. Production and staging use
different domains and have no shared database credentials, JWT signing keys,
SMTP credentials or persistent volumes. PostgreSQL, API and Worker remain on
private Docker networks.

Development uses Mailpit. Staging uses Mailpit or an email provider sandbox
that cannot contact arbitrary real customers. Production uses a transactional
SMTP provider through the existing `IEmailSender` abstraction. Sender-domain
SPF, DKIM and DMARC configuration is required before production invitations or
reminders are enabled.

PostgreSQL receives a nightly encrypted custom-format `pg_dump`. Backups are
copied to storage outside the VPS and retained for fourteen daily restore
points plus weekly long-term restore points. Backup failure raises an alert.
A documented restore into staging is performed at least monthly. The initial
recovery objectives are a 24-hour RPO and a four-hour RTO.

API, Worker and Web emit structured JSON logs to standard output. Requests
carry a correlation identifier across Web and API. Logs and telemetry must not
contain passwords, JWTs, raw invitation tokens, SMTP credentials or customer
PII by default. Central error tracking receives sanitized unhandled exceptions.

External uptime monitoring checks the public Web path and API readiness through
a controlled operational route. Host monitoring alerts on disk exhaustion,
memory pressure, sustained CPU pressure and repeated container restarts.
Backup failure and deployment health-check failure are separately actionable.

.NET services use OpenTelemetry for traces, metrics and logs with an
environment-configured OTLP exporter. The first VPS will not self-host a full
Prometheus, Grafana and Loki stack. The telemetry backend may change without
changing domain or application code.

## Positive Consequences

- Loss of the VPS does not imply loss of every database backup.
- Restore capability is exercised instead of assumed.
- Development email cannot accidentally become production delivery.
- Structured, correlated telemetry shortens incident diagnosis.
- Sensitive authentication and customer data are excluded from normal logs.
- OpenTelemetry avoids application-level observability vendor lock-in.
- The first runtime remains operationally small enough for one team.

## Negative Consequences

- Off-site storage, SMTP, uptime and error tracking add external dependencies.
- A 24-hour RPO can still lose up to one day of data.
- Restore drills and host patching require recurring operational work.
- Staging and production isolation on one VPS is logical, not physical.
- Full historical metrics exploration is deferred.

## Reconsideration Triggers

Revisit this decision when:

- Customer commitments require an RPO below 24 hours or an RTO below four
  hours
- Managed PostgreSQL provides point-in-time recovery for production
- Telemetry volume or incident response requires a dedicated observability
  platform
- Compliance requires centralized secret management, immutable audit storage
  or longer backup retention
- Multiple notification channels require provider-specific delivery tracking
- Staging must be physically isolated from production

