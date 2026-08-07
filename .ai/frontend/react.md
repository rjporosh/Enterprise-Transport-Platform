# React Enterprise Frontend Engineering Rules

## 1. PURPOSE

This document defines React-specific engineering rules for production-grade enterprise frontend applications.

This document extends:

```text
.ai/MASTER-RULE.md
.ai/AI_RULES.md
.ai/communication.md
.ai/observability.md
.ai/testing-and-performance.md
.ai/frontend/react.md
```

The shared platform rules remain authoritative.

This document defines React-specific implementation decisions only.

Applications must be:

```text
Production-ready
Enterprise-grade
Secure
Scalable
Maintainable
Testable
Accessible
Observable
Performant
Multi-tenant ready
Reusable
API-platform independent
```

---

# 2. VERSION POLICY

Always use the latest stable React ecosystem versions available at implementation time.

Do not permanently lock this document to an old React version.

The AI must inspect:

```text
package.json
package-lock.json
pnpm-lock.yaml
yarn.lock
vite.config.*
next.config.*
tsconfig.json
```

where applicable.

Determine the project's existing:

```text
React
React DOM
TypeScript
Vite / Next.js
Router
State Management
UI Framework
Testing Framework
```

versions before changing anything.

Use the latest compatible stable versions when creating or upgrading.

Do not downgrade merely to avoid compatibility work.

---

# 3. EXISTING PROJECT FIRST

Before modifying the frontend, inspect:

```text
src/
public/
package.json
tsconfig.json
vite.config.*
next.config.*
eslint.config.*
```

and the project's actual structure.

Understand:

```text
Routing
Authentication
Authorization
API Clients
State Management
Design System
Environment Configuration
Testing
Build Configuration
Observability
```

Reuse existing architecture where it is sound.

Do not redesign the entire application unnecessarily.

---

# 4. REACT ARCHITECTURE

Prefer feature-oriented architecture.

Recommended structure:

```text
src/
├── app/
│   ├── router/
│   ├── providers/
│   ├── config/
│   └── App.*
│
├── core/
│   ├── auth/
│   ├── api/
│   ├── http/
│   ├── guards/
│   ├── telemetry/
│   ├── errors/
│   └── config/
│
├── shared/
│   ├── components/
│   ├── hooks/
│   ├── utilities/
│   ├── types/
│   └── constants/
│
├── features/
│   ├── dashboard/
│   ├── users/
│   ├── notifications/
│   ├── payments/
│   ├── routes/
│   └── bookings/
│
├── layouts/
└── assets/
```

Adapt this to the existing project.

Do not force the exact structure when the existing architecture is already strong.

---

# 5. COMPONENT DESIGN

Components should primarily handle:

```text
Rendering
User Interaction
Presentation State
Composition
Inputs
Outputs
```

Avoid putting large business workflows directly inside components.

Avoid:

```text
Huge Components
Huge JSX Files
Direct API Calls Everywhere
Duplicated Business Logic
Complex Side Effects
```

---

# 6. FUNCTIONAL COMPONENTS

Use functional components.

Prefer:

```tsx
function UserList() {
    return (
        <section>
            ...
        </section>
    );
}
```

Avoid introducing class components into new code unless a legacy boundary genuinely requires one.

---

# 7. HOOKS

Use React hooks appropriately:

```text
useState
useEffect
useMemo
useCallback
useRef
useContext
```

and project-approved custom hooks.

Do not create custom hooks merely to wrap one trivial line of code.

---

# 8. CUSTOM HOOKS

Use custom hooks for reusable application behavior.

Examples:

```text
useAuth()
usePermissions()
useTenant()
useNotifications()
usePagination()
useDebouncedSearch()
useApiQuery()
```

Keep hooks focused.

Avoid creating a giant:

```text
useEverything()
```

hook.

---

# 9. EFFECTS

Use `useEffect` only for actual side effects.

Do not use effects to derive ordinary state that can be calculated directly.

Avoid effect chains such as:

```text
State A
 ↓
Effect
 ↓
State B
 ↓
Effect
 ↓
State C
```

Prefer derived state or a proper state/query architecture.

---

# 10. STATE MANAGEMENT

Use the simplest state management solution that satisfies the application.

Possible choices:

```text
React State
Context
Zustand
Redux Toolkit
TanStack Query
Jotai
Other established project standard
```

Do not introduce Redux solely because the application is enterprise-grade.

