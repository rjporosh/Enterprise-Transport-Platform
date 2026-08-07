# Route Service — Enterprise Production Requirements

## 1. Mission

Complete ONLY the Route Service to production-grade, enterprise-ready quality.

This is NOT a demo.

The service must be reusable across:

* Bus Ticketing
* Transport Management
* Fleet/Transit systems
* Booking
* Travel platforms
* Logistics
* Delivery systems
* SaaS transportation platforms
* ERP integrations
* Other route/network-based applications

Read and obey:

* `CLAUDE.md`
* `.ai/*.md`
* Existing solution architecture
* Existing shared abstractions
* Existing coding conventions

Read the existing Route Service completely before making changes.

Do NOT redesign the whole solution.

Do NOT modify unrelated services.

Do NOT ask unnecessary questions.

Make reasonable enterprise-level architectural decisions automatically.

Stop only if a decision affects another service, shared contract, shared database, security boundary, or overall architecture.

---

# 2. Responsibility Boundary

Route Service owns the transport route/network domain.

It is responsible for:

* Routes
* Stops
* Route stops
* Stop sequence
* Route segments
* Direction
* Route metadata
* Distance
* Estimated travel duration
* Route status
* Route versioning where appropriate
* Route lifecycle
* Route search
* Route topology

It must NOT own:

* Authentication
* Payment
* Notification
* Bus/fleet management
* Booking/ticket ownership
* Seat occupancy
* Financial transactions

Never directly access another service's database.

---

# 3. SaaS Architecture

The service must be SaaS-ready.

Conceptually:

```text id="x3n7cz"
Tenant
 └── Company
      └── Organization
            └── Routes
                  ├── Stops
                  ├── Segments
                  └── Route Versions
```

Where applicable support:

* TenantId
* CompanyId
* OrganizationId

Every protected query must enforce tenant/company/organization boundaries.

Never allow cross-tenant route access.

---

# 4. Route Aggregate

Implement a proper Route aggregate.

Potential information:

* RouteId
* TenantId
* CompanyId
* OrganizationId
* RouteCode
* RouteName
* Description
* OriginStopId
* DestinationStopId
* Direction
* Status
* TotalDistance
* EstimatedDuration
* IsActive
* CreatedAt
* UpdatedAt
* CreatedBy
* UpdatedBy
* Version/RowVersion

Do not blindly create fields.

Inspect the existing model and preserve established conventions.

---

# 5. Stops

Implement reusable Stop management.

A Stop may contain:

* StopId
* TenantId
* CompanyId
* OrganizationId
* StopCode
* Name
* Description
* Address
* Latitude
* Longitude
* City
* District/Region where applicable
* Timezone where applicable
* Status
* IsActive
* CreatedAt
* UpdatedAt

Support:

* Create
* Get
* Update
* Activate
* Deactivate
* Soft delete
* Restore where appropriate
* Search
* Pagination
* Filtering

---

# 6. Geographic Coordinates

Where coordinates are supported:

* Validate latitude range
* Validate longitude range
* Preserve precision appropriately
* Avoid floating-point misuse where domain precision requires better representation

Latitude:

```text id="w7h1ce"
-90 <= latitude <= 90
```

Longitude:

```text id="z8d2yx"
-180 <= longitude <= 180
```

Never silently accept invalid coordinates.

---

# 7. Route Stops

A route consists of ordered stops.

Example:

```text id="l5d4pk"
Route
  │
  ├── 1. Dhaka
  ├── 2. Savar
  ├── 3. Tangail
  ├── 4. Sirajganj
  └── 5. Bogura
```

Route-stop association must contain appropriate information such as:

* RouteId
* StopId
* Sequence
* DistanceFromOrigin
* EstimatedArrivalOffset
* EstimatedDepartureOffset
* PickupAllowed
* DropoffAllowed
* Status

Sequence must be unique within a route.

Never allow duplicate sequence numbers unless the domain explicitly supports them.

---

# 8. Route Segments

Represent adjacent route segments where useful.

Example:

```text id="4a8e9c"
Dhaka
  ↓
Savar
  ↓
Tangail
  ↓
Bogura
```

Segments may contain:

