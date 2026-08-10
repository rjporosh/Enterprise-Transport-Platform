# Angular Enterprise Frontend Engineering Rules

## 1. PURPOSE

This document defines Angular-specific engineering rules for production-grade enterprise frontend applications.

It extends:

```text
.ai/MASTER-RULE.md
.ai/AI_RULES.md
.ai/communication.md
.ai/observability.md
.ai/testing-and-performance.md
.ai/frontend/angular.md
```

The shared platform rules are authoritative.

This document defines Angular-specific implementation decisions only.

The application must be:

```text
Production-ready
Enterprise-grade
Secure
Scalable
Maintainable
Testable
Accessible
Observable
Reusable
Multi-tenant ready
Mobile/API compatible
```

---

# 2. VERSION POLICY

Always use the latest stable Angular version available at implementation time.

Do not permanently lock this document to an old Angular version.

The AI must inspect:

```text
package.json
package-lock.json
angular.json
tsconfig.json
tsconfig.app.json
```

and determine the current project version.

Use the latest compatible:

```text
Angular
Angular CLI
TypeScript
RxJS
Angular Material
CDK
ESLint
Testing libraries
```

when creating or upgrading a project.

Do not downgrade simply to avoid compatibility work.

---

# 3. EXISTING PROJECT FIRST

Before changing the frontend, inspect:

```text
src/
angular.json
package.json
tsconfig.json
public/
assets/
styles/
```

Understand:

```text
Routing
Authentication
Authorization
State Management
API Clients
Shared Components
Design System
Environment Configuration
Testing
Build Configuration
```

Reuse existing architecture where it is sound.

Do not redesign the entire frontend unnecessarily.

---

# 4. ANGULAR ARCHITECTURE

Prefer modern Angular architecture:

```text
src/
├── app/
│   ├── core/
│   │   ├── auth/
│   │   ├── guards/
│   │   ├── interceptors/
│   │   ├── services/
│   │   ├── models/
│   │   └── config/
│   │
│   ├── shared/
│   │   ├── components/
│   │   ├── directives/
│   │   ├── pipes/
│   │   └── utilities/
│   │
│   ├── features/
│   │   ├── dashboard/
│   │   ├── users/
│   │   ├── notifications/
│   │   └── ...
│   │
│   ├── layout/
│   ├── app.routes.ts
│   └── app.config.ts
│
├── assets/
└── styles/
```

Adapt to the existing project.

Do not force this exact structure if the current architecture already follows an equally strong pattern.

---

# 5. STANDALONE COMPONENTS

Prefer standalone Angular components.

Example:

```typescript
@Component({
  standalone: true,
  selector: 'app-user-list',
  imports: [
    CommonModule,
    ReactiveFormsModule
  ],
  templateUrl: './user-list.component.html'
})
export class UserListComponent {}
```

Avoid introducing unnecessary NgModules in modern Angular applications.

---

# 6. SIGNALS

Prefer Angular Signals for local and UI state where appropriate.

Use:

```text
signal()
computed()
effect()
input()
output()
```

where they improve clarity.

Do not convert every observable into a signal merely because signals exist.

Use RxJS where streams and asynchronous composition are the better abstraction.

---

# 7. RXJS

Use RxJS for:

```text
HTTP Streams
WebSocket
Event Streams
Complex Async Composition
Cancellation
Debouncing
Polling
Reactive Workflows
```

Avoid unnecessary nested subscriptions.

Prefer operators such as:

```text
switchMap
concatMap
mergeMap
exhaustMap
catchError
retry
timeout
debounceTime
distinctUntilChanged
takeUntilDestroyed
```

according to the actual use case.

---

# 8. COMPONENT RESPONSIBILITY

Components should primarily handle:

```text
UI
User Interaction
Presentation State
Input
Output
```

Do not place large business workflows directly inside components.

Avoid:

```text
Huge Components
Huge Templates
Direct HTTP Calls Everywhere
Business Rules in HTML
Duplicate API Logic
```

---

# 9. SERVICES

Services should encapsulate:

```text
API Communication
Business Coordination
Shared State
Authentication
Authorization
Configuration
Notifications
```

