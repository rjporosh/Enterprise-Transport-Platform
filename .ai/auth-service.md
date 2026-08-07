# Auth Service — Enterprise SaaS Identity, Authentication & Authorization Requirements

## 1. Mission

Complete ONLY the Auth/Identity Service to production-grade enterprise quality.

The Auth Service must be a reusable standalone identity platform suitable for:

* HRM
* ERP
* Accounting
* HMS
* Payroll
* Booking
* Ticketing
* SaaS
* E-commerce
* Payment
* Payment Gateway
* Transport
* Other enterprise applications

Do not hardcode Transport-specific concepts into authentication.

The service must be reusable across multiple products and tenants.

Read and obey `CLAUDE.md` and all applicable `.ai/*.md` rules first.

Do not ask unnecessary questions.

Make reasonable enterprise-level decisions automatically.

Stop only if a change requires modification of another service, shared contract, shared database, or global architecture.

---

# 2. Identity and SaaS Architecture

The identity model must be SaaS-ready.

Support:

```text
Company
  └── Organization
        └── Users
              ├── Roles
              ├── Permissions
              ├── Modules
              ├── Policies
              └── Claims
```

Where applicable every authenticated request must carry:

* CompanyId
* OrganizationId
* TenantId
* UserId
* CorrelationId
* TraceId
* IP address

Do not assume a single company, organization or tenant.

Identity data must be isolated according to tenant/company/organization boundaries.

Never allow cross-tenant data access.

---

# 3. Authentication

Implement:

* Login
* Logout
* Username/email authentication
* Password authentication
* OTP authentication
* Two-factor authentication
* Access token
* Refresh token
* Token rotation
* Token revocation
* Session management
* Account activation
* Account deactivation
* Account lockout
* Failed login tracking

Use ASP.NET Core Identity where appropriate.

Use standards-based OAuth2/OpenID Connect/IdentityServer where appropriate.

Never implement a custom insecure authentication protocol.

---

# 4. Login + 2FA + OTP

Implement configurable login flows:

```text
Credentials
    ↓
Credential Validation
    ↓
2FA Required?
 ┌──┴───────┐
 No         Yes
 │           │
Token      Generate OTP
Issue          ↓
             Verify OTP
                ↓
             Token Issue
```

Support:

* OTP generation
* OTP hashing where persisted
* OTP expiration
* OTP attempt limits
* OTP resend limits
* OTP lockout
* OTP replay prevention
* OTP provider abstraction
* OTP audit trail

OTP providers must be replaceable.

Possible providers:

* SMS
* Email
* Notification Service
* Authenticator
* Future providers

Authentication logic must not depend directly on a provider.

---

# 5. Forgot Password

Implement complete password recovery.

Support:

* Forgot password request
* Secure reset token
* Token expiration
* Single-use reset token
* Reset password
* Token invalidation
* Rate limiting
* Audit logging
* OTP verification where appropriate

Do not reveal whether an account exists.

Use generic public responses.

---

# 6. Password Reset

Implement secure password reset.

Requirements:

* Password policy validation
* Password history validation
* Reset-token validation
* Reset-token expiration
* Reset-token single-use
* Reset-token replay prevention
* Session/token invalidation after password reset
* Audit logging

---

# 7. Password History — Mandatory

The previous **3 passwords MUST NOT be reusable**.

Example:

```text
Current password
Previous password #1
Previous password #2
Previous password #3
```

A newly selected password must be compared securely against all three previous password hashes.

Reject the password if it matches any of them.

Never store historical passwords as plaintext.

Store secure password hashes only.

Configuration:

```text
PasswordHistoryCount = 3
```

The value must be configurable for future enterprise policies.

---

# 8. Change Password

Implement:

* Current password verification
* New password validation
* Password history validation
* Password update
* Password-history persistence
* Session invalidation where required
* Token invalidation where required
* Audit logging

Return ALL applicable validation errors.

---

# 9. Security Questions

Implement reusable security-question support.

Support:

* Add security questions
* Update security questions
* Remove security questions where policy permits
* Multiple questions
* Secure answer hashing
* Answer normalization
* Answer verification
* Attempt limits
* Lockout
* Recovery flow
* Audit logging

Never store answers as plaintext.

Security-question answers must never appear in logs, API responses or database query logs.

Security questions must not bypass stronger security controls for sensitive operations.

