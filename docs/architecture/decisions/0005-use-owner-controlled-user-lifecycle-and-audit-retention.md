# ADR 0005: Use Owner-Controlled User Lifecycle and Audit Retention

- Status: Accepted
- Date: 2026-07-30

## Context

ServicePilot must preserve historical ownership of appointments, changes and
security-sensitive operations without allowing an organization to lose its
last administrator.

User records are small. Deleting inactive users for storage reasons would save
negligible space while breaking historical references.

## Considered Options

### Hard Delete Users

This removes personal data quickly but breaks historical relationships and
weakens auditability.

### Allow Owner and Admin to Manage Users

This distributes administration but increases policy complexity and the risk of
an Admin changing an Owner account.

### Owner-Controlled Soft Deactivation

This keeps historical identity stable and provides one clear authorization
boundary for the MVP.

## Decision

Only an active Owner may invite users, change roles, activate users or
deactivate users.

Users will not be hard deleted. Inactive users cannot authenticate. An Owner
cannot change their own role or status, and the last active Owner cannot be
demoted or deactivated.

User lifecycle operations will create immutable audit events containing
OrganizationId, ActorUserId, Action, EntityType, EntityId and OccurredAtUtc.
Audit metadata must not contain passwords, tokens or unnecessary personal data.

Audit events will be retained for five years. After that period, actor links and
metadata will be anonymized automatically while non-personal action and timing
information may remain for aggregate reporting.

## Positive Consequences

- Historical references remain valid
- Organizations cannot accidentally remove their last Owner
- Authorization rules remain simple
- Security-sensitive changes remain auditable
- Storage remains predictable without premature partitioning

## Negative Consequences

- Personal data remains in inactive user records
- Owners are a centralized administration boundary
- Retention and anonymization require background processing
- Recovery is required if every Owner loses access

## Reconsideration Triggers

This decision should be reconsidered if:

- Organizations require delegated administrators
- Legal requirements mandate a different retention period
- Data volume requires partitioning or archival storage
- Users require a formal right-to-erasure workflow
- A platform support recovery role is introduced
