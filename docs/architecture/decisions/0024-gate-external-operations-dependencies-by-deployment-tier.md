# ADR 0024: Gate External Operations Dependencies by Deployment Tier

- Status: Accepted
- Date: 2026-08-08

## Context

The first public ServicePilot environment is a disposable staging VPS. An
S3-compatible repository, transactional SMTP provider and hosted telemetry
backend have not yet been selected. Requiring them would block the staging
deployment, while silently allowing production without them would create an
unacceptable recovery and operations risk.

## Decision

The core VPS runtime and external operations dependencies are separate Compose
layers. `DEPLOYMENT_TIER`, `OFFSITE_BACKUP_ENABLED`,
`OBSERVABILITY_ENABLED` and `SMTP_MODE` form the operator-facing contract.

Staging may use private Mailpit and explicitly disable off-site backup and
hosted telemetry. Such an environment is disposable and cannot hold customer
data. Production validation requires encrypted off-site Restic backup, hosted
OpenTelemetry export and external TLS SMTP. Production cannot select the
Mailpit profile.

Each environment uses a distinct Compose project name, domain, restricted
environment file, release state and persistent volume. Optional layers are
selected by the shared Compose wrapper so deployment, backup and rollback
commands cannot accidentally assemble different runtime models.

All application and infrastructure runtime images are immutable registry
digest references. A release manifest identifies API, Web, Worker, Migrator
and Backup as one promotable release.

## Consequences

- Public staging can be rehearsed before provider accounts are purchased.
- Missing production recovery, email or telemetry controls fail during
  preflight rather than after launch.
- Staging without off-site backup must be treated as disposable.
- Operators must manage explicit environment gates and separate Compose
  overlays.

## Reconsideration Triggers

Revisit this decision when external services become managed platform bindings,
staging begins holding protected data, or ServicePilot moves away from the
single-VPS Compose deployment model.
