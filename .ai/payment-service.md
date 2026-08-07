# Payment Service — Enterprise Production Requirements

## 1. Mission

Complete ONLY the Payment Service to production-grade, enterprise-ready quality.

This is NOT a demo implementation.

The service must be reusable across:

* Bus Ticketing
* Booking
* E-commerce
* SaaS
* ERP
* HRM
* Accounting
* Subscription systems
* Payment platforms
* Payment Gateway integrations
* Other enterprise applications

Read and obey:

* `CLAUDE.md`
* `.ai/*.md`
* Existing architecture
* Existing coding conventions
* Existing shared contracts

Do not redesign the whole solution.

Do not modify unrelated services.

Do not ask unnecessary questions.

Make reasonable enterprise-level decisions automatically.

Stop only when a decision affects another service, shared contract, shared database, security boundary, or overall architecture.

---

# 2. Responsibility Boundary

Payment Service owns payment transaction lifecycle.

It is responsible for:

* Payment intent
* Payment transaction
* Payment status
* Payment methods
* Payment confirmation
* Payment failure
* Refund
* Payment reconciliation
* Payment audit
* Payment idempotency
* Payment events

It must NOT own:

* Booking
* Bus
* Route
* Notification
* Authentication
* User identity

Reference those services through contracts.

Never access another service's database directly.

---

# 3. Payment Lifecycle

Implement a robust payment state machine.

Possible states:

```text id="8c6r4z"
Pending
    ↓
Processing
    ↓
Succeeded
    ↓
Refunded

Pending → Failed
Processing → Failed
Succeeded → PartiallyRefunded
PartiallyRefunded → Refunded
```

Prevent invalid state transitions.

Do not allow:

* Successful payment to become pending accidentally
* Duplicate payment confirmation
* Duplicate refund
* Refund greater than captured amount
* Refund of invalid transaction
* Payment mutation without authorization

State transitions must be auditable.

---

# 4. Payment Intent

Support a payment-intent model.

A payment intent should contain appropriate information such as:

* PaymentIntentId
* CompanyId
* OrganizationId
* TenantId
* Customer/User reference
* Order/Booking reference
* Amount
* Currency
* Payment method reference
* Status
* Provider reference
* Metadata
* Expiration
* CreatedAt
* UpdatedAt
* CorrelationId

Do not store sensitive card data.

---

# 5. Money Handling

Never use floating-point types for financial amounts.

Use appropriate decimal/money representation.

Always define:

* Amount
* Currency
* Currency precision
* Rounding rules

Currency must not be inferred from locale.

Never silently round financial values.

Financial calculations must be deterministic.

---

# 6. Payment Transaction

Implement:

* Create payment
* Get payment
* Get by ID
* Get by external reference
* Process payment
* Confirm payment
* Fail payment
* Cancel payment where supported
* Refund payment
* Partial refund
* Full refund
* Payment history
* Transaction search

Support:

* Pagination
* Filtering
* Sorting
* Search

---

# 7. Payment Methods

Implement extensible payment-method abstraction.

Examples:

* Card
* Bank transfer
* Mobile financial service
* Wallet
* Cash
* Other provider-specific methods

Do not hardcode provider-specific business logic into the Payment domain.

Payment providers must be replaceable.

---

# 8. Payment Provider Abstraction

Use an abstraction/factory architecture.

Conceptually:

```text id="9ozm7y"
IPaymentProviderFactory
          │
    ┌─────┼───────────┐
    │     │           │
 Provider A       Provider B
    │                 │
 Gateway A          Gateway B
```

Provider selection must be configuration-driven.

Example:

```text id="l2tr4e"
Payment:Provider=ProviderA
```

Changing the configured provider must not require rewriting payment business logic.

Never implement fake payment providers.

If a real external provider cannot be configured in the environment:

* Implement the real provider abstraction
* Implement the actual integration structure
* Document required credentials/configuration
* Do not fake successful payments

---

# 9. Payment Gateway Boundary

Keep Payment Service and Payment Gateway/provider integrations logically separated.

Payment Service owns:

* Payment state
* Payment transaction
* Business rules
* Idempotency
* Financial audit

Gateway/provider integration owns:

* Provider API
* Provider authentication
* Provider-specific request/response
* Provider-specific status mapping
* Webhooks

Do not leak provider-specific models into the domain.