Do not create one giant `CommonService`.

Prefer focused services.

---

# 10. FEATURE-BASED ARCHITECTURE

Organize business functionality by feature.

Example:

```text
features/
├── auth/
├── dashboard/
├── users/
├── notifications/
├── payments/
├── routes/
├── bookings/
└── reports/
```

Each feature should own its:

```text
Components
Routes
Models
Services
State
Tests
```

where practical.

---

# 11. CORE VS SHARED

`core/` contains singleton application infrastructure.

Examples:

```text
Authentication
HTTP Interceptors
Global Configuration
App Initialization
Telemetry
```

`shared/` contains reusable UI and utility functionality.

Do not put business-specific components into `shared/` merely because multiple pages currently use them.

---

# 12. ROUTING

Use modern Angular routing.

Prefer lazy-loaded feature routes:

```typescript
{
  path: 'notifications',
  loadChildren: () =>
    import('./features/notifications/notifications.routes')
      .then(m => m.NOTIFICATION_ROUTES)
}
```

Avoid loading the entire application eagerly.

---

# 13. ROUTE GUARDS

Use guards for:

```text
Authentication
Authorization
Tenant Access
Module Access
Permission Checks
Unsaved Changes
```

But remember:

**Frontend guards are not security boundaries.**

The backend must independently enforce authorization.

---

# 14. PERMISSION SYSTEM

The frontend must support centralized authorization concepts:

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

Do not scatter raw permission strings throughout templates.

Centralize them.

---

# 15. MODULE MANAGEMENT

Support dynamic module visibility where required.

For example:

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

The UI should hide modules the authenticated user cannot access.

The backend remains authoritative.

---

# 16. MULTI-TENANCY

The frontend must support:

```text
Tenant
Company
Organization
Branch
```

where applicable.

Tenant/company context should be obtained from authenticated context and approved APIs.

Do not blindly trust arbitrary tenant IDs supplied by users.

---

# 17. TENANT SWITCHING

If tenant switching is supported:

```text
User
 ↓
Select Tenant
 ↓
Update Auth Context
 ↓
Refresh Permissions
 ↓
Refresh Relevant Data
 ↓
Continue
```

Avoid stale data from the previous tenant.

---

# 18. HTTP CLIENT

Use Angular's centralized HTTP client.

Do not scatter raw API URLs across components.

Prefer:

```text
Environment Configuration
API Configuration Service
Feature API Services
```

---

# 19. HTTP INTERCEPTORS

Centralize cross-cutting HTTP behavior.

Typical responsibilities:

```text
Authentication
Access Token
CorrelationId
TraceId
Language
Tenant Context where appropriate
Error Handling
Request Timing
Retry
```

Do not put business logic inside interceptors.

---

# 20. CORRELATION ID

Every API request should participate in distributed tracing.

Support:

```text
CorrelationId
TraceId
```

The frontend should:

```text
Generate if required
Propagate
Capture
Display when useful
```

This allows developers to correlate:

```text
Angular
 ↓
Gateway
 ↓
Backend Service
 ↓
Database
```

---

# 21. GLOBAL ERROR HANDLING

Implement centralized HTTP/application error handling.

Handle:

```text
400
401
403
404
409
422
429
500
502
503
504
```

where applicable.

Do not implement independent error handling in every component.

---

# 22. RESULT PATTERN

Consume the backend Result Pattern consistently.

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

The frontend must display all relevant errors from a single API response.

Do not show only the first error when multiple actionable errors exist.

---

# 23. GRACEFUL ERROR MESSAGES

Users should receive friendly messages.

Never display raw:

```text
Stack Trace
SQL
Exception
File Path
Internal Server Error Details
```

Instead:

```text
Something went wrong. Please try again.
```

or the localized server-provided message.

Developers can use:

```text
CorrelationId
TraceId
Logs
```

for diagnosis.

---

# 24. CENTRALIZED LANGUAGE SELECTION

Support at minimum:

```text
English
Bangla
```

Design the system so additional languages can be added without rewriting components.

Language selection may be based on:

```text
User Preference
Tenant Preference
Browser Language
Explicit Selection
Backend Preference
```

according to the platform architecture.