Enterprise does not mean "put everything in Redux."

---

# 11. STATE CATEGORIES

Separate:

```text
UI State
Server State
Authentication State
Tenant State
Permission State
Application State
Form State
```

Do not put all state into one global store.

---

# 12. SERVER STATE

Use a server-state solution where appropriate.

For example:

```text
TanStack Query
```

or the project's existing equivalent.

Server state should handle:

```text
Loading
Success
Error
Caching
Refetching
Invalidation
Pagination
Retries
```

Avoid manually rebuilding a query/cache framework unless necessary.

---

# 13. CACHING

Cache carefully.

Safe candidates may include:

```text
Reference Data
Read-only Metadata
Configuration
Non-sensitive Lookup Data
```

Do not cache sensitive user-specific data without an explicit invalidation strategy.

Invalidate data after mutations where necessary.

---

# 14. ROUTING

Use the project's routing solution.

For React SPA applications, a mature router such as:

```text
React Router
```

may be used.

Routes should be feature-oriented and lazy-loaded where practical.

---

# 15. LAZY ROUTES

Lazy-load large or rarely used features.

Example concept:

```tsx
const Reports = lazy(() => import('./features/reports/Reports'));
```

Use appropriate suspense boundaries.

Do not eagerly load every enterprise module.

---

# 16. ROUTE PROTECTION

Protect routes for:

```text
Authentication
Authorization
Tenant Access
Module Access
Permission Access
```

But remember:

**Frontend route protection is not a security boundary.**

The backend must independently enforce authorization.

---

# 17. PERMISSION MANAGEMENT

Support centralized:

```text
User
Tenant
Company
Organization
Role
Module
Permission
Resource
Action
```

Example:

```text
users.read
users.create
users.update
users.delete
payments.refund
reports.export
```

Do not scatter arbitrary permission strings throughout JSX.

Centralize permissions.

---

# 18. MODULE MANAGEMENT

Support dynamic module visibility.

Example:

```text
Dashboard
Users
Inventory
POS
Payments
Notifications
Routes
Bookings
Reports
Administration
```

Modules unavailable to the user should not appear in navigation.

Backend authorization remains authoritative.

---

# 19. MULTI-TENANCY

Support:

```text
Tenant
Company
Organization
Branch
```

where applicable.

Tenant context should come from authenticated context and approved APIs.

Never trust arbitrary tenant identifiers supplied by users.

---

# 20. TENANT SWITCHING

If tenant switching exists:

```text
Select Tenant
 ↓
Update Auth Context
 ↓
Refresh Permissions
 ↓
Invalidate Tenant-specific Queries
 ↓
Load New Tenant Data
```

Never leave stale data from the previous tenant visible.

---

# 21. HTTP CLIENT

Centralize API communication.

Do not scatter URLs across components.

Prefer:

```text
API Configuration
HTTP Client
Feature API Services
```

Example abstraction:

```text
Component
   ↓
Feature Hook / Service
   ↓
API Client
   ↓
Gateway / BFF
```

---

# 22. API ABSTRACTION

Business features should not directly depend on transport implementation.

Preferred concept:

```text
Feature
   ↓
Application API Interface
   ↓
Communication Adapter
   ↓
HTTP / gRPC-Web / BFF
```

This allows the communication mechanism to evolve without rewriting feature components.

---

# 23. HTTP / gRPC-WEB

Where supported by the backend architecture, the frontend may communicate using:

```text
HTTP/REST
gRPC-Web
BFF
Gateway
```

The browser should not directly connect to infrastructure-level internal communication systems.

---

# 24. YARP / OCELOT

The frontend must remain compatible with:

```text
YARP
Ocelot
```

or another API Gateway/BFF.

Typical architecture:

```text
React
  ↓
YARP / Ocelot / BFF
  ↓
Backend Services
```

API routes must be centrally configurable.

Do not hardcode gateway routing throughout feature components.

---

# 25. CORRELATION ID

Every API request should participate in distributed tracing.

Support:

```text
CorrelationId
TraceId
```

The frontend should propagate these values according to the platform architecture.

Useful diagnostic chain:

```text
React
 ↓
Gateway
 ↓
Service
 ↓
Database
```

---

# 26. HTTP INTERCEPTOR / MIDDLEWARE LAYER

React itself does not provide Angular-style HTTP interceptors.

Implement equivalent centralized behavior through:

```text
Fetch Wrapper
Axios Interceptor
API Client Middleware
Query Client Configuration
```