---

# 10. Webhooks

Support provider webhook processing where applicable.

Requirements:

* Signature verification
* Timestamp validation
* Replay protection
* Idempotent processing
* Event validation
* Provider event mapping
* Correlation ID
* Audit logging

Never trust webhook payloads without verification.

Never process the same provider event twice.

---

# 11. Idempotency — Mandatory

Payment mutation APIs MUST support:

```text id="e4a0xq"
Idempotency-Key
```

Especially:

* Payment creation
* Payment processing
* Payment confirmation
* Refund
* Partial refund

Persist idempotency state.

Handle:

* Duplicate requests
* Concurrent requests
* Same key + same request
* Same key + different request
* Key expiration
* Replay attempts

Example:

```text id="c9u8bc"
Request A
Idempotency-Key: abc123
        ↓
Payment created

Request B
Idempotency-Key: abc123
        ↓
Return original result
```

Never create duplicate financial transactions because of request retries.

---

# 12. Concurrency

Implement optimistic concurrency.

Protect against:

* Double payment
* Double confirmation
* Double refund
* Concurrent status changes

Use appropriate concurrency tokens/versioning.

A concurrent modification must produce a controlled result rather than corrupting payment state.

---

# 13. Refunds

Support:

* Full refund
* Partial refund
* Refund reason
* Refund reference
* Refund status
* Refund provider reference
* Refund audit

Validate:

```text id="m7x6r8"
RefundAmount <= AvailableRefundableAmount
```

Prevent duplicate refund processing.

Refund operations must be idempotent.

---

# 14. Reconciliation

Implement reconciliation capability where applicable.

Support:

* Provider transaction reference
* Internal transaction reference
* Reconciliation status
* Provider status
* Internal status
* Mismatch detection
* Reconciliation audit

Potential background jobs:

* Pending payment reconciliation
* Provider status reconciliation
* Failed transaction reconciliation

Never automatically mark a financial transaction successful merely because a provider query failed.

---

# 15. Database

Use the platform database abstraction.

Primary provider:

* PostgreSQL

Support provider abstraction where technically feasible:

* PostgreSQL
* SQL Server
* MySQL
* Oracle
* SQLite
* MS Access where applicable
* MongoDB where appropriate

Provider selection must be configuration-driven.

Example:

```text id="9d2e2a"
Database:Provider=PostgreSQL
```

Business logic must not depend on database provider.

Database-specific code belongs in Infrastructure.

MongoDB must use a proper document persistence adapter rather than pretending to be relational EF Core.

---

# 16. Database Provider Factory

Use:

```text id="uq5xka"
IDatabaseProviderFactory
```

with provider-specific adapters.

Keep:

* Domain
* Application
* CQRS
* Business rules

independent from:

* SQL dialect
* Database drivers
* Provider-specific implementation

---

# 17. API

Implement production-grade REST APIs.

Include appropriate endpoints for:

* Create payment
* Get payment
* Get payment by ID
* Search payments
* Process payment
* Confirm payment
* Cancel payment
* Refund
* Partial refund
* Payment history
* Payment status
* Provider webhook
* Reconciliation

Use:

* OpenAPI Scalar
* Validation
* Authorization
* Result Pattern
* Localization
* Correlation ID
* Trace ID
* Idempotency-Key
* Rate limiting

---

# 18. Result Pattern

Use the centralized Result Pattern.

Return all applicable errors.

Example:

```json id="8a6n0d"
{
  "success": false,
  "message": "Payment could not be completed.",
  "errors": [
    {
      "code": "PAYMENT_AMOUNT_INVALID",
      "field": "amount",
      "message": "The payment amount is invalid."
    },
    {
      "code": "PAYMENT_CURRENCY_INVALID",
      "field": "currency",
      "message": "The currency is not supported."
    }
  ],
  "traceId": "..."
}
```

Never expose:

* Card information
* Provider secrets
* Internal stack traces
* Database credentials
* Sensitive financial data

---

# 19. Centralized Error Handling

Use the platform centralized exception pipeline.

Handle:

* Validation exceptions
* Domain exceptions
* Payment provider exceptions
* Database exceptions
* Concurrency exceptions
* Timeout exceptions
* Network exceptions
* Authentication failures
* Authorization failures
* Unexpected exceptions

Do not duplicate exception handling in every controller.

