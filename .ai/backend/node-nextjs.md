# Node.js + Next.js Enterprise Engineering Rules

## 1. PURPOSE

This document defines Next.js-specific implementation rules for enterprise applications and services.

It extends:

```text
.ai/backend/node.md
.ai/MASTER-RULE.md
.ai/AI_RULES.md
.ai/communication.md
.ai/observability.md
.ai/testing-and-performance.md
```

The common platform rules remain authoritative.

This document defines how those rules are implemented using:

```text
Node.js
+
TypeScript
+
Next.js
```

Do not duplicate or contradict the common rules.

---

# 2. VERSION POLICY

Always use the latest stable and supported Next.js version available at implementation time.

Do not permanently hardcode a future or obsolete version.

Before implementation inspect:

```text
package.json
package-lock.json
pnpm-lock.yaml
yarn.lock
next.config.*
tsconfig.json
```

Use the newest compatible stable:

```text
Node.js
Next.js
React
TypeScript
```

versions.

Do not downgrade merely to avoid fixing compatibility issues.

---

# 3. EXISTING PROJECT FIRST

Before making changes, inspect:

```text
app/
pages/
src/
components/
lib/
middleware.*
next.config.*
package.json
tests/
public/
Dockerfile
docker-compose.*
.env*
```

Determine whether the application uses:

```text
App Router
Pages Router
```

Do not migrate the entire application from Pages Router to App Router unless explicitly required.

---

# 4. NEXT.JS ARCHITECTURE

For new applications prefer the App Router unless the existing project has an established architecture that should be preserved.

Typical structure:

```text
src/
├── app/
│   ├── api/
│   ├── (auth)/
│   ├── (dashboard)/
│   ├── layout.tsx
│   ├── page.tsx
│   ├── loading.tsx
│   ├── error.tsx
│   └── not-found.tsx
│
├── components/
├── features/
├── lib/
├── services/
├── hooks/
├── types/
├── validators/
├── config/
└── middleware.ts
```

Adapt to the existing project.

Do not create folders merely to satisfy a theoretical architecture.

---

# 5. SERVER-FIRST ARCHITECTURE

Prefer server-side execution for:

```text
Database Access
Authentication
Authorization
Secrets
Internal Service Communication
Sensitive Business Logic
```

Use Client Components only when browser interactivity requires them.

Do not move sensitive logic into the browser.

---

# 6. SERVER COMPONENTS

Server Components should be the default when using App Router.

Use them for:

```text
Data Fetching
Server-Side Rendering
Authorization Checks
Database Operations
Internal API Calls
Sensitive Configuration
```

Do not add:

```tsx
"use client";
```

unless the component actually requires client-side behavior.

---

# 7. CLIENT COMPONENTS

Client Components are appropriate for:

```text
Stateful UI
Browser APIs
Event Handlers
Interactive Forms
Animations
Real-Time UI
Client-Side Libraries
```

Keep them as small as practical.

Avoid turning entire pages into Client Components unnecessarily.

---

# 8. SERVER ACTIONS

Use Server Actions for appropriate server-side mutations.

Server Actions must still enforce:

```text
Authentication
Authorization
Tenant Isolation
Validation
Rate Limiting where required
Idempotency where required
Audit Logging
Error Handling
```

Never assume that because a function is a Server Action it is automatically secure.

---

# 9. SERVER ACTION INPUT

Never trust input passed to a Server Action.

Validate:

```text
Form Data
Arguments
IDs
Tenant Context
Authorization Context
```

using the project's centralized validation system.

---

# 10. API ROUTES

Next.js Route Handlers may be used for HTTP APIs.

Typical structure:

```text
app/api/
├── auth/
├── users/
├── notifications/
├── payments/
└── health/
```

Keep Route Handlers thin.

Preferred flow:

```text
Route Handler
 ↓
Validation
 ↓
Authentication
 ↓
Authorization
 ↓
Application Service
 ↓
Repository / External Adapter
 ↓
Result
 ↓
HTTP Response
```

---

# 11. ROUTE HANDLERS

Do not place large business logic directly inside:

```text
route.ts
```

Avoid:

```text
Route Handler
+
Database Queries
+
Business Rules
+
RabbitMQ
+
External APIs
+
Complex Validation
```

all in one file.

Separate responsibilities.

---

# 12. RESULT PATTERN

All API responses must follow the centralized Result Pattern.

Example:

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

Multiple independent errors should be returned together where possible.

Do not create a different error structure for each Route Handler.

---

# 13. CENTRALIZED ERROR HANDLING

Use Next.js error boundaries and centralized server-side error handling appropriately.

Relevant mechanisms may include:

```text
error.tsx
global-error.tsx
not-found.tsx
Route Handler error handling
Server Action error handling
Application Error abstraction
```

Do not expose:

```text
Stack Trace
Database Errors
Connection Strings
File System Paths
Secrets
Internal Infrastructure
```

to users.

---

# 14. ERROR CLASSIFICATION

Use centralized application errors such as:

```text
ValidationError
AuthenticationError
AuthorizationError
NotFoundError
ConflictError
ConcurrencyError
DependencyUnavailableError
TimeoutError
RateLimitError
BusinessRuleError
```

Do not create unnecessary custom error classes.

---

# 15. GRACEFUL DEPENDENCY FAILURE

If:

```text
Database
Redis
RabbitMQ
gRPC Service
HTTP Service
External API
```

is unavailable, return a graceful user-facing message.

Example:

```text
The service is temporarily unavailable. Please try again later.
```

Technical details must go to centralized runtime/exception logs.

---

# 16. SERVER-SIDE ERROR LOGGING

Every unexpected server-side exception should include, where available:

```text
Entry Point
Endpoint
Route
Method
File Name
File Location
Line Number
Root Cause
Exact Exception
Possible Solution
Best Practice
CorrelationId
TraceId
Timestamp
```

Never fabricate source locations.

Use source maps where available to map runtime JavaScript back to TypeScript.

---

# 17. ERROR BOUNDARIES

Use:

```text
error.tsx
```

for route-level rendering failures.

Use:

```text
global-error.tsx
```

for application-level failures when appropriate.

Error boundaries are for UI failure handling.

They do NOT replace:

```text
Centralized API Error Handling
Structured Logging
Application Error Classification
Observability
```

---

# 18. NOT FOUND

Use:

```text
not-found.tsx
```

for user-friendly 404 experiences.

API 404 responses must still use the centralized Result Pattern.

---

# 19. LOADING STATES

Use:

```text
loading.tsx
```

for appropriate route-level loading experiences.

Loading UI should not hide actual server failures.

---

# 20. AUTHENTICATION

Authentication must occur server-side whenever possible.

Never trust:

```text
Client-side state
localStorage
React state
URL parameters
hidden form fields
```

as proof of identity.

Use the project's centralized authentication system.

---

# 21. AUTHORIZATION

Authorization must happen on the server.

Check where applicable:

```text
User
Tenant
Company
Organization
Role
Permission
Module
Resource
```

Client-side route guards are UX controls, not security controls.

---

# 22. MULTI-TENANCY

Tenant identity must come from trusted authenticated context.

Do not trust:

```text
?tenantId=
```

or a client-provided body field as the authoritative tenant.

Typical flow:

```text
Authenticated User
 ↓
Tenant Context
 ↓
Authorization
 ↓
Application Service
 ↓
Repository
```

Every tenant-aware database operation must enforce isolation.

---

# 23. REQUEST CONTEXT

Where applicable, request context should contain:

```text
UserId
TenantId
CompanyId
OrganizationId
CorrelationId
TraceId
Roles
Permissions
IP
```

Do not pass Express-style request objects into domain logic.

Next.js-specific request objects belong at the interface boundary.

---

# 24. CORRELATION ID

Every server request should participate in the platform correlation model.

Support:

```text
CorrelationId
TraceId
```

Propagate them to:

```text
HTTP
gRPC
RabbitMQ
Database instrumentation
Background Jobs
Logs
```

---

# 25. MIDDLEWARE

Use Next.js middleware for suitable request-boundary concerns such as:

```text
Authentication Routing
Security Headers
Locale Detection
Correlation
Redirects
Lightweight Request Rules
```

Do not place heavy business logic in middleware.

Avoid:

```text
Database-heavy operations
Large Business Workflows
Complex Transactions
Long-running Tasks
```

inside middleware.

---

# 26. IP ADDRESS

When determining client IP:

```text
Browser
 ↓
CDN/Proxy
 ↓
YARP/Ocelot/Ingress
 ↓
Next.js
```

ensure trusted proxy behavior is correctly configured.

Never blindly trust arbitrary:

```text
X-Forwarded-For
X-Real-IP
```

headers.

---

# 27. RATE LIMITING

Rate limiting must account for deployment topology.

Possible dimensions:

```text
IP
User
Tenant
Company
API Key
Endpoint
```

Critical mutation endpoints may require stricter limits.

Rate limiting must not rely solely on client-side code.

---

# 28. IDEMPOTENCY

Critical mutation APIs should support:

```text
Idempotency-Key
```

where required.

Particularly important for:

```text
Payments
Orders
Bookings
Notifications
External Side Effects
```

The idempotency implementation must be server-side.

---

# 29. HTTP COMMUNICATION

Next.js server-side code may communicate with internal services through:

```text
HTTP
gRPC
RabbitMQ
```

as defined by:

```text
.ai/communication.md
```

Do not embed provider-specific communication logic into UI components.

---

# 30. COMMUNICATION ABSTRACTION

Preferred:

```text
UI / Server Action / Route Handler
            ↓
Application Service
            ↓
Communication Interface
            ↓
HTTP / gRPC / RabbitMQ Adapter
```

Changing the communication provider should not require rewriting business logic.

---

# 31. YARP AND OCELOT

The frontend/application must remain compatible with enterprise gateways such as:

```text
YARP
Ocelot
```

Do not hardcode internal service URLs throughout React components.

Prefer centralized configuration:

```text
Service Client
 ↓
Gateway
 ↓
Target Service
```

---

# 32. DIRECT INTERNAL SERVICE ACCESS

Browser-side JavaScript must not directly access protected internal services unless explicitly designed and secured.

Prefer:

```text
Browser
 ↓
Next.js Server
 ↓
Gateway/Internal Service
```

This keeps:

```text
Secrets
Tokens
Internal URLs
Service Credentials
```

on the server.

---

# 33. SERVICE CLIENTS

Create typed service clients where required:

```text
services/
├── auth.client.ts
├── notification.client.ts
├── payment.client.ts
├── booking.client.ts
└── route.client.ts
```

Do not scatter raw:

```text
fetch()
```

calls across hundreds of components.

---

# 34. HTTP RETRY

Retries must be applied only where safe.

Do not blindly retry:

```text
POST
PATCH
DELETE
```

without idempotency guarantees.

Every retry must respect:

```text
Timeout
Retry Count
Backoff
Circuit Breaker
Idempotency
```

---

# 35. CIRCUIT BREAKER

Server-side service clients should support circuit breaking where required.

Example:

```text
Next.js Server
 ↓
Service Client
 ↓
Circuit Breaker
 ↓
Internal Service
```

When a downstream service is unhealthy, fail gracefully rather than creating cascading failures.

---

# 36. TIMEOUTS

Every external service request must have a defined timeout.

Never allow an internal API call to hang indefinitely.

Use appropriate deadlines for:

```text
HTTP
gRPC
Database
Messaging
```

---

# 37. DATABASE ACCESS

Server-side database access must remain isolated from Client Components.

Never import database clients into:

```text
"use client"
```

components.

Database access belongs in:

```text
Server Components
Server Actions
Route Handlers
Application Services
Repositories
```

according to architecture.

---

# 38. DATABASE ABSTRACTION

Where the platform requires provider abstraction:

```text
DATABASE_PROVIDER=postgres
```

may conceptually select:

```text
PostgreSQL
MySQL
SQL Server
Oracle
SQLite
MongoDB
```

Use the approved provider/factory abstraction.

Do not pretend provider-specific capabilities are identical.

---

# 39. ORM

Use the existing project's ORM/data-access standard.

Possible technologies include:

```text
Prisma
Drizzle
TypeORM
Mongoose
```

Do not introduce multiple ORMs into the same service without a compelling architectural reason.

---

# 40. CONNECTION MANAGEMENT

Do not create uncontrolled database connections during hot reload or per-request execution.

Use the ORM/driver's supported pooling and lifecycle strategy.

Pay particular attention to:

```text
Development Hot Reload
Serverless Deployment
Container Deployment
Multiple Replicas
```

---

# 41. SERVERLESS COMPATIBILITY

If deployed using serverless infrastructure:

```text
Lambda
Vercel Functions
Cloud Functions
```

avoid assuming process-local state is permanent.

Do not rely on:

```text
In-memory locks
In-memory queues
Local filesystem persistence
Singleton state across invocations
```

for distributed application guarantees.

---

# 42. CACHE

Redis may be used for:

```text
Caching
Rate Limiting
Idempotency
Distributed Locks
Temporary State
Sessions
```

Do not treat Redis as the authoritative database unless explicitly designed.

---

# 43. REACT STATE

Do not store authoritative security/business state only in React state.

Client state is:

```text
UI State
UX State
Cached View State
```

not the source of truth for authorization.

---

# 44. FORM VALIDATION

Client-side validation improves UX.

It does NOT replace server-side validation.

Use:

```text
Client Validation
+
Server Validation
```

for important operations.

Where possible share validation schemas without exposing sensitive server-only logic.

---

# 45. SERVER ACTION VALIDATION

Every Server Action must validate its input independently.

Never assume that only your own UI can call a Server Action.

Treat it as a public attack surface within the application's security boundary.

