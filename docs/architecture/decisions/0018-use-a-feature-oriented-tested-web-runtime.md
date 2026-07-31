# ADR 0018: Use a feature-oriented tested web runtime

- Status: Accepted
- Date: 2026-07-31

## Context

The web application needs a maintainable repository location, explicit server
boundaries, contract checks and a production-like local demo.

## Considered Options

### Separate Frontend Repository

This isolates tooling but makes coordinated API changes and the demo workflow
slower.

### Monorepo Framework

Nx or Turborepo can coordinate many applications, but ServicePilot has one web
application and does not need that operational layer.

### Simple Application Folder

A single npm project inside the repository keeps changes atomic without adding
a monorepo framework.

## Decision

The Next.js application lives in `apps/web`, uses npm and commits its lockfile.
Code is organized by App Router routes, feature modules, shared components and
server-only infrastructure. Generated OpenAPI types are never edited manually.

Vitest and React Testing Library cover pure logic and interactive client
components. Playwright covers critical browser flows and async Server
Components against the real demo stack. Existing xUnit and Testcontainers
coverage remains authoritative for backend business behavior.

The OpenAPI generation check, formatting, linting, type checking, unit tests
and production build are phase quality gates. Critical browser flows add the
E2E gate.

The web application runs as a Node.js container in Compose on port 3000 and
calls the API through the internal service address. API URLs and session data
remain server-only; no public browser API URL is configured.

## Consequences

### Positive

- One repository captures atomic full-stack changes.
- Feature boundaries remain easy to navigate.
- Contract drift and browser regressions are detected.
- The complete demo starts through one Compose environment.

### Negative

- The repository contains both .NET and Node tooling.
- E2E tests cost more runtime than isolated tests.
- Node hosting is required because static export cannot provide the BFF.

## Reconsideration Triggers

Revisit when multiple web applications require shared packages, frontend
release ownership diverges, or deployment scale requires independent
repositories or a monorepo orchestration tool.
