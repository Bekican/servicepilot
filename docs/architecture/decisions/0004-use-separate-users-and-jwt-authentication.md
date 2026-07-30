# ADR 0004: Use Separate Users and JWT Authentication

- Status: Accepted
- Date: 2026-07-30

## Context

ServicePilot needs authenticated users, role-based authorization and reliable
organization-level data isolation.

An employee represents a person who participates in service operations. A user
represents an identity that can sign in to the system. Some employees may never
sign in, and some organization owners may need an account before an operational
employee record exists.

The first demo is an HTTP API and may later be consumed by web and mobile
clients.

## Considered Options

### Use Employee as the Authentication Identity

This minimizes the initial number of tables but couples workforce data to
authentication lifecycle and permissions.

### Use a Separate User Model with JWT Access Tokens

This keeps authentication identity separate from employee operations while
remaining small enough for the MVP.

### Use ASP.NET Core Identity

This provides a broad identity feature set, but introduces more schema and
workflow complexity than the first demo currently requires.

## Decision

ServicePilot will use a separate tenant-scoped `User` model.

Passwords will be hashed with the framework password hasher. The application
will never store or log plaintext passwords.

The API will issue signed JWT access tokens containing user identifier,
organization identifier and role claims. Login will require organization slug,
email and password because the same email may exist in different organizations.

The first demo will use fixed roles and access tokens only. Refresh tokens,
multi-organization memberships and external identity providers are deferred
until a concrete requirement justifies them.

Organization registration will create the organization and its initial Owner
account in one transaction.

## Consequences

### Positive

- Authentication and employee lifecycles remain independent
- Organization identity is explicit in every authenticated request
- Tenant-scoped email uniqueness is supported
- The API can be consumed by different client types
- The MVP avoids the full ASP.NET Core Identity schema

### Negative

- Authentication workflows must be implemented and tested explicitly
- JWT signing key management becomes an operational responsibility
- Access tokens cannot be revoked immediately in the first demo
- Fixed roles are less flexible than permission-based authorization
- The user and employee models contain some similar profile fields

## Reconsideration Triggers

This decision should be reconsidered if:

- Users need memberships in multiple organizations
- External single sign-on or social login is required
- Immediate token revocation becomes necessary
- Account recovery and multi-factor authentication are required
- Fixed roles no longer express the required permissions