Return graceful localized responses.

---

# 20. Runtime Error Logging

Write centralized runtime errors to:

```text id="1o7s5d"
logs/runtime-error-logs/
```

Daily:

```text id="w9v8ma"
runtime-error-dd-MM-yyyy.txt
```

Each error should include where available:

* Timestamp
* Service
* Environment
* Endpoint
* HTTP method
* Background service/Quartz job name
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
* Payment ID where safe
* Provider reference where safe
* CompanyId
* OrganizationId
* TenantId
* UserId where safe
* CorrelationId
* TraceId
* IP where appropriate

Never log:

* Card number
* CVV
* PIN
* Password
* OTP
* Access token
* Refresh token
* Provider secrets

---

# 21. Build Error Logging

Write actual build/compiler errors to:

```text id="x3q1bj"
logs/build-error-logs/
```

Daily:

```text id="x4h7kd"
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

Do not generate fake build errors.

---

# 22. Query Logging

Write query diagnostics to:

```text id="z4w6a1"
logs/query-logs/
```

Daily:

```text id="t2z9se"
query-dd-MM-yyyy.txt
```

Where technically available record:

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
* Rows affected
* Exception
* Root cause
* Optimization suggestion
* Potential index suggestion

Never log payment secrets or sensitive payment credentials.

---

# 23. Correlation and Distributed Tracing

Every payment request must support:

* CorrelationId
* TraceId
* ParentSpanId where available

Propagate through:

* HTTP
* gRPC
* RabbitMQ
* Payment provider calls
* Background workers
* Quartz
* Webhooks

A payment must be traceable from:

```text id="r6gl3v"
Client
 ↓
Gateway
 ↓
Payment Service
 ↓
Payment Provider
 ↓
RabbitMQ
 ↓
Notification
```

---

# 24. SaaS Context

All payment operations must be tenant-aware.

Support:

* TenantId
* CompanyId
* OrganizationId
* UserId

Payment records must never cross tenant/company/organization boundaries.

Authorization must verify the current context before returning payment information.

---

# 25. Audit Logging

Audit all financial operations.

Include:

* TenantId
* CompanyId
* OrganizationId
* UserId
* PaymentId
* Operation
* Previous status
* New status
* Amount where appropriate
* Currency
* Provider
* Provider reference where safe
* Timestamp
* IP
* User agent
* CorrelationId
* TraceId
* Result

Audit:

* Payment creation
* Payment processing
* Payment confirmation
* Payment failure
* Cancellation
* Refund
* Partial refund
* Webhook processing
* Reconciliation
* Manual administrative actions

Never log payment secrets.

---

# 26. Security

Implement:

* Authentication
* Authorization
* Permission checks
* Tenant isolation
* Company isolation
* Organization isolation
* Rate limiting
* Idempotency
* Replay protection
* Webhook signature verification
* Secure secrets configuration
* Secure provider credentials

Never store raw card details unless the architecture explicitly requires compliant storage.

Prefer provider tokenization.

---

# 27. Rate Limiting

Protect:

* Payment creation
* Payment confirmation
* Refund
* Webhook
* Provider callback
* Search APIs
* Administrative operations

Use appropriate limits by:

* IP
* User
* Tenant
* Company
* Organization
* Endpoint

Do not allow rate limiting to cause duplicate financial operations.

---

# 28. Communication Architecture

Support:

* HTTP
* gRPC
* RabbitMQ
* YARP
* Ocelot

Use appropriate abstractions.

Do not make synchronous and asynchronous transports falsely interchangeable.

---

# 29. Communication Factory

Use provider/factory architecture.

Conceptually:

```text id="y9k1vw"
ICommunicationProviderFactory
             │
       ┌─────┼───────────┐
       │     │           │
      HTTP  gRPC      RabbitMQ
