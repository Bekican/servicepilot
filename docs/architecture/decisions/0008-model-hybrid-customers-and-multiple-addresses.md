# ADR 0008: Model Hybrid Customers and Multiple Addresses

- Status: Accepted
- Date: 2026-07-30

## Context

Technical service organizations work with both individuals and companies.
Customers may have multiple service locations, and users need readable customer
numbers instead of database identifiers.

Duplicate contact information should be prevented inside a tenant during the
MVP, while inactive customers must remain available for historical records.

## Considered Options

### Separate Individual and Company Tables

This provides strict schemas but duplicates shared contact and lifecycle
behavior.

### One Unstructured Customer Record

This is flexible but cannot reliably enforce type-specific requirements.

### Typed Customer with Separate Addresses

This keeps shared lifecycle rules together while preserving individual and
company validation.

## Decision

Customer will be an organization-owned aggregate with type `Individual` or
`Company`.

Individual customers require first and last name. Company customers require
company name and may contain an optional contact person. Email and phone are
optional. Email will be trimmed and lowercased. Phone input must use E.164 and
will be normalized before storage.

Normalized email and phone will each be protected by nullable, tenant-scoped
unique indexes. This strict MVP rule may be relaxed if real organizations need
shared household or branch contact information.

Customers may have zero or more addresses, with at most one active primary
address. Customers and addresses will use soft deactivation.

Customer numbers will be allocated by an atomic tenant counter and displayed as
`CUS-000001`. Numbers are immutable and never reused.

## Positive Consequences

- Individuals and companies share one lifecycle
- Type-specific requirements remain explicit
- Multiple service locations are supported
- Contact duplicates are prevented per tenant
- Human-readable customer references are available

## Negative Consequences

- Some columns are nullable by customer type
- Hard uniqueness can reject legitimate shared contact information
- Tenant counters add concurrency-sensitive persistence logic
- Address lifecycle requires additional API operations

## Reconsideration Triggers

This decision should be reconsidered if:

- Shared household or company contact information is common
- Customers require multiple contacts
- Addresses require versioning or geospatial search
- Customer numbering rules become configurable
- Legal erasure requires anonymization workflows
