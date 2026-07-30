# ADR 0006: Use Expiring Email Invitations

- Status: Accepted
- Date: 2026-07-30

## Context

Organization Owners need to add users without sending plaintext or temporary
passwords. Invitation links must be safe to resend, expire automatically and
work in local demos without requiring a production email vendor.

## Considered Options

### Owner Sets an Initial Password

This is simple but requires insecure password sharing and additional forced
password-change state.

### Email a Temporary Password

This automates delivery but exposes a reusable credential through email.

### Email a Single-Use Password-Setup Link

This avoids password sharing and lets the invited user choose their own
password.

## Decision

Only an Owner may create or resend an invitation and select the initial role.

The invitation token will contain at least 32 random bytes. Only its SHA-256
hash will be stored. The raw token may appear only in the email link.

Invitations expire after 24 hours and can be used once. Creating or resending an
invitation invalidates any previous pending invitation for the same
organization and normalized email.

The User is created only after the invitation is accepted and the password is
hashed. Used or expired invitation records will be cleaned after 30 days while
their non-sensitive audit events remain.

Email delivery will use an Application `IEmailSender` port and an SMTP
Infrastructure adapter. Local development will use Mailpit on SMTP port 1025
with its web interface on port 8025. Production SMTP settings and the public
invitation base URL will come from secret configuration.

## Positive Consequences

- Plaintext and temporary passwords are never distributed
- Tokens are single-use and time limited
- Local email behavior is testable
- Email delivery remains vendor independent
- Resending has deterministic invalidation semantics

## Negative Consequences

- Invitation persistence and cleanup are required
- Email delivery can fail independently
- Account creation requires an additional user step
- SMTP and public-link configuration become operational responsibilities

## Reconsideration Triggers

This decision should be reconsidered if:

- External identity providers handle onboarding
- Organizations need bulk provisioning
- Enterprise customers require SCIM
- Invitation delivery requires multiple channels
- A different expiration period is required