```

Provider configuration must be changeable without modifying business logic.

Example:

```text id="s2x4mj"
Communication:Provider=Grpc
```

or:

```text id="0xy1cm"
Communication:Provider=Http
```

or:

```text id="3d8e6b"
Communication:Provider=RabbitMq
```

---

# 30. YARP / Ocelot

Support API Gateway integration through:

* YARP
* Ocelot

Gateway responsibilities:

* Routing
* Authentication forwarding
* Authorization integration
* Correlation propagation
* Trace propagation
* Rate limiting
* Resilience
* Service discovery

Never put payment business logic inside the gateway.

---

# 31. HTTP

Use typed HTTP clients.

Support:

* Timeout
* Retry
* Circuit breaker
* Correlation ID
* Trace ID
* Authentication
* CancellationToken
* Error mapping

Do not scatter raw HTTP calls throughout the application.

---

# 32. gRPC

Support gRPC for internal low-latency operations where appropriate.

Include:

* Proto contracts
* Versioning
* Authentication
* Authorization
* Correlation
* Tracing
* Deadline
* Cancellation
* Health checks
* Error mapping

---

# 33. RabbitMQ

Use RabbitMQ for asynchronous events.

Potential events:

* PaymentCreated
* PaymentProcessing
* PaymentSucceeded
* PaymentFailed
* PaymentCancelled
* PaymentRefunded
* PaymentPartiallyRefunded
* PaymentReconciliationRequired
* PaymentReconciled

Events must be:

* Versioned
* Idempotently consumable
* Correlation-aware
* Trace-aware
* Independent of persistence entities

---

# 34. Outbox Pattern

Financial events must use a reliable event-publishing mechanism.

Implement an Outbox Pattern where appropriate.

Transaction:

```text id="l2qz8j"
Database Transaction
       │
       ├── Payment Update
       │
       └── Outbox Event
               ↓
        Background Publisher
               ↓
            RabbitMQ
