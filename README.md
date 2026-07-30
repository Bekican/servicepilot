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
