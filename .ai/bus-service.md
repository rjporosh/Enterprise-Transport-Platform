# Bus Service — Enterprise Production Requirements

## 1. Mission

Complete ONLY the Bus Service to production-grade, enterprise-ready quality.

This is NOT a demo.

The service must be reusable across:

* Bus Ticketing
* Transport Management
* Fleet Management
* Booking
* Logistics
* Travel platforms
* SaaS transport products
* ERP/HRM integrations
* Other enterprise transportation systems

Read and obey:

* `CLAUDE.md`
* `.ai/*.md`
* Existing solution architecture
* Existing shared abstractions
* Existing coding conventions

Read the existing Bus Service completely before changing anything.

Do not redesign the whole solution.

Do not modify unrelated services.

Do not ask unnecessary questions.

Make reasonable enterprise-level decisions automatically.

Stop only when a change affects another service, shared contract, shared database, security boundary, or overall architecture.

---

# 2. Responsibility Boundary

Bus Service owns the bus/fleet domain.

It is responsible for:

* Bus
* Bus type
* Fleet
* Operator/company ownership
* Vehicle identity
* Registration information
* Capacity
* Seat configuration
* Facilities/features
* Bus status
* Vehicle lifecycle
* Bus metadata
* Bus availability information relevant to the bus itself
* Bus operational audit

It must NOT own:

* Authentication
* Payment
* Notification
* Route business logic
* Ticket booking business logic
* Booking/payment transactions

Never directly access another service's database.

---

# 3. SaaS Architecture

Every applicable business entity must support SaaS isolation.

Support:

```text
Tenant
 └── Company
      └── Organization
            └── Fleet
                  └── Bus
```

Where applicable:

* TenantId
* CompanyId
* OrganizationId

must be available.

Never allow cross-tenant/company/organization access.

---

# 4. Bus Entity

Implement a production-grade Bus aggregate/entity.

Potential properties include:

* BusId
* TenantId
* CompanyId
* OrganizationId
* FleetId
* OperatorId/reference
* RegistrationNumber
* VehicleNumber
* BusCode
* Manufacturer
* Model
* ModelYear
* ChassisNumber where appropriate
* EngineNumber where appropriate
* Color
* Capacity
* SeatLayoutId/reference
* BusTypeId
* Status
* IsActive
* CreatedAt
* UpdatedAt
* CreatedBy
* UpdatedBy
* RowVersion/concurrency token

Do not copy fields blindly.

Inspect the existing domain and preserve established naming conventions.

Sensitive vehicle information must not unnecessarily appear in public APIs.

---

# 5. Bus Types

Support configurable bus types.

Examples:

* Standard
* AC
* Non-AC
* Sleeper
* Semi-Sleeper
* Double Decker
* Luxury
* Executive
* Custom

Do not hardcode these as an enum if the existing architecture requires administrator-configurable types.

Support:

* Create
* Read
* Update
* Activate/deactivate
* Delete/soft delete where appropriate
* Search
* Filtering
* Pagination

---

# 6. Fleet Management

Support fleet organization.

A fleet may contain multiple buses.

Support:

* Create fleet
* Update fleet
* Activate/deactivate fleet
* Assign bus
* Remove bus
* Fleet search
* Fleet filtering
* Fleet pagination

Prevent invalid assignment.

A bus should not accidentally belong to multiple active fleets when the domain policy permits only one.

---

# 7. Bus Status

Implement a controlled lifecycle.

Possible statuses:

```text
Available
Active
Inactive
Maintenance
OutOfService
Retired
Suspended
```

Actual states must follow the existing domain model.

Prevent invalid transitions.

Example:

```text
Active
  ↓
Maintenance
  ↓
Active
```

or:

```text
Active
  ↓
Retired
```

Retired vehicles must not be accidentally assigned to new operations.

---

# 8. Capacity

Capacity must be validated.

Support:

* Total capacity
* Passenger capacity
* Seat count
* Sleeper count where applicable
* Special capacity where applicable

Never allow negative or impossible capacity.