---

# 25. LOCALIZATION

Never hardcode user-facing strings directly into components.

Avoid:

```html
<button>Save</button>
```

for applications requiring localization.

Prefer translation keys:

```html
<button>{{ 'COMMON.SAVE' | translate }}</button>
```

The exact translation framework should follow the existing project.

---

# 26. LANGUAGE FALLBACK

Always define a fallback language.

Recommended:

```text
Requested Language
        ↓
Available Translation
        ↓
English
        ↓
Safe Default
```

Missing translations must not break the UI.

---

# 27. FORMS

Prefer Reactive Forms for complex enterprise forms.

Use:

```text
FormControl
FormGroup
FormArray
Validators
Typed Forms
```

where appropriate.

Avoid large forms with hundreds of controls in one component without logical grouping.

---

# 28. FORM VALIDATION

Validate at:

```text
Client
+
Server
```

Client validation improves UX.

Server validation provides actual security and correctness.

Never trust frontend validation alone.

---

# 29. VALIDATION ERRORS

Map backend validation errors to form fields where possible.

Example:

```text
email → Email control
phone → Phone control
password → Password control
```

Display all relevant errors.

---

# 30. PASSWORD / AUTHENTICATION FLOWS

The frontend must support the centralized authentication capabilities when provided:

```text
Login
2FA
OTP Login
Forgot Password
Reset Password
Security Questions
Password Change
Password History
Session Management
Logout
```

Do not implement password policy independently from the centralized authentication service.

---

# 31. TWO-FACTOR AUTHENTICATION

Support:

```text
OTP
Authenticator App
Email OTP
SMS OTP
```

according to backend capabilities.

Never log OTP values.

---

# 32. PASSWORD HISTORY

The frontend may display password policy information such as:

```text
Previous passwords cannot be reused.
```

The actual rule, including:

```text
Last 3 passwords cannot be reused
```

must be enforced by the authentication backend.

---

# 33. TOKEN HANDLING

Never expose authentication tokens unnecessarily.

Follow the authentication architecture.

Consider secure cookie-based authentication where appropriate.

If browser storage is required, understand the XSS/security implications.

Never log:

```text
Access Token
Refresh Token
JWT
OTP
Password
Security Answer
```

---

# 34. SESSION EXPIRATION

Handle expired sessions centrally.

Typical flow:

```text
API → 401
 ↓
Authentication Service
 ↓
Refresh if permitted
 ↓
Retry safe request
 ↓
Otherwise logout
 ↓
Redirect to login
```

Do not endlessly retry authentication failures.

---

# 35. RETRY POLICY

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
422
Business Rule Errors
```

Never automatically retry unsafe mutations unless idempotency is supported.

---

# 36. IDEMPOTENCY

For operations such as:

```text
Payment
Booking
Order
Notification
Financial Transaction
```

support:

```text
Idempotency-Key
```

when the backend requires it.

Generate and preserve the same key across safe retries of the same operation.

---

# 37. RATE LIMITING

Handle:

```text
429 Too Many Requests
```

gracefully.

Display a user-friendly message and respect server-provided retry information where available.

Do not implement aggressive client retries.

---

# 38. API VERSIONING

Centralize API version configuration.

Example:

```text
/api/v1
```

Do not hardcode API versions across dozens of components.

---

# 39. API PROVIDER ABSTRACTION

Frontend communication should be abstracted behind application-level interfaces/services.

Example:

```text
Feature
 ↓
IApiClient / Service
 ↓
HTTP Adapter
```

The frontend should not care whether the backend communication is:

```text
HTTP
gRPC-Web
Gateway
BFF
```

where the architecture supports multiple providers.

---

# 40. GATEWAY SUPPORT

The Angular application should communicate through the configured gateway/BFF when required.

Compatible gateway architecture:

```text
Angular
 ↓
YARP / Ocelot / BFF
 ↓
