# ADR 0001: Use a Modular Monolith

- Status: Accepted
- Date: 2026-07-28

## Context

ServicePilot contains multiple business capabilities such as organizations,
customers, assets, work orders, scheduling, inventory and notifications.

These capabilities require clear boundaries, but the project does not yet have
proven requirements for independent deployment or independent scaling.

Starting with multiple microservices would introduce additional operational
complexity:

- Network communication
- Distributed transactions
- Eventual consistency
- Multiple deployments
- Service discovery
- Distributed tracing
- More complex local development
- More complex testing

## Decision

ServicePilot will start as a modular monolith.

The application will be deployed as a small number of processes, while business
capabilities will remain logically separated through module boundaries.

The initial deployable processes are:

- ServicePilot.Api
- ServicePilot.Worker

## Consequences

### Positive

- Simple local development
- Easier transactions
- Easier debugging
- Lower operational cost
- Clear module boundaries
- Modules may be extracted later if needed

### Negative

- All API modules share one deployment lifecycle
- Poor boundaries could turn the system into a tightly coupled monolith
- Independent scaling is limited

## Reconsideration Triggers

This decision should be reconsidered if a module requires:

- Independent scaling
- Independent deployment
- A different availability requirement
- A different data ownership model
- A separate engineering team