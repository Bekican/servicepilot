# ADR 0002: Use PostgreSQL as the Primary Database

- Status: Accepted
- Date: 2026-07-28

## Context

ServicePilot manages relational and transactional business data, including:

- Organizations
- Users
- Customers
- Assets
- Work orders
- Technician appointments
- Inventory movements
- Maintenance contracts
- Payments

The system requires:

- Transactions
- Foreign keys
- Unique constraints
- Check constraints
- Concurrency control
- Reliable relational queries
- Indexing
- Auditability

Scheduling may also benefit from PostgreSQL range types and exclusion
constraints.

## Decision

PostgreSQL will be used as the primary source of truth.

Redis and message brokers may be added for specialized use cases, but they will
not replace PostgreSQL as the authoritative data store.

## Consequences

### Positive

- Strong transactional guarantees
- Mature relational modelling
- Rich indexing support
- Strong constraint support
- Advanced concurrency mechanisms
- Support for range types
- Open-source ecosystem

### Negative

- Relational schema changes require migrations
- Incorrect indexing can cause performance problems
- Scaling writes horizontally requires additional complexity

## Reconsideration Triggers

The decision may be reconsidered for a specific workload if:

- The data is not relational
- A specialized search workload is required
- A high-volume event stream requires separate storage
- Analytical workloads negatively affect transactional workloads