Services
```

Do not bypass the gateway unless explicitly required.

---

# 41. YARP / OCELOT

Frontend configuration must remain compatible with:

```text
YARP
Ocelot
```

API routes should be configurable rather than scattered throughout the application.

---

# 42. REAL-TIME COMMUNICATION

When required, support:

```text
SignalR
WebSocket
Server-Sent Events
```

according to backend architecture.

Use real-time communication for:

```text
Notifications
Live Dashboard
Booking Status
Vehicle Tracking
Operational Events
```

where appropriate.

---

# 43. EVENT-DRIVEN UI

The frontend may react to backend events through:

```text
SignalR
WebSocket
SSE
```

Do not connect browsers directly to RabbitMQ.

Browser clients should use an appropriate gateway/realtime adapter.

---

# 44. STATE MANAGEMENT

Use the simplest state management solution that satisfies the requirement.

Possible approaches:

```text
Signals
RxJS
Component Store
NgRx
```

Do not introduce NgRx merely because the application is large.

Use centralized state when there is genuine shared state complexity.

---

# 45. STATE CATEGORIES

Separate:

```text
UI State
Server State
Authentication State
Tenant State
Permission State
Application State
```

Do not put everything into one global store.

---

# 46. SERVER STATE

API data should have appropriate:

```text
Loading
Success
Empty
Error
Refreshing
Stale
```

states.

Avoid duplicated API state across multiple components.

---

# 47. LOADING STATES

Every asynchronous UI operation should have a clear state.

Examples:

```text
Loading
Saving
Deleting
Refreshing
Uploading
Downloading
```

Do not freeze the entire application for a single API operation unless appropriate.

---

# 48. EMPTY STATES

Every data-driven screen should handle:

```text
No Data
No Search Results
No Permission
Loading
Error
```

Do not display a blank page.

---

# 49. TABLES

Enterprise tables should support where applicable:

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

Avoid loading unbounded datasets.

---

# 50. SEARCH

Search should support:

```text
Debounce
Cancellation
Pagination
Server-side Search
Loading
Empty State
```

Use server-side search for large datasets.

---

# 51. FILTERS

Filters should be:

```text
Typed
Validated
Serializable
Shareable
Restorable
```

where appropriate.

---

# 52. URL STATE

Important navigation/filter state may be represented in query parameters.

Example:

```text
/users?page=2&search=porosh&status=active
```

This improves:

```text
Bookmarking
Sharing
Back/Forward Navigation
```

---

# 53. DIALOGS

Dialogs should be focused and reusable.

Avoid deeply nested dialogs.

Critical destructive actions require confirmation.

---

# 54. DESTRUCTIVE ACTIONS

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

Do not allow accidental destructive operations.

---

# 55. NOTIFICATIONS / TOASTS

Use a centralized notification service.

Support:

```text
Success
Information
Warning
Error
```

Do not create ad-hoc toast implementations in every feature.

---

# 56. ACCESSIBILITY

Follow WCAG principles.

Ensure:

```text
Keyboard Navigation
Focus Management
ARIA Labels
Color-independent Meaning
Readable Contrast
Screen Reader Support
Form Labels
Error Association
```

Do not rely solely on color.

---

# 57. RESPONSIVE DESIGN

Support appropriate layouts for:

```text
Desktop
Tablet
Mobile
```

Do not assume desktop-only usage.

---

# 58. DESIGN SYSTEM

Use a centralized design system.

If Angular Material is used:

```text
Material
CDK
Theme Tokens
Reusable Components
```

should be centralized.

Avoid manually recreating Material components unnecessarily.

---

# 59. COMPONENT REUSE

Create reusable components for genuine repeated patterns:

```text
Data Table
Search
Pagination
Form Field
Modal
Confirm Dialog
File Upload
Empty State
Error State
Loading State
```

Do not abstract components that are used only once unless there is architectural value.

---

# 60. THEMING

Support centralized:

```text
Theme
Typography
Spacing
Colors
Dark Mode where required
```

Avoid hardcoded visual values throughout templates.

---

# 61. CSS / SCSS

Use the project's established styling architecture.

Prefer:

```text
SCSS
Design Tokens
CSS Variables
Component Styles
```

Avoid excessive global CSS.

Avoid `!important` unless genuinely necessary.

---

# 62. SECURITY

Never trust frontend state.

Security-sensitive operations must be validated by the backend.

Protect against:

```text
XSS
CSRF
Open Redirect
Token Leakage
Unsafe HTML
Clickjacking
```

---

# 63. XSS

Never render untrusted HTML directly.

Avoid unnecessary use of:

```typescript
DomSanitizer.bypassSecurityTrustHtml()
```

If HTML rendering is unavoidable, sanitize it properly.

---

# 64. OPEN REDIRECT

Do not redirect users to arbitrary URLs supplied by query parameters.

Whitelist valid destinations.

---

# 65. FILE UPLOAD

Validate:

```text
Filename
MIME Type
Size
Extension
```

Client validation is only for UX.

The backend must validate the file independently.

---

# 66. DOWNLOAD SECURITY

Do not expose private files through predictable URLs.

Use backend authorization and secure download mechanisms.

---

# 67. AUDIT UI

Where the backend exposes audit information, provide appropriate views containing:

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

Do not expose sensitive secrets.

---

# 68. OBSERVABILITY

Frontend observability should integrate with:

```text
OpenTelemetry
TraceId
CorrelationId
Centralized Logging
Metrics
Error Tracking
```

according to the project.

---

# 69. FRONTEND ERROR LOGGING

Capture meaningful:

```text
Unhandled Errors
HTTP Failures
Navigation Errors
Critical UI Errors
```

Include:

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

Never include passwords or tokens.

---

# 70. PERFORMANCE MONITORING

Monitor:

```text
Initial Load
Largest Contentful Paint
Interaction Responsiveness
API Latency
Bundle Size
Route Load Time
Memory Usage
Error Rate
```

---

# 71. LAZY LOADING

Lazy load feature areas whenever practical.

Do not eagerly load:

```text
Large Reports
Admin Modules
Rarely Used Features
Heavy Libraries
```

---

# 72. CODE SPLITTING

Keep initial JavaScript payload small.

Analyze bundle sizes periodically.

Do not import entire libraries when a smaller import is available.

---

# 73. CHANGE DETECTION

Prefer modern Angular performance patterns.

Use signals and appropriate component architecture.

Avoid unnecessary global state mutations that trigger excessive UI updates.

---

# 74. IMAGE OPTIMIZATION

Use:

```text
Responsive Images
Modern Formats
Lazy Loading
Proper Dimensions
Compression
```

where appropriate.

---

# 75. CACHING

Cache carefully.

Safe candidates may include:

```text
Static Assets
Reference Data
Configuration
Read-only Metadata
```

Do not cache sensitive user-specific data without a clear invalidation strategy.

---

# 76. SERVICE WORKER

Use PWA/service workers only when the application genuinely benefits from them.

Do not introduce offline caching into security-sensitive applications without understanding stale-data implications.

---

# 77. OFFLINE SUPPORT

If offline support is required:

```text
Detect Offline
Queue Safe Operations
Sync
Resolve Conflicts
Show State
```

Do not blindly replay financial or destructive operations.

---

# 78. TESTING

Follow:

```text
.ai/testing-and-performance.md
```

Test at multiple levels.

Required where applicable:

```text
Unit Tests
Component Tests
Service Tests
Integration Tests
E2E Tests
Load/Performance Tests
```

---

# 79. UNIT TESTS

Test:

```text
Services
Pipes
Guards
Interceptors
Validators
State Logic
Utility Functions
```

Focus on business behavior rather than implementation details.

---

# 80. COMPONENT TESTS

Test:

```text
Rendering
Inputs
Outputs
User Interaction
Loading
Empty
Error
Success
Authorization Visibility
```

---

# 81. E2E TESTS

Use an appropriate browser automation framework such as:

```text
Playwright
Cypress
```

according to the project standard.

Critical workflows should be covered.

---

# 82. CRITICAL E2E FLOWS

Where applicable:

```text
Login
OTP
Forgot Password
Reset Password
2FA
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