Seat configuration must remain consistent with declared capacity.

---

# 9. Seat Configuration

Where the existing platform requires seat management, support a reusable seat-layout model.

Examples:

```text
1 + 1
2 + 1
2 + 2
Sleeper
Custom
```

Support:

* Seat number
* Row
* Column
* Position
* Seat type
* Seat status/configuration
* Deck where applicable

Do not put booking occupancy logic into Bus Service.

The Bus Service defines the vehicle's seat structure.

Booking/Ticketing determines actual reservation/occupancy.

---

# 10. Bus Facilities

Support configurable facilities/features.

Examples:

* AC
* Wi-Fi
* USB Charging
* Toilet
* TV
* Reclining Seat
* Blanket
* Water
* Reading Light

Facilities must be extensible.

Do not hardcode business rules throughout the application.

---

# 11. Bus CRUD

Implement production-grade CRUD.

Support:

* Create
* Get by ID
* Get by code
* Update
* Soft delete
* Restore where appropriate
* Activate
* Deactivate
* Search
* Filtering
* Sorting
* Pagination

Use:

* CQRS
* MediatR where existing architecture uses it
* FluentValidation
* Domain validation
* Authorization
* Result Pattern

---

# 12. Search

Provide efficient search.

Possible search fields:

* BusCode
* RegistrationNumber
* VehicleNumber
* Manufacturer
* Model
* BusType
* Fleet
* Status

Support:

* Pagination
* Sorting
* Filtering
* Search
* Date filters where appropriate

Do not load the entire bus table into memory.

---

# 13. Optimistic Concurrency

Implement optimistic concurrency.

Protect against:

* Two administrators updating the same bus
* Simultaneous status changes
* Concurrent fleet assignment
* Concurrent seat-layout changes

Return a controlled concurrency error.

Do not silently overwrite another user's changes.

---

# 14. Soft Delete

Use soft delete where appropriate.

Do not physically delete operationally important historical buses unless the domain explicitly permits it.

Deleted buses must not appear in normal queries.

Provide administrator recovery where appropriate.

---

# 15. Audit

Audit important operations.

Include:

* TenantId
* CompanyId
* OrganizationId
* UserId
* BusId
* Action
* Previous value/state where appropriate
* New value/state
* Timestamp
* IP
* User agent
* CorrelationId
* TraceId

Audit:

* Bus creation
* Update
* Status changes
* Fleet assignment
* Fleet removal
* Seat-layout changes
* Activation
* Deactivation
* Soft delete
* Restore
* Administrative changes

---

# 16. Result Pattern

Use the centralized Result Pattern.

Example:

```json
{
  "success": false,
  "message": "The bus could not be updated.",
  "errors": [
    {
      "code": "BUS_REGISTRATION_EXISTS",
      "field": "registrationNumber",
      "message": "A bus with this registration number already exists."
    },
    {
      "code": "BUS_CAPACITY_INVALID",
      "field": "capacity",
      "message": "The configured capacity does not match the seat configuration."
    }
  ],
  "traceId": "..."
}
```

Return all relevant validation errors in one response.

Do not expose internal stack traces.

---

# 17. Centralized Error Handling

Use the platform's centralized exception-handling pipeline.

Handle:

* Validation errors
* Domain errors
* Database errors
* Concurrency errors
* Network errors
* Dependency errors
* Unexpected exceptions

Do not duplicate exception-handling logic across controllers.

Return graceful localized messages.

---

# 18. Runtime Error Logging

Write runtime errors to:

```text
logs/runtime-error-logs/
```

Daily:

```text
runtime-error-dd-MM-yyyy.txt
```

Include where available:

* Timestamp
* Service name
* Environment
* Endpoint
* HTTP method
* Background service name
* Quartz job name
* Class
* Method
* File
* Exact file location
* Line number
* Exception type
* Exact exception message
* Inner exception
* Root cause
* Possible solution
* TenantId
* CompanyId
* OrganizationId
* UserId where safe
* CorrelationId
* TraceId
* IP where appropriate

Never log secrets.

---

# 19. Build Error Logging