* SegmentId
* RouteId
* FromStopId
* ToStopId
* Sequence
* Distance
* EstimatedDuration
* Status

Validate:

* FromStop != ToStop
* Segment order
* Segment continuity
* Distance >= 0
* Duration >= 0

The last stop should not unexpectedly produce a segment to nowhere.

---

# 9. Route Topology

Validate route structure.

A valid route should generally satisfy:

```text id="8l8s0c"
Stop[0]
   ↓
Segment[0]
   ↓
Stop[1]
   ↓
Segment[1]
   ↓
Stop[2]
```

Ensure:

* No invalid gaps
* No duplicate sequence
* No disconnected segments
* No invalid references
* Correct first/last stop
* Correct segment ordering

Reject malformed route topology.

---

# 10. Direction

Support route direction where applicable.

Examples:

* Forward
* Reverse
* Up
* Down
* Inbound
* Outbound

Do not assume every transportation system uses only two directions.

Make the model extensible.

---

# 11. Route Lifecycle

Implement controlled status transitions.

Possible statuses:

```text id="q4j2d8"
Draft
Active
Inactive
Suspended
Archived
```

Actual statuses must follow the existing domain model.

Example:

```text id="1j9v5s"
Draft
  ↓
Active
  ↓
Suspended
  ↓
Active
```

Archived routes must not accidentally become active.

---

# 12. Route Versioning

Where route modifications affect historical operations, support route versioning.

A route change should not silently rewrite historical route information.

Potential model:

```text id="m4u7sw"
Route
 ├── Version 1
 ├── Version 2
 └── Version 3
```

Use versioning where required by the existing architecture.

Do not introduce unnecessary complexity if historical route immutability is already guaranteed another way.

---

# 13. Route CRUD

Implement production-grade CRUD.

Support:

* Create route
* Get route
* Get route by ID
* Get route by code
* Update route
* Activate route
* Deactivate route
* Suspend route
* Archive route
* Soft delete where appropriate
* Restore where appropriate

Use:

* CQRS
* MediatR where already used
* FluentValidation
* Domain validation
* Authorization
* Result Pattern

---

# 14. Route Creation

Route creation must validate:

* Route code uniqueness
* Route name requirements
* Origin stop
* Destination stop
* Stop existence
* Stop ordering
* Sequence uniqueness
* Segment continuity
* Geographic values
* Distance
* Duration
* Tenant ownership
* Company ownership
* Organization ownership

Do not allow a route referencing another tenant's stop.

---

# 15. Route Update

Protect route updates using optimistic concurrency.

Do not silently overwrite another administrator's changes.

If a stale version is submitted:

Return a controlled concurrency error containing:

* Error code
* User-friendly message
* TraceId

Do not expose internal database exceptions.

---

# 16. Search

Implement efficient route search.

Support searching by:

* RouteCode
* RouteName
* Origin
* Destination
* Stop
* Direction
* Status
* City/region where applicable

Support:

* Pagination
* Filtering
* Sorting
* Search

Do not load all routes into memory.

---

# 17. Stop Search

Support stop search by:

* StopCode
* Name
* City
* Region
* Coordinates where applicable
* Status

Use database-side filtering.

For coordinate-based search, use appropriate geospatial capability where available.

Do not implement expensive full-table in-memory distance calculations.

---

# 18. Nearby Stop Search

Where supported by the database/provider:

Provide a reusable nearby-stop query.

Input may include:

* Latitude
* Longitude
* Radius

Return:

* Stop
* Distance
* Relevant metadata

Do not claim geospatial support for providers that do not actually support it.

Use provider-specific adapters when necessary.

---

# 19. Pagination

All collection endpoints must support pagination.

Example:

```text id="k5y2c9"
page
pageSize
sortBy
sortDirection
```

Enforce reasonable maximum page size.

Never allow an unbounded collection request.

---

# 20. Filtering

Support structured filters.

Examples:

```text id="7q2pax"
status
direction
city
region
origin
destination
isActive
createdFrom
createdTo
```

Do not construct unsafe dynamic SQL.

Use parameterized queries and existing query abstractions.

---

# 21. Result Pattern

Use the platform Result Pattern.

Return all relevant validation errors.

Example:

```json id="r6v8z0"
{
  "success": false,
  "message": "The route could not be created.",
  "errors": [
    {
      "code": "ROUTE_DUPLICATE_SEQUENCE",
      "field": "stops[2].sequence",
      "message": "The stop sequence must be unique."
    },
    {
      "code": "ROUTE_STOP_TENANT_MISMATCH",
      "field": "stops[3].stopId",
      "message": "The selected stop does not belong to the current organization."
    }
  ],
  "traceId": "..."
}
```

Never expose stack traces.

---

# 22. Centralized Error Handling

Use the centralized exception pipeline.

Handle:

* Validation exceptions
* Domain exceptions
* Database exceptions
* Concurrency exceptions
* Geospatial/provider exceptions
* Network exceptions
* Dependency exceptions
* Unexpected exceptions

Controllers must remain thin.

---

# 23. Centralized Localization

All user-facing messages must use the centralized localization mechanism.

Minimum languages:

* English
* Bangla

Architecture must support future languages without rewriting business logic.

Do not hardcode user-facing messages throughout controllers/services.

Error codes must remain language-neutral.

Example:

```text id="n7u3k4"
ROUTE_NOT_FOUND
ROUTE_DUPLICATE_CODE
ROUTE_INVALID_SEQUENCE
ROUTE_STOP_NOT_FOUND
```

The localized message is resolved separately.

---

# 24. Runtime Error Logging

Write runtime errors to:

```text id="a2m7pc"
logs/runtime-error-logs/
```

Daily:

```text id="9z4t6f"
runtime-error-dd-MM-yyyy.txt
```

Include where available:

* Timestamp
* Service
* Environment
* Endpoint
* HTTP method
* Background service
* Quartz job
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

# 25. Build Error Logging

Write actual compiler/build errors to:

```text id="y8k2nc"
logs/build-error-logs/
```

Daily:

```text id="b9h3lq"
build-error-dd-MM-yyyy.txt
```

Record:

* Timestamp
* Project
* Command
* Error code
* Exact message
* File
* Line
* Column
* Root cause
* Possible solution

Do not fabricate build logs.

---

# 26. Query Logging

Write query diagnostics to:

```text id="e4n8sd"
logs/query-logs/
```

Daily:

```text id="w6k9qp"
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
* Query start time
* Query end time
* Total execution time
* Rows affected/returned
* Exception
* Root cause
* Possible optimization
* Index recommendation where appropriate

Never log secrets or sensitive security data.

---

# 27. Database Abstraction

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

```text id="j2z6qm"
Database:Provider=PostgreSQL
```

Changing provider configuration must not require changes to domain/business logic.

Database-specific implementation must remain in Infrastructure.

MongoDB requires a document-oriented adapter.

Do not fake relational behavior.

---

# 28. Database Provider Factory

Use a provider abstraction such as:

```text id="u3f5aa"
IDatabaseProviderFactory
```

Keep provider-specific concerns isolated.

The Application layer must not know:

* SQL dialect
* Database driver
* EF provider
* Mongo driver
* Connection implementation

---

# 29. Communication Architecture

Support:

* HTTP
* gRPC
* RabbitMQ
* YARP
* Ocelot

Use proper abstractions.

Do not treat messaging and request-response as identical.

---

# 30. Communication Provider Factory

Use configuration-driven provider selection.

Conceptually:

```text id="g5x2vr"
ICommunicationProviderFactory
             │
       ┌─────┼──────────┐
       │     │          │
      HTTP  gRPC     RabbitMQ