depending on the project.

Centralize:

```text
Authentication
CorrelationId
TraceId
Language
Tenant Context where appropriate
Error Handling
Retry
Timeout
```

---

# 27. CENTRALIZED ERROR HANDLING

Implement centralized error handling for:

```text
API Errors
Network Errors
Unexpected Exceptions
Route Errors
Rendering Errors
```

Use:

```text
Error Boundary
API Error Handler
Global Error Handler
```

as appropriate.

Do not implement unrelated error semantics in every component.

---

# 28. RESULT PATTERN

The frontend must understand the backend Result Pattern.

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

Display all actionable errors from a single response.

Do not silently discard additional errors.

---

# 29. GRACEFUL ERROR MESSAGES

Users should never see:

```text
Stack Trace
SQL Query
Exception Type
Internal File Path
Internal Server Details
```

Instead show:

```text
Something went wrong. Please try again.
```

or the appropriate localized backend message.

Developers can use:

```text
CorrelationId
TraceId
Central Logs
```

for diagnosis.

---

# 30. ERROR BOUNDARIES

Use React Error Boundaries for rendering failures.

An error boundary should:

```text
Catch UI failures
Display graceful fallback
Expose correlation information where appropriate
Log diagnostics
Allow recovery/reload
```

Do not display sensitive diagnostics to end users.

---

# 31. CENTRALIZED LANGUAGE SELECTION

Support at minimum:

```text
English
Bangla
```

Design localization so additional languages can be added without rewriting components.

Language preference may be based on:

```text
User Preference
Tenant Preference
Browser Language
Explicit Selection
Backend Configuration
```

according to platform architecture.

---

# 32. LOCALIZATION

Do not hardcode user-facing strings in JSX.

Use the project's localization framework.

For example:

```tsx
t('common.save')
```

instead of:

```tsx
<button>Save</button>
```

The exact library should follow the project standard.

---

# 33. LANGUAGE FALLBACK

Use a deterministic fallback:

```text
Requested Language
 ↓
Available Translation
 ↓
English
 ↓
Safe Default
```

Missing translations must not break the application.

---

# 34. INTERNATIONALIZATION

Design for:

```text
Bangla
English
Future Languages
Different Text Lengths
Date Formats
Time Formats
Currency
Number Formats
RTL possibility
```

Do not assume English-only UI dimensions.

---

# 35. DATE / TIME

Clearly distinguish:

```text
UTC
User Time Zone
Tenant Time Zone
Server Time
```

Server timestamps should not be blindly converted without understanding their semantics.

---

# 36. CURRENCY

Never hardcode currency formatting.

Use:

```text
Currency Code
Locale
Tenant Configuration
```

where applicable.

---

# 37. AUTHENTICATION

Support centralized authentication flows:

```text
Login
OTP Login
2FA
Forgot Password
Reset Password
Security Questions
Change Password
Password History
Session Management
Logout
```

according to the backend authentication service.

---

# 38. TWO-FACTOR AUTHENTICATION

Support where backend capability exists:

```text
OTP
Authenticator App
Email OTP
SMS OTP
```

Never log:

```text
OTP
Password
Access Token
Refresh Token
Security Answer
```

---

# 39. PASSWORD HISTORY

The frontend may display password policy information.

For example:

```text
Previous passwords cannot be reused.
```

The backend must enforce:

```text
Last 3 passwords cannot be reused
```

or whatever policy the authentication service defines.

---

# 40. TOKEN SECURITY

Do not expose authentication tokens unnecessarily.

Prefer the project's secure authentication model.

If browser storage is used, explicitly evaluate:

```text
XSS
Token Theft
Session Expiration
Refresh Strategy
```

Never log authentication tokens.

---

# 41. SESSION EXPIRATION

Centralize handling:

```text
API → 401
 ↓
Refresh if permitted
 ↓
Retry safe request
 ↓
Otherwise Logout
 ↓
Redirect Login
```

Avoid infinite retry loops.

---

# 42. RETRY POLICY

Retry only transient failures.

Potentially retry:

```text
Network Failure
Timeout
503
504
```

Do not blindly retry:

```text
400
401
403
404
409
422
Business Errors
```

Unsafe mutations must not be retried automatically unless idempotency is supported.

---

# 43. IDEMPOTENCY

Support `Idempotency-Key` for operations such as:

```text
Payment
Booking
Order
Financial Transaction
Notification
```

