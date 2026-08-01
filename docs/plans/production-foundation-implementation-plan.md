# ServicePilot Production Foundation Implementation Plan

This plan turns the demo-ready modular monolith into a manually deployable and
recoverable VPS runtime. It implements ADR 0019 and ADR 0020 without adding
product features. The phase contains exactly two main implementation jobs.

## Scope

- Automated pull-request quality checks
- Immutable API, Worker and Web container images
- Manual staging and production deployment
- Caddy TLS termination and private application networking
- A separate one-shot migration process
- Environment and secret boundaries
- Off-site database backup and tested restore
- Structured logs, correlation, telemetry and operational alerts
- Deployment, rollback, backup and restore runbooks

## Non-Goals

- Kubernetes or microservices
- Automatic production deployment
- Multi-region or high-availability infrastructure
- Managed PostgreSQL in the initial VPS implementation
- A self-hosted Prometheus, Grafana and Loki platform
- Product features such as Employee availability or Work Orders

## Job 1: Build and Release Pipeline

1. Add a pull-request CI workflow that runs backend build, unit, architecture
   and integration tests, migration drift, frontend format, lint, typecheck,
   Vitest, production build and the isolated Playwright acceptance flow.
2. Build API, Worker, Web and one-shot Migrator images once, tag them with the
   commit SHA and publish them to the container registry.
3. Add explicit manual staging and production deployment workflows. Both take
   an immutable image version; neither rebuilds source code.
4. Protect deployments with environment-specific concurrency, credentials and
   an explicit production target. Use required reviewers when the repository
   plan supports them, but do not make deployment safety depend solely on a
   paid GitHub feature.
5. Add a remote deployment script that:
   - validates the requested image version,
   - pulls all required images,
   - runs the Migrator as a one-shot container,
   - updates the application containers,
   - waits for readiness,
   - records the successful version,
   - restores the previous application images if readiness fails.
6. Keep database rollback manual. Document and enforce additive
   expand-and-contract migrations for releases that must remain rollback-safe.

### Job 1 Acceptance Criteria

- Pull requests cannot pass the quality workflow when any existing backend,
  frontend, migration or E2E gate fails.
- A commit produces immutable, traceable API, Worker, Web and Migrator images.
- Staging and production deploy only after a manual trigger.
- Production deploys the exact image version already selected for staging.
- Two deployments of the same version are idempotent.
- A failed migration does not update API, Web or Worker.
- A failed readiness check returns application containers to the previous
  version without running a down migration.
- `Database:MigrateOnStartup` is false in staging and production.
- Deployment logs do not expose runtime secrets.

## Job 2: Secure and Observable VPS Runtime

1. Add production-oriented Compose definitions for Caddy, Web, API, Worker,
   Migrator and PostgreSQL. Staging and production use separate projects,
   networks, volumes, domains and environment files. Only Caddy publishes
   public HTTP/HTTPS ports.
2. Add Caddy configuration for automatic HTTPS, secure response headers and
   proxying to Web. Configure trusted forwarded headers and canonical public
   invitation URLs.
3. Add environment templates and validation without adding real secrets.
   Document root/deploy ownership and restricted file permissions on the VPS.
4. Configure development Mailpit, staging-safe email and production
   transactional SMTP boundaries. Document SPF, DKIM and DMARC prerequisites.
5. Add a nightly encrypted PostgreSQL custom-format backup job, off-site upload,
   retention and failure signalling. Add an explicit staging restore command
   and a monthly restore-drill checklist.
6. Add structured JSON logging, Web-to-API correlation IDs, sensitive-data
   redaction and OpenTelemetry instrumentation with an optional OTLP exporter.
7. Document external uptime, error, host-resource, container-restart,
   deployment and backup alerts without self-hosting a full observability stack.
8. Add VPS bootstrap and operations runbooks for SSH hardening, firewall,
   security updates, first deployment, rollback, backup and restore.

### Job 2 Acceptance Criteria

- Staging and production cannot share database users, JWT keys, SMTP secrets,
  networks or persistent volumes.
- PostgreSQL, API, Worker and Mailpit have no public production ports.
- HTTPS redirects and Web routing work through Caddy.
- Containers run as non-root users.
- Missing mandatory production configuration fails fast without printing the
  secret value.
- Staging cannot send email to arbitrary real customer addresses.
- Backups leave the VPS, are encrypted and follow the retention policy.
- A backup can be restored into a fresh staging database by following the
  documented command.
- Logs correlate Web and API requests and contain no JWT, password or raw
  invitation token.
- Readiness, uptime, host pressure, restart and backup-failure alerts have
  documented owners and actions.

## Implementation Order

1. Close Job 1 and verify the first manual staging deployment with disposable
   staging data.
2. Close Job 2 and complete a backup/restore drill before production is
   considered available.
3. Run the complete MVP acceptance gate against the final staging release.
4. Perform the first production deployment only after the operations runbooks
   and secret inventory have been reviewed.

## Phase Completion

Phase 2 is complete when both jobs meet their acceptance criteria, the same
immutable version passes staging and production smoke checks, a failed release
has been rolled back in a controlled exercise, and a database backup has been
restored successfully into an isolated staging database.

