# ServicePilot Frontend MVP Implementation Plan

Each phase contains at most two main implementation jobs and closes with the
relevant format, build, test and contract checks.

## Phase 1: Backend Web Contracts

1. Add organization time-zone persistence, registration validation and
   organization-local dashboard scheduling semantics.
2. Add session capabilities, technician lookup, invitation listing and
   appointment presentation/filter/action contracts.

## Phase 2: Web Foundation

1. Scaffold `apps/web` with TypeScript, App Router, Tailwind, shadcn/ui, typed
   OpenAPI generation and server-only API access.
2. Implement BFF login, registration, invitation acceptance, logout and
   authenticated capability-aware shell.

## Phase 3: Customer and Service Workflows

1. Implement customer list, create, detail, update, deactivation and address
   lifecycle.
2. Implement service list, create, update and active-status management.

## Phase 4: User Lifecycle

1. Implement user list, role change and status change with Owner safeguards.
2. Implement pending invitations, create/resend and public acceptance flow.

## Phase 5: Appointment Operations

1. Implement seven-day agenda, filters, detail, creation, duration calculation
   and technician assignment.
2. Implement allowed state transitions, overlap feedback and
   technician-scoped behavior.

## Phase 6: Reminders and Dashboard

1. Implement reminder failure visibility and manager retry.
2. Implement the operational dashboard using real metrics, today's agenda,
   attention items and appointment status distribution.

## Phase 7: Demo Hardening

1. Add the web container, Compose wiring, seed-compatible URLs and single
   command startup.
2. Add frontend unit tests, critical Playwright flows, responsive and
   accessibility checks, documentation and final quality gates.
