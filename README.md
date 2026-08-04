# ServicePilot

ServicePilot is a multi-tenant field service management SaaS for small and
medium-sized technical service companies.

## MVP Scope

The first demo will cover:

- Organization creation
- Authentication and authorization
- User and role management
- Customer management
- Service definition
- Appointment creation and status management
- Basic reminders and notifications
- A simple dashboard
- Organization-level data isolation

Broader employee operations, advanced reporting, inventory, payments and
high-scale messaging are outside the first demo unless an MVP use case requires
them.

## Architecture

ServicePilot starts as a modular monolith with the following dependency
direction:

```text
Api
|-- Application
|-- Infrastructure
`-- Contracts

Worker
|-- Application
`-- Infrastructure

Infrastructure
|-- Application
`-- Domain

Application
`-- Domain

Domain
`-- No project dependency
```

The API and Worker are separate processes. Business capabilities remain inside
one codebase and share PostgreSQL while module boundaries are kept explicit.

## Current Technology

- .NET 10
- ASP.NET Core
- PostgreSQL
- Entity Framework Core
- Docker
- Mailpit
- Next.js 16 App Router
- TypeScript
- Tailwind CSS and shadcn/ui
- xUnit
- Testcontainers

Redis, message brokers and observability tools will be introduced only when a
concrete use case justifies their operational cost.

## Current Status

Implemented:

- Organization domain and persistence model
- Application validation and slug normalization
- Application pre-check plus PostgreSQL unique constraint for organization
  slug consistency
- Unit and PostgreSQL integration tests, including concurrent requests
- Employee domain and persistence groundwork
- Organization and initial Owner onboarding through `POST /api/auth/register`
- Tenant-scoped user authentication with JWT access tokens
- Framework password hashing
- Authenticated tenant context from the `organization_id` claim
- Owner-only, 24-hour and single-use email invitations
- SHA-256 invitation token persistence without raw tokens
- SMTP email delivery through `IEmailSender`
- Local email inspection through Mailpit
- Owner-only user listing, role changes and activation/deactivation
- Self-modification and last-active-Owner protection
- Immutable user lifecycle audit events
- Active-Owner authorization against current database state
- Hybrid Individual/Company customers with soft deactivation
- Tenant-scoped customer email and E.164 phone uniqueness
- Concurrency-safe `CUS-000001` customer numbering
- Optional multiple addresses with one active primary address
- Tenant-scoped service catalog with default duration and active status
- Tenant-scoped appointment creation, listing, assignment and state transitions
- Offset-aware appointment input with UTC persistence
- Application overlap pre-check and PostgreSQL exclusion constraint
- Transactional appointment reminders with durable Worker processing
- Reminder retry intervals of 1, 5 and 30 minutes
- Visible failed/skipped reminder states and manager-triggered manual retry
- Owner/Admin UTC-day dashboard summary
- Daily invitation cleanup and five-year audit anonymization
- Health/readiness endpoints, safe HTTP logging and API security headers
- Fixed-window rate limits for authentication and invitation endpoints
- Containerized API, Worker, PostgreSQL and Mailpit demo environment
- Next.js BFF with an HttpOnly JWT session cookie
- Responsive dashboard based on real tenant-scoped API data
- Login, registration and invitation acceptance screens
- Customer, service, user, appointment and reminder operations
- Organization-time-zone scheduling with UTC API persistence

Deferred beyond the current MVP:

- Employee application use cases beyond technician assignment
- Inventory, payments, customer satisfaction and advanced reporting

## Engineering Goals

The project is also used to study:

- Modular monolith boundaries
- Multi-tenancy
- Authentication and authorization
- Transactions and concurrency
- Database constraints and indexing
- Idempotency and failure handling
- Background processing
- Integration testing
- Observability and performance

Architecture decisions are recorded under
[`docs/architecture/decisions`](docs/architecture/decisions).

## Provider-neutral VPS staging

The repository includes a production-shaped staging Compose model, an
immutable backup image and Linux operator scripts for deployment, rollback,
nightly encrypted Restic backups and isolated restore drills. Only Caddy
publishes public ports; the database and application services remain private.

Provisioning requirements, first-release sequencing and recovery commands are
documented in
[`docs/operations/staging-vps-and-offsite-backups.md`](docs/operations/staging-vps-and-offsite-backups.md).
Real domains, registry digests, database secrets, Restic credentials and
S3-compatible endpoints belong in `/etc/servicepilot/staging.env`, never in
source control.

Run the free local S3-compatible backup and restore rehearsal with Docker
Desktop and MinIO:

```powershell
.\scripts\verify-offsite-backup-local.ps1
```

## Local Infrastructure

Start the complete demo, apply migrations and create the idempotent demo
tenant:

```powershell
./scripts/start-demo.ps1
```

Demo credentials:

```text
Organization: servicepilot-demo
Email: owner@servicepilot.local
Password: Demo1234!
```

The web application is available at `http://localhost:3000`, the API at
`http://localhost:5267`, and Mailpit at `http://localhost:8025`. Mailpit accepts
SMTP traffic on `localhost:1025`.

