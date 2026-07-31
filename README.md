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
