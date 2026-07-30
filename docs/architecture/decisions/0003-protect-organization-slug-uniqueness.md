# ADR 0003: Protect Organization Slug Uniqueness at Application and Database Boundaries

- Status: Accepted
- Date: 2026-07-30

## Context

Each organization in ServicePilot must have a unique slug.

The slug may be used to identify an organization and support tenant-related
operations. Duplicate organization slugs could cause ambiguous organization
identification and incorrect tenant resolution.

Checking for an existing slug only in the application is not sufficient.
Two concurrent requests may both complete the existence check before either
request inserts its organization.

Therefore, the system must provide both a clear response for normal duplicate
requests and a final consistency guarantee for concurrent requests.

## Considered Options

### Application Check Only

The application checks whether the slug already exists before inserting the
organization.

This provides a clear application flow but cannot prevent duplicates during
concurrent requests.

### Database Unique Constraint Only

The application attempts the insert directly and relies on the database unique
constraint.

This guarantees data consistency but uses database exceptions as part of the
normal duplicate request flow.

### Application Check and Database Unique Constraint

The application first checks whether the slug exists. The database unique
constraint remains the final consistency boundary.

This provides a clear response for normal duplicate requests and protects the
data during concurrent requests.

## Decision

ServicePilot will use both an application-level duplicate check and a database
unique constraint for organization slugs.

The application check will detect normal duplicate requests before attempting
the insert.

PostgreSQL will enforce the unique constraint as the final consistency
boundary. If concurrent requests pass the application check, only one insert
will succeed.

The expected unique constraint violation will be translated into an
application-level conflict and returned as HTTP 409 Conflict.

Unexpected database errors will not be treated as duplicate slug conflicts.

## Consequences

### Positive

- Duplicate organization slugs cannot be persisted
- Concurrent requests are handled safely
- Normal duplicate requests receive a clear response
- The business rule remains visible in the application flow
- PostgreSQL remains the final authority for data consistency

### Negative

- Organization creation requires an additional existence query
- The uniqueness rule is represented in both the application and database
- Database constraint violations must be translated carefully
- Constraint-specific error handling introduces additional implementation complexity

## Reconsideration Triggers

This decision should be reconsidered if:

- The additional existence query becomes a measured performance problem
- Organization creation traffic increases significantly
- A reusable and technology-independent constraint translation mechanism is
  introduced
- Slug uniqueness rules change, such as becoming region-specific or
  organization-type-specific