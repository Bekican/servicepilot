# VPS Deployment, Backup and Recovery Runbook

## Deployment boundary

The first Internet deployment is a disposable staging environment on one
Ubuntu LTS VPS. It must not hold real customer data while encrypted off-site
backup is disabled. Caddy is the only public service and publishes TCP 80/443
and UDP 443. Web, API, Worker, Migrator and PostgreSQL remain on private Docker
networks. Mailpit publishes its UI only on VPS loopback for SSH tunnelling.

The runtime is split into three Compose layers:

- `compose.staging.yaml`: PostgreSQL, optional Mailpit, Migrator, API, Worker,
  Web and Caddy
- `compose.staging.observability.yaml`: optional private OpenTelemetry
  Collector and hosted exporter
- `compose.staging.backup.yaml`: optional Restic/PostgreSQL backup operation

`scripts/staging-compose.sh` selects the optional layers from the restricted
environment file. Operators should use the deployment and backup scripts
instead of assembling Compose arguments manually.

## VPS and DNS preparation

Use a supported Ubuntu LTS VPS with at least 2 vCPU, 4 GiB RAM and 40 GiB disk.
Create a non-root `servicepilot` operator and install Docker Engine with the
Compose plugin, Git, `curl`, `jq` and `getent`. Place the repository at
`/opt/servicepilot/staging` and make the operator its owner.

Before deployment:

1. Point the staging domain A/AAAA records to the VPS and wait for public DNS
   resolution.
2. Allow inbound TCP 22, 80 and 443 and UDP 443. Restrict SSH by source IP when
   practical. Never publish PostgreSQL.
3. Authenticate the operator to GHCR with a read-only package token.
4. Copy `deploy/staging/.env.example` to `/etc/servicepilot/staging.env`, fill
   real values, set owner/group to the operator and run `chmod 600`.
5. Resolve the PostgreSQL, Mailpit, Caddy and optional Collector image tags to
   real registry SHA-256 digests. Example repeated digests are rejected.

The first staging environment uses:

```dotenv
DEPLOYMENT_TIER=staging
COMPOSE_PROJECT_NAME=servicepilot-staging
OFFSITE_BACKUP_ENABLED=false
OBSERVABILITY_ENABLED=false
SMTP_MODE=mailpit
DOTNET_ENVIRONMENT_NAME=Staging
```

Validate without printing secret values:

```sh
cd /opt/servicepilot/staging
SERVICEPILOT_ENV_FILE=/etc/servicepilot/staging.env \
  /bin/sh scripts/validate-staging-environment.sh
```

The validator emits a warning while backup is disabled. This is acceptable
only for disposable staging.

## Immutable release and first deployment

Run the `Publish Immutable Images` GitHub workflow. It publishes API, Web,
Worker, Migrator and Backup images. Resolve each published tag to its registry
digest and create a private release manifest using
`deploy/staging/release-manifest.example.json` as the shape. The manifest is
not a secret, but it must describe the exact five artefacts being promoted.

Deploy with:

```sh
/bin/sh scripts/deploy-staging.sh /secure/path/release-manifest.json
```

The script checks Docker, free disk, configuration, immutable references and
DNS; pulls the candidate images; starts PostgreSQL; runs forward-only
migrations; rolls out API, Worker, Web and Caddy; then waits for Worker health
and both HTTPS readiness endpoints. A failed application rollout restores the
previous application images. It never runs a down migration or automatically
restores PostgreSQL, so schema changes must follow expand-and-contract.

Useful checks:

```sh
curl --fail https://staging.example.net/ops/api/ready
curl --fail https://staging.example.net/ops/web/ready
SERVICEPILOT_ENV_FILE=/etc/servicepilot/staging.env \
SERVICEPILOT_RELEASE_FILE=.deploy-state/staging/current-release.env \
  /bin/sh scripts/staging-compose.sh ps
```

Rollback application images only:

```sh
/bin/sh scripts/rollback-staging.sh
```

## Private staging mail

Staging captures all email in Mailpit and cannot deliver real messages. Its UI
binds only to `127.0.0.1:8025` on the VPS. Access it through an SSH tunnel:

```sh
ssh -L 8025:127.0.0.1:8025 servicepilot@staging.example.net
```

Then open `http://127.0.0.1:8025` locally. Do not add a public Caddy route for
Mailpit. Production sets `SMTP_MODE=external`, enables TLS and provides the
real SMTP credentials; Mailpit is then outside the active Compose profile.

## Enabling hosted observability

Set `OBSERVABILITY_ENABLED=true`, provide an immutable Collector image,
`OTEL_BACKEND_ENDPOINT=https://...` and the exact authorization header. The
Collector alone receives hosted credentials. Applications send bounded,
fail-open OTLP traffic over the private Docker network. Run a normal immutable
deployment after changing the setting so all services are recreated with
telemetry enabled.

## Enabling encrypted off-site backup

Before storing real data, create a dedicated least-privilege S3-compatible
bucket identity and keep the Restic password in a second protected credential
record. Set `OFFSITE_BACKUP_ENABLED=true` and fill the HTTPS repository, Restic
password and S3 credentials.

Initialize and prove the first recovery point:

```sh
/bin/sh scripts/initialize-restic-staging.sh
/bin/sh scripts/backup-staging.sh
/bin/sh scripts/restore-drill-staging.sh
```

Install the supplied systemd unit and timer for nightly backups. The default
schedule is 02:15 UTC with random delay and catch-up after downtime. Configure
a dead-man-switch heartbeat whose grace period exceeds expected backup time.
The heartbeat fires only after dump validation, isolated local restore,
encrypted upload, retention/prune and `restic check` all succeed.

Run `scripts/restore-drill-staging.sh` at least monthly and record timestamp,
duration, snapshot and operator. The drill downloads the latest matching
snapshot, verifies checksum and catalogue, restores into a temporary database,
compares non-PII table counts and removes the verification database.

The same code can be rehearsed free on Docker Desktop and MinIO:

```powershell
.\scripts\verify-offsite-backup-local.ps1
```

## Production promotion gate

Production uses a separate domain, environment file, Compose project name,
volume and release state directory. Never rename the staging project in place.
Before the first customer is admitted, all of the following must be true:

- `DEPLOYMENT_TIER=production` and `DOTNET_ENVIRONMENT_NAME=Production`
- `COMPOSE_PROJECT_NAME` explicitly identifies production
- encrypted off-site backup is enabled and a restore drill has passed
- hosted observability and its alert path are enabled
- external TLS SMTP is configured and delivery has been verified
- DNS, TLS renewal, firewall, SSH restriction and disk capacity are verified
- the exact release ran successfully in staging and rollback was rehearsed
- backup retention remains at least 14 daily and 8 weekly snapshots

The validator rejects production when backup, observability or external SMTP
is missing. RAG/LLM work begins only after the staging release and operational
drills are stable; it ships behind staging verification and feature flags, not
as an untested change directly in production.
