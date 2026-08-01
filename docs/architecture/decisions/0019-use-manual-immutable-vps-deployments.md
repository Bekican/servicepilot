# ADR 0019: Use Manual Immutable VPS Deployments

- Status: Accepted
- Date: 2026-08-01

## Context

ServicePilot has a tested local demo consisting of a Next.js BFF, a .NET API,
a .NET Worker and PostgreSQL. The next stage must make the same modular
monolith deployable without introducing Kubernetes, automatic production
releases or environment-specific builds.

The first deployment must remain affordable and understandable while creating
a safe path from local development to staging and production. Database schema
changes must not be coupled to API process startup, and a failed application
release must be recoverable without attempting an automatic database rollback.

## Considered Options

### Managed Application Platform

A managed platform reduces server administration, but hides part of the
deployment model that this project is intended to teach and may impose
platform-specific runtime and pricing constraints.

### Kubernetes

Kubernetes offers advanced scheduling and scaling, but ServicePilot currently
has one product team, one web application and a small number of processes. Its
operational cost is not justified by a measured requirement.

### Linux VPS with Docker Compose

A single Linux VPS preserves the existing container model, keeps the first
deployment inexpensive and exposes the operational boundaries explicitly. It
also allows the same immutable images to move to another container platform
later.

## Decision

ServicePilot will initially deploy to one Linux VPS with Docker Compose. The
deployable runtime consists of Caddy, Web, API, Worker and PostgreSQL. Staging
and production will use separate Compose project names, networks, volumes,
databases, users, domains and secrets even when they share the VPS.

The environments are:

- Development on the developer machine
- Staging on the VPS for production-like verification
- Production on the VPS for real customer traffic

CI checks run automatically for pull requests. Container images are built once
and identified by an immutable commit SHA or release tag. Deployments do not
happen automatically. Staging and production are started by separate manual
workflows, and production requires an explicit production selection or
approval. The exact image already verified in staging is promoted to
production without rebuilding it.

Only Caddy publishes ports 80 and 443. Caddy terminates TLS and proxies public
traffic to the Next.js Web container. Web reaches API through the private
Docker network. API, Worker, PostgreSQL and production email infrastructure do
not publish public application ports. Production does not run Mailpit.

Runtime secrets are stored in environment-specific, access-restricted files on
the VPS and are never committed or copied into images. GitHub stores only the
credentials required to access the image registry and initiate deployment.

Database migrations run as a separate one-shot migration container before the
application is updated. `Database:MigrateOnStartup` remains disabled in
staging and production. A successful migration is followed by the application
update and readiness checks. A failed readiness check restores the previous
application image, but never runs an automatic down migration.

Schema evolution follows expand-and-contract: additive and backward-compatible
changes ship before old schema is removed. Destructive cleanup happens only in
a later release after the old application version can no longer require it.

PostgreSQL may initially run on the same VPS. It must move to managed
PostgreSQL before real customer usage or when recovery, availability or scale
requirements exceed the VPS design.

## Positive Consequences

- The existing container architecture can be deployed without a platform
  rewrite.
- Staging verifies the same artefact that production receives.
- Manual deployment keeps release timing under explicit control.
- Private container networking reduces the public attack surface.
- Migration failure is isolated from normal API startup.
- Application rollback remains simple and deterministic.
- The system retains a clear path to a managed container platform later.

## Negative Consequences

- The team owns VPS patching, firewall, storage and deployment scripts.
- Staging and production can compete for the same host resources.
- A single VPS is an infrastructure single point of failure.
- Application rollback cannot reverse a destructive database migration.
- Manual releases require discipline and a documented runbook.

## Reconsideration Triggers

Revisit this decision when:

- Real customer data requires managed PostgreSQL and point-in-time recovery
- Availability requirements cannot tolerate a single VPS
- Web, API or Worker require independent horizontal scaling
- Multiple teams need independent release ownership
- Manual deployments become a release bottleneck
- Regulatory requirements require a managed secret or deployment platform