```

Never publish a financial event in a way that can silently lose the event after the database transaction succeeds.

Implement:

* Outbox table/storage
* Event status
* Retry
* Published timestamp
* Error information
* Attempt count
* Idempotency

---

# 35. Resilience

Use Polly/platform resilience abstraction.

Support:

* Retry
* Exponential backoff
* Jitter
* Timeout
* Circuit breaker

Retry only operations that are safe.

For payment provider operations, carefully distinguish:

```text id="v7y3ol"
Unknown result
```

from:

```text id="z9r4st"
Definite failure
```

Never retry blindly when the provider may already have charged the customer.

---

# 36. Unknown Payment State

A provider timeout does NOT necessarily mean payment failed.

Support an appropriate state such as:

```text id="a7p5xq"
PendingVerification
```

or equivalent architecture-approved state.

When provider result is unknown:

1. Do not create duplicate payment.
2. Record the uncertainty.
3. Reconcile using provider reference.
4. Use idempotency.
5. Log the incident.
6. Notify appropriate systems when status is resolved.

---

# 37. Background Jobs / Quartz

Use Quartz.NET where scheduled processing is required.

Potential jobs:

* Pending payment reconciliation
* Provider status synchronization
* Outbox publishing
* Failed event retry
* Expired payment cleanup
* Reconciliation retry
* Audit maintenance

Every job must log:

* Job name
* Trigger
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

# 38. Caching

Use Redis only where appropriate.

Potential cache candidates:

* Provider configuration
* Supported currencies
* Non-sensitive payment metadata
* Provider capability metadata

Never cache sensitive payment secrets.

Never rely on cache as the financial source of truth.

---

# 39. Health Checks

Implement health checks for:

* Database
* RabbitMQ
* Redis where used
* Payment provider dependencies where appropriate

Separate:

* Liveness
* Readiness

Do not expose sensitive connection information through health endpoints.

---

# 40. Observability

Integrate:

* Serilog
* OpenTelemetry
* Metrics
* Distributed tracing
* Health checks

Support:

* Jaeger
* Prometheus
* Grafana
* Seq
* Kibana
* Graylog where configured

Useful metrics:

* Payment count
* Success rate
* Failure rate
* Refund rate
* Provider latency
* Payment latency
* Reconciliation failures
* Webhook failures
* Idempotency conflicts
* Circuit breaker openings
* Database latency
* Outbox backlog

---

# 41. Testing

Maintain:

```text id="l8v2so"
tests/
├── unit/
├── integration/
└── load-test/
```

Unit tests must cover:

* Payment state machine
* Money calculations
* Currency validation
* Idempotency
* Refund rules
* Concurrency
* Provider mapping
* Webhook verification
* Authorization
* Tenant isolation
* Result Pattern
* Validators

Integration tests must cover:

* Payment API
* Refund
* Webhooks
* Database
* Outbox
* RabbitMQ
* Redis where applicable
* gRPC
* Provider integration abstractions

---

# 42. Performance Testing

`tests/load-test/` is mandatory.

Create separate:

### NBomber

* Payment load test
* Payment stress test
* Refund load test
* Query performance test

### k6

* Payment API load
* Payment API stress
* Refund load
* Webhook load

### Apache JMeter

* Payment API performance
* Provider callback performance
* Query performance

Create:

```text id="8n7j2v"
docs/programmers-guide/payment-performance-testing.md
```

Document:

* Installation
* Configuration
* Exact commands
* Environment variables
* Test data
* Result locations
* Metrics
* Interpretation
* Bottleneck analysis

Never run destructive payment load tests against real financial providers.

---

# 43. Database Documentation

Create:

```text id="j6p0mx"
docs/programmers-guide/payment-database.md
```

Document exact verified commands from the solution root for:

* Add migration
* Update database
* Remove migration
* List migrations
* Rollback/revert

Do not invent commands.

---

# 44. Developer Guide

Maintain:

```text id="r4n7c3"
docs/programmers-guide/
```

Include:

* Payment architecture
* Payment lifecycle
* State machine
* Money handling
* Currency
* Payment providers
* Payment Gateway boundary
* Webhooks
* Idempotency
* Concurrency
* Refunds
* Reconciliation
* Outbox
* RabbitMQ
* HTTP
* gRPC
* YARP
* Ocelot
* Database Provider Factory
* Communication Provider Factory
* Result Pattern
* Centralized error handling
* Runtime logs
* Build logs
* Query logs
* Audit
* SaaS tenancy
* Company/Organization
* Rate limiting
* Retry
* Circuit breaker
* Quartz
* Redis
* OpenTelemetry
* Docker
* Testing
* Load testing
* Troubleshooting

---

# 45. Docker

Provide production-ready Docker support.

Include:

* Dockerfile
* Health checks
* Environment configuration
* Secret configuration
* Non-root execution where appropriate
* No embedded credentials

Ensure compatibility with the existing Docker Compose environment.

---

# 46. CI/CD

Support:

* Restore
* Build
* Unit tests
* Integration tests
* Security checks
* Docker build
* Static analysis
* Dependency checks where configured

Never depend on local developer configuration.

---

# 47. Verification

Before completion verify:

* Payment creation
* Payment processing
* Payment confirmation
* Payment failure
* Cancellation
* Full refund
* Partial refund
* Idempotency-Key
* Concurrent requests
* Unknown provider state
* Webhooks
* Webhook signature verification
* Outbox
* RabbitMQ
* HTTP
* gRPC
* YARP
* Ocelot
* Database abstraction
* Result Pattern
* Centralized exceptions
* Runtime logs
* Build logs
* Query logs
* Audit
* SaaS isolation
* Company isolation
* Organization isolation
* Rate limiting
* Retry
* Circuit breaker
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

If infrastructure is unavailable, document:

* What could not be verified
* Why
* Exact command needed for verification

---

# 48. Git Rules

After every logical milestone:

1. Inspect changed files.
2. Build affected projects.
3. Run applicable tests.
4. Update documentation.
5. Review implementation.
6. Generate a professional commit message.
7. Continue automatically.

Never:

* Delete `.git`
* Reinitialize Git
* Rewrite history
* Force push
* Remove existing commits
* Modify unrelated services

---

# 49. Completion Criteria

The Payment Service is NOT complete merely because a payment endpoint returns success.

It is complete only when it provides production-grade:

* Payment lifecycle
* Money handling
* Payment providers
* Gateway integration
* Webhooks
* Idempotency
* Concurrency protection
* Refunds
* Reconciliation
* Outbox
* Events
* HTTP
* gRPC
* RabbitMQ
* YARP
* Ocelot
* Database abstraction
* SaaS isolation
* Company/Organization context
* Authorization
* Rate limiting
* Retry
* Circuit breaker
* Centralized errors
* Structured diagnostic logs
* Audit
* Observability
* Testing
* Load/stress testing
* Docker
* CI/CD
* Developer documentation

No fake payment success.

No fake provider integration.

No plaintext financial secrets.

No duplicate financial transactions.

No silent payment-state corruption.

No unresolved TODO/FIXME/HACK/stubs unless an external credential or infrastructure dependency genuinely prevents the final integration.

In such cases implement the real abstraction and document the exact external configuration required.
