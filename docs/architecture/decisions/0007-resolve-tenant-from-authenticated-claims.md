# ADR 0007: Resolve Tenant from Authenticated Claims

- Status: Accepted
- Date: 2026-07-30

## Context

Every tenant-owned operation must use the authenticated organization boundary.
Accepting OrganizationId from request bodies or query strings would allow a
caller to attempt cross-tenant access by changing input values.

HTTP requests and background jobs have different execution contexts, so tenant
identity must have explicit sources for both.

## Considered Options

### Accept OrganizationId from Each Request

This is easy to implement but makes tenant isolation dependent on every caller
and every endpoint remembering to validate the value.

### Use Global EF Core Query Filters

This reduces repetitive filtering but can hide query behavior and is vulnerable
to accidental filter bypass.

### Use an Explicit Tenant Context

This keeps tenant ownership visible in use cases and repository contracts while
using authenticated claims as the source of truth.

## Decision

HTTP operations will obtain OrganizationId only from the validated
`organization_id` JWT claim through `ITenantContext`.

Tenant-owned request contracts will not contain OrganizationId. Repository
reads and writes will include the tenant identifier explicitly. Requests for a
resource owned by another tenant will return 404 to avoid revealing existence.
Cross-tenant writes must persist no changes.

Background jobs will carry a trusted OrganizationId in their persisted payload
and will not depend on HttpContext.

## Positive Consequences

- Callers cannot choose their tenant
- Tenant boundaries remain visible in code and tests
- Cross-tenant access can be tested at API and database levels
- Background processing has an explicit tenant source

## Negative Consequences

- Repository methods require tenant-aware signatures
- Every tenant-owned query must apply the boundary correctly
- Missing or invalid claims must fail before business logic
- Global reporting requires a separate privileged path

## Reconsideration Triggers

This decision should be reconsidered if:

- Users gain memberships in multiple organizations
- Platform-wide administration is introduced
- Database-per-tenant isolation is adopted
- A safe query-filter policy is proven and enforced by architecture tests
