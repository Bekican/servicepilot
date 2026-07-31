# ADR 0017: Use organization time for scheduling

- Status: Accepted
- Date: 2026-07-31

## Context

Appointment users may open ServicePilot from devices in different time zones.
Displaying the same schedule in each device's local zone would make a shared
operational calendar ambiguous.

## Considered Options

### Browser Local Time

This is easy but can show different business times to different users.

### Per-User Time Zone

This supports distributed teams but adds preference management and more complex
schedule communication.

### Organization Time Zone

All users share one business schedule while persistence remains in UTC.

## Decision

Each Organization stores an IANA time-zone identifier. Registration suggests
the browser time zone and requires a valid value. Appointment input is
interpreted in the organization zone, sent with an explicit offset and stored
in UTC.

Selecting a Service calculates the initial appointment end from its default
duration. Users may override the end; existing appointments never change when
a Service default changes.

New appointments cannot begin in the past. Business hours, shifts and an
availability recommendation engine remain outside the MVP.

Appointment read models expose allowed status transitions and whether the
current user may assign a technician. These values drive UI actions but do not
replace backend authorization and state-machine validation.

Overlap returns `409` without clearing form data or silently selecting another
technician. Appointment filters are represented in the URL and default to the
next seven organization-local days.

## Consequences

### Positive

- All tenant users see one unambiguous schedule.
- UTC persistence remains stable.
- UI actions follow backend state and authorization.
- Service duration improves scheduling speed without coupling historical data.

### Negative

- Organization registration and persistence gain a time-zone field.
- Local-to-UTC conversion needs DST-aware tests.
- Per-user display preferences are deferred.

## Reconsideration Triggers

Revisit when organizations operate independent branches in multiple time
zones, technicians require personal display zones, or business-hour and shift
management becomes a validated requirement.
