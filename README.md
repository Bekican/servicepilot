# ServicePilot

ServicePilot is a multi-tenant field service management SaaS for small and
medium-sized technical service companies.

The system helps service businesses manage:

- Organizations and users
- Customers and locations
- Customer assets and equipment
- Service requests
- Work orders
- Technician scheduling
- Inventory movements
- Recurring maintenance
- Notifications
- Audit logs

## Primary Goal

The primary goal of this project is to build a production-oriented backend
while studying essential backend engineering concepts:

- HTTP and API design
- Relational data modelling
- Multi-tenancy
- Authentication and authorization
- Transactions
- Concurrency control
- Database constraints
- Caching
- Background processing
- Message queues
- Idempotency
- Observability
- Testing
- Performance
- Failure handling

## Architecture

The project starts as a modular monolith.

```text
Api
├── Application
├── Infrastructure
└── Contracts

Worker
├── Application
└── Infrastructure

Infrastructure
├── Application
└── Domain

Application
└── Domain

Domain
└── No project dependency



Technology Stack
.NET
ASP.NET Core
PostgreSQL
Entity Framework Core
Redis
RabbitMQ
Docker Compose
OpenTelemetry
Prometheus
Grafana
xUnit
Testcontainers
k6
Current Status

The project is under active development.