---

# 10. Roles

Implement reusable role management.

Support:

* Create role
* Update role
* Delete role
* Restore role
* Assign role to user
* Remove role from user
* Assign role to organization
* Role activation/deactivation
* Role hierarchy where appropriate

Do not hardcode application-specific roles.

---

# 11. Permission Management

Implement first-class permission management.

Support:

* Permission creation
* Permission update
* Permission activation/deactivation
* Permission assignment to roles
* Permission removal from roles
* Permission checking
* Permission inheritance where appropriate

Use stable permission identifiers.

Example:

```text
users.read
users.create
users.update
users.delete

payments.read
payments.create
payments.refund

routes.read
routes.create
routes.update
```

Do not hardcode permissions throughout controllers.

---

# 12. Module Management

Implement reusable module management for SaaS products.

Support:

* Module creation
* Module update
* Module activation/deactivation
* Module assignment to company
* Module assignment to organization
* Module entitlement
* Module permissions
* Module access validation

Example:

```text
Company
 ├── HRM
 ├── Payroll
 ├── Accounting
 ├── Inventory
 └── Transport
```

A user must not receive permissions for a module that the company/organization does not own or have enabled.

---

# 13. Authorization Management

Implement centralized authorization.

Support:

* Role-based authorization
* Permission-based authorization
* Policy-based authorization
* Claims
* Module authorization
* Tenant/company authorization
* Organization authorization

Authorization decisions must consider:

```text
Tenant
Company
Organization
User
Role
Permission
Module
Policy
```

Do not rely solely on frontend authorization.

Backend authorization is mandatory.

---

# 14. IdentityServer / OpenID Connect

Use IdentityServer/OpenID Connect where present or required.

Support standards-based:

* OAuth 2.0
* OpenID Connect
* Access tokens
* Refresh tokens
* Scopes
* Claims
* Clients
* Policies
* Discovery metadata

Use appropriate flows for the application type.

Never expose client secrets.

Never implement custom token formats unnecessarily.

---

# 15. Token Management

Implement:

* Access token
* Refresh token
* Refresh token rotation
* Refresh token revocation
* Token expiration
* Session tracking
* Token reuse detection where appropriate
* Logout revocation
* Password-change invalidation
* Security-event invalidation

Do not log token values.

---

# 16. Idempotency

Sensitive mutation APIs must support idempotency where appropriate.

Support:

```text
Idempotency-Key
```

Use it for operations such as:

* OTP request
* Password reset request
* User creation
* Role assignment
* Permission assignment
* Module assignment
* Other non-safe repeatable operations

Persist idempotency state where required.

Handle:

* Duplicate requests
* Concurrent requests
* Request fingerprint
* Expiration
* Replay protection

Do not blindly make every GET endpoint idempotency-aware.

---

# 17. Correlation and Distributed Tracing

Every request must support:

```text
CorrelationId
TraceId
```

Propagate them through:

* HTTP
* gRPC
* RabbitMQ
* Background workers
* Quartz jobs

Where available also track:

```text
ParentSpanId
```

The same request must be traceable across multiple services.

---

# 18. IP Tracing

For security-sensitive operations record:

* IP address
* Forwarded IP where trusted/properly configured
* User agent
* Device/session identifier where appropriate
* Timestamp
* CorrelationId
* TraceId

Do not blindly trust spoofable forwarding headers.

Only trust configured proxy/gateway sources.

Use IP information for:

* Security audit
* Rate limiting
* Suspicious activity analysis
* Authentication diagnostics

---

# 19. Rate Limiting

Implement rate limiting.

Protect at minimum:

* Login
* OTP request
* OTP verification
* Forgot password
* Reset password
* Security-question verification
* Token endpoints
* Sensitive administrative operations

Support appropriate limits by:

* IP
* User
* Tenant
* Company
* Organization
* Endpoint

Avoid account-enumeration vulnerabilities.

---

# 20. Communication Architecture

The service must support multiple communication mechanisms through proper abstractions.

Supported mechanisms:

* HTTP
* gRPC
* RabbitMQ
* API Gateway

Do not implement one giant interface that falsely treats all communication mechanisms identically.

Use separate contracts behind a common communication/factory layer.

---

# 21. Communication Factory

Use an abstract factory/provider architecture.

Conceptually:

```text
ICommunicationProviderFactory
             │
      ┌──────┼────────┐
      │      │        │
    HTTP    gRPC   RabbitMQ
   Provider Provider Provider
```

Provider selection must be configuration-driven.

Example concept:

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

Changing the configured provider should not require business-logic changes.

Preserve protocol semantics.

HTTP/gRPC are request-response.

RabbitMQ is asynchronous event messaging.

---

# 22. API Gateway

Support API Gateway integration using:

* YARP
* Ocelot

Gateway implementation must remain separate from service business logic.

Provider/configuration should determine which gateway implementation is active where the platform requires gateway abstraction.

Support:

* Routing
* Authentication forwarding
* Authorization integration
* Correlation ID propagation
* Trace propagation
* Rate limiting
* Load balancing where applicable
* Service discovery integration
* Resilience

Do not place business logic inside the gateway.

---

# 23. HTTP

Support HTTP communication for synchronous integrations.

Requirements:

* Typed HTTP clients
* Timeout
* Retry where safe
* Circuit breaker
* Correlation ID
* Trace propagation
* Authentication propagation
* Result mapping
* Graceful dependency failure

Never scatter raw `HttpClient` calls throughout business logic.

---

# 24. gRPC

Support internal gRPC communication where appropriate.

Requirements:

* Proto contracts
* Versioning
* Authentication
* Authorization
* Correlation ID
* Trace propagation
* Timeout/deadline
* CancellationToken
* Error mapping
* Health checks

Do not expose database entities through gRPC.

---

# 25. RabbitMQ

Support asynchronous events.

Potential events:

* UserCreated
* UserActivated
* UserDeactivated
* UserLocked
* UserUnlocked
* LoginSucceeded
* LoginFailed
* OtpRequested
* OtpVerified
* PasswordChanged
* PasswordReset
* TwoFactorEnabled
* TwoFactorDisabled
* SessionRevoked
* RoleChanged
* PermissionChanged
* ModuleAssigned

Implement:

* Durable messaging where appropriate
* Retry
* Dead-letter handling
* Idempotent consumers
* Correlation ID
* Trace ID
* Event versioning

---

# 26. Retry Policy

Use Polly or the platform resilience abstraction.

Support:

* Retry
* Exponential backoff
* Jitter
* Timeout
* Circuit breaker

Do not retry:

* Invalid credentials
* Invalid OTP
* Validation failures
* Authorization failures

Do not blindly retry non-idempotent operations.

---

# 27. Circuit Breaker

Implement circuit breaker for external dependencies such as:

* Notification provider
* Email provider
* SMS provider
* Other identity dependencies

When a dependency fails:

1. Detect failure.
2. Open circuit when threshold is reached.
3. Return graceful localized response.
4. Log the dependency failure.
5. Allow recovery according to policy.

---

# 28. Centralized Error Handling

Implement ONE centralized exception-handling pipeline.

Handle:

* ValidationException
* DomainException
* AuthenticationException
* AuthorizationException
* DatabaseException
* ConcurrencyException
* TimeoutException
* NetworkException
* External dependency exceptions
* Unexpected exceptions

Do not implement separate try/catch blocks in every controller.

Return a consistent Result/Error structure.

---

# 29. Result Pattern

Every API must use the centralized Result Pattern.

Example:

```json
{
  "success": false,
  "message": "The request could not be completed.",
  "errors": [
    {
      "code": "AUTH_PASSWORD_HISTORY",
      "field": "password",
      "message": "The new password cannot match one of the previous 3 passwords."
    },
    {
      "code": "AUTH_OTP_EXPIRED",
      "field": "otp",
      "message": "The OTP has expired."
    }
  ],
  "traceId": "..."
}
```

Return all relevant errors in one response.

Do not expose sensitive implementation details.

---

# 30. Centralized Error Logging

All unhandled exceptions must flow through centralized logging.

Every exception log must attempt to include:

* Timestamp
* Service name
* Environment
* Endpoint name OR background service name OR Quartz job name
* HTTP method where applicable
* Method name
* Class name
* File name
* Exact file location
* Line number where available
* Exception type
* Exact exception message
* Inner exception
* Root cause
* Possible solution
* CorrelationId
* TraceId
* UserId where safe
* CompanyId
* OrganizationId
* TenantId
* IP address where appropriate

Never log secrets.

---