```

Examples:

```text id="b1v7s4"
Communication:Provider=Http
```

```text id="f6k2ca"
Communication:Provider=Grpc
```

```text id="m8n4yx"
Communication:Provider=RabbitMq
```

Changing provider must not require rewriting domain/business logic.

---

# 31. HTTP

Use typed HTTP clients.

Support:

* Timeout
* Retry
* Exponential backoff
* Jitter
* Circuit breaker
* CorrelationId
* TraceId
* Authentication propagation
* Cancellation
* Graceful dependency failure

Never scatter raw `HttpClient` calls.

---

# 32. gRPC

Support internal gRPC.

Requirements:

* Proto contracts
* Versioning
* Authentication
* Authorization
* CorrelationId
* TraceId
* Deadline
* Cancellation
* Error mapping
* Health checks

Never expose EF entities directly through gRPC.

---

# 33. RabbitMQ

Support asynchronous route events.

Potential events:

* RouteCreated
* RouteUpdated
* RouteActivated
* RouteDeactivated
* RouteSuspended
* RouteArchived
* StopCreated
* StopUpdated
* StopDeactivated
* RouteStopAdded
* RouteStopRemoved
* RouteTopologyChanged
* RouteVersionCreated

Events must:

* Be versioned
* Be idempotently consumable
* Carry CorrelationId
* Carry TraceId
* Avoid database-entity coupling

---

# 34. Outbox Pattern

Use an Outbox Pattern for reliable event publishing.

Transaction:

```text id="a7x3vk"
Database Transaction
       │
       ├── Route Change
       │
       └── Outbox Event
               ↓
       Background Publisher
               ↓
           RabbitMQ