# 83. API MOCKING

Use mocks only where they provide value.

Do not make every test dependent on mocks.

Critical integration flows should also be tested against real backend contracts.

---

# 84. CONTRACT TESTING

Frontend API models must match backend contracts.

Where possible validate:

```text
Request Schema
Response Schema
Error Schema
Pagination
Authentication
Headers
```

against generated OpenAPI or contract definitions.

---

# 85. LOAD TESTING

Frontend-related API performance must be tested using the platform tools:

```text
tests/load-test/
```

Required tools:

```text
NBomber
k6
Apache JMeter
```

Use them primarily against APIs/gateways rather than attempting to load-test the browser itself with the wrong tool.

---

# 86. ANGULAR BUILD

Before completion verify:

```bash
npm install
npm run build
```

or the project's actual configured commands.

Also verify:

```text
Lint
Type Check
Unit Tests
E2E
Production Build
```

---

# 87. NPM SECURITY

Run appropriate dependency/security checks.

Example:

```bash
npm audit
```

Do not blindly apply automated fixes to production dependencies.

Review breaking changes.

---

# 88. LOCK FILE

Commit the appropriate:

```text
package-lock.json
```

or existing package manager lock file.

Do not randomly delete lock files to "fix" dependency problems.

---

# 89. ENVIRONMENT CONFIGURATION

