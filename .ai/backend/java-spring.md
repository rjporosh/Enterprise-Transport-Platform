# Java / Spring Backend Engineering Rules

## 1. Purpose

This document defines stack-specific engineering rules for:

* Java backend applications
* Spring Framework
* Spring Boot
* Spring Web / REST
* Spring Security
* Spring Data
* JPA / Hibernate
* Spring Batch where applicable
* gRPC
* RabbitMQ
* Kafka where explicitly required
* Background processing
* Enterprise microservices

These rules complement:

```text
.ai/MASTER-RULE.md
.ai/AI_RULES.md
.ai/communication.md
.ai/observability.md
.ai/testing-and-performance.md
```

Do not duplicate those documents here.

---

# 2. JAVA / SPRING VERSION POLICY

The project must use the latest stable and supported Java/Spring ecosystem that is compatible with the application.

Do NOT permanently hardcode an old Java or Spring version into these rules.

When starting or extending a project:

```text
Latest Stable Supported Java
        +
Compatible Spring Boot
        +
Compatible Spring Framework
        +
Compatible Dependencies
        =
Preferred Stack
```

Do NOT use:

```text
EOL Java
EOL Spring Boot
Preview releases
Milestone releases
RC releases
Nightly builds
```

for production unless explicitly required.

---

# 3. VERSION DETECTION

Before implementation inspect:

```text
pom.xml
build.gradle
build.gradle.kts
gradle.properties
gradle/libs.versions.toml
mvnw
gradlew
Dockerfile
CI/CD configuration
```

Determine:

```text
Java Version
Spring Boot Version
Spring Framework Version
Build Tool
Dependency Versions
Database Driver
ORM
Testing Framework
```

The repository configuration is the source of truth for the current project.

---

# 4. VERSION UPGRADE POLICY

If the existing project already uses a supported newer version:

```text
Keep it.
```

If a project uses an older version:

1. Determine whether upgrading is within scope.
2. Check Spring Boot compatibility.
3. Check Java compatibility.
4. Check third-party dependencies.
5. Check database drivers.
6. Check security libraries.
7. Check Docker runtime images.
8. Check CI/CD.
9. Check breaking changes.
10. Build and test after upgrade.

Never upgrade the entire ecosystem merely because a newer version exists.

---

# 5. BUILD TOOL

Respect the existing build system:

```text
Maven
Gradle
Gradle Kotlin DSL
```

Do not convert Maven to Gradle or Gradle to Maven without an explicit requirement.

Use the project's wrapper when available:

```bash
./mvnw
```

or:

```bash
./gradlew
```

This ensures the project uses the expected tool version.

---

# 6. PROJECT ARCHITECTURE

Follow the existing architecture.

Where Clean Architecture / Hexagonal Architecture is established, maintain the separation:

```text
Domain
Application
Infrastructure
Interface / API
```

Typical dependency direction:

```text
API
 ↓
Application
 ↓
Domain

Infrastructure
 ↓
Application
 ↓
Domain
```

The domain must not become coupled to:

```text
Spring MVC
Spring Data
Hibernate
RabbitMQ
REST
Database
Infrastructure
```

unless explicitly justified by the existing architecture.

---

# 7. SPRING BOOT APPLICATION

Use Spring Boot's conventions.

Prefer:

```text
@SpringBootApplication
@Configuration
@Component
@Service
@Repository
@RestController
```

where appropriate.

Do not create unnecessary configuration classes.

Do not bypass Spring Dependency Injection with excessive manual object construction.

---

# 8. DEPENDENCY INJECTION

Prefer constructor injection.

Example:

```java
@Service
public class NotificationService {

    private final NotificationRepository repository;

    public NotificationService(NotificationRepository repository) {
        this.repository = repository;
    }
}
```

Avoid field injection:

```java
@Autowired
private NotificationRepository repository;
```

unless an existing project convention explicitly requires it.

Constructor injection improves:

```text
Testability
Immutability
Dependency visibility
Maintainability
```

---

# 9. CONTROLLER RULES

Controllers must remain thin.

Preferred flow:

```text
Controller
    ↓
Application Service / Use Case
    ↓
Domain
    ↓
Repository / Infrastructure
```

Do not put:

```text
Business Logic
Database Queries
Messaging Logic
Complex Validation
External API Logic
```

inside controllers.

---

# 10. REST API

Use REST semantics consistently.

Support where applicable:

```text
GET
POST
PUT
PATCH
DELETE
```

Use meaningful HTTP status codes.

Examples:

```text
200 OK
201 Created
202 Accepted
204 No Content
400 Bad Request
401 Unauthorized
403 Forbidden
404 Not Found
409 Conflict
422 Unprocessable Entity
429 Too Many Requests
500 Internal Server Error
503 Service Unavailable
```

Do not return `200 OK` for every outcome.

---

# 11. DTOs

Do not expose JPA entities directly through public APIs.

Use DTOs:

```text
Request DTO
Response DTO
Command DTO
Query DTO
```

Benefits:

```text
API Stability
Security
Validation
Versioning
Decoupling
```

---

# 12. VALIDATION

Use Jakarta Bean Validation where appropriate:

```text
@NotNull
@NotBlank
@Size
@Email
@Pattern
@Min
@Max
```

For example:

```java
public record CreateUserRequest(
    @NotBlank String name,
    @Email String email
) {}
```

Validation must exist on the backend even when the frontend validates the same fields.

---

# 13. BUSINESS VALIDATION

Separate structural validation from business rules.

Example:

```text
@NotBlank
→ DTO validation

"Account cannot be closed while invoices are unpaid"
→ Application/domain business rule
```

Do not place complex business rules inside controller annotations.

---

# 14. CQRS

Use CQRS where required by the architecture.

Typical structure:

```text
application/
├── command/
├── query/
├── handler/
├── dto/
└── validator/
```

Do not introduce CQRS into every trivial CRUD operation merely for ceremony.

Use it when separate read/write models or complex workflows justify it.

---

# 15. DOMAIN MODEL

Domain entities should represent meaningful business behavior.

Avoid turning the domain into a collection of getters/setters with all business logic in services.

At the same time, do not force infrastructure concerns into domain objects.

Domain should remain independent from:

```text
Spring
Hibernate
REST
RabbitMQ
Database
```

where practical.

---

# 16. JPA / HIBERNATE

When JPA/Hibernate is used:

Use appropriate:

```text
@Entity
@Table
@Id
@Version
@OneToMany
@ManyToOne
@OneToOne
@ManyToMany
```

relationships.

Avoid unnecessary bidirectional relationships.

Avoid blindly using:

```java
FetchType.EAGER
```

Prefer explicit loading strategies.

---

# 17. JPA N+1 PREVENTION

Always consider N+1 query problems.

Use appropriate:

```text
JOIN FETCH
@EntityGraph
Projection
Batch Fetching
Explicit Queries
```

depending on the use case.

Do not solve N+1 by blindly making every relationship eager.

---

# 18. TRANSACTIONS

Use Spring transactions appropriately:

```java
@Transactional
```

Transactions should generally be placed at the application/service boundary.

Avoid unnecessarily long transactions.

Do not perform slow external HTTP calls inside a database transaction unless there is a deliberate reason.

---

# 19. OPTIMISTIC CONCURRENCY

For entities where concurrent modification is possible, use optimistic locking.

Typical JPA mechanism:

```java
@Version
private Long version;
```

Handle optimistic locking failures gracefully.

Never silently overwrite another user's changes.

---

# 20. DATABASE MIGRATIONS

Use the project's established migration framework.

Possible:

```text
Flyway
Liquibase
```

Do not introduce a second migration system.

Migration scripts must be:

```text
Versioned
Repeatable where appropriate
Reviewable
Tested
Backward-aware
```

Never manually modify production database structures outside the migration strategy unless explicitly required.

---

# 21. REPOSITORIES

If Spring Data is used:

```java
public interface UserRepository
        extends JpaRepository<User, Long> {
}
```

Keep repositories focused on persistence.

Do not put business workflows inside repositories.

---

# 22. DATABASE QUERIES

Queries must be:

```text
Parameterized
Bounded
Indexed where appropriate
Efficient
Observable
```

Avoid:

```text
N+1
Full table scans
Unbounded queries
Loading huge entity graphs
Repeated identical queries
```

Use projections for read-heavy operations when appropriate.

---

# 23. PAGINATION

Large collections must use pagination unless there is a documented reason not to.

Spring Data supports:

```text
Pageable
Page<T>
Slice<T>
```

Use a maximum page size.

Never allow a public endpoint to return unlimited records.

---

# 24. FILTERING AND SEARCH

Search/filter functionality must be:

```text
Validated
Parameterized
Bounded
Indexed where appropriate
```

Never concatenate untrusted input into SQL.

---

# 25. RAW SQL

Native SQL is allowed when technically justified.

It must be:

```text
Parameterized
Reviewed
Tested
Observable
Secure
```

Never construct native SQL using raw user input.

---

# 26. SOFT DELETE

If required by the project, implement a consistent soft-delete strategy.

Typical fields:

```text
deleted
deletedAt
deletedBy
```

Ensure normal queries exclude deleted records.

Administrative recovery/access must require appropriate authorization.

---

# 27. RESULT PATTERN

Use the centralized result/error model defined by:

```text
.ai/MASTER-RULE.md
.ai/AI_RULES.md
```

Responses should support multiple errors:

```json
{
  "success": false,
  "message": "Validation failed.",
  "errors": [
    {
      "code": "REQUIRED",
      "field": "email",
      "message": "Email is required."
    },
    {
      "code": "INVALID",
      "field": "phone",
      "message": "Phone number is invalid."
    }
  ],
  "traceId": "..."
}
```

Do not expose Java stack traces to clients.

---

# 28. GLOBAL EXCEPTION HANDLING

Use centralized Spring exception handling.

Prefer:

```text
@RestControllerAdvice
@ExceptionHandler
ProblemDetail
```

where compatible with the project's Spring version.

Do not duplicate exception handling across every controller.

---

# 29. ERROR MAPPING

Map technical exceptions to safe application errors.

Examples:

```text
DataIntegrityViolationException
→ DATA_CONFLICT

OptimisticLockingFailureException
→ CONCURRENCY_CONFLICT

TimeoutException
→ DEPENDENCY_TIMEOUT

ConnectException
→ DEPENDENCY_UNAVAILABLE

MethodArgumentNotValidException
→ VALIDATION_ERROR
```

Never expose internal database details to clients.

---

# 30. LOCALIZATION

Support centralized localization.

Minimum:

```text
English
Bangla
```

Use Spring's localization facilities where appropriate:

```text
MessageSource
messages.properties
messages_bn.properties
```

Do not hardcode user-facing messages throughout controllers/services.

The architecture must allow future languages.

---

# 31. AUTHENTICATION

Use the centralized authentication architecture.

Where applicable:

```text
Spring Security
OAuth2
OpenID Connect
JWT
Identity Provider
```

Do not create a separate authentication implementation inside each service.

---

# 32. AUTHORIZATION

Support:

```text
Roles
Permissions
Modules
Tenant
Company
Organization
Resource
```

Use Spring Security method/policy mechanisms where appropriate.

Examples:

```text
@PreAuthorize
@Secured
AuthorizationManager
```

Never rely only on frontend authorization.

---

# 33. MULTI-TENANCY

For SaaS applications, preserve:

```text
TenantId
CompanyId
OrganizationId
UserId
CorrelationId
TraceId
```

Tenant context must come from trusted authentication/infrastructure.

Never trust arbitrary tenant IDs supplied by clients.

Database isolation must be enforced at the appropriate layer.

---

# 34. AUDIT LOGGING

Critical actions should be auditable.

Record where appropriate:

```text
User
Tenant
Company
Organization
Action
Entity
EntityId
Timestamp
CorrelationId
TraceId
Result
```

Never log:

```text
Password
OTP
JWT
Refresh Token
API Key
Database Password
Secrets
Payment Credentials
```

---

# 35. HTTP CLIENTS

Use the project's configured Spring HTTP client mechanism.

Depending on Spring version/project architecture:

```text
RestClient
WebClient
Feign / OpenFeign
```

Do not introduce multiple HTTP client technologies without reason.

Centralize:

```text
Timeout
Retry
Circuit Breaker
Authentication
CorrelationId
Tracing
```

where applicable.

---

# 36. RESILIENCE

Use the centralized resilience strategy.

Where appropriate:

```text
Retry
Exponential Backoff
Jitter
Timeout
Circuit Breaker
Rate Limiting
Bulkhead
Idempotency
```

If Resilience4j is already used, follow its conventions.

Never retry every exception blindly.

Never create infinite retries.

---

# 37. gRPC

For synchronous internal communication:

Use strongly typed gRPC contracts.

Support:

```text
Deadline
Cancellation
Metadata
Authentication
CorrelationId
TraceId
Resilience
```