---

# 46. CSRF

For cookie-based authentication, implement appropriate CSRF protection.

Do not assume:

```text
SameSite
```

alone is always sufficient for every deployment architecture.

Review:

```text
SameSite
Secure
HttpOnly
Origin
CSRF Token
```

according to the authentication architecture.

---

# 47. COOKIES

Authentication cookies should generally use:

```text
HttpOnly
Secure
Appropriate SameSite
```

Never store sensitive authentication credentials in:

```text
localStorage
sessionStorage
```

without an explicit, justified security architecture.

---

# 48. SECRETS

Server-only secrets must never be exposed through client-side environment variables.

In Next.js, treat variables exposed through:

```text
NEXT_PUBLIC_*
```

as public.

Never place:

```text
Database Password
JWT Secret
API Secret
Private Key
RabbitMQ Credentials
Internal Service Credentials
```

behind `NEXT_PUBLIC_`.

---

# 49. ENVIRONMENT VARIABLES

Validate configuration at startup/build time as appropriate.

Separate:

```text
Public Configuration
Server-only Configuration
```

explicitly.

---

# 50. STATIC GENERATION

Static generation is appropriate for content that does not require per-user authorization.

Do not statically render private tenant data.

Be extremely careful with caching authenticated responses.

---

# 51. CACHING

Before caching any authenticated response determine:

```text
Who can see this data?
How long can it remain stale?
Can another tenant receive it?
Can another user receive it?
How is it invalidated?
```

Never accidentally cache private responses publicly.

---

# 52. REVALIDATION

When using Next.js caching/revalidation:

```text
Time-based Revalidation
Tag-based Revalidation
Path Revalidation
```

must respect tenant and authorization boundaries.

Never invalidate or cache data across tenants unintentionally.

---

# 53. TENANT-AWARE CACHE KEYS

Tenant-aware data should use tenant-aware cache keys.

Conceptually:

```text
tenant:{tenantId}:users:{userId}
```

rather than:

```text
users:{userId}
```

when the resource is tenant-scoped.

---

# 54. IMAGE OPTIMIZATION

Use Next.js image optimization where appropriate.

Do not allow arbitrary remote image sources without configuring trusted domains/patterns.

Validate user-uploaded image sources.

---

# 55. FILE UPLOADS

Do not store important production files permanently on local application disk if the deployment model is ephemeral.

Prefer appropriate object storage where required.

Validate:

```text
Size
Type
Extension
Content
Filename
Path
```

Never trust client metadata.

---

# 56. SECURITY HEADERS

Configure security headers appropriately.

Consider:

```text
Content-Security-Policy
Strict-Transport-Security
X-Content-Type-Options
Referrer-Policy
Frame Protection
Permissions Policy
```

Do not blindly copy a CSP that breaks legitimate application functionality.

---

# 57. CSP

When using a Content Security Policy:

```text
Scripts
Styles
Images
Fonts
Frames
Connections
```

must be explicitly considered.

If nonces are required, implement them consistently.

Do not disable CSP simply because it is inconvenient.

---

# 58. XSS PROTECTION

Never render untrusted HTML directly.

Avoid unnecessary:

```tsx
dangerouslySetInnerHTML
```

If HTML rendering is genuinely required:

```text
Sanitize
Validate
Restrict
```

the content first.

---

# 59. URL VALIDATION

Do not blindly redirect to arbitrary user-provided URLs.

Prevent:

```text
Open Redirect
javascript:
data:
```

and similar unsafe schemes where applicable.

---

# 60. SERVER-SIDE FETCH

Server-side `fetch()` calls must protect against:

```text
SSRF
Untrusted URLs
Internal network access
Unexpected redirects
```

Do not fetch arbitrary URLs supplied by users without strict validation.

---

# 61. GRAPHQL

If GraphQL is introduced:

```text
Authentication
Authorization
Complexity Limits
Depth Limits
Rate Limiting
Query Validation
```

must be implemented.

Do not expose unrestricted schema access in production.

---

# 62. REAL-TIME COMMUNICATION

For:

```text
WebSocket
SSE
SignalR-compatible gateways
```

authentication and authorization must be applied at connection/subscription level.

Do not assume an authenticated page means every real-time channel is authorized.

---

# 63. RABBITMQ

Next.js applications should normally publish/consume events through server-side infrastructure.

Do not connect browsers directly to RabbitMQ.

Preferred:

```text
Next.js Server
 ↓
Event Publisher
 ↓
RabbitMQ
```

Consumers should generally run in dedicated workers/services rather than inside a frontend web process unless there is a deliberate architecture.

---

# 64. OUTBOX