when required by the backend.

The same operation must retain the same idempotency key across safe retries.

---

# 44. RATE LIMITING

Handle:

```text
429 Too Many Requests
```

gracefully.

Respect:

```text
Retry-After
```

when provided.

Do not perform aggressive client-side retry loops.

---

# 45. API VERSIONING

Centralize API base URL and version configuration.

Example:

```text
/api/v1
```

Do not duplicate API version strings across features.

---

# 46. FORMS

For complex enterprise forms use an established form architecture.

Forms must support:

```text
Typed Values
Validation
Server Validation
Field Errors
Dirty State
Submission State
Accessibility
```

---

# 47. FORM VALIDATION

Validation occurs at:

```text
Client
+
Server
```

Client validation improves UX.

Server validation is authoritative.

Never trust client validation for security.

---

# 48. BACKEND VALIDATION ERRORS

Map server validation errors to the corresponding form fields.

Example:

```text
email → Email field
phone → Phone field
password → Password field
```

Display all actionable validation errors.

---

# 49. TABLES

Enterprise data tables should support where applicable:

```text
Pagination
Sorting
Filtering
Search
Column Selection
Responsive Layout
Export
Bulk Actions
Loading
Empty State
Error State
```

Never load huge datasets into browser memory unnecessarily.

---

# 50. SEARCH

Search should support:

```text
Debouncing
Cancellation
Server-side Search
Pagination
Loading
Empty State
Error State
```

Use server-side search for large datasets.

---

# 51. FILTERING

Filters should be:

```text
Typed
Validated
Serializable
Restorable
Shareable
```

where appropriate.

---

# 52. URL STATE

Important state can be represented in query parameters.

Example:

```text
/users?page=2&search=porosh&status=active
```

Benefits:

```text
Bookmarking
Sharing
Back/Forward Navigation
```

---

# 53. LOADING STATES

Every async operation should communicate state:

```text
Loading
Saving
Deleting
Refreshing
Uploading
Downloading
```

Avoid blocking the entire application unnecessarily.

---

# 54. EMPTY STATES

Every data-driven screen must handle:

```text
No Data
No Search Results
No Permission
Loading
Error
```

Never render a confusing blank screen.

---

# 55. SKELETON UI

Use skeleton/loading placeholders for appropriate large screens.

Do not animate everything.

Loading UX should improve perceived performance rather than become visual noise.

---

# 56. NOTIFICATIONS / TOASTS

Use a centralized notification service.

Support:

```text
Success
Information
Warning
Error
```

Do not implement separate toast mechanisms in every feature.

---

# 57. NOTIFICATION SERVICE

Where the backend Notification Service exists, support:

```text
Unread Count
Read/Unread
Mark Read
Archive/Delete where permitted
Pagination
Filtering
Real-time Updates
```

The browser should receive realtime events through:

```text
SignalR
WebSocket
SSE
```

or an appropriate frontend-facing gateway.

Do not connect the browser directly to RabbitMQ.

---

# 58. REAL-TIME COMMUNICATION

Support where required:

```text
SignalR
WebSocket
SSE
```

Appropriate use cases:

```text
Notifications
Live Dashboard
Booking Status
Vehicle Tracking
Operational Updates
Payment Status
```

---

# 59. EVENT-DRIVEN UI

Browser applications should consume frontend-safe event streams.

Architecture:

```text
Backend Event
 ↓
Event Adapter / Service
 ↓
SignalR / WebSocket / SSE
 ↓
React
```

Do not expose internal infrastructure such as RabbitMQ directly to browsers.

---

# 60. API TIMEOUT

Every API request should have a reasonable timeout.

Never allow the UI to wait indefinitely.

Long-running operations should use:

```text
Async Job
Polling
SignalR
WebSocket
SSE
```

instead of keeping HTTP requests open unnecessarily.

---

# 61. POLLING

When polling:

```text
Use bounded intervals
Stop when complete
Cancel on navigation
Handle failures
Avoid overlapping requests
Respect server state
```

---

# 62. DESIGN SYSTEM

Use a centralized design system.

Possible systems:

```text
MUI
Ant Design
Chakra UI
Tailwind
Custom Design System
```

Follow the existing project's choice.

Do not introduce multiple competing UI frameworks without architectural justification.

---

# 63. COMPONENT REUSE

Create reusable components for genuine repeated patterns:

```text
DataTable
Search
Pagination
FormField
Modal
ConfirmDialog
FileUpload
EmptyState
ErrorState
LoadingState
```

Do not abstract everything.

---

# 64. STYLING

Use the project's established styling architecture.

Prefer:

```text
CSS Modules
SCSS
Tailwind
Design Tokens
CSS Variables
```

according to project standards.

Avoid uncontrolled global CSS.

Avoid excessive `!important`.

---

# 65. RESPONSIVE DESIGN

Support appropriate:

```text
Desktop
Tablet
Mobile
```

layouts.

Do not assume desktop-only usage.

---

# 66. ACCESSIBILITY

Follow WCAG principles.

Ensure:

```text
Keyboard Navigation
Focus Management
ARIA
Screen Reader Support
Form Labels
Error Association
Contrast
Visible Focus
```

Do not communicate important information through color alone.

---

# 67. MODALS / DIALOGS

Dialogs must:

```text
Trap Focus
Support Escape
Have Accessible Labels
Handle Loading
Handle Errors
Return Focus
```

Critical destructive actions require confirmation.

---

# 68. DESTRUCTIVE ACTIONS

For:

```text
Delete
Cancel
Refund
Deactivate
Remove
```

provide:

```text
Clear Description
Confirmation
Loading State
Success Feedback
Failure Feedback
```

---

# 69. FILE UPLOAD

Validate client-side:

```text
Filename
Extension
MIME Type
Size
```

But the backend must independently validate everything.

Never assume client validation provides security.

---

# 70. FILE DOWNLOAD

Private files must require authorization.

Do not expose predictable unrestricted URLs.

---

# 71. AUDIT UI

Where audit APIs exist, expose useful information:

```text
User
Tenant
Company
Organization
Action
Resource
Timestamp
IP
CorrelationId
Result
```

Never expose secrets.

---

# 72. OBSERVABILITY

Integrate frontend observability with the platform:

```text
OpenTelemetry
TraceId
CorrelationId
Central Logging
Error Tracking
Metrics
```

according to actual project infrastructure.

---

# 73. FRONTEND ERROR LOGGING

Capture meaningful:

```text
Unhandled Errors
Render Errors
API Failures
Navigation Errors
Critical UI Errors
```

Useful diagnostic metadata:

```text
Timestamp
Application
Route
Component where known
Browser
CorrelationId
TraceId
Error Code
```

Never log:

```text
Password
OTP
Access Token
Refresh Token
Security Answer
Secrets
```

---

# 74. PERFORMANCE MONITORING

Monitor where appropriate:

```text
Initial Load
Route Load Time
Largest Contentful Paint
Interaction Responsiveness
API Latency
Bundle Size
Memory
Error Rate
```

---

# 75. BUNDLE OPTIMIZATION

Keep initial JavaScript payload small.

Use:

```text
Code Splitting
Lazy Loading
Tree Shaking
Dynamic Imports
```

Avoid importing entire libraries when smaller imports are available.

---

# 76. IMAGE OPTIMIZATION

Use:

```text
Responsive Images
Lazy Loading
Modern Formats
Compression
Correct Dimensions
```

where appropriate.

---

# 77. WEB WORKERS

Use Web Workers for genuinely CPU-intensive browser operations.

Examples:

```text
Large Data Processing
Complex Calculations
Heavy Transformations
```

Do not move trivial logic into workers.

---

# 78. OFFLINE SUPPORT

If required:

```text
Detect Offline
Queue Safe Operations
Synchronize
Resolve Conflicts
Display Sync State
```

Never blindly replay financial or destructive operations.

---

# 79. PWA

Use PWA/service workers only when business requirements justify them.

Do not introduce aggressive offline caching into security-sensitive applications without an explicit strategy.

---

# 80. TESTING

Follow:

```text
.ai/testing-and-performance.md
```

Required testing levels where applicable:

```text
Unit Tests
Component Tests
Integration Tests
E2E Tests
API Contract Tests
Load/Performance Tests
```

---

# 81. UNIT TESTS

Test:

```text
Hooks
Utilities
Services
API Clients
Permission Logic
Validators
State Logic
```

Focus on behavior.

Avoid testing implementation details unnecessarily.

---

# 82. COMPONENT TESTS

Test:

```text
Rendering
Props
User Interaction
Callbacks
Loading
Empty
Error
Success
Authorization
Accessibility
```

---

# 83. E2E TESTS

Use an approved framework such as:

```text
Playwright
Cypress
```

according to project standards.

---

# 84. CRITICAL E2E FLOWS

Where applicable:

```text
Login
OTP
2FA
Forgot Password
Reset Password
Dashboard
CRUD
Search
Filtering
Booking
Payment
Notification
Logout
Permission Boundaries
Tenant Switching
```

---

# 85. API CONTRACT TESTING

Validate frontend expectations against backend contracts.

Verify:

```text
Request Schema
Response Schema
Result Pattern
Error Schema
Pagination
Authentication
Headers
```

Use OpenAPI/generated types where practical.

---

# 86. MOCKING

Use mocks for:

```text
Unit Tests
Isolated Component Tests
Unavailable Dependencies
Specific Failure Scenarios
```

But critical integration flows should also use real backend contracts.

Do not make the entire frontend test suite dependent on fake APIs.

---

# 87. LOAD / STRESS / PERFORMANCE TESTING

The project's API performance testing structure must include:

```text
tests/load-test/
```

Required tools:

```text
NBomber
k6
Apache JMeter
```

Use them appropriately:

```text
NBomber → .NET/API load and stress testing
k6      → HTTP/API load and stress testing
JMeter  → API/performance testing
```

Browser performance should be tested with browser tooling rather than incorrectly using API load tools as browser simulators.

---

# 88. BUILD VALIDATION

Before completion verify the project's real commands.

Typical:

```bash
npm install
npm run build
npm run lint
npm test
```

If the project uses different scripts, follow `package.json`.

Do not invent commands.

---

# 89. TYPE SAFETY

Use TypeScript.

Avoid unnecessary:

```typescript
any
```

Prefer:

```text
Interfaces
Types
Generics
Discriminated Unions
Unknown
Runtime Validation
```

where appropriate.

---

# 90. API TYPES

Centralize API models.

Do not redefine the same API response type in multiple features.

Where possible generate types from OpenAPI.

---

# 91. RUNTIME VALIDATION

Compile-time TypeScript types do not validate runtime API responses.

For critical external boundaries, use runtime validation where appropriate.

Possible libraries:

```text
Zod
Valibot
Other approved validator
```

Follow existing project standards.

---

# 92. DEPENDENCY MANAGEMENT

Before adding a package:

```text
Check existing dependencies
Check maintenance status
Check bundle impact
Check security
Check license
Check compatibility
```

Do not add dependencies for trivial functionality already supported by the platform.

---

# 93. NPM SECURITY

Run appropriate security checks:

```bash
npm audit
```

where applicable.

Do not blindly execute automated dependency upgrades that introduce breaking changes.

---

# 94. LOCK FILE

Commit the project's package manager lock file:

```text
package-lock.json
pnpm-lock.yaml
yarn.lock
```

Do not randomly delete lock files to solve dependency problems.

---

# 95. ENVIRONMENT CONFIGURATION

Never hardcode:

```text
API URLs
Gateway URLs
Third-party public configuration
Environment-specific settings
```

Use appropriate environment configuration.

Remember:

**Frontend environment variables are not secrets.**

Anything delivered to the browser can be inspected.

Never place secrets into frontend environment variables.

---

# 96. CI/CD

CI should verify:

```text
Install
Lint
Type Check
Unit Tests
Component Tests
E2E Tests
Production Build
Dependency Security
Bundle Analysis where appropriate
Docker Build where applicable
```

---

# 97. DOCKER

For SPA deployments, prefer:

```text
Build Stage
 ↓
Static Assets
 ↓
Nginx / Appropriate Web Server
```

Do not ship the complete Node development environment into the production runtime unless required.

For Next.js projects, follow the dedicated Next.js architecture rules rather than blindly applying SPA deployment rules.

---

# 98. CONTAINER SECURITY

Production containers should:

```text
Run as non-root where possible
Use minimal runtime images
Contain only required artifacts
Expose only required ports
Avoid secrets
Use immutable builds
```

---

# 99. GATEWAY ARCHITECTURE

Typical:

```text
Browser
 ↓
CDN / Reverse Proxy
 ↓
YARP / Ocelot / BFF
 ↓
Enterprise Services
```

Keep gateway-specific routing outside business feature components.

---

# 100. MOBILE COMPATIBILITY

Backend contracts consumed by React must remain framework-neutral and compatible with:

```text
.NET MAUI
Kotlin Android
```

Do not design APIs solely around React.

---