Write actual build errors to:

```text
logs/build-error-logs/
```

Daily:

```text
build-error-dd-MM-yyyy.txt
```

Include:

* Timestamp
* Project
* Build command
* Error code
* Exact error
* File
* Line
* Column
* Root cause
* Possible solution

Never fabricate build-error records.

---

# 20. Query Logging

Write query diagnostics to:

```text
logs/query-logs/
```

Daily:

```text
query-dd-MM-yyyy.txt
```

Where technically available include:

* Timestamp
* Service
* Endpoint
* Handler
* Repository
* Method
* File
* Line
* Database provider
* Database server
* Generated query
* Safe parameters
* Start time
* End time
* Total execution time
* Rows affected/returned
* Exception
* Root cause
* Optimization suggestion
* Possible index recommendation

Never log credentials or secrets.

---

# 21. Database Abstraction

Use the platform database abstraction.

Primary provider:

* PostgreSQL

Support configuration-driven provider selection where technically feasible:

* PostgreSQL
* SQL Server
* MySQL
* Oracle
* SQLite
* MS Access where applicable
* MongoDB where appropriate

Example:

```text
Database:Provider=PostgreSQL
```

Changing the configured provider must not require changing domain/business logic.

Database-specific implementation belongs in Infrastructure.

MongoDB must use a document-specific adapter.

Do not fake relational compatibility.

---

# 22. Database Provider Factory

Use an abstraction similar to:

```text
IDatabaseProviderFactory
```

Provider-specific implementation must remain isolated.

The Application layer must not know:

* SQL dialect
* Database driver
* EF provider
* Mongo driver
* Provider-specific connection behavior

---

# 23. Communication Architecture

Support:

* HTTP
* gRPC
* RabbitMQ
* YARP
* Ocelot

Use proper abstractions.

Do not pretend that synchronous request-response and asynchronous messaging have identical semantics.

---

# 24. Communication Provider Factory

Use a configuration-driven factory.

Conceptually:

```text
ICommunicationProviderFactory
             │
       ┌─────┼──────────┐
       │     │          │
      HTTP  gRPC     RabbitMQ
```

Example:

```text
Communication:Provider=Grpc
```

or:

```text
Communication:Provider=Http
```

or:

```text
Communication:Provider=RabbitMq
```

Changing provider configuration must not require rewriting business logic.

---

# 25. HTTP

Use typed HTTP clients.

Support:

* Timeout
* Retry
* Circuit breaker
* CorrelationId
* TraceId
* Authentication propagation
* CancellationToken
* Graceful dependency failure
* Error mapping

Do not scatter raw HTTP calls throughout business logic.

---

# 26. gRPC

Support internal gRPC communication.

Requirements:

* Proto contracts
* Versioning
* Authentication
* Authorization
* CorrelationId
* TraceId
* Deadline/timeout
* Cancellation
* Error mapping
* Health checks

Never expose persistence entities directly.

---

# 27. RabbitMQ

Use RabbitMQ for asynchronous events.

Potential events:

* BusCreated
* BusUpdated
* BusActivated
* BusDeactivated
* BusMaintenanceStarted
* BusMaintenanceCompleted
* BusRetired
* BusFleetAssigned
* BusFleetRemoved
* SeatLayoutChanged

Events must:

* Be versioned
* Be idempotently consumable
* Include correlation information
* Include trace information
* Avoid persistence-entity coupling

---

# 28. Outbox Pattern

Use an Outbox Pattern where reliable event publishing is required.

Transaction:

```text
Database Transaction
       │
       ├── Bus Change
       │
       └── Outbox Event
               ↓
       Background Publisher
               ↓
           RabbitMQ
```

Implement:

* Outbox persistence
* Retry
* Attempt count
* Published timestamp
* Error information
* Idempotency

Do not silently lose events after a successful database transaction.

---

# 29. Resilience

Use Polly/platform resilience abstractions.

Support:

* Retry
* Exponential backoff
* Jitter
* Timeout
* Circuit breaker

Do not retry:

* Validation errors
* Authorization failures
* Invalid requests

Use retries only where safe.

---

# 30. API Gateway

Support:

* YARP
* Ocelot

Gateway responsibilities:

* Routing
* Authentication integration
* Authorization integration
* Correlation propagation
* Trace propagation
* Rate limiting
* Resilience
* Service discovery

Never put Bus business logic inside the gateway.

---

# 31. Correlation and Distributed Tracing

Every request must support:

* CorrelationId
* TraceId
* ParentSpanId where available

Propagate through:

* HTTP
* gRPC
* RabbitMQ
* Background workers
* Quartz
* Gateway

A request should be traceable across:

```text
Client
 ↓
Gateway
 ↓
Bus Service
 ↓
Other Service
 ↓
RabbitMQ
```

---

# 32. Idempotency

Use `Idempotency-Key` for mutation operations where duplicate requests could create an inconsistent state.

Examples:

* Bus creation
* Fleet assignment
* Status transition
* Administrative mutation

Handle:

* Duplicate request
* Concurrent request
* Same key + same request
* Same key + different request
* Expiration
* Replay

Do not blindly add idempotency to read-only APIs.

---

# 33. Rate Limiting

Protect:

* Administrative CRUD
* Status changes
* Search APIs
* Fleet operations
* Bulk operations

Support appropriate limits by:

* IP
* User
* Tenant
* Company
* Organization
* Endpoint

---

# 34. Background Jobs / Quartz

Use Quartz.NET for scheduled maintenance where appropriate.

Possible jobs:

* Expired data cleanup
* Soft-deleted record maintenance
* Fleet synchronization
* Outbox publishing
* Failed event retry
* Audit maintenance

Every job must log:

* Job name
* Trigger name
* Start time
* End time
* Duration
* CorrelationId
* TraceId
* Exception
* File
* Method
* Line
* Root cause
* Possible solution

---

# 35. Caching

Use Redis where caching provides measurable benefit.

Possible candidates:

* Bus types
* Facilities
* Seat-layout definitions
* Fleet metadata
* Frequently accessed read models

Never use cache as the authoritative source of bus state.

Invalidate/update cache correctly after mutations.

---

# 36. Health Checks

Implement:

### Liveness

Indicates whether the process is running.

### Readiness

Checks required dependencies.

Support checks for:

* Database
* RabbitMQ
* Redis where used
* Required external dependencies

Never expose credentials or sensitive connection details.

---

# 37. Observability

Use:

* Serilog
* OpenTelemetry
* Metrics
* Distributed tracing
* Health checks

Support configured observability infrastructure such as:

* Seq
* Jaeger
* Prometheus
* Grafana
* Kibana
* Graylog

Useful metrics:

* Bus CRUD latency
* Search latency
* Database latency
* Dependency latency
* Error rate
* Concurrency conflicts
* Event publishing failures
* Outbox backlog
* Cache hit/miss rate
* API request count

---

# 38. Testing

Maintain:

```text
tests/
├── unit/
├── integration/
└── load-test/
```

Unit tests must cover:

* Bus validation
* State transitions
* Capacity validation
* Seat configuration
* Fleet assignment
* Tenant isolation
* Authorization
* Idempotency
* Result Pattern
* Concurrency

Integration tests must cover:

* CRUD
* Search
* Filtering
* Pagination
* Database
* Outbox
* RabbitMQ
* HTTP
* gRPC
* Redis where used
* Health checks
* Authorization

---

# 39. Performance Testing

`tests/load-test/` is mandatory.

Create separate suites.

## NBomber

Test:

* Bus list load
* Bus search load
* Bus create/update load
* Stress scenarios

## k6

Test:

* REST API load
* Search load
* CRUD load
* Stress scenarios

## Apache JMeter

Test:

* API performance
* Search performance
* Concurrent CRUD
* Database-heavy queries

Create:

```text
docs/programmers-guide/bus-performance-testing.md
```

Document:

* Installation
* Exact commands
* Configuration
* Test data
* Result locations
* Result interpretation
* Performance thresholds

Never run destructive tests against production.