Backend infrastructure-only development remains available with
`docker compose up -d postgres mailpit`.

For frontend-only development:

```powershell
cd apps/web
Copy-Item .env.example .env.local
npm ci
npm run dev
```

The frontend talks to the API only from the Next.js server/BFF. Browser code
never receives the JWT access token.

The local invitation link base URL and SMTP sender are configured in
`appsettings.Development.json`. Production values must be supplied through
environment variables or secret configuration:

- `Smtp__Host`
- `Smtp__Port`
- `Smtp__EnableSsl`
- `Smtp__FromAddress`
- `Smtp__FromName`
- `Smtp__Username`
- `Smtp__Password`
- `Invitations__PublicBaseUrl`

Invitation endpoints:

```text
POST /api/users/invitations
POST /api/users/invitations/{id}/resend
POST /api/auth/invitations/accept
```

User management endpoints:

```text
GET   /api/users
PATCH /api/users/{id}/role
PATCH /api/users/{id}/status
```

Customer endpoints are available under `/api/customers`, including create,
list, detail, update, deactivate and nested address lifecycle operations.

Service catalog endpoints are available under `/api/services`.

Appointment endpoints:

```text
POST  /api/appointments
GET   /api/appointments
GET   /api/appointments/{id}
PATCH /api/appointments/{id}/technician
PATCH /api/appointments/{id}/status
```

Reminder endpoints:

```text
GET  /api/reminders
POST /api/reminders/{id}/retry
```

The API persists the appointment and its reminder in one transaction. The
Worker claims due reminders atomically and sends email outside that
transaction, so an SMTP failure never rolls back the appointment.

Dashboard endpoint:

```text
GET /api/dashboard/summary
```

The Worker deletes used or expired invitations after 30 days. Audit events are
retained; events older than five years have actor and metadata values
anonymized.

Operational endpoints:

```text
GET /health/live
GET /health/ready
```

## MVP acceptance gate

The acceptance gate builds and tests the backend and frontend, checks migration
drift, then runs the browser journey against an isolated Docker Compose stack.
It does not read from or write to the normal demo database.

Install the Chromium browser once:

```powershell
cd apps/web
npx playwright install chromium
```

Run the complete gate from the repository root:

```powershell
.\scripts\verify-mvp.ps1
```

The isolated stack uses web `13000`, API `15267`, PostgreSQL `15432` and
Mailpit `18025`. It is removed after the run, including when a test fails.

## Database migrator

Production-oriented deployments use `ServicePilot.Migrator` as a one-shot
process before API, Web and Worker are updated. The process applies pending EF
Core migrations and exits with `0`; an error exits with `1`. Running it again
against an up-to-date database is safe and also exits successfully.