Never hardcode:

```text
API URL
Gateway URL
Environment Secrets
Tenant Secrets
Third-party API Keys
```

Use Angular environment/build configuration appropriate to the project.

Remember:

**Frontend environment values are not secrets.**

Anything shipped to the browser can be inspected.

---

# 90. CI/CD

CI should verify:

```text
Install
Lint
Type Check
Unit Tests
E2E Tests
Production Build
Dependency Security
Bundle Analysis where appropriate
Docker Build if containerized
```

---

# 91. DOCKER

If Angular is containerized:

Prefer:

```text
Build Stage
 ↓
Static Assets
 ↓
Nginx / Appropriate Web Server
```

Do not ship the full Node development environment into the production runtime image unless required.

---

# 92. CONTAINER SECURITY

Production frontend containers should:

```text
Run as non-root where possible
Contain minimal dependencies
Expose only required ports
Use immutable builds
Avoid secrets
```

---

# 93. API GATEWAY DEPLOYMENT

Typical architecture:

```text
Browser
   ↓
CDN / Reverse Proxy
   ↓
YARP / Ocelot / Gateway
   ↓
Backend Services
```

Keep gateway-specific routing outside feature components.

---

# 94. MOBILE COMPATIBILITY

The frontend API contract must also remain compatible with:

```text
.NET MAUI
Kotlin Android
```

and other clients.

Do not design backend contracts solely around Angular.

---

# 95. REACT COMPATIBILITY

Shared backend APIs must remain framework-neutral.

Angular-specific assumptions must never leak into:

```text
Backend
API Contract
Event Contract
Authentication Contract
```

---

# 96. API ERROR UX

For a failed request:

```text
Technical Error
 ↓
Central Error Handler
 ↓
Translate Error Code
 ↓
Friendly User Message
 ↓
Optional Field Errors
 ↓
CorrelationId for Support
```

---

# 97. NO DUPLICATED ERROR HANDLING

Do not write:

```typescript
catchError(...)
```

with completely different error semantics in every component.

Centralize common handling.

Feature-specific handling may be added where the UI genuinely needs different behavior.

---

# 98. API TIMEOUT

Client requests should have reasonable timeouts.

Do not let the UI wait forever.

Long-running operations should use:

```text
Async Job
Polling
SignalR
WebSocket
```

rather than keeping HTTP requests open unnecessarily.

---

# 99. POLLING

When polling:

```text
Use bounded intervals
Stop when complete
Cancel on navigation
Handle errors
Avoid overlapping requests
```

Prefer `switchMap` or appropriate RxJS patterns.

---

# 100. REAL-TIME NOTIFICATIONS

Notification UI should support:

```text
Unread Count
Read/Unread
Mark Read
Delete/Archive
Filtering
Pagination
Real-time Updates
```

when provided by the Notification Service.

---

# 101. AUDITABLE USER ACTIONS

Critical frontend actions should generate backend audit events through the API.

The frontend must not be considered the audit source of truth.

---

# 102. ACCESSIBILITY TESTING

Include automated and manual accessibility checks where practical.

Verify:

```text
Keyboard
Focus
Labels
ARIA
Contrast
Screen Reader
Forms
Dialogs
```

