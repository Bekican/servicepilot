# Authentication and Tenant Context - Engineering Notes

## Problem

ServicePilot needs an initial organization Owner, secure login and a reliable
organization identifier for every authenticated request.

Employee and User are separate concepts. Employee belongs to operational
workforce management. User represents an identity that can authenticate and
receive permissions.

## Selected Design

- Separate tenant-scoped `User` model
- Organization slug, email and password login
- Framework password hashing
- Signed JWT access tokens
- Fixed MVP roles
- `organization_id` and `role` claims
- Request-scoped tenant context
- Organization and initial Owner creation in one transaction

## Request Flow

```text
POST /api/auth/register
  -> AuthenticationController
  -> RegisterOrganizationOwnerHandler
  -> validate and normalize input
  -> create Organization and Owner User
  -> hash password
  -> one SaveChanges transaction
  -> generate JWT
  -> HTTP 201

POST /api/auth/login
  -> AuthenticationController
  -> LoginHandler
  -> find Organization by slug
  -> find User by organization and normalized email
  -> verify password hash
  -> generate JWT
  -> HTTP 200

Authenticated request
  -> JWT bearer validation
  -> organization_id claim
  -> HttpTenantContext
  -> tenant-scoped application and persistence logic
```

## Why Login Includes Organization Slug

User email is unique inside an organization, not globally. The same person may
have accounts in different organizations. Organization slug selects the tenant
before the email lookup.

The database protects this rule with:

```text
UNIQUE (organization_id, email)
```

## Transaction and Concurrency

Registration adds Organization and Owner to the same EF Core change tracker and
calls `SaveChangesAsync` once. EF Core persists both in one transaction.

The application performs an early organization slug check for a clear conflict
response. The PostgreSQL unique index remains the final consistency boundary
for concurrent registrations.

If two requests use the same slug:

- One registration returns `201 Created`
- One registration returns `409 Conflict`
- PostgreSQL contains one Organization and one Owner

## Security Decisions

- Plaintext passwords are never persisted or returned
- Password hashing uses the framework `PasswordHasher`
- Invalid organization, email and password combinations share one login error
- JWT issuer, audience, signature and lifetime are validated
- JWT signing key must contain at least 32 characters
- Production signing key must be supplied through secure configuration
- Access tokens contain user, organization and role identity

## Deferred Capabilities

- Refresh tokens and token revocation
- Password reset
- Multi-factor authentication
- External identity providers
- Multi-organization memberships
- Dynamic permissions
- Rate limiting and login lockout

These capabilities should be added when the demo or production requirements
justify their complexity.

## Test Strategy

Unit tests cover:

- User invariants and normalization
- Registration success and validation
- Duplicate organization handling
- Login success and invalid credentials

PostgreSQL integration tests cover:

- Organization and Owner persistence
- Password hash storage
- Case-insensitive login input
- Unauthorized access without a token
- Tenant claims returned by an authenticated endpoint
- Same email in different organizations
- Concurrent registration consistency

## Technical Interview Summary

ServicePilot separates authentication identity from employee operations. A
registration transaction creates the tenant and its first Owner atomically.
Login first resolves the tenant by slug and then resolves the normalized email
inside that tenant. Password verification and JWT generation are behind
Application interfaces, so use cases do not depend directly on ASP.NET Core
security implementations. PostgreSQL constraints remain the final boundary for
tenant-scoped uniqueness and concurrent writes.
