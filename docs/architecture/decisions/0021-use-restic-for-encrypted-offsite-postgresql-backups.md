# ADR 0021: Use Restic for Encrypted Off-Site PostgreSQL Backups

- Status: Accepted
- Date: 2026-08-02

## Context

ServicePilot can create a PostgreSQL custom-format backup, verify its checksum
and restore it into an isolated local staging database. This proves the logical
backup and restore process, but a backup stored only on the same computer or
VPS does not protect against host loss, disk failure, theft or operator error.

ADR 0020 requires nightly encrypted PostgreSQL backups outside the VPS, an
initial recovery point objective of 24 hours and recurring restore drills. The
remaining decision is how backup encryption, remote storage, scheduling and
failure notification will work without coupling ServicePilot to one storage
provider.

## Considered Options

### Keep Backups Only on the VPS

This is simple and inexpensive, but the database and every recovery point can
be lost in the same infrastructure incident. It does not satisfy ADR 0020.

### Write Custom Encryption and Upload Scripts

Custom scripts can upload `pg_dump` archives to object storage, but the team
would own encryption, repository integrity, deduplication, retention and
credential-handling behavior. This creates unnecessary security risk.

### Use Provider-Specific Backup Features

A storage or VPS provider may offer snapshots and managed backup features.
These can be useful as an additional layer, but make recovery dependent on the
same provider and do not preserve the current provider-neutral deployment
model.

### Use Restic with S3-Compatible Off-Site Storage

Restic provides client-side encryption, integrity verification, snapshots and
retention operations. An S3-compatible repository allows the storage provider
to change without changing application or domain code.

## Decision

ServicePilot will use Restic to store PostgreSQL backup artefacts in an
encrypted repository outside the VPS. PostgreSQL continues to produce a
custom-format `pg_dump`; its archive, checksum and non-PII verification
metadata become the Restic backup input.

The remote repository will use an S3-compatible interface. The first storage
provider is intentionally not locked by this ADR. Repository URL, access key,
secret key and Restic password are runtime operations configuration and must
never be committed, printed in logs or included in container images.

The backup schedule will support three triggers:

- An automatic nightly production backup
- A mandatory pre-deployment backup before a release can run migrations
- An operator-triggered manual backup for incidents and maintenance

The initial production RPO is 24 hours. A failed nightly backup therefore must
be visible before the next backup window.

Backup completion is reported through an environment-configured external
heartbeat endpoint. A heartbeat is sent only after dump creation, archive
validation, checksum generation, Restic upload and repository verification
all succeed. Any failed step exits non-zero, emits a sanitized operational log
and causes the missing or failed heartbeat to raise an external alert.

Restore capability, rather than upload success alone, is the acceptance
boundary. At least monthly, an encrypted off-site snapshot will be downloaded
and restored into an isolated staging database. The drill must verify archive
integrity, schema footprint and critical business-table counts without
overwriting the active staging database.

Detailed long-term retention automation is deferred while ServicePilot has no
real customer data. It is a mandatory first-customer readiness gate, not a
post-incident task. Before the first real customer is onboarded, production
must satisfy ADR 0020 by retaining at least fourteen daily restore points plus
weekly restore points. The exact number of weekly and future monthly restore
points will be decided from storage cost, customer commitments and compliance
requirements at that gate.

Restic credentials will belong to a dedicated least-privilege backup identity.
Secret files on the VPS will be readable only by the restricted operations
identity. Backup metadata will contain timestamps, versions, checksums and
aggregate verification counts, but no row-level customer PII, passwords, JWTs,
raw invitation tokens or SMTP credentials.

Provider snapshots may be enabled later as a second recovery layer, but they
do not replace the encrypted Restic repository or tested PostgreSQL restore
procedure.

## Positive Consequences

- Losing the VPS does not also destroy every database recovery point.
- Backups are encrypted before leaving the controlled host.
- S3 compatibility avoids binding recovery to one storage vendor.
- Pre-deployment backups reduce the recovery risk of schema changes.
- External heartbeat monitoring makes silent backup failure visible.
- Monthly restore drills prove that encrypted remote snapshots are usable.
- Existing PostgreSQL backup and verification scripts remain the logical
  foundation of the production flow.

## Negative Consequences

- Restic, object storage and heartbeat monitoring add operational dependencies.
- Losing the Restic password can make every encrypted backup unusable.
- A 24-hour RPO can still permit up to one day of data loss.
- Restore drills consume engineering time and temporary staging storage.
- Pre-deployment backup verification increases deployment duration.
- Retention and repository pruning must be operated carefully and monitored.

## Reconsideration Triggers

Revisit this decision when:

- Customer commitments require an RPO below 24 hours
- Managed PostgreSQL provides tested point-in-time recovery
- Backup volume or restore time threatens the four-hour RTO
- Compliance requires immutable/WORM storage or region-specific retention
- A second independent backup repository becomes necessary
- S3-compatible storage no longer meets cost, durability or access needs
- Database size makes logical dumps unsuitable as the primary recovery method
