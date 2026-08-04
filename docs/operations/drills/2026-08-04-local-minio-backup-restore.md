# Local MinIO Backup and Restore Drill — 2026-08-04

## Scope

This drill exercised the production backup and restore scripts against a local
S3-compatible MinIO repository on Docker Desktop. It used the existing isolated
local staging PostgreSQL database and did not target the normal demo database.

## Versions

- PostgreSQL image: `postgres:17-alpine`
- Restic: `0.18.1`
- MinIO: `RELEASE.2025-09-07T16-13-09Z`
- MinIO Client: `RELEASE.2025-08-13T08-35-41Z-cpuv1`

## Verified results

- Backup operations image built successfully as a non-root container.
- MinIO became healthy and the drill bucket was created idempotently.
- PostgreSQL produced a custom-format archive readable by `pg_restore`.
- The archive restored successfully into an isolated verification database
  before upload.
- SHA-256, non-PII verification metadata and archive were uploaded through
  Restic client-side encryption.
- Restic retention, prune and repository check completed successfully.
- The repository returned encrypted snapshots for host
  `servicepilot-staging-minio-drill`.
- The latest snapshot downloaded successfully from MinIO.
- BusyBox-compatible SHA-256 verification passed.
- The downloaded archive restored into a new isolated database.
- Restored schema and business-table counts matched backup metadata.
- Temporary verification databases were removed after completion.
- Staging API readiness remained healthy after PostgreSQL joined the drill
  operations network.
- A repeat run passed and reused the persistent MinIO repository.

## Finding and correction

The first restore attempt found that Alpine BusyBox supports `sha256sum -c`
but not the GNU long option `sha256sum --check`. The restore script was changed
to the portable short option and the complete drill then passed twice.

The initial full staging build exceeded the orchestration command's ten-minute
window but completed successfully immediately afterward. Subsequent runs used
`-SkipStagingStart` and completed from cached images in under two minutes.

## Limits

This drill proves application-level archive integrity, Restic encryption,
S3-compatible protocol behavior and restore logic. MinIO and PostgreSQL still
run on the same physical computer, and local MinIO uses HTTP inside the Docker
network. It does not prove real off-site durability, public DNS, production
TLS, provider credentials, internet failure handling, external heartbeat
alerting, VPS loss recovery or the four-hour RTO target.