Provide the database connection string through configuration rather than a
command-line argument:

```powershell
$env:ConnectionStrings__Database = "Host=...;Database=...;Username=...;Password=..."
dotnet run --project src/ServicePilot.Migrator
```

The E2E Compose environment runs this migrator before starting the API and
executes it a second time as an idempotency check. The local demo may continue
to use startup migration for developer convenience; staging and production
keep `Database:MigrateOnStartup=false`.

## Local staging runtime

The production-shaped local staging stack is isolated from the normal demo by
its Compose project, networks and volumes. Only Caddy publishes host ports;
Web, API, Worker and PostgreSQL remain on private container networks. The API
does not migrate on startup: the one-shot Migrator must complete before the
application services start.

Start it from the repository root:

```powershell
.\scripts\start-staging-local.ps1
```

The first run creates the ignored `.env.staging.local` file from
`deploy/staging/.env.local.example`. Its values are local-only placeholders and
must never be copied to a public staging or production server.

Local endpoints:

```text
Web:           https://localhost:8443
API readiness: https://localhost:8443/ops/api/ready
Mailpit:       https://mailpit.localhost:8443
HTTP redirect: http://localhost:8080
```

Caddy issues these certificates from its internal CA. The browser may warn
until that local root certificate is trusted. The local Caddyfile deliberately
does not send HSTS, because HSTS on `localhost` would also affect the normal
HTTP demo on port `3000`.

Stop the local staging containers without deleting their data:

```powershell
.\scripts\stop-staging-local.ps1
```

Run the HTTPS smoke and persistence gate while local staging is running:

```powershell
.\scripts\verify-staging-local.ps1
```

The gate creates an isolated organization through the real UI, creates a
Customer and Service, sends and accepts a Technician invitation through
Mailpit, restarts every long-running staging container, and logs in again to
prove that PostgreSQL data survived. It also verifies that Caddy kept the same
local CA certificate across the restart. Raw invitation tokens are kept only
in test memory and Playwright tracing is disabled for this staging flow.

## CI and immutable images

The `Quality Gate` GitHub Actions workflow runs for pull requests and pushes to
`main`. It uses the same `scripts/verify-mvp.ps1` command as local development,
so local and CI acceptance rules cannot drift apart. The EF CLI version is
pinned in `.config/dotnet-tools.json` and restored by the verification script.

Container publication is deliberately separate from deployment. Run the
`Publish Immutable Images` workflow manually for the exact commit that should
become a release candidate. It first calls the complete quality gate and only
then publishes API, Web, Worker and Migrator images to GitHub Container
Registry with tags in this form:

```text
ghcr.io/<owner>/servicepilot-api:sha-<git-commit>
ghcr.io/<owner>/servicepilot-web:sha-<git-commit>
ghcr.io/<owner>/servicepilot-worker:sha-<git-commit>
ghcr.io/<owner>/servicepilot-migrator:sha-<git-commit>
```

These tags identify source commits. Because registry tags are technically
mutable, staging and production deployment must resolve and pin the published
image digest for all four services. A digest identifies the exact bytes that
passed the gate; a later rebuild cannot silently change an active deployment.

## PostgreSQL recovery and release rollback

While local staging is running, take a custom-format PostgreSQL backup and
prove that it can be restored into an isolated verification database:

```powershell
.\scripts\verify-recovery-local.ps1
```

Rehearse a local manifest-driven release and return to the previous application
images with:

```powershell
.\scripts\deploy-staging-local.ps1 `
  -ReleaseManifest .\deploy\staging\release-manifest.local.example.json `
  -AllowMutableLocalImages `
  -SkipPull

.\scripts\rollback-staging-local.ps1
```

Remote staging and production manifests must pin every image with
`@sha256:<digest>`. Application rollback never restores PostgreSQL or runs a
down migration. See
`docs/operations/postgresql-recovery-and-release-rollback.md` for the recovery
model, production backup requirements and incident procedure.
