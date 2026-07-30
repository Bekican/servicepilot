# ADR 0010: Use Durable Reminders with Visible Failures

- Status: Accepted
- Date: 2026-07-30

## Context

Appointment creation must not fail because an email provider is unavailable.
Reminder work must survive process restarts, retry transient failures and make
permanent failures visible to users.

## Considered Options

### Send Reminder During Appointment Creation

This is simple but couples appointment success to an external provider.

### Fire-and-Forget In-Memory Work

This keeps the request fast but loses work on process failure.

### Persist Reminder Work and Process It in the Worker

This creates a durable boundary between the business transaction and delivery.

## Decision

Appointment creation will persist Reminder work in the same transaction as the
Appointment. Email delivery will happen later in the Worker.

Reminder states are Pending, Processing, Sent, Failed and Skipped. Missing
customer email produces Skipped without failing the Appointment.

Transient failures will retry after 1, 5 and 30 minutes. After three failed
attempts, the Reminder becomes Failed with a sanitized error and last-attempt
time visible to authorized users. Owner, Admin and Dispatcher may request a
manual retry.

An alternate delivery channel will be called a fallback only when another
configured channel is actually attempted.

## Positive Consequences

- Appointment availability is independent of email availability
- Reminder work survives restarts
- Failures are observable and recoverable
- Retry behavior is deterministic
- Future channels can use the same durable workflow

## Negative Consequences

- Reminder delivery is eventually consistent
- Worker leasing and duplicate processing must be handled
- Failure state and manual retry APIs are required
- Delivery status adds persistence and monitoring overhead

## Reconsideration Triggers

This decision should be reconsidered if:

- Delivery volume requires a message broker
- Provider webhooks become the authoritative delivery status
- Multiple fallback channels are required
- Reminder timing becomes organization configurable
- Retry volume affects transactional database performance
