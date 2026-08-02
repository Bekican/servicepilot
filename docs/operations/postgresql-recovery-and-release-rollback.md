# PostgreSQL Recovery and Release Rollback Runbook

## Purpose

ServicePilot treats application rollback and database recovery as two separate
operations. A failed application release may automatically return Web, API and
Worker to their previous immutable images. PostgreSQL is never automatically
restored and an EF Core down migration is never run automatically.

This separation prevents a routine release problem from overwriting valid
customer data created after a backup or destructively reversing a schema
change.

## Local recovery proof

Run the complete local backup and restore drill while local staging is up:

```powershell
.\scripts\verify-recovery-local.ps1
```

The backup flow:

1. Runs `pg_dump` in PostgreSQL custom format inside the database container.
2. Verifies that `pg_restore` can read the archive catalogue.
3. Copies the binary archive to the ignored `backups/staging-local` directory.
4. Writes SHA-256, PostgreSQL version, source commit and non-PII table counts.

The restore flow verifies the checksum, creates a new database from
`template0`, restores with `--exit-on-error`, runs `ANALYZE`, and compares the
restored schema footprint and business-table counts with the metadata. It
never targets `servicepilot_staging`. The generated verification database is
dropped after a successful drill unless `-KeepVerificationDatabase` is used.

The individual commands are:

```powershell
$backup = .\scripts\backup-staging-local.ps1
.\scripts\restore-staging-local.ps1 -BackupPath $backup
```

Local backup files are recovery artefacts, not source code, and are ignored by
Git.

## Release and application rollback

A release manifest identifies all four application images. Remote staging and
production manifests must use registry digest references rather than mutable
tags:

```text
ghcr.io/example/servicepilot-api@sha256:<64 hexadecimal characters>
```

For a local release rehearsal:

```powershell
.\scripts\deploy-staging-local.ps1 `
  -ReleaseManifest .\deploy\staging\release-manifest.local.example.json `
  -AllowMutableLocalImages `
  -SkipPull
```

The deploy command validates the manifest and Compose model, obtains a tested
backup, runs the candidate Migrator once, updates application containers and
checks Web and API readiness. A readiness failure restores the previous image
references. Secrets stay in the ignored environment file and are never stored
in the release manifest.

To deliberately return to the previous application release:

```powershell
.\scripts\rollback-staging-local.ps1
```

Rollback does not run Migrator and does not modify PostgreSQL. Therefore every
schema release must use expand-and-contract:

1. Add backward-compatible schema first.
2. Deploy code that can work with both old and new schema.
3. Remove obsolete schema only in a later release after the rollback window.

## Production backup architecture

Before production, the same verified logical-backup flow will be scheduled
nightly on the VPS. The production pipeline must add these operational layers:

- Encrypt before data leaves the VPS, preferably through an encrypted `restic`
  repository rather than custom cryptography.
- Copy to storage outside the VPS; the local Docker volume is not a backup.
- Keep fourteen daily restore points plus the agreed weekly long-term points.
- Alert when dump, archive validation, encryption or upload fails.
- Perform and record a restore into staging at least monthly.
- Restrict backup credentials and files to the deployment/backup identity.
- Never include database passwords, raw invitation tokens or row-level PII in
  metadata and logs.

The initial objectives from ADR 0020 are an RPO of 24 hours and an RTO of four
hours. These are targets, not guarantees, until an offsite restore drill proves
them on the real VPS.

## Database incident recovery

Database restore is a deliberate disaster-recovery procedure, not a deployment
button:

1. Put the application into maintenance/read-only mode and stop writers.
2. Preserve a final backup of the current damaged state for investigation.
3. Select a verified restore point using its timestamp, checksum and metadata.
4. Restore into a new database; do not overwrite the damaged database in place.
5. Run schema checks, critical table counts and a read-only application smoke.
6. Change the connection to the recovered database and run readiness checks.
7. Keep the old database isolated until the incident is closed.
8. Record actual data loss and recovery time, then update RPO/RTO assumptions.

If only an application release is bad while PostgreSQL data is healthy, use
application rollback instead of database recovery.