---

# 40. Developer Guide

Maintain:

```text
docs/programmers-guide/
```

Include:

* Bus architecture
* Domain model
* CRUD
* Entity creation
* CQRS
* Validation
* Repository
* Unit of Work where applicable
* Database abstraction
* Database provider factory
* Migrations
* HTTP
* gRPC
* RabbitMQ
* YARP
* Ocelot
* Communication factory
* Outbox
* Quartz
* Cron expressions
* Background workers
* Redis
* Error handling
* Runtime logs
* Build logs
* Query logs
* Audit
* SaaS tenancy
* Company/Organization
* Idempotency
* Correlation ID
* Rate limiting
* Retry
* Circuit breaker
* OpenTelemetry
* Testing
* Load testing
* Docker
* CI/CD
* Troubleshooting
* Best practices

Keep documentation concise and developer-friendly.

---

# 41. Database Commands

Create:

```text
docs/programmers-guide/bus-database.md
```

Document exact verified root-level commands for:

* Add migration
* Update database
* Remove migration
* List migrations
* Rollback/revert

Do not invent commands.

Inspect the actual solution and verify commands.

---

# 42. Docker

Provide production-ready Docker support.

Include:

* Dockerfile
* Health checks
* Environment configuration
* Secret configuration
* Non-root execution where appropriate
* No embedded credentials

Ensure compatibility with existing Docker Compose.

---

# 43. CI/CD

Support:

* Restore
* Build
* Unit tests
* Integration tests
* Static analysis
* Security/dependency checks where configured
* Docker build

CI must not depend on local developer configuration.

---

# 44. Verification

Before declaring completion, verify:

* Bus CRUD
* Bus types
* Fleet management
* Seat configuration
* Facilities
* Status lifecycle
* Pagination
* Filtering
* Search
* Soft delete
* Restore
* Optimistic concurrency
* SaaS isolation
* Company isolation
* Organization isolation
* Authorization
* Idempotency-Key
* CorrelationId
* TraceId
* IP tracing where applicable
* Rate limiting
* HTTP
* gRPC
* RabbitMQ
* YARP
* Ocelot
* Outbox
* Database abstraction
* Centralized errors
* Result Pattern
* Runtime error logs
* Build error logs
* Query logs
* Audit
* Quartz
* Redis where configured
* OpenTelemetry
* Health checks
* Docker
* Unit tests
* Integration tests
* NBomber
* k6
* JMeter

Never claim a feature or test was verified unless it actually ran.

If infrastructure prevents verification, document:

* What could not be verified
* Why
* Exact command required to verify it

---

# 45. Git Rules

After every logical milestone:

1. Inspect changed files.
2. Build affected projects.
3. Run applicable tests.
4. Update documentation.
5. Review implementation.
6. Generate a professional Git commit message.
7. Continue automatically.

Never:

* Delete `.git`
* Reinitialize Git
* Rewrite history
* Force push
* Remove existing commits
* Modify unrelated services

---

# 46. Completion Criteria

Bus Service is complete only when it provides production-grade:

* Bus management
* Fleet management
* Bus types
* Seat configuration
* Facilities
* Vehicle lifecycle
* CRUD
* Search/filter/pagination
* Soft delete
* Optimistic concurrency
* SaaS isolation
* Company/Organization context
* Authorization
* Idempotency
* HTTP
* gRPC
* RabbitMQ
* YARP
* Ocelot
* Outbox
* Database abstraction
* Centralized error handling
* Structured diagnostic logging
* Audit logging
* Rate limiting
* Retry
* Circuit breaker
* Quartz
* Redis where useful
* OpenTelemetry
* Health checks
* Unit tests
* Integration tests
* NBomber
* k6
* JMeter
* Docker
* CI/CD
* Developer documentation

No fake implementations.

No cross-service database access.

No business logic hidden in controllers.

No secrets in source code.

No unresolved TODO/FIXME/HACK/stubs unless an external infrastructure dependency genuinely prevents final integration.

In such cases implement the real abstraction and document the exact external configuration required.
