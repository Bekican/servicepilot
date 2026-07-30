# ADR 0009: Enforce Appointment Scheduling Rules

- Status: Accepted
- Date: 2026-07-30

## Context

Appointments coordinate customers, services and technicians across time.
Application-only availability checks cannot prevent concurrent requests from
booking the same technician.

Scheduling must also have explicit time, status and cancellation semantics.

## Considered Options

### Application Availability Check Only

This gives clear errors but cannot close the concurrency race.

### Database Constraint Only

This protects consistency but turns normal availability conflicts into
exception-driven control flow.

### Application Check and PostgreSQL Exclusion Constraint

This provides clear normal responses and a final concurrency boundary.

## Decision

Every Appointment belongs to an Organization and requires a Customer and
Service. Technician assignment is optional while Scheduled but is required
before confirmation.

The API will accept offset-aware ISO-8601 timestamps. Instants will be stored in
UTC as a half-open interval `[StartAtUtc, EndAtUtc)`, with end strictly after
start.

The same Customer may have overlapping appointments. The same active
Technician may not have overlapping Scheduled, Confirmed or InProgress
appointments.

The application will perform an availability pre-check. PostgreSQL will enforce
the final rule with an exclusion constraint. Cancelled appointments release the
slot.

Allowed transitions are:

- Scheduled to Confirmed or Cancelled
- Confirmed to InProgress or Cancelled
- InProgress to Completed
- Completed and Cancelled are terminal

## Positive Consequences

- Concurrent technician bookings are prevented
- Time semantics remain unambiguous
- Status changes are testable domain behavior
- Cancellation consistently releases availability

## Negative Consequences

- PostgreSQL-specific range and exclusion features are required
- Migration and exception translation are more complex
- Technician assignment and status updates require transaction care
- Time-zone display remains a client or organization-setting concern

## Reconsideration Triggers

This decision should be reconsidered if:

- Appointments require multiple technicians
- Flexible travel or preparation buffers are introduced
- Recurring schedules require a separate model
- Customer overlap must also be prohibited
- The primary database no longer supports exclusion constraints
