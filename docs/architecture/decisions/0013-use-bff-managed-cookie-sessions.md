# ADR 0013: Use BFF-managed cookie sessions

- Status: Accepted
- Date: 2026-07-31

## Context

The .NET API issues JWT access tokens. Storing those tokens in local storage
would expose them to browser JavaScript and increase the impact of an XSS
vulnerability.

## Considered Options

### Browser Local Storage

This is easy for SPA-style API calls, but JavaScript can read the token.

### Browser-Readable Cookies

This keeps cookie transport semantics but still exposes the token to
JavaScript.

### BFF-Managed HttpOnly Cookie

The BFF stores the API access token in an HttpOnly cookie and forwards it as a
bearer token only from server-side code.

## Decision

The BFF will store the JWT in a cookie with:

- `HttpOnly`,
- `SameSite=Lax`,
- `Secure` in production,
- `Path=/`,
- expiry aligned with the JWT expiry.

The MVP will not introduce refresh tokens. Access tokens expire after sixty
minutes and the user signs in again.

Production browser traffic will not call the .NET API directly. Login and
invitation acceptance obtain a token through the BFF. Logout deletes the
cookie. A downstream `401` clears the session and redirects to login.

Mutation boundaries will validate same-origin requests. UI visibility remains
a usability feature; .NET authorization remains the security authority.

## Consequences

### Positive

- Browser JavaScript cannot read the access token.
- Authentication transport is centralized.
- Cross-origin API configuration is reduced.

### Negative

- The MVP requires re-login after token expiry.
- Cookie and origin behavior requires integration and E2E coverage.
- Native clients cannot reuse the BFF session mechanism.

## Reconsideration Triggers

Revisit when long-lived sessions, mobile clients, explicit revocation or
multiple web origins require refresh-token rotation or another session model.
