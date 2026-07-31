# ADR 0011: Simplify API / Domain dependencies

- Status: Accepted
- Date: 2026-07-31

## Context

ServicePilot is designed as a modular monolith. The API process should act
mainly as an HTTP adapter, while business rules and domain invariants should
live in the Application and Domain layers.

Currently the API project references domain concepts such as user role constants
and user entities directly. That makes the API layer less isolated and can blur
layer boundaries.

## Considered Options

### Keep Role Checks in the API

This keeps the current implementation small, but makes API authorization
handlers depend on domain entities, repositories and repeated role lists.

### Move Role Constants to Contracts

This removes a direct Domain use from the API, but gives HTTP transport
contracts ownership of business authorization vocabulary.

### Use Application Capabilities

This keeps fixed roles in the Domain while exposing authorization decisions
through one Application service. The API asks whether a user has a capability
instead of loading a User entity or interpreting role names.

## Decision

API controllers and authorization handlers will not reference Domain entities,
Domain role constants or repositories directly.

The Application layer will expose one `IUserAuthorizationService`. It will
resolve the current, tenant-scoped User and map fixed Domain roles to
Application capabilities such as:

- AccessSystem
- ManageUsers
- ManageCustomers
- ManageServices
- ManageAppointments
- ViewDashboard
- RetryReminders

The API will express policies in capabilities and will not repeat role lists.
Dynamic permission tables and organization-defined roles remain outside the
MVP.

`UserRoles` remains in the Domain because supported roles are a User invariant.
Authorization capabilities remain in Application because they describe
use-case access. They will not be moved to Contracts.

The API composition root may reference Infrastructure for dependency
registration, configuration, migrations and operational health wiring.
Controllers and authorization handlers must consume Application abstractions
instead of Infrastructure implementations.

Database readiness will use ASP.NET Core health checks instead of injecting the
EF Core DbContext into an API controller.

Architecture tests will enforce that the API assembly does not directly depend
on the Domain assembly.

## Consequences

### Positive

- Stronger layer separation
- API becomes a thinner HTTP adapter
- Easier to enforce architecture rules and dependency direction
- Better testability for API layer
- Reduces accidental domain leakage into HTTP concerns
- Role-to-capability mapping has one source
- Future policy changes do not require editing endpoint role lists

### Negative

- Slight upfront effort to create shared abstractions and services
- More indirection in authorization logic
- Capabilities remain code-defined until dynamic permissions are justified

## Reconsideration Triggers

Revisit this decision if:

- the API needs to own domain-specific business rules,
- a separate integration project needs to reuse the same HTTP-based logic,
- the shared abstractions become too generic and lose meaning.
- organization-defined roles or dynamic permissions become a requirement,
- capability checks require resource-specific facts that cannot be represented
  by user identity alone.