Keep `.proto` files versioned and backward compatible.

---

# 38. RABBITMQ

When RabbitMQ is configured:

Support as appropriate:

```text
Publisher Confirms
Acknowledgement
Retry
Dead Letter Queue
Outbox
Inbox
Idempotency
CorrelationId
TraceId
```

Never silently swallow broker failures.

---

# 39. EVENT-DRIVEN ARCHITECTURE

Events must represent meaningful domain/business events.

Examples:

```text
OrderCreated
PaymentCompleted
TicketIssued
NotificationRequested
UserRegistered
```

Avoid events such as:

```text
SomethingChanged
DataUpdated
ProcessDone
```

when a more meaningful business event exists.

---

# 40. OUTBOX PATTERN

For critical events:

```text
Database Transaction
        +
Outbox Event
        ↓
Atomic Commit
        ↓
Background Publisher
        ↓
RabbitMQ
```

This prevents business state from committing while event publication silently fails.

---

# 41. BACKGROUND PROCESSING

Use appropriate Spring mechanisms:

```text
@Scheduled
Spring TaskScheduler
Spring Batch
Quartz
Message Consumers
```

according to requirements.

Background processes must support:

```text
Cancellation where applicable
Retry
Idempotency
Concurrency control
Logging
Metrics
Failure handling
```

---

# 42. QUARTZ

If Quartz is required:

Document:

```text
Job
Trigger
Cron Expression
Concurrency
Retry
Failure Handling
Monitoring
Manual Execution
```

Prevent duplicate job execution where business correctness requires it.

---

# 43. REDIS / CACHING

When Redis is configured:

Use it for appropriate:

```text
Caching
Distributed Locks where justified
Rate Limiting
Idempotency
Short-lived state
```

Do not treat Redis as the authoritative database unless explicitly designed that way.

Cache invalidation must be considered.

---

# 44. LOGGING

Use structured logging.

Prefer the project's configured:

```text
SLF4J
Logback
Log4j2
```

Do not scatter `System.out.println()` through production code.

Never log secrets.

---

# 45. EXCEPTION LOGGING

Follow:

```text
.ai/observability.md
```

Where configured maintain:

```text
logs/build-errors/
logs/runtime-error-logs/
logs/exception-logs/
logs/query-logs/
```

Exception information should allow developers to identify:

```text
Entry Point
Endpoint
Background Job
Method
File
Location
Line
Root Cause
Exception Message
Possible Solution
Best Practice
CorrelationId
TraceId
```

without exposing secrets.

---

# 46. QUERY LOGGING

Where enabled, query diagnostics should provide:

```text
Database Provider
Service
Endpoint
Method
File
Line
Generated Query
Start Time
End Time
Execution Time
CorrelationId
TraceId
```

Sensitive parameters must be masked.

Do not log credentials.

---

# 47. OPENTELEMETRY

Where configured, instrument:

```text
Spring Web
HTTP Clients
Database
JPA/Hibernate
gRPC
RabbitMQ
Background Jobs
```

Use OpenTelemetry for:

```text
Distributed Tracing
Metrics
Context Propagation
```

Follow:

```text
.ai/observability.md
```

---

# 48. HEALTH CHECKS

Expose appropriate:

```text
Liveness
Readiness
Dependency Health
```

Do not make optional dependencies incorrectly cause liveness failure.

Health endpoints must not expose sensitive configuration.

---

# 49. API DOCUMENTATION

If OpenAPI is used:

Maintain accurate:

```text
Requests
Responses
Authentication
Authorization
Errors
Status Codes
Pagination
Filtering
```

For Spring projects using SpringDoc, follow the repository's configured version.

Do not introduce multiple OpenAPI generators unnecessarily.

---

# 50. TESTING

Use the project's established testing stack.

Typical:

```text
JUnit
Mockito
AssertJ
Spring Boot Test
Testcontainers
```

Do not introduce another test framework without reason.

---

# 51. UNIT TESTS

Unit tests should cover:

```text
Domain Rules
Application Logic
Validators
Handlers
Critical Business Rules
Error Handling
```

Tests must be deterministic.

Avoid unnecessary Spring context startup for pure unit tests.

---

# 52. INTEGRATION TESTS

Where applicable test:

```text
REST API
Database
JPA
Authentication
Authorization
RabbitMQ
External Integrations
Transactions
Migrations
```

Testcontainers may be used where appropriate.