For reliable database + event operations:

```text
Business Transaction
 ↓
Database
 +
Outbox
 ↓
Background Publisher
 ↓
RabbitMQ
```

Do not rely on:

```text
database commit
then
RabbitMQ publish
```

without failure recovery.

---

# 65. BACKGROUND JOBS

Do not run critical scheduled jobs inside a frontend web process unless the deployment architecture explicitly guarantees correct job execution.

Prefer:

```text
Dedicated Worker
Queue
Scheduler
External Job System
```

This prevents duplicate jobs when multiple Next.js replicas run.

---

# 66. CRON

If Next.js deployment supports scheduled triggers, treat them as triggers rather than guaranteed singleton workers.

Every scheduled operation must consider:

```text
Duplicate Execution
Idempotency
Distributed Locking
Retry
Failure
Observability
```

---

# 67. HEALTH CHECKS

Provide appropriate health endpoints when Next.js is deployed as a service.

Example:

```text
GET /api/health/live
GET /api/health/ready
```

Liveness should indicate process availability.

Readiness may verify critical dependencies.

Do not make liveness depend on every external service.

---

# 68. OPENAPI

If Next.js exposes APIs, maintain OpenAPI documentation where required.

Document:

```text
Request
Response
Authentication
Authorization
Errors
Pagination
Filtering
Idempotency
Rate Limits
```

Use Scalar where it is part of the platform standard.

---

# 69. API VERSIONING

Public APIs should use a versioning strategy when breaking compatibility is possible.

Example:

```text
/api/v1/users
/api/v2/users
```

Never silently change a public contract used by mobile or external clients.

---

# 70. MOBILE CLIENT COMPATIBILITY

Assume API consumers may include:

```text
Angular
React
MAUI
Kotlin Android
iOS
External Clients
```

Therefore API contracts must remain platform-neutral.

Do not create APIs that depend on React-specific behavior.

---

# 71. ANGULAR / REACT FRONTEND COMPATIBILITY

Next.js APIs must expose consistent:

```text
Result Pattern
Error Codes
Pagination
Authentication
CorrelationId
```

so Angular and React clients can consume them consistently.

---

# 72. MAUI / KOTLIN COMPATIBILITY

Mobile applications may have slower or unreliable connections.

APIs should support:

```text
Idempotency
Pagination
Compact Payloads
Timeouts
Retry-safe Operations
Consistent Errors
```

Avoid unnecessarily large responses.

---

# 73. AUDIT LOGGING

Critical operations should generate centralized audit records.

Include:

```text
User
Tenant
Company
Organization
Action
Resource
ResourceId
Timestamp
CorrelationId
Result
```

Do not store credentials or sensitive secrets in audit logs.

---

# 74. OBSERVABILITY

Integrate Next.js server-side execution with:

```text
OpenTelemetry
Distributed Tracing
Metrics
Structured Logging
```

as defined by:

```text
.ai/observability.md
```

Capture:

```text
Route
Method
Duration
Status
TraceId
CorrelationId
User
Tenant
Dependency
```

where appropriate and safe.

---

# 75. LOG FILE STRUCTURE

Follow the centralized log structure:

```text
logs/
├── build-errors/
│   └── build-error-dd-mm-yy.txt
│
├── runtime-error-logs/
│   └── runtime-error-dd-mm-yy.txt
│
├── query-logs/
│   └── query-dd-mm-yy.txt
│
└── exception-logs/
    └── exception-logs-dd-mm-yy.txt
```

Do not invent a separate Next.js logging standard.

Use the project's centralized logger.

---

# 76. QUERY LOGGING

Where database instrumentation allows, capture:

```text
Endpoint
Method
File
Line
Database Provider
Query
Start Time
End Time
Total Duration
CorrelationId
TraceId
```

Never log secrets.

Parameter values must be sanitized or masked when sensitive.

---

# 77. BUILD ERROR LOGGING

Build failures should be captured in the centralized build-error format where CI/local automation supports it.

Include:

```text
Timestamp
Project
File
Line
Column
Exact Error
Root Cause
Possible Solution
```

Do not replace the actual compiler output with vague summaries.

---

# 78. RUNTIME ERROR LOGGING

Runtime failures should include:

```text
Timestamp
Service
Entry Point
Route
Method
File
Line
Root Cause
Exact Exception
Possible Solution
CorrelationId
TraceId
```

---

# 79. EXCEPTION LOGGING

Use:

```text
logs/exception-logs/exception-logs-dd-mm-yy.txt
```

for centralized exception diagnostics according to the platform observability rules.

---

# 80. NO SENSITIVE LOGGING

Never log:

```text
Passwords
OTP
JWT
Refresh Tokens
Session Cookies
API Keys
Private Keys
Payment Credentials
Database Credentials
```

Mask sensitive information.

---

# 81. PERFORMANCE

Monitor:

```text
TTFB
Server Rendering Duration
API Duration
Database Duration
External Service Duration
Memory
CPU
Event Loop Lag
Payload Size
```

Do not optimize based solely on intuition.

---

# 82. N+1 REQUESTS

Avoid patterns where one page causes:

```text
1 request
+
100 sequential API requests
```

Use appropriate:

```text
Batch APIs
Parallel Fetching
Server-side aggregation
Caching
```

where justified.

---

# 83. PARALLEL SERVER FETCHING

Independent server-side operations may execute concurrently where safe.

Conceptually:

```typescript
const [users, roles, permissions] = await Promise.all([
    getUsers(),
    getRoles(),
    getPermissions()
]);
```

Do not parallelize operations that depend on one another.

---

# 84. FETCH CANCELLATION

Long-running server requests should support cancellation/abort behavior where the platform and client support it.

Avoid wasting resources after the request is no longer needed.

---

# 85. MEMORY

Avoid retaining:

```text
Large request bodies
Large query results
Large cached objects
User-specific data
```

longer than required.

Paginate large datasets.

---

# 86. TYPE SAFETY

Use strict TypeScript settings wherever possible.

Avoid:

```typescript
any
```

without a documented reason.

Prefer:

```text
unknown
Typed DTOs
Discriminated Unions
Generics
Validation Schemas
```

---

# 87. API TYPES

Do not duplicate API types manually in multiple locations when a reliable contract-generation strategy is available.

Consider:

```text
OpenAPI
Generated Types
Shared Contracts
```

while keeping service boundaries independent.

---

# 88. DATABASE ENTITY EXPOSURE

Never send ORM/database entities directly to clients.

Use:

```text
Entity
 ↓
Application Result
 ↓
Response DTO
```

This prevents accidental exposure of internal fields.

---

# 89. CLIENT BUNDLE SAFETY

Never import server-only modules into Client Components.

Examples of server-only code:

```text
Database Client
Private API Client
Secret Manager
Filesystem
Server Credentials
RabbitMQ Client
Internal gRPC Client
```

Keep server and client dependencies clearly separated.

---

# 90. SERVER-ONLY MODULES

Where supported by the framework, mark sensitive modules as server-only.

The goal is to fail early if a developer accidentally imports them into browser code.

---

# 91. PUBLIC ENVIRONMENT VARIABLES

Anything named:

```text
NEXT_PUBLIC_*
```

must be considered public.

Never place secrets there.

---

# 92. INTERNATIONALIZATION

The platform supports centralized language selection.

At minimum:

```text
Bangla
English
```

Support future languages without rewriting business logic.

Language selection should be centralized and reusable.

Do not hardcode translated strings throughout business logic.

---

# 93. LOCALIZATION

Use translation keys such as:

```text
notification.created
validation.required
auth.invalid_credentials
service.unavailable
```

rather than embedding language-specific business messages everywhere.

The server may return stable error codes while the frontend resolves localized messages.

---

# 94. USER-FACING ERROR MESSAGES

Technical exceptions and user-facing messages must remain separate.

Example:

```text
Technical:
ECONNREFUSED PostgreSQL:5432

User:
The service is temporarily unavailable.
```

The frontend should receive a graceful message and stable error code.

---

# 95. SECURITY QUESTIONS / PASSWORD RULES

If Next.js participates in authentication flows, it must use the centralized authentication service.

Security requirements such as:

```text
OTP Login
Forgot Password
Reset Password
Security Questions
Password History
Last 3 Passwords Cannot Be Reused
Password Policy
Session Security
```

must not be independently reimplemented in the frontend.

The server/authentication service remains authoritative.

---

# 96. MODULE AND PERMISSION MANAGEMENT

UI visibility may be based on permissions, but server authorization remains mandatory.

The frontend may hide:

```text
Module
Menu
Button
Action
```

based on permissions.

But APIs must independently verify authorization.

---

# 97. SSR SECURITY

Never render confidential data into HTML that an unauthorized user could receive through caching or shared rendering.

Review:

```text
SSR
Caching
Revalidation
CDN
Headers
Cookies
```

together.

---

# 98. SEO

For public-facing applications, use appropriate Next.js metadata APIs.

Do not expose private tenant or authenticated information through metadata.

---

# 99. ACCESSIBILITY

Enterprise UI must follow accessibility standards.

At minimum consider:

```text
Keyboard Navigation
ARIA
Focus Management
Color Contrast
Screen Readers
Form Errors
Loading States
```