# 31. Runtime Error Logs

Runtime/dependency errors must be written to:

```text
logs/runtime-error-logs/
```

Daily file format:

```text
runtime-error-dd-MM-yyyy.txt
```

Example:

```text
logs/runtime-error-logs/runtime-error-07-08-2026.txt
```

When a dependency is unavailable, log a graceful structured diagnostic.

Example information:

```text
Timestamp:
Service:
Dependency:
Endpoint/Job:
Exception:
Exact Message:
File:
Line:
Root Cause:
Possible Solution:
CorrelationId:
TraceId:
```

The API response must remain user-friendly.

---

# 32. Build Error Logs

Build/compiler failures must be documented under:

```text
logs/build-error-logs/
```

Daily format:

```text
build-error-dd-MM-yyyy.txt
```

Record:

* Timestamp
* Project
* Build command
* Error code
* Exact compiler error
* File
* Exact file location
* Line number
* Column where available
* Root cause
* Possible solution

Do not fabricate build-error logs.

These logs are for actual encountered build failures.

---

# 33. Query Logs

Database query diagnostics must be written under:

```text
logs/query-logs/
```

Daily format:

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
* Database server/provider
* Database name where safe
* Generated query
* Parameters where safe
* Query start time
* Query end time
* Total execution time
* Rows affected/returned
* Exception if any
* Root cause
* Possible optimization
* Suggested index where appropriate

Database providers may include:

* PostgreSQL
* SQL Server
* MySQL
* Oracle
* SQLite
* MS Access
* MongoDB

Never log:

* Passwords
* Tokens
* Secrets
* Sensitive security answers
* Sensitive personal data

---

# 34. Database Abstraction

Use a provider abstraction/factory.

Conceptually:

```text
IDatabaseProviderFactory
            │
   ┌────────┼─────────┬────────┐
   │        │         │        │
Postgres  SQLServer  MySQL   Oracle
   │
 SQLite
   │
 MSAccess
   │
 MongoDB
```

Provider selection must be configuration-driven.

Example:

```text
Database:Provider=PostgreSQL
```

Changing the provider should require configuration changes rather than business-logic rewrites where the provider capabilities support the same model.

Do not create fake compatibility.

MongoDB must use an appropriate document persistence adapter rather than pretending it is relational EF Core.

---

# 35. Database Provider Isolation

Database-specific code must remain inside Infrastructure.

The following layers must NOT know database implementation details:

* Domain
* Application
* CQRS handlers
* Business rules

Only infrastructure/provider adapters should know:

* EF Core provider
* MongoDB driver
* SQL dialect
* Database-specific configuration

---

# 36. Audit

Implement centralized SaaS-aware audit logging.

Audit records must support:

* CompanyId
* OrganizationId
* TenantId
* UserId
* Action
* Module
* Permission
* Resource
* ResourceId
* Timestamp
* IP
* User Agent
* CorrelationId
* TraceId
* Result
* Failure reason where safe

Audit:

* Login
* Logout
* Password change
* Password reset
* OTP
* 2FA
* Security questions
* User creation
* User modification
* Role changes
* Permission changes
* Module changes
* Token/session revocation
* Account lock/unlock

Never audit sensitive values themselves.

---

# 37. Authorization Context

Every protected request should resolve a centralized security context containing, where available:

```text
TenantId
CompanyId
OrganizationId
UserId
Roles
Permissions
Modules
Claims
CorrelationId
TraceId
IpAddress
```

Do not repeatedly parse these values independently in every controller.

Use a centralized abstraction.

---

# 38. Background Jobs / Quartz

Use Quartz.NET for scheduled work where appropriate.

Possible jobs:

* Expired OTP cleanup
* Expired reset-token cleanup
* Session cleanup
* Token cleanup
* Password-history cleanup according to policy
* Audit maintenance
* Security-event processing

Every Quartz job must log:

* Quartz job name
* Trigger name
* Start time
* End time
* Execution duration
* Correlation/trace context where available
* Exception
* File
* Method
* Root cause
* Possible solution

---

# 39. Testing

Maintain:

```text
tests/
├── unit/
├── integration/
└── load-test/
```

Unit and integration tests must cover:

* Login
* OTP
* 2FA
* Forgot password
* Reset password
* Password history
* Security questions
* Lockout
* Tokens
* Sessions
* Roles
* Permissions
* Modules
* Policies
* Multi-tenancy
* Company isolation
* Organization isolation
* Idempotency
* Rate limiting
* Authorization
* Audit

