# ADR 0015: Use an accessible operational dashboard system

- Status: Accepted
- Date: 2026-07-31

## Context

ServicePilot needs a consistent dashboard UI that can be implemented quickly
without hiding authorization or error behavior behind visual abstractions.

## Considered Options

### Bespoke Component Library

This offers full control but spends MVP time rebuilding accessible primitives.

### Closed Component Package

This is fast initially but makes deep styling and ownership dependent on a
third-party abstraction.

### Tailwind and shadcn/ui

This provides accessible primitives whose source is owned by the application.

## Decision

The UI will use Tailwind CSS, shadcn/ui and Radix-based primitives. Components
will be separated into base UI, shared ServicePilot components and
feature-owned components.

The authenticated shell uses a desktop sidebar and a mobile drawer. Navigation
and action visibility are capability-based, while .NET policies remain
authoritative.

The MVP ships a polished light theme with tokens prepared for a later dark
theme. Every page must implement loading, empty, error and success states.
Critical mutations wait for the API response and require confirmation where
the result is difficult to reverse.

Components target WCAG 2.2 AA practices, including semantic HTML, keyboard
operation, visible focus, labelled fields, contrast and modal focus handling.

The visual direction is an operational dashboard: dark navy navigation, light
workspace, cobalt accent, restrained borders and shadows, compact status
information and no invented analytics.

## Consequences

### Positive

- Consistent and maintainable UI primitives.
- Accessible interaction patterns.
- Capability-aware navigation.
- A design that reflects real operational data.

### Negative

- shadcn source changes become application-owned maintenance.
- Dark mode is deferred.
- Critical mutations may feel less immediate than optimistic updates.

## Reconsideration Triggers

Revisit when branding requires a separate design system, audited accessibility
findings require different primitives, or a validated need for dark mode
appears.