Prefer realistic infrastructure over fragile mocks for integration tests.

---

# 53. PERFORMANCE

Investigate:

```text
Slow SQL
N+1
Large Object Graphs
Excessive Serialization
Thread Blocking
Connection Pool Exhaustion
Slow HTTP Calls
Unbounded Memory Usage
```

Use profiling and load testing when required.

Do not optimize blindly.

---

# 54. BLOCKING CODE

In reactive applications:

Do not introduce blocking calls into reactive pipelines.

If the application uses:

```text
WebFlux
Reactor
Mono
Flux
```

respect reactive programming principles.

If the application uses traditional Spring MVC:

Do not introduce reactive architecture merely because it is newer.

Follow the existing model.

---

# 55. CONNECTION POOLS

Database and HTTP connection pools must be appropriately configured.

Monitor:

```text
Pool Size
Active Connections
Idle Connections
Timeouts
Connection Leaks
```

Do not blindly increase pool sizes.

---

# 56. DOCKER

Use appropriate Java runtime images.

Prefer multi-stage builds where appropriate:

```text
Build Image
    ↓
Runtime Image
```

Never place secrets inside images.

Verify:

```text
Health
Ports
Environment
Memory
JVM Options
Graceful Shutdown
```

---

# 57. JVM CONFIGURATION

Do not blindly copy JVM flags from unrelated projects.

Tune based on:

```text
Application Type
Memory Limit
Container Limit
Traffic
GC Behavior
Performance Measurements
```

Container memory constraints must be considered.

---

# 58. CI/CD

Verify applicable:

```text
Build
Unit Tests
Integration Tests
Static Analysis
Security Scanning
Dependency Scanning
Docker Build
Migration Validation
```

Use:

```bash
./mvnw
```

or:

```bash
./gradlew
```

according to the repository.

---

# 59. SECURITY

Follow secure Spring practices:

```text
Input Validation
Authentication
Authorization
CSRF where applicable
CORS
Secure Headers
TLS
Parameterized Queries
Dependency Security
Secrets Management
Rate Limiting
```

Never disable security controls simply to make development easier.

---

# 60. DEPENDENCY MANAGEMENT

Before adding a dependency:

1. Check existing dependencies.
2. Check whether Spring already provides the capability.
3. Check maintenance status.
4. Check compatibility.
5. Check known vulnerabilities.
6. Check license implications where relevant.
7. Add only when justified.

Avoid dependency bloat.

---

# 61. CODE QUALITY

Before committing inspect:

```text
Unused Imports
Dead Code
Duplicate Logic
Incorrect Bean Scope
Circular Dependencies
Transaction Boundaries
Exception Handling
Security
Concurrency
Database Queries
Logging
Performance
```

Keep classes focused.

Avoid giant services.

Avoid giant controllers.

Avoid god objects.

---

# 62. MIGRATION COMMANDS

Document the exact commands required by the actual project.

For Maven:

```bash
./mvnw clean verify
```

For Gradle:

```bash
./gradlew clean build
```

Database migration commands must follow the project's configured Flyway/Liquibase strategy.

Never invent migration commands without checking the repository.

---

# 63. DEFINITION OF DONE

A Java/Spring feature is complete only when applicable:

```text
[ ] Implementation complete
[ ] Build passes
[ ] Unit tests pass
[ ] Integration tests pass
[ ] Validation implemented
[ ] Global error handling implemented
[ ] Localization implemented where required
[ ] Authentication verified
[ ] Authorization verified
[ ] Multi-tenancy verified
[ ] Database changes verified
[ ] Migration verified
[ ] Messaging verified
[ ] HTTP/gRPC communication verified
[ ] Logging verified
[ ] Observability verified
[ ] Security reviewed
[ ] Docker verified
[ ] CI/CD verified
[ ] Documentation updated
[ ] Git commit created
```

---

# 64. FINAL PRINCIPLE

Do not make Java/Spring resemble .NET, Python, Node, Angular, React, MAUI, or Kotlin.

Reuse the **architectural principles**, not the implementation syntax.

Use idiomatic:

```text
Java
Spring Boot
Spring Security
Spring Data
JPA/Hibernate
Jakarta APIs
```

where appropriate.

The goal is:

```text
Correct
Secure
Reliable
Observable
Performant
Maintainable
Scalable
Enterprise-grade
Production-ready
```

# END OF JAVA / SPRING BACKEND RULES