---

# 40. Performance Tests

`tests/load-test/` is mandatory.

Create separate suites:

### NBomber

* Login load
* Login stress
* OTP load
* Token load
* Authorization load

### k6

* Login load
* OTP load
* Token load
* Password-reset load
* API stress

### Apache JMeter

* Authentication API performance
* Authorization API performance
* Token API performance

Do not combine these tools.

Create:

```text
docs/programmers-guide/auth-performance-testing.md
```

Document exact installation, commands, configuration, test data and result interpretation.

Never execute destructive performance tests against production.

---

# 41. Docker

Provide production-ready Docker support.

Include:

* Dockerfile
* Health checks
* Environment variables
* Secret configuration
* Non-root execution where appropriate
* Dependency configuration

Never embed credentials.

---

# 42. CI/CD

Support:

* Restore
* Build
* Unit tests
* Integration tests
* Security tests
* Docker build
* Static analysis
* Dependency/security checks where configured

CI must not depend on developer-local configuration.

---

# 43. Developer Documentation

Maintain:

```text
docs/programmers-guide/
```

Include:

* Architecture
* Authentication flow
* 2FA
* OTP
* Password policy
* Password history
* Security questions
* Roles
* Permissions
* Modules
* Authorization
* SaaS tenancy
* Company/Organization context
* IdentityServer
* OpenID Connect
* Token management
* Idempotency
* Correlation ID
* Rate limiting
* IP tracing
* HTTP
* gRPC
* RabbitMQ
* YARP
* Ocelot
* Communication Factory
* Database Provider Factory
* Logging
* Runtime errors
* Build errors
* Query logs
* Quartz
* Testing
* Load testing
* Docker
* CI/CD
* Troubleshooting

Documentation must contain exact commands verified against the repository.

---

# 44. Database Commands Documentation

Create:

```text
docs/programmers-guide/auth-database.md
```

Document exact root-level commands for:

* Add migration
* Update database
* Remove migration
* List migrations
* Rollback/revert procedure

Do not invent project paths or commands.

Inspect the actual solution and verify commands before documenting them.

---

# 45. Git Rules

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

# 46. Verification

Before declaring completion verify:

* Login
* OTP
* 2FA
* Forgot password
* Reset password
* Password history — previous 3 passwords rejected
* Security questions
* Roles
* Permissions
* Modules
* Policies
* Company isolation
* Organization isolation
* Tenant isolation
* Idempotency-Key
* CorrelationId
* TraceId
* IP tracing
* Rate limiting
* Retry
* Circuit breaker
* HTTP
* gRPC
* RabbitMQ
* YARP
* Ocelot
* Database provider abstraction
* Centralized exception handling
* Result Pattern
* Runtime error logging
* Build error logging
* Query logging
* Audit logging
* Quartz
* Redis where configured
* OpenTelemetry
* Docker
* Unit tests
* Integration tests
* NBomber
* k6
* JMeter

Never claim something was verified unless it actually ran.

If infrastructure is unavailable, explicitly document:

* What could not be verified
* Why
* Exact command required for verification

---

# 47. Completion Criteria

The Auth Service is complete only when it is a reusable enterprise SaaS identity platform rather than merely a login API.

It must provide:

* Authentication
* 2FA/OTP
* Password recovery
* Password history
* Security questions
* Roles
* Permissions
* Modules
* Policies
* Token management
* Session management
* SaaS tenant/company/organization context
* Audit
* Idempotency
* Rate limiting
* IP tracing
* Correlation/trace propagation
* HTTP/gRPC/RabbitMQ communication
* YARP/Ocelot gateway integration
* Communication factory abstraction
* Database provider abstraction
* Centralized errors
* Structured diagnostic logs
* Resilience
* Observability
* Testing
* Performance testing
* Docker
* CI/CD
* Developer documentation

No fake authentication.

No fake OTP.

No fake security implementation.

No plaintext passwords.

No plaintext security answers.

No plaintext historical passwords.

No hardcoded secrets.

No unresolved TODO/FIXME/HACK/stubs unless an external credential or infrastructure dependency genuinely prevents the final integration.

In such a case implement the real abstraction and document the exact external configuration required.
