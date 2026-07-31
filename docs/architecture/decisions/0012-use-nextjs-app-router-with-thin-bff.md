# ADR 0012: Use Next.js App Router with a thin BFF

- Status: Accepted
- Date: 2026-07-31

## Context

ServicePilot needs a browser UI for authentication, tenant-scoped operations
and the MVP dashboard. The .NET modular monolith already owns business rules,
authorization and persistence. A frontend must not become a second business
backend.

## Considered Options

### React SPA Calling the API Directly

This is simple to host, but exposes bearer-token handling to browser code and
creates a separate client-side session boundary.

### Next.js Without a BFF

This provides server rendering while still allowing the browser to call the
.NET API directly. It leaves two web access paths and weakens the session
boundary.

### Next.js App Router with a Thin BFF

Next.js owns browser presentation and session adaptation. The .NET API remains
the canonical business backend.

## Decision

The web application will use Next.js App Router and TypeScript. It will run as
a Node.js process and use a thin backend-for-frontend boundary.

The BFF may:

- adapt browser cookies to API bearer authentication,
- compose presentation data,
- translate API errors into UI-safe results,
- perform server rendering and presentation cache invalidation.

The BFF will not implement domain invariants, tenant authorization, duplicate
checks, state machines or persistence.

## Consequences

### Positive

- Access tokens remain outside browser JavaScript.
- The browser has one same-origin application boundary.
- Server Components can load authenticated data without exposing secrets.
- Business logic remains centralized in .NET.

### Negative

- The web application requires a Node.js runtime.
- Some requests pass through an additional application boundary.
- Developers must keep the BFF thin.

## Reconsideration Triggers

Revisit this decision if a non-browser client becomes the primary product,
Next.js server hosting becomes operationally unsuitable, or presentation
composition grows into independent business workflows.