Do not sacrifice accessibility merely for visual design.

---

# 100. TESTING

Follow:

```text
.ai/testing-and-performance.md
```

Use the project's established tools.

Typical stack:

```text
Vitest/Jest
React Testing Library
Playwright
Supertest
Testcontainers
```

Do not introduce unnecessary duplicate testing frameworks.

---

# 101. UNIT TESTS

Test:

```text
Business Rules
Validation
Server Actions
Service Clients
Error Mapping
Permission Logic
Utility Functions
```

---

# 102. COMPONENT TESTS

Test important interactive components using the project's approved component testing framework.

Focus on behavior rather than implementation details.

---

# 103. END-TO-END TESTS

Use Playwright or the project's approved equivalent for:

```text
Login
OTP
Password Reset
Critical CRUD
Permission Boundaries
Tenant Isolation
Checkout
Payment
Booking
Notifications
```

where applicable.

---

# 104. API TESTS

Verify:

```text
Authentication
Authorization
Validation
Result Pattern
HTTP Status
Error Codes
CorrelationId
Idempotency
Rate Limiting
```

---

# 105. LOAD TESTS

Maintain:

```text
tests/load-test/
```

according to platform rules.

Required performance tooling:

```text
NBomber
k6
Apache JMeter
```

Use the appropriate tool for the target.

Document:

```text
How to Run
Environment
Scenario
Users
Duration
Thresholds
Results
```

---

# 106. DOCKER

Use multi-stage builds where appropriate.

Production image should contain only what is necessary to run the application.

Consider:

```text
Standalone Next.js Output
```

where appropriate for containerized deployments.

---

# 107. NEXT.JS STANDALONE OUTPUT

If suitable, configure:

```text
output: "standalone"
```

for a smaller production container.

Verify that all required runtime assets are included.

Do not enable it blindly if the existing deployment architecture is incompatible.

---

# 108. NON-ROOT CONTAINER

Production containers should run as a non-root user where possible.

Do not require root privileges without justification.

---

# 109. GRACEFUL SHUTDOWN

When Next.js is deployed as a long-running Node process, graceful shutdown should account for:

```text
HTTP Server
Database
Redis
RabbitMQ
Workers
Telemetry
Logs
```

Do not terminate while critical work is being silently discarded.

---

# 110. CI/CD

CI should verify:

```text
Dependency Install
Type Check
Lint
Unit Tests
Component Tests
E2E Tests where appropriate
Build
Security Checks
Docker Build
```

Performance testing may run in dedicated environments.

---

# 111. DEPENDENCY SECURITY

Regularly inspect dependencies.

Pay attention to:

```text
Next.js
React
Node.js
Authentication Libraries
Database Drivers
Image Processing Libraries
HTTP Clients
```

Do not blindly run automated dependency upgrades in production without testing.

---

# 112. DEPENDENCY FAILURES

If an internal service is down:

```text
Detect
 ↓
Trace
 ↓
Log
 ↓
Retry if safe
 ↓
Circuit Break if necessary
 ↓
Graceful User Response
```

Do not display raw infrastructure errors.

---

# 113. BUILD VALIDATION

Before declaring the application complete:

```text
npm/pnpm/yarn install
typecheck
lint
test
build
```

must succeed according to the project's scripts.

Use the package manager already selected by the repository.

---

# 114. PROGRAMMER GUIDE

Maintain:

```text
docs/programmers-guide/
```

with concise guides for:

```text
Next.js Architecture
App Router
Server Components
Client Components
Server Actions
Route Handlers
Authentication
Authorization
Middleware
API Clients
HTTP
gRPC
RabbitMQ
Database
Caching
Background Jobs
Testing
Load Testing
Deployment
Troubleshooting
```

Document actual project commands.

Do not document commands that have not been verified.

---

# 115. MIGRATION DOCUMENTATION

Maintain a Markdown guide containing exact commands for:

```text
Create Migration
Run Migration
Rollback where supported
Seed Database
Reset Development Database
```

Commands must match the actual ORM and project configuration.

---

# 116. LOG OBSERVABILITY

Developers must be able to determine:

```text
What failed?
Where did it fail?
Which endpoint?
Which method?
Which service?
Which file?
Which line?
Which dependency?
What was the root cause?
What was the exact exception?
What should be changed?
```

without manually searching dozens of unrelated logs.

---

# 117. LOG CORRELATION

Every relevant log should be searchable using:

```text
CorrelationId
TraceId
Service
TenantId
UserId
Endpoint
```

where safe and appropriate.

This allows:

```text
Angular
React
MAUI
Kotlin
Next.js
Backend Services
RabbitMQ
```

