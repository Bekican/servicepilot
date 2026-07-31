# ADR 0016: Scope the frontend MVP to supported workflows

- Status: Accepted
- Date: 2026-07-31

## Context

The first frontend must demonstrate the complete existing business workflow
without exposing placeholder pages or inventing unsupported backend features.

## Considered Options

### Dashboard-Only Demo

This is visually fast but does not prove the underlying workflow.

### Broad Product Mock

This appears feature-rich but introduces dead controls and fake data.

### End-to-End Supported MVP

This implements only workflows backed by real APIs and persistence.

## Decision

Public routes are login, organization registration and invitation acceptance.
Protected routes cover dashboard, customers, services, appointments, reminders
and users.

Large customer and appointment forms use dedicated pages. Small service,
invitation, role and status operations may use dialogs.

The first appointment view is a filterable seven-day agenda. Calendar and
drag-and-drop scheduling are deferred.

Before frontend feature work, the API will provide:

- current identity and capabilities,
- active technician lookup,
- appointment display fields and filters,
- pending invitation listing.

Technicians see only their assigned appointments and receive `404` for another
technician's appointment. Reminder management is visible only to users with the
retry capability.

Audit-log, inventory, revenue, customer-rating, advanced reporting and
organization-settings screens remain outside the MVP.

## Consequences

### Positive

- Every visible action has a real backend.
- The demo proves the complete workflow.
- Frontend code does not join unrelated API resources to construct appointment
  rows.

### Negative

- Some visually attractive dashboard concepts are deferred.
- Small read-contract additions are required before UI implementation.
- The first scheduling view is less visual than a calendar.

## Reconsideration Triggers

Revisit after the agenda workflow is validated, when dashboard analytics gain
historical data, or when a customer-facing portal becomes part of the product.
