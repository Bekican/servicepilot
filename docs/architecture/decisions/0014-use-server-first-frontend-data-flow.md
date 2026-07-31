# ADR 0014: Use a server-first frontend data flow

- Status: Accepted
- Date: 2026-07-31

## Context

The frontend needs authenticated reads, interactive forms and type-safe .NET
contracts without duplicating server state or domain validation.

## Considered Options

### Fully Client-Side Data Fetching

This provides one interaction model but delays initial data, exposes more BFF
routes and makes client caches the default.

### Server Components Only

This minimizes client JavaScript but becomes awkward for interactive tables,
polling and rich forms.

### Server-First Hybrid

Server Components load page data while Client Components are used only for
browser interaction. Server Actions perform ordinary mutations.

## Decision

Initial page reads will use Server Components and a shared `server-only` API
client. Forms and ordinary mutations will use Server Actions. Explicit Route
Handlers are reserved for authentication, polling or cases that require an HTTP
endpoint.

TanStack Query will be added only where live client-side server state,
background refetching or complex interaction justifies it.

The MVP will not use Redux or Zustand. Local UI state stays in components,
filters and pagination stay in URL search parameters, and server state stays at
the data boundary.

Forms use React Hook Form and Zod for immediate user feedback. The .NET
Application, Domain and database remain authoritative.

TypeScript API types will be generated from the .NET OpenAPI document.
`openapi-fetch` will provide a small typed client instead of a large generated
SDK.

## Consequences

### Positive

- Fast authenticated initial rendering.
- Minimal browser exposure of session details.
- URL-addressable list state.
- Compile-time detection of API contract drift.
- No unnecessary global state store.

### Negative

- Developers must understand server and client component boundaries.
- Zod provides a deliberate UX-level duplicate of some simple validation.
- OpenAPI generation becomes a build responsibility.

## Reconsideration Triggers

Revisit when a screen requires frequent polling, offline support, complex
optimistic updates or substantial cross-page client state.