```

Implement:

* Outbox storage
* Retry
* Attempt count
* PublishedAt
* Error information
* Idempotency
* Cleanup

Never silently lose an event after the database transaction succeeds.

---

# 35. Idempotency

Support:

```text id="c8v5ma"
Idempotency-Key
```

for appropriate mutations:

* Route creation
* Route update
* Route activation/deactivation
* Stop creation
* Route-stop modification
* Bulk route changes

Handle:

* Duplicate request
* Concurrent request
* Same key + same payload
* Same key + different payload
* Expiration
* Replay

Do not blindly add idempotency requirements to GET endpoints.

---

# 36. Optimistic Concurrency

Protect:

* Route updates
* Stop updates
* Route topology changes
* Status changes
* Version changes

Use appropriate concurrency tokens.

Never silently overwrite another administrator's changes.

---

# 37. Rate Limiting

Protect:

* CRUD
* Search
* Nearby-stop queries
* Bulk operations
* Administrative operations

Support limits by:

* IP
* User
* Tenant
* Company
* Organization
* Endpoint

---

# 38. Audit Logging

Audit:

* Route creation
* Route update
* Route activation
* Route deactivation
* Route suspension
* Route archival
* Stop creation
* Stop update
* Stop deletion
* Route-stop changes
* Topology changes
* Route version changes

Include:

* TenantId
* CompanyId
* OrganizationId
* UserId
* Action
* Resource
* ResourceId
* Previous state where appropriate
* New state where appropriate
* Timestamp
* IP
* User agent
* CorrelationId
* TraceId

---

# 39. Quartz / Background Jobs

Use Quartz.NET where scheduled processing is required.

Potential jobs:

* Outbox publishing
* Failed event retry
* Soft-deleted data maintenance
* Route consistency validation
* Route-cache refresh
* Audit maintenance
* Geographic metadata synchronization where applicable

Every job must log:

* Job name
* Trigger name
* Start
* End
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

# 40. Caching

Use Redis where beneficial.

Potential cache candidates:

* Active routes
* Route metadata
* Stop metadata
* Bus-independent route topology
* Frequently requested search/reference data

Never treat Redis as the source of truth.

Invalidate cache after mutations.

Do not cache tenant data without tenant-aware cache keys.

Example:

```text id="x5p9dz"
tenant:{tenantId}:company:{companyId}:route:{routeId}
```

---

# 41. Health Checks

Implement:

### Liveness

Process health.

### Readiness

Dependency readiness.

Check:

* Database
* RabbitMQ
* Redis where used
* Required external dependencies

Do not expose credentials.

---

# 42. Observability

Use:

* Serilog
* OpenTelemetry
* Metrics
* Distributed tracing
* Health checks

Support:

* Seq
* Jaeger
* Prometheus
* Grafana
* Kibana
* Graylog where configured

Useful metrics:

* Route CRUD latency
* Search latency
* Nearby-stop latency
* Database latency
* Error rate
* Concurrency conflicts
* Outbox backlog
* Event publishing failures
* Cache hit/miss
* Request count
* Dependency latency

---

# 43. CQRS

Use CQRS where it fits the existing architecture.

Commands:

* CreateRoute
* UpdateRoute
* ActivateRoute
* DeactivateRoute
* SuspendRoute
* ArchiveRoute
* CreateStop
* UpdateStop
* AddRouteStop
* RemoveRouteStop
* ReorderRouteStops

Queries:

* GetRoute
* GetRouteByCode
* SearchRoutes
* GetStops
* SearchStops
* GetNearbyStops
* GetRouteTopology
* GetRouteVersion

Keep commands and queries independently optimized.

---

# 44. Validation

Use FluentValidation where already established.

Validate:

* Route code
* Route name
* Stop references
* Sequence
* Coordinates
* Distance
* Duration
* Direction
* Status transition
* Tenant ownership
* Company ownership
* Organization ownership

Return all validation errors.

Do not stop at the first validation error unless the framework requires it.

---

# 45. Testing

Maintain:

```text id="j4q8pw"
tests/
├── unit/
├── integration/
└── load-test/
```

Unit tests must cover:

* Route validation
* Stop validation
* Route topology
* Sequence validation
* Segment validation
* State transitions
* Tenant isolation
* Authorization
* Idempotency
* Concurrency
* Result Pattern

Integration tests must cover:

* Route CRUD
* Stop CRUD
* Route-stop relationships
* Search
* Filtering
* Pagination
* Nearby-stop queries where supported
* Database
* Outbox
* RabbitMQ
* HTTP
* gRPC
* Redis where used
* Health checks
* Authorization

---

# 46. Performance Testing

`tests/load-test/` is mandatory.

Create separate test suites.

## NBomber

Test:

* Route listing
* Route search
* Stop search
* Route creation/update
* Nearby-stop queries
* Stress scenarios

## k6

Test:

* REST API load
* Search load
* CRUD load
* Concurrent route requests
* Stress scenarios

## Apache JMeter

Test:

* Route API performance
* Search performance
* Concurrent CRUD
* Database-heavy queries

Create:

```text id="z7w2pm"
docs/programmers-guide/route-performance-testing.md
```

Document:

* Installation
* Exact commands
* Configuration
* Test data
* Result locations
* Metrics
* Result interpretation
* Performance thresholds

Never run destructive tests against production.

---

# 47. Developer Documentation

Maintain:

```text id="f4n9ks"
docs/programmers-guide/
```

Include:

* Route architecture
* Domain model
* Route aggregate
* Stops
* Route stops
* Segments
* Topology
* Directions
* Versioning
* CRUD
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

# 48. Database Commands

Create:

```text id="y1r5va"
docs/programmers-guide/route-database.md
```

Document exact verified commands from the repository root for:

* Add migration
* Update database
* Remove migration
* List migrations
* Rollback/revert

Do not invent project paths or commands.

Inspect the actual solution and verify commands before documenting them.

---

# 49. Docker

Provide production-ready Docker support.

Include:

* Dockerfile
* Health checks
* Environment configuration
* Secret configuration
* Non-root execution where appropriate
* No embedded credentials

Ensure compatibility with the existing Docker Compose setup.

---

# 50. CI/CD

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

# 51. Verification

Before declaring completion verify:

* Route CRUD
* Stop CRUD
* Route-stop ordering
* Route segments
* Topology validation
* Direction
* Route status lifecycle
* Route versioning where implemented
* Search
* Filtering
* Sorting
* Pagination
* Nearby-stop search where implemented
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
* Bangla/English localization
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

Never claim a test passed unless it actually ran.

If infrastructure prevents verification, document:

* What could not be verified
* Why
* Exact command required to verify it

---

# 52. Git Rules

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
* Rewrite Git history
* Force push
* Remove existing commits
* Modify unrelated services

---

# 53. Completion Criteria

The Route Service is complete only when it provides production-grade:

* Route management
* Stop management
* Route-stop sequencing
* Route segments
* Topology validation
* Direction
* Route lifecycle
* Versioning where required
* CRUD
* Search/filter/pagination
* Geospatial capability where supported
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
* Bangla/English localization
* Structured diagnostic logging
* Query logging
* Build logging
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

No business logic hidden inside controllers.

No hardcoded secrets.

No unbounded queries.

No silent concurrency overwrites.

No unresolved TODO/FIXME/HACK/stubs unless an external infrastructure dependency genuinely prevents final integration.

In such cases implement the real abstraction and document the exact external configuration required.