# 101. ANGULAR COMPATIBILITY

Shared backend APIs must remain independent of React.

The same backend contracts should be consumable by:

```text
React
Angular
MAUI
Kotlin
```

without framework-specific assumptions.

---

# 102. AUDITABILITY

Critical operations must be auditable through backend APIs.

The frontend is not the authoritative audit source.

---

# 103. SECURITY REVIEW

Before release verify:

```text
Authentication
Authorization
Tenant Isolation
XSS
CSRF
Open Redirect
Token Handling
File Upload
File Download
Rate Limiting
Idempotency
Sensitive Logging
Dependency Security
```

---

# 104. PERFORMANCE REVIEW

Before release verify:

```text
Initial Bundle
Lazy Routes
API Latency
Rendering Performance
Large Tables
Large Forms
Memory Usage
Image Size
Caching
Network Requests
```

---

# 105. FINAL PRODUCTION CHECKLIST

```text
[ ] Latest supported React ecosystem
[ ] Existing architecture inspected
[ ] TypeScript
[ ] Feature-based architecture
[ ] Functional components
[ ] Appropriate hooks
[ ] Proper state management
[ ] Server-state management
[ ] Lazy routes
[ ] Centralized API client
[ ] CorrelationId
[ ] TraceId
[ ] Centralized error handling
[ ] Result Pattern
[ ] Multi-error support
[ ] Bangla/English localization
[ ] Future localization support
[ ] Authentication
[ ] OTP
[ ] 2FA
[ ] Forgot Password
[ ] Reset Password
[ ] Security Questions where required
[ ] Password history policy support
[ ] Centralized authorization
[ ] Role management where required
[ ] Module management
[ ] Permission management
[ ] Tenant context
[ ] Company context
[ ] Organization context
[ ] Rate-limit handling
[ ] Idempotency support
[ ] Retry policy
[ ] Timeout handling
[ ] YARP compatibility
[ ] Ocelot compatibility
[ ] HTTP abstraction
[ ] gRPC-Web where required
[ ] SignalR/WebSocket/SSE where required
[ ] Notification integration
[ ] Responsive design
[ ] Accessibility
[ ] Design system
[ ] Secure token handling
[ ] XSS protection
[ ] CSRF review
[ ] Secure file upload
[ ] Secure file download
[ ] Audit UI where required
[ ] OpenTelemetry integration
[ ] Error observability
[ ] Performance monitoring
[ ] Unit tests
[ ] Component tests
[ ] Integration tests
[ ] E2E tests
[ ] API contract tests
[ ] NBomber tests
[ ] k6 tests
[ ] JMeter tests
[ ] Production build verified
[ ] Dependency security checked
[ ] Docker verified where applicable
[ ] CI/CD verified
[ ] Documentation updated
[ ] No secrets committed
[ ] No unrelated projects modified
[ ] Professional Git commit created
```

---

# 106. REUSABILITY

This architecture must remain reusable across:

```text
HRM
ERP
Accounting
HMS
Payroll
POS
Inventory
Transport
Ticketing
Booking
Payment
Payment Gateway
Notification
SaaS
Education
Healthcare
```

React must remain the presentation layer, not the source of backend architectural coupling.

---

# 107. FINAL ARCHITECTURE

```text
.ai/
├── MASTER-RULE.md
├── AI_RULES.md
├── communication.md
├── observability.md
├── testing-and-performance.md
│
└── frontend/
    ├── angular.md
    └── react.md
```

Runtime architecture:

```text
React
  ↓
Application API Layer
  ↓
HTTP / gRPC-Web / BFF
  ↓
YARP / Ocelot
  ↓
Enterprise Services
  ↓
Database / Cache / Messaging
```

Observability:

```text
React
 ↓
CorrelationId / TraceId
 ↓
Gateway
 ↓
Service
 ↓
OpenTelemetry
 ↓
Logs / Metrics / Traces
```

Communication:

```text
React
 ↓
Gateway / BFF
 ↓
HTTP / gRPC-Web
```

Realtime:

```text
Service Events
 ↓
Realtime Adapter
 ↓
SignalR / WebSocket / SSE
 ↓
React
```

The React application must remain:

```text
Enterprise-grade
Production-ready
Secure
Accessible
Observable
Performant
Multi-tenant
Localized
Reusable
Testable
Maintainable
Backend-framework independent
```

# END OF REACT ENTERPRISE FRONTEND ENGINEERING RULES
