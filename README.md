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

Not yet implemented:

- Employee application use cases and API endpoints
- User management beyond the initial Owner account
- Permission policies beyond fixed role claims
- The remaining MVP vertical slices

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

Start PostgreSQL and Mailpit:

```powershell
docker compose up -d
```

Mailpit accepts SMTP traffic on `localhost:1025`. Its web interface is
available at `http://localhost:8025`.

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