to be traced through one distributed workflow.

---

# 118. API GATEWAY COMPATIBILITY

Next.js applications must work behind:

```text
YARP
Ocelot
NGINX
Ingress
CDN
Load Balancer
```

without assuming direct internet exposure.

---

# 119. MOBILE/API CONTRACT STABILITY

Mobile applications may remain deployed for months.

Never make breaking API changes without:

```text
Versioning
Backward Compatibility
Migration Plan
```

---

# 120. FRONTEND/BACKEND CONTRACT

The frontend must not compensate for inconsistent backend contracts.

Backend APIs must consistently provide:

```text
success
message
errors[]
error.code
error.field
traceId
```

where defined by the platform Result Pattern.

---

# 121. NO DUPLICATE PLATFORM LOGIC

Do not implement separate:

```text
Error Framework
Logging Framework
Correlation Framework
Communication Framework
Result Framework
Localization Framework
```

inside every Next.js project.

Reuse the organization's approved packages/modules where available.

---

# 122. REUSABILITY

The Next.js implementation should remain reusable for:

```text
HRM
ERP
Accounting
HMS
Payroll
Ticketing
Booking
Payment
Payment Gateway
Notification
SaaS
Transport
Inventory
POS
```

Do not hardcode business-specific assumptions into shared infrastructure.

---

# 123. PRODUCTION READINESS CHECKLIST

Before declaring the Next.js application complete:

```text
[ ] Latest supported Node.js selected
[ ] Latest compatible Next.js selected
[ ] React compatibility verified
[ ] TypeScript strictness verified
[ ] Existing router architecture understood
[ ] App Router/Pages Router decision verified
[ ] Server Components used appropriately
[ ] Client Components minimized
[ ] Server Actions secured
[ ] Route Handlers secured
[ ] Centralized Result Pattern implemented
[ ] Centralized Error Handling implemented
[ ] Error Boundaries implemented where required
[ ] Authentication verified
[ ] Authorization verified
[ ] Tenant isolation verified
[ ] Company/Organization context verified
[ ] CorrelationId verified
[ ] TraceId verified
[ ] Idempotency verified where required
[ ] Rate limiting verified
[ ] IP/proxy handling verified
[ ] CORS verified where applicable
[ ] CSRF protection reviewed
[ ] Secure cookies verified
[ ] Secrets protected
[ ] NEXT_PUBLIC variables reviewed
[ ] Database access server-only
[ ] Database abstraction verified where required
[ ] Redis verified where required
[ ] HTTP communication verified
[ ] gRPC communication verified
[ ] RabbitMQ integration verified
[ ] Retry policy verified
[ ] Timeout verified
[ ] Circuit breaker verified
[ ] Outbox verified where required
[ ] Background jobs isolated
[ ] Scheduled jobs verified
[ ] Health endpoints verified
[ ] OpenTelemetry verified
[ ] Structured logging verified
[ ] Runtime error logging verified
[ ] Exception logging verified
[ ] Query logging verified
[ ] Audit logging verified
[ ] Localization verified
[ ] Bangla/English support verified
[ ] Future language extension supported
[ ] OpenAPI verified where applicable
[ ] Scalar verified where applicable
[ ] Angular compatibility verified
[ ] React compatibility verified
[ ] MAUI compatibility verified
[ ] Kotlin compatibility verified
[ ] Unit tests pass
[ ] Component tests pass
[ ] E2E tests pass
[ ] Load tests available
[ ] k6 tests available
[ ] JMeter tests available
[ ] NBomber tests available where applicable
[ ] Docker build verified
[ ] Non-root container verified
[ ] Graceful shutdown verified
[ ] CI/CD verified
[ ] Migration commands documented
[ ] Programmer Guide updated
[ ] Security review completed
[ ] Git commit created
```

---

# 124. FINAL ARCHITECTURE RULE

The platform hierarchy is:

```text
.ai/MASTER-RULE.md
        ↓
.ai/AI_RULES.md
        ↓
.ai/communication.md
.ai/observability.md
.ai/testing-and-performance.md
        ↓
.ai/backend/node.md
        ↓
.ai/backend/node-nextjs.md
        ↓
Next.js Application
```

`node.md` defines common Node.js rules.

This document defines Next.js-specific implementation.

Business logic must remain framework-independent wherever practical.

The final system must be:

```text
Production-ready
Enterprise-grade
Secure
Observable
Scalable
Testable
Maintainable
Reusable
Multi-tenant
Communication-provider independent
Database-provider independent where required
Frontend-platform compatible
Mobile-client compatible
```

# END OF NODE.JS + NEXT.JS ENGINEERING RULES
