# Staging VPS and Off-Site Backup Runbook

## Current boundary

The repository contains a provider-neutral staging runtime. A VPS, DNS zone,
S3-compatible bucket and heartbeat monitor have deliberately not been selected
yet. Provider credentials and domains are runtime inputs; changing a provider
must not require an application-code change.

The runtime consists of Caddy, Web, API, Worker, Migrator, PostgreSQL, a private
Mailpit email sink and a one-shot backup image. Only Caddy publishes ports 80
and 443. PostgreSQL and all application processes remain on an internal Docker
network. The backup container additionally joins an outbound operations
network so it can reach off-site storage without publishing a port.

## Free local MinIO drill

Before purchasing infrastructure, Docker Desktop can exercise the same backup
image and restore code against a local S3-compatible MinIO repository:

```powershell
.\scripts\verify-offsite-backup-local.ps1
```

The first run starts local staging, builds the backup image, creates the MinIO
bucket, uploads a Restic snapshot, lists the encrypted repository and restores
the latest snapshot into an isolated PostgreSQL database. Later runs can skip
the application rebuild:

```powershell
.\scripts\verify-offsite-backup-local.ps1 -SkipStagingStart
```

MinIO binds only to loopback: API port `19000` and console port `19001`. The
local Docker connection uses HTTP by design; Restic still encrypts content
before upload. This proves S3 protocol compatibility and client-side
encryption, but it is not an off-site disaster-recovery proof and does not
replace production HTTPS. Local credentials live in the ignored
`.env.minio.local` file, and MinIO data is retained in a dedicated volume for
repeat drills.

## VPS prerequisites

Use a supported Ubuntu LTS server with a dedicated non-root `servicepilot`
operator. Install Docker Engine with the Compose plugin, `curl`, `jq` and Git.
Before the first deployment:

1. Point the staging DNS A/AAAA records at the VPS.
2. Allow inbound TCP 22, 80 and 443 and UDP 443. Restrict SSH by source IP when
   practical. Do not publish a PostgreSQL port.
3. Place the repository at `/opt/servicepilot/staging` and make the restricted
   operator its owner.
4. Copy `deploy/staging/.env.example` to
   `/etc/servicepilot/staging.env`, replace every placeholder, assign it to the
   `servicepilot` operator and set mode 600. The systemd job runs as that user.
5. Authenticate Docker to GHCR using a read-only package credential.
6. Create a dedicated least-privilege S3-compatible access key limited to the
   staging backup bucket or prefix.

Validate configuration without printing secret values:

```sh
cd /opt/servicepilot/staging
/bin/sh scripts/validate-staging-environment.sh
```

Staging deliberately sends all mail to its private Mailpit container. Mailpit
has no public route, so staging cannot send messages to real recipients. A
separate transactional SMTP setup remains a production gate.

## First release

Publish the five images using the `Publish Immutable Images` workflow. Resolve
the registry digest for the backup image and put it in
`SERVICEPILOT_BACKUP_IMAGE`. Put the four application digests in a release
manifest matching `deploy/staging/release-manifest.example.json`.

Run the initial deployment:

```sh
/bin/sh scripts/deploy-staging.sh \
  deploy/staging/release-manifest.example.json
```

The first release starts an empty PostgreSQL database and therefore has no
pre-existing application data to back up. It runs Migrator once, rolls out the
application and records the successful immutable references. If readiness
fails, the failed initial release is not recorded as current.

Initialize the encrypted repository after the first successful release, then
create and test the first recovery point:

```sh
/bin/sh scripts/initialize-restic-staging.sh
/bin/sh scripts/backup-staging.sh
/bin/sh scripts/restore-drill-staging.sh
```

Keep the Restic password in a separately protected credential record. Losing
it makes every encrypted snapshot unusable.

## Later releases and rollback

Every later deployment must complete a verified off-site backup before the
candidate Migrator can run. A dump, upload, retention, repository check or
configured heartbeat failure stops deployment before migration.

The deploy process runs forward-only migrations, replaces API, Worker, Web and
Caddy, and waits up to three minutes for public readiness. A readiness failure
returns the application containers to their previous immutable images. It
never runs a down migration and never restores PostgreSQL automatically.

For an operator-triggered application rollback:

```sh
/bin/sh scripts/rollback-staging.sh
```

Schema releases must therefore follow expand-and-contract compatibility.

## Nightly schedule

Install the supplied systemd unit and timer after adjusting their paths if the
checkout is not `/opt/servicepilot/staging`:

```sh
sudo cp deploy/staging/systemd/servicepilot-backup.* /etc/systemd/system/
sudo systemctl daemon-reload
sudo systemctl enable --now servicepilot-backup.timer
systemctl list-timers servicepilot-backup.timer
```

The default schedule runs nightly at 02:15 UTC with up to 15 minutes of random
delay and catches up after downtime. Configure `BACKUP_HEARTBEAT_URL` with a
dead-man-switch monitor whose grace period exceeds the expected backup
duration. The heartbeat is called only after dump validation, encrypted upload,
retention, pruning and `restic check` succeed.

Defaults retain 14 daily and 8 weekly snapshots. These values are configurable
but must not be reduced before first-customer retention requirements are
reviewed.

## Monthly recovery drill

Run at least monthly:

```sh
/bin/sh scripts/restore-drill-staging.sh
```

Each backup is restored locally into a temporary database before upload, so
its recorded counts describe the archive rather than a later view of the live
database. The monthly command then downloads the latest matching encrypted snapshot, verifies its
SHA-256 checksum and archive catalogue, restores it into a uniquely named
database, compares schema and non-PII business-table counts, and removes the
verification database. It never targets the active staging database.

Record the snapshot time, result, duration and operator in the operations log.
Upload success by itself is not recovery proof.

## Provider-selection gates

Before provisioning, compare candidate VPS and S3 providers for region,
durability, egress fees, bucket versioning or object-lock support, incident
history and account-level MFA. Prefer different failure domains for the VPS
and backup storage. Selection must also include a domain/DNS provider and an
external heartbeat service.