---

# 103. BROWSER SUPPORT

Follow the current Angular-supported browser matrix.

Do not add unnecessary polyfills for unsupported legacy browsers unless explicitly required.

---

# 104. INTERNATIONALIZATION

Design all UI for localization from the beginning:

```text
Text Length
Date
Time
Currency
Number Format
RTL possibility
Bangla typography
```

Do not assume English text length.

---

# 105. DATE / TIME

Always distinguish:

```text
UTC
Local Time
Tenant Time Zone
User Time Zone
```

Do not blindly use browser local time for server-generated timestamps.

---

# 106. CURRENCY

Never hardcode currency formatting.

Use:

```text
Currency Code
Locale
Tenant Configuration
```

where applicable.

---

# 107. TABLE EXPORT

Exports should be generated server-side for large datasets.

Do not load millions of rows into the browser merely to export them.

---

# 108. REPORTS

Large reports should support:

```text
Async Generation
Progress
Download
Expiration
Authorization
```

where required.

---

# 109. BULK OPERATIONS

Bulk operations must support:

```text
Selection
Validation
Confirmation
Progress
Partial Failure Reporting
Success Summary
Error Summary
```

Do not silently fail individual items.

---

# 110. USER FEEDBACK

Every important action should clearly communicate:

```text
What happened
Whether it succeeded
Whether work is still running
What the user can do next
```

---

# 111. FINAL PRODUCTION CHECKLIST

Before declaring an Angular application complete:

```text
[ ] Latest supported Angular version
[ ] Latest compatible TypeScript
[ ] Existing architecture inspected
[ ] Standalone architecture used where appropriate
[ ] Feature-based architecture
[ ] Lazy loading
[ ] Signals used appropriately
[ ] RxJS used appropriately
[ ] Centralized HTTP client
[ ] HTTP interceptors
[ ] CorrelationId
[ ] TraceId
[ ] Centralized error handling
[ ] Result Pattern support
[ ] Multi-error display
[ ] Bangla/English localization
[ ] Future localization support
[ ] Authentication
[ ] OTP
[ ] 2FA
[ ] Forgot Password
[ ] Reset Password
[ ] Security Questions where required
[ ] Centralized authorization
[ ] Role management UI where required
[ ] Module management UI where required
[ ] Permission management
[ ] Tenant context
[ ] Company context
[ ] Organization context
[ ] Rate-limit handling
[ ] Idempotency support
[ ] Retry policy
[ ] API timeout
[ ] YARP compatibility
[ ] Ocelot compatibility
[ ] HTTP abstraction
[ ] Real-time communication where required
[ ] SignalR/WebSocket/SSE where required
[ ] Responsive design
[ ] Accessibility
[ ] Design system
[ ] Secure token handling
[ ] XSS protection
[ ] CSRF review
[ ] Secure file upload UX
[ ] Audit UI where required
[ ] OpenTelemetry integration
[ ] Error observability
[ ] Performance monitoring
[ ] Unit tests
[ ] Component tests
[ ] Integration tests
[ ] E2E tests
[ ] NBomber API load testing
[ ] k6 load/stress testing
[ ] JMeter performance testing
[ ] Production build verified
[ ] Dependency security checked
[ ] Docker build verified where required
[ ] CI/CD verified
[ ] Programmer documentation updated
[ ] No secrets committed
[ ] No unrelated files modified
[ ] Professional Git commit created
```

---

# 112. REUSABILITY

This Angular architecture must be reusable across:

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

The architecture must remain domain-independent.

---

# 113. FINAL ARCHITECTURE

```text
.ai/MASTER-RULE.md
        ↓
.ai/AI_RULES.md
        ↓
.ai/communication.md
.ai/observability.md
.ai/testing-and-performance.md
        ↓
.ai/frontend/angular.md
        ↓
Angular Application
        ↓
YARP / Ocelot / BFF
        ↓
Enterprise Backend Services
```

The Angular frontend must never become tightly coupled to one backend implementation.

The final application should be:

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
API-platform independent
```

# END OF ANGULAR ENTERPRISE FRONTEND ENGINEERING RULES
