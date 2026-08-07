# Kotlin Multiplatform & Mobile Enterprise Engineering Rules

## 1. PURPOSE

This document defines production-grade Kotlin engineering rules for enterprise mobile and large-screen applications.

It applies to:

* Android Phones
* Android Tablets
* Foldables
* Large-screen Android Devices
* Android Smart TVs / TV-class devices
* iOS through Kotlin Multiplatform where applicable
* Other supported Kotlin targets where the project architecture requires them

This document extends:

```text
.ai/MASTER-RULE.md
.ai/AI_RULES.md
.ai/communication.md
.ai/observability.md
.ai/testing-and-performance.md
.ai/mobile/kotlin.md
```

The shared platform rules remain authoritative.

This document defines Kotlin/mobile-specific implementation rules.

---

# 2. CORE PRINCIPLE

This is NOT a demo application.

All mobile applications must be designed as:

```text
Production-ready
Enterprise-grade
Secure
Scalable
Maintainable
Testable
Observable
Accessible
Responsive
Offline-capable where required
Multi-tenant ready
Localization-ready
Cross-platform where applicable
```

Do not optimize for "works on my phone."

Optimize for:

```text
Works correctly across supported devices,
screen sizes, orientations, operating systems,
network conditions, accessibility settings,
and production environments.
```

---

# 3. VERSION POLICY

Always use the latest stable Kotlin ecosystem versions available at implementation time.

Do NOT permanently lock this document to an old Kotlin, Android SDK, Gradle, Compose, or Kotlin Multiplatform version.

Before implementation inspect:

```text
gradle/libs.versions.toml
build.gradle.kts
settings.gradle.kts
gradle.properties
AndroidManifest.xml
Package configuration
Existing Kotlin source
Existing platform modules
```

Determine the project's actual versions before changing anything.

Use the latest compatible stable versions unless the existing project has a documented compatibility constraint.

Never downgrade merely to avoid fixing compatibility issues.

---

# 4. EXISTING PROJECT FIRST

Before modifying a mobile project:

1. Inspect the complete project structure.
2. Understand existing architecture.
3. Inspect Gradle configuration.
4. Inspect dependency management.
5. Inspect Android/iOS platform boundaries.
6. Inspect navigation.
7. Inspect authentication.
8. Inspect networking.
9. Inspect persistence.
10. Inspect testing.
11. Inspect CI/CD.
12. Inspect existing documentation.

Reuse good existing conventions.

Do not redesign the entire application unnecessarily.

Do not modify unrelated applications or services.

---

# 5. GIT SAFETY

This rule is NON-NEGOTIABLE.

## If `.git` already exists

Never:

```text
Delete .git
Reinitialize Git
Rewrite Git history
Force-push history
Delete branches
```

unless explicitly instructed by the user.

Preserve the existing repository.

## If `.git` does not exist

Initialize Git at the appropriate project root:

```bash
git init
```

Then create an appropriate initial commit after verifying the project.

---

# 6. PROFESSIONAL GIT COMMITS

After every meaningful milestone, feature, or logical phase:

```text
Implement
→ Build
→ Test
→ Review
→ Fix
→ Documentation
→ Git status
→ Professional commit
→ Continue
```

Do not create meaningless commits such as:

```text
update
changes
done
fix stuff
```

Prefer Conventional Commit style.

Examples:

```text
feat(auth): implement OTP authentication flow

feat(ui): implement adaptive dashboard layout

feat(sync): implement offline synchronization

feat(notification): implement realtime notification handling

fix(auth): handle expired refresh sessions

test(ui): add orientation and adaptive layout tests

perf(ui): optimize large-screen rendering

docs(mobile): document adaptive UI architecture

chore(build): update Kotlin toolchain
```

Do not stop after every commit waiting for approval.

Continue automatically unless the change requires architectural approval.

---

# 7. ARCHITECTURE

Prefer:

```text
Presentation
    ↓
Application
    ↓
Domain
    ↓
Infrastructure
```

Where appropriate:

```text
UI
 ↓
ViewModel / Presentation Logic
 ↓
Use Case
 ↓
Repository Interface
 ↓
Repository Implementation
 ↓
Remote / Local Data Source
```

Keep business logic independent from Android UI.

---

# 8. CLEAN ARCHITECTURE

Use Clean Architecture where project complexity justifies it.

Recommended separation:

```text
domain/
application/
data/
presentation/
platform/
```

Do not create excessive abstractions for trivial applications.

Enterprise does not mean abstraction for abstraction's sake.

---

# 9. DOMAIN LAYER

Domain code should not depend directly on:

```text
Android UI
Compose
Activities
Fragments
Views
HTTP clients
Room
Platform-specific storage
```

Domain logic should remain as platform-independent as reasonably possible.

---

# 10. SHARED BUSINESS LOGIC

For Kotlin Multiplatform projects, prefer sharing:

```text
Domain Models
Use Cases
Validation
Business Rules
Authentication Logic
Authorization Logic
Networking Abstractions
Repositories
Caching Logic
Synchronization Logic
Localization Resources where practical
Telemetry Abstractions
```

Platform-specific implementations may provide:

```text
Secure Storage
Biometrics
Camera
Bluetooth
Push Notifications
File System
Platform UI
Device APIs
```

---

# 11. KOTLIN MULTIPLATFORM

Use Kotlin Multiplatform when cross-platform code sharing provides genuine value.

Possible architecture:

```text
shared/
├── commonMain/
├── androidMain/
└── iosMain/
```

The shared module should not become a dumping ground for every platform concern.

Keep platform-specific capabilities behind interfaces.

---

# 12. ANDROID

Android-specific implementation may use:

```text
Jetpack
Jetpack Compose
AndroidX
Room
WorkManager
Navigation
DataStore
Biometric APIs
```

Use current stable platform recommendations.

Follow the project's existing UI framework.

---

# 13. IOS

When Kotlin Multiplatform targets iOS:

Keep shared business logic in Kotlin.

Use native/platform-appropriate UI where required.

Possible approaches include:

```text
Compose Multiplatform
SwiftUI + shared Kotlin logic
Platform-specific UI
```

Do not force a single UI technology when it creates unnecessary platform limitations.

---

# 14. SMART TV

TV applications are NOT merely enlarged phone applications.

Support:

```text
Remote control
D-pad navigation
Focus management
Large viewing distance
Landscape layouts
Large typography
TV-safe spacing
Focus indicators
```

Do not assume touch interaction.

Every actionable element must be reachable through supported TV input methods.

---

# 15. TABLETS

Tablet layouts must use available space intelligently.

Do not simply stretch a phone layout.

Use:

```text
Navigation Rail
Navigation Drawer
Two-pane layouts
Master-detail layouts
Adaptive grids
Responsive columns
```

where appropriate.

---

# 16. FOLDABLE DEVICES

Support changing device posture and available window size.

The UI must react to:

```text
Fold
Unfold
Half-open
Rotation
Window resize
Multi-window
```

Do not assume a fixed screen dimension.

---

# 17. RESPONSIVE DESIGN — HARD REQUIREMENT

A screen must NEVER be designed around a single device resolution.

Every screen must adapt to:

```text
Compact
Medium
Expanded
```

and where applicable:

```text
Phone Portrait
Phone Landscape
Tablet Portrait
Tablet Landscape
Foldable
TV
Large Display
Multi-window
Split-screen
```

No:

```text
Clipping
Overlapping
Off-screen controls
Broken navigation
Unreadable text
Fixed-width content overflow
Broken dialogs
Broken tables
Broken forms
```

is acceptable.

---

# 18. PORTRAIT AND LANDSCAPE

Every applicable screen must be tested in:

```text
Portrait
Landscape
```

Orientation changes must preserve where appropriate:

```text
Form Data
Navigation State
User Context
Unsaved Work
Scroll Position
```

Do not destroy user input during rotation.

---

# 19. ADAPTIVE UI

Prefer adaptive layouts rather than device-specific hacks.

Avoid logic such as:

```kotlin
if (width == 390.dp) {
    ...
}
```

Prefer window-size classes and adaptive layout concepts.

Layout should respond to available space rather than specific device models.

---

# 20. RESPONSIVE TYPOGRAPHY

Typography must support:

```text
Dynamic Font Scaling
Accessibility Font Scaling
Different Screen Densities
Different Languages
Long Text
Bangla Text
Large TV Text
```

Do not use hardcoded text sizes that become unusable when accessibility settings change.

---

# 21. RESPONSIVE COMPONENTS

Every reusable component must define behavior for:

```text
Compact
Medium
Expanded
```

where applicable.

For example:

```text
Phone:
Bottom Navigation

Tablet:
Navigation Rail

Large Tablet / Desktop-class:
Navigation Rail + Expanded Content

TV:
Focus-driven Navigation
```

---

# 22. SINGLE SCREEN REQUIREMENT

A single logical screen should remain usable across supported form factors.

Example:

```text
Dashboard
```

must adapt rather than requiring separate duplicated implementations merely because the screen is wider.

Separate platform implementations are acceptable only when interaction models genuinely differ.

---

# 23. NAVIGATION

Navigation must be:

```text
Predictable
State-aware
Deep-linkable where applicable
Accessible
Responsive
Restorable
```

Support platform-specific navigation patterns where appropriate.

---

# 24. BACK NAVIGATION

Handle platform navigation correctly.

Android:

```text
Back Gesture
Back Button
System Navigation
```

TV:

```text
Back
Remote Navigation
```

iOS:

```text
Navigation Stack
Swipe Back where applicable
```

Never break platform-native navigation expectations.

---

# 25. STATE RESTORATION

Handle process death and configuration changes appropriately.

Important state should survive:

```text
Rotation
Configuration Changes
Background/Foreground
Process Recreation
```

where required.

Do not rely entirely on in-memory UI state for critical user workflows.

---

# 26. VIEWMODEL

Use ViewModels where appropriate for Android UI state.

ViewModels should not become giant application containers.

Keep business operations in use cases/services where appropriate.

---

# 27. COROUTINES

Use Kotlin Coroutines for asynchronous operations.

Prefer structured concurrency.

Avoid:

```kotlin
GlobalScope.launch
```

for application logic.

Use appropriate lifecycle-aware scopes.

---

# 28. FLOW

Use Kotlin Flow/StateFlow/SharedFlow where appropriate for reactive state.

Distinguish:

```text
State
Event
Stream
```

Do not use SharedFlow as a universal state container.

---

# 29. UI STATE

Represent UI state explicitly.

Example:

```text
Loading
Success
Empty
Error
```

For complex screens:

```text
Loading
Content
ValidationError
NetworkError
PermissionError
Empty
```

Avoid dozens of unrelated Boolean flags.

---

# 30. NETWORKING

Centralize networking.

Typical architecture:

```text
UI
 ↓
Use Case
 ↓
Repository
 ↓
API Client
 ↓
Gateway / BFF
 ↓
Backend
```

Do not make HTTP calls directly from UI components.

---

# 31. HTTP

Support backend APIs through:

```text
HTTP/REST
```

where appropriate.

Use the project's approved client.

Possible implementations:

```text
Ktor Client
Retrofit
OkHttp
```

Do not introduce multiple HTTP stacks without justification.

---

# 32. GRPC

Where supported:

```text
gRPC
```

may be used for mobile/backend communication.

Keep transport behind an abstraction where practical.

Do not expose transport details throughout the domain layer.

---

# 33. API GATEWAY

Mobile applications must remain compatible with:

```text
YARP
Ocelot
BFF
API Gateway
```

Typical architecture:

```text
Mobile
 ↓
API Gateway / BFF
 ↓
Backend Services
```

The mobile client should not need to understand internal service topology.

---

# 34. EVENT-DRIVEN SYSTEMS

Mobile clients should NOT directly connect to:

```text
RabbitMQ
Kafka
Internal Message Brokers
```

Instead:

```text
Backend Event
 ↓
Realtime Adapter
 ↓
SignalR / WebSocket / SSE / Push
 ↓
Mobile
```

---

# 35. REALTIME COMMUNICATION

Where required, support:

```text
SignalR
WebSocket
SSE
Push Notifications
```

Use realtime communication for:

```text
Notifications
Booking Updates
Vehicle Tracking
Payment Status
Operational Alerts
Live Dashboards
```

---

# 36. PUSH NOTIFICATIONS

Integrate through the platform's notification architecture.

Support where applicable:

```text
Android Push
iOS Push
Deep Links
Notification Actions
Unread Count
Notification State
```

Do not put business-critical state only inside push payloads.

---

# 37. CORRELATION ID

Every API request should participate in distributed tracing.

Support:

```text
CorrelationId
TraceId
RequestId
```

Typical flow:

```text
Mobile
 ↓
Gateway
 ↓
Service
 ↓
Database
```

The same diagnostic context should be traceable across the system.

---

# 38. API ERROR HANDLING

Implement centralized error handling.

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
Network Failure
Timeout
Serialization Failure
Unknown Error
```

Do not duplicate error handling in every screen.

---

# 39. RESULT PATTERN

Support the platform Result Pattern.

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

Display all actionable errors.

Do not silently discard errors.

---

# 40. GRACEFUL USER ERRORS

Never display:

```text
Stack Trace
SQL
Internal File Path
Internal Server Details
Exception Class
```

to users.

Instead:

```text
Something went wrong. Please try again.
```

or the appropriate localized message.

Developer diagnostics belong in centralized logs/observability.

---

# 41. RETRY POLICY

Retry only transient failures.

Potential candidates:

```text
Timeout
Temporary Network Failure
503
504
```

Do not automatically retry:

```text
400
401
403
404
409
422
Business Validation Errors
```

Unsafe operations must not be retried unless idempotency is guaranteed.

---

# 42. IDEMPOTENCY

Support:

```text
Idempotency-Key
```

for operations where duplicate execution is dangerous.

Especially:

```text
Payments
Bookings
Orders
Ticket Purchases
Refunds
Financial Transactions
```

---

# 43. RATE LIMITING

Handle:

```text
429 Too Many Requests
```

gracefully.

Respect:

```text
Retry-After
```

where provided.

Never implement uncontrolled retry loops.

---

# 44. AUTHENTICATION

Support the platform authentication system:

```text
Login
OTP
2FA
Forgot Password
Reset Password
Security Questions
Change Password
Password History
Logout
Session Expiration
```

The backend remains authoritative.

---

# 45. TWO-FACTOR AUTHENTICATION

Support where backend capability exists:

```text
OTP
Authenticator
SMS
Email
Biometric second factor where appropriate
```

Never log:

```text
Password
OTP
Access Token
Refresh Token
Security Answers
```

---

# 46. TOKEN STORAGE

Never store sensitive tokens in ordinary unencrypted preferences.

Use platform-secure storage mechanisms.

Examples:

```text
Android Keystore
iOS Keychain
Encrypted Storage
```

Use platform abstraction for Kotlin Multiplatform.

---

# 47. BIOMETRIC AUTHENTICATION

Where appropriate support:

```text
Fingerprint
Face Authentication
Device Biometrics
```

Biometric APIs must protect authentication secrets rather than replacing backend authorization.

---

# 48. SESSION MANAGEMENT

Typical flow:

```text
API → 401
 ↓
Refresh if allowed
 ↓
Retry safe request
 ↓
If refresh fails
 ↓
Clear session
 ↓
Navigate to Login
```

Avoid infinite refresh loops.

---

# 49. PERMISSIONS

Support centralized:

```text
User
Tenant
Company
Organization
Branch
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
payments.refund
reports.export
```

Frontend permissions control UX.

Backend permissions provide actual security.

---

# 50. MULTI-TENANCY

Support:

```text
TenantId
CompanyId
OrganizationId
BranchId
```

where applicable.

Never trust arbitrary tenant IDs supplied by the user.

Tenant context must come from authenticated/authorized context.

---

# 51. TENANT SWITCHING

When switching tenants:

```text
Select Tenant
 ↓
Update Context
 ↓
Refresh Permissions
 ↓
Invalidate Tenant Data
 ↓
Reload Data
```

Never display stale data from another tenant.

---

# 52. LOCALIZATION

Minimum required languages:

```text
English
Bangla
```

Design for future languages.

Do not hardcode user-facing strings.

Use centralized localization.

---

# 53. LANGUAGE SELECTION

Support language selection through:

```text
User Preference
Tenant Preference
Device Language
Explicit Selection
```

according to application requirements.

Use deterministic fallback:

```text
Requested Language
 ↓
Available Translation
 ↓
English
 ↓
Safe Default
```

---

# 54. INTERNATIONALIZATION

Support:

```text
Bangla
English
Future Languages
Long Text
Short Text
Date Formats
Time Formats
Number Formats
Currency
RTL where required
```

Do not assume English text lengths.

---

# 55. DATE / TIME

Clearly distinguish:

```text
UTC
Device Time Zone
Tenant Time Zone
Server Time
```

Do not silently reinterpret timestamps.

---

# 56. CURRENCY

Never hardcode currency formatting.

Use:

```text
Currency Code
Locale
Tenant Configuration
```

where applicable.

---

# 57. OFFLINE-FIRST

Mobile applications should support offline operation where the business domain requires it.

Architecture:

```text
UI
 ↓
Use Case
 ↓
Repository
 ↓
Local Database
 ↓
Synchronization Engine
 ↓
Remote API
```

---

# 58. LOCAL DATABASE

Use an appropriate persistent storage mechanism.

Possible technologies:

```text
Room
SQLDelight
Realm
Other approved solution
```

Do not introduce multiple database frameworks without justification.

---

# 59. CACHE STRATEGY

Define:

```text
Cache Duration
Invalidation
Synchronization
Conflict Resolution
Sensitive Data Handling
```

Do not cache sensitive information indefinitely.

---

# 60. OFFLINE OPERATION QUEUE

For operations that can safely execute offline:

```text
User Action
 ↓
Local Queue
 ↓
Persist
 ↓
Network Available
 ↓
Synchronize
 ↓
Server Confirmation
 ↓
Mark Completed
```

Financial/destructive operations require special idempotency and conflict handling.

---

# 61. CONFLICT RESOLUTION

When local and server state conflict:

```text
Detect
 ↓
Classify
 ↓
Resolve
 ↓
Notify User if Necessary
 ↓
Persist Final State
```

Never silently overwrite important user data.

---

# 62. BACKGROUND PROCESSING

Use platform-appropriate scheduling.

Android may use:

```text
WorkManager
```

for reliable background work.

Avoid unrestricted background execution.

---

# 63. BACKGROUND RETRY

Background jobs must support:

```text
Retry
Backoff
Cancellation
Network Constraints
Battery Constraints
Failure Handling
```

Avoid endless retries.

---

# 64. FILE STORAGE

Separate:

```text
Temporary Files
Cache
Application Data
User Documents
Sensitive Data
```

Never place sensitive files into publicly accessible storage.

---

# 65. CAMERA / DEVICE APIs

Platform-specific capabilities must remain behind abstractions where possible.

Examples:

```text
Camera
GPS
Bluetooth
NFC
Biometrics
Contacts
Files
Notifications
```

Request only necessary permissions.

---

# 66. LOCATION

If location is required:

```text
Request Minimum Permission
Explain Purpose
Handle Denial
Handle Approximate Location
Handle Background Restrictions
```

Never collect location without business justification.

---

# 67. ACCESSIBILITY

Support:

```text
Screen Readers
TalkBack
VoiceOver
Large Text
High Contrast
Keyboard / External Input where applicable
TV Focus Navigation
```

All important actions must remain accessible.

---

# 68. TV FOCUS

For TV-class applications:

```text
Every actionable element
must have predictable focus behavior.
```

Provide:

```text
Visible Focus
Logical Focus Order
D-pad Navigation
Back Navigation
```

Never create touch-only controls on TV.

---

# 69. RESPONSIVE TABLES / GRIDS

Large data sets must adapt.

Phone:

```text
Cards / Compact List
```

Tablet:

```text
Expanded List / Grid
```

TV:

```text
Readable Large Grid / Focusable Rows
```

Do not force a desktop-sized table onto a small phone.

---

# 70. RESPONSIVE FORMS

Forms must adapt:

```text
Phone:
Single column

Tablet:
Multi-column where appropriate

TV:
Large readable controls
Focus navigation
```

Never allow controls to overlap.

---

# 71. LOADING STATES

Every asynchronous screen should represent:

```text
Loading
Success
Empty
Error
Refreshing
Saving
Deleting
Uploading
Downloading
```

---

# 72. EMPTY STATES

Support:

```text
No Data
No Search Results
No Permission
Offline
Error
```

Never show a confusing blank screen.

---

# 73. SEARCH

Support where required:

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

# 74. PAGINATION

Support:

```text
Page Number
Page Size
Cursor
Total Count
Loading
Error
```

according to the backend contract.

Never load millions of records into a mobile device.

---

# 75. FORMS AND VALIDATION

Validate at:

```text
Client
+
Server
```

Client validation improves UX.

Server validation is authoritative.

---

# 76. SERVER VALIDATION

Map backend validation errors to the appropriate fields.

Display all actionable errors.

Do not show only the first error when the API provides multiple useful errors.

---

# 77. SECURITY

Review:

```text
Authentication
Authorization
Token Storage
Secure Storage
Certificate/Network Security
Deep Links
WebViews
File Handling
Logging
Screenshots
Clipboard
Backup
Root/Jailbreak
Sensitive Data
```

where applicable.

---

# 78. WEBVIEW SECURITY

If WebViews are required:

```text
Restrict URLs
Disable unnecessary capabilities
Validate navigation
Avoid injecting secrets
Avoid loading untrusted content
```

Do not use WebView as a shortcut for implementing the entire application.

---

# 79. DEEP LINKS

Deep links must:

```text
Validate Input
Check Authentication
Check Authorization
Check Tenant Context
Handle Expired Sessions
Avoid Open Redirects
```

Never trust deep-link parameters.

---

# 80. SENSITIVE SCREENS

For highly sensitive screens, consider platform security controls such as:

```text
Screenshot Restrictions
Secure Window
Clipboard Restrictions
Screen Obscuring
```

according to business requirements.

---

# 81. LOGGING

Use centralized logging.

Logs should support:

```text
Timestamp
Application
Version
Device
OS
Screen
Entry Point
Method
File
Location where available
Line where available
Exception
Root Cause
Possible Solution
CorrelationId
TraceId
```

Never log:

```text
Passwords
OTP
Tokens
Security Answers
Encryption Keys
Secrets
Sensitive Personal Data
```

---

# 82. EXCEPTION LOGGING

Exceptions should be captured centrally.

Developer diagnostics should make it possible to identify:

```text
Entry Point
Screen
Endpoint
Method
File
File Location
Line Number where available
Root Cause
Exact Exception
Possible Solution
Best Practice
CorrelationId
TraceId
```

Do not expose these diagnostics directly to normal users.

---

# 83. OBSERVABILITY

Integrate with:

```text
OpenTelemetry
Central Logging
Metrics
Distributed Tracing
```

where supported.

Mobile telemetry should include useful context without collecting unnecessary personal information.

---

# 84. PERFORMANCE

Monitor:

```text
App Startup
Screen Load
API Latency
Rendering
Memory
CPU
Battery
Network Usage
Database Queries
Crash Rate
ANR
```

where supported.

---

# 85. MEMORY MANAGEMENT

Avoid:

```text
Large In-memory Collections
Unbounded Caches
Leaking Contexts
Long-lived References
Unnecessary Bitmaps
```

Release resources appropriately.

---

# 86. IMAGE OPTIMIZATION

Use:

```text
Correct Resolution
Compression
Caching
Lazy Loading
Modern Formats
```

Avoid loading full-resolution images when thumbnails are sufficient.

---

# 87. NETWORK OPTIMIZATION

Minimize:

```text
Requests
Payload Size
Repeated Queries
Unnecessary Polling
```

Use:

```text
Pagination
Caching
Compression
Batching where appropriate
```

---

# 88. BATTERY

Background work must be battery-aware.

Avoid:

```text
Continuous Polling
High-frequency GPS
Unnecessary Wake Locks
Unbounded Background Jobs
```

Use platform scheduling mechanisms.

---

# 89. TESTING

Required where applicable:

```text
Unit Tests
Integration Tests
UI Tests
Navigation Tests
Accessibility Tests
Orientation Tests
Adaptive Layout Tests
Offline Tests
Network Failure Tests
Authentication Tests
Performance Tests
Memory Tests
```

---

# 90. DEVICE MATRIX

Before production release test representative:

```text
Small Phone
Large Phone
Tablet
Large Tablet
Foldable
Android TV where applicable
iOS Device where applicable
```

Test both:

```text
Portrait
Landscape
```

where supported.

---

# 91. ACCESSIBILITY TESTING

Test:

```text
Small Text
Large Text
Screen Reader
Keyboard / External Input
TV Remote
High Contrast
Focus Navigation
```

---

# 92. NETWORK FAILURE TESTING

Test:

```text
No Internet
Slow Internet
Intermittent Internet
Timeout
DNS Failure
Server 500
Server 503
Rate Limit
Expired Token
```

The application must fail gracefully.

---

# 93. OFFLINE TESTING

Test:

```text
Read Offline
Create Offline
Update Offline
Queue Operations
Reconnect
Synchronization
Conflict
Failed Synchronization
Retry
```

where offline functionality exists.

---

# 94. UI TESTING

Critical flows must be automated where practical:

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
Notifications
Logout
Tenant Switching
Permissions
```

---

# 95. BUILD VERIFICATION

Before declaring a milestone complete:

```text
Gradle Build
Static Analysis
Unit Tests
Integration Tests
UI Tests where applicable
```

Use the actual project commands.

Never invent build commands.

---

# 96. STATIC ANALYSIS

Use the project's configured tools.

Potential tools:

```text
Kotlin Compiler
Detekt
Ktlint
Android Lint
```

Do not introduce duplicate tooling unnecessarily.

---

# 97. DEPENDENCY MANAGEMENT

Before adding a dependency:

```text
Check existing dependencies
Check compatibility
Check maintenance
Check security
Check license
Check size/performance
Check platform support
```

Avoid dependency bloat.

---

# 98. GRADLE

Use centralized dependency management where appropriate.

Prefer:

```text
gradle/libs.versions.toml
```

for modern Gradle projects when compatible with the project.

Do not duplicate version declarations unnecessarily.

---

# 99. SECRETS

Never commit:

```text
API Secrets
Private Keys
Signing Credentials
Passwords
Tokens
Cloud Credentials
Keystore Passwords
```

Use secure CI/CD secret management.

---

# 100. APP CONFIGURATION

Separate:

```text
Development
Testing
Staging
Production
```

configuration.

Never embed production secrets in the application binary.

Remember:

**Mobile application binaries are inspectable.**

---

# 101. CI/CD

CI must verify as appropriate:

```text
Dependency Restore
Build
Lint
Static Analysis
Unit Tests
Integration Tests
UI Tests
Security Checks
Artifact Generation
```

Release pipelines should handle:

```text
Signing
Versioning
Environment Configuration
Artifact Publishing
```

securely.

---

# 102. RELEASE VERSIONING

Use consistent:

```text
Version Name
Version Code
Build Number
```

strategy.

Do not manually edit versions in multiple unrelated locations.

---

# 103. CRASH / ANR MONITORING

Production applications should monitor:

```text
Crashes
ANRs
Fatal Exceptions
Startup Failures
Critical API Errors
```

where supported by the observability platform.

---

# 104. API CONTRACTS

The mobile application must remain compatible with backend contracts consumed by:

```text
React
Angular
MAUI
Kotlin
```

Do not create backend contracts that are unnecessarily specific to Kotlin.

---

# 105. COMMUNICATION ABSTRACTION

Keep communication behind abstractions where appropriate:

```text
Application
 ↓
Communication Interface
 ↓
Provider
```

Possible providers:

```text
HTTP
gRPC
Gateway
BFF
Realtime Adapter
```

Switching the provider should not require rewriting business logic.

Do not build a fake abstraction simply to satisfy this rule.

---

# 106. NOTIFICATION COMMUNICATION

Notification architecture may use:

```text
Push
SignalR
WebSocket
SSE
Polling
```

depending on requirements.

Internal event brokers such as RabbitMQ remain backend infrastructure.

---

# 107. ERROR RECOVERY

For every important operation define:

```text
Success
Validation Failure
Authentication Failure
Authorization Failure
Network Failure
Timeout
Server Failure
Offline
Retry
Recovery
```

Do not design only the happy path.

---

# 108. UX CONSISTENCY

Use centralized:

```text
Colors
Typography
Spacing
Buttons
Inputs
Dialogs
Navigation
Errors
Loading
Notifications
```

Avoid feature-specific visual chaos.

---

# 109. DESIGN SYSTEM

Where appropriate maintain reusable:

```text
Buttons
Inputs
Cards
Tables
Dialogs
Navigation
Form Components
Loading Components
Error Components
Empty States
```

The design system must support responsive behavior.

---

# 110. NO PIXEL-HACKING

Do not fix responsive issues by repeatedly adding:

```text
Magic Numbers
Negative Margins
Device-specific Widths
Device Model Checks
Excessive Absolute Positioning
```

Solve the layout architecture instead.

---

# 111. RESPONSIVE QUALITY GATE

A feature cannot be considered complete if it works only on one device.

Before committing a UI milestone verify:

```text
[ ] Phone portrait
[ ] Phone landscape
[ ] Tablet portrait
[ ] Tablet landscape
[ ] Foldable where applicable
[ ] TV where applicable
[ ] Dynamic font size
[ ] Long text
[ ] Bangla text
[ ] Loading state
[ ] Empty state
[ ] Error state
[ ] Offline state where applicable
```

---

# 112. PERFORMANCE QUALITY GATE

Before committing:

```text
[ ] Startup acceptable
[ ] No obvious memory leaks
[ ] No excessive recomposition
[ ] No unnecessary API calls
[ ] No unbounded cache
[ ] Large lists optimized
[ ] Images optimized
[ ] Background jobs controlled
```

---

# 113. SECURITY QUALITY GATE

Before committing:

```text
[ ] No secrets in source
[ ] No tokens in logs
[ ] Secure token storage
[ ] Authentication handled centrally
[ ] Authorization handled
[ ] Tenant isolation respected
[ ] Deep links validated
[ ] WebViews reviewed
[ ] Sensitive screens reviewed
[ ] Network security reviewed
```

---

# 114. DOCUMENTATION

Maintain:

```text
docs/programmers-guide/
```

Include where applicable:

```text
Mobile Architecture
Project Structure
Kotlin/KMP Setup
Android Setup
iOS Setup
Adaptive UI
Responsive Design
Navigation
Authentication
Authorization
Localization
Offline Mode
Synchronization
Push Notifications
API Communication
Error Handling
Observability
Testing
Performance
Release
Troubleshooting
```

Documentation must remain concise and developer-friendly.

---

# 115. TROUBLESHOOTING

Document common failures such as:

```text
Gradle Build Failure
Dependency Conflict
Kotlin Version Conflict
Android SDK Problem
iOS Build Problem
Signing Problem
Network Failure
Authentication Failure
Offline Sync Failure
Push Notification Failure
Orientation Layout Failure
TV Focus Failure
```

Include:

```text
Actual Problem
Root Cause
Exact Location
Possible Solution
Recommended Practice
```

where known.

---

# 116. MILESTONE WORKFLOW

For every logical milestone:

```text
1. Inspect
2. Plan internally
3. Implement
4. Build
5. Test
6. Review
7. Fix
8. Update Documentation
9. Verify Git
10. Commit
11. Continue
```

Do not stop for unnecessary questions.

If a minor decision is missing:

```text
Choose the best enterprise-compatible solution
and continue.
```

Ask for approval only when a decision would materially affect:

```text
Shared Architecture
Shared Contracts
Shared Database
Other Services
Security Model
Public API
Existing Production Behavior
```

---

# 117. DO NOT MODIFY UNRELATED PROJECTS

When implementing a mobile feature:

Do not modify:

```text
Backend Services
Frontend Applications
Other Mobile Applications
Shared Infrastructure
Database
```

unless the change is explicitly required and architecturally approved.

---

# 118. DO NOT DELETE EXISTING FUNCTIONALITY

Never remove working functionality merely to simplify implementation.

If replacement is required:

```text
Implement
Test
Migrate
Verify
Then remove old code only when safe
```

---

# 119. NO FAKE IMPLEMENTATIONS

Never leave production functionality as:

```text
TODO
NotImplementedException
Fake API
Fake Repository
Hardcoded Production Data
Dummy Authentication
Fake Payment
```

unless explicitly documented as a temporary development stub.

---

# 120. FINAL PRODUCTION CHECKLIST

```text
[ ] Latest supported Kotlin ecosystem
[ ] Existing architecture inspected
[ ] Clean architecture where appropriate
[ ] Kotlin Coroutines
[ ] Flow / StateFlow where appropriate
[ ] ViewModel where appropriate
[ ] Kotlin Multiplatform where required
[ ] Android support
[ ] iOS support where required
[ ] Tablet support
[ ] Foldable support
[ ] Smart TV support where required
[ ] Portrait support
[ ] Landscape support
[ ] Adaptive layouts
[ ] Dynamic font scaling
[ ] Accessibility
[ ] TV focus navigation
[ ] Responsive forms
[ ] Responsive tables
[ ] Centralized navigation
[ ] State restoration
[ ] HTTP abstraction
[ ] gRPC where required
[ ] YARP compatibility
[ ] Ocelot compatibility
[ ] BFF compatibility
[ ] CorrelationId
[ ] TraceId
[ ] Centralized error handling
[ ] Result Pattern
[ ] Multi-error support
[ ] Retry policy
[ ] Timeout
[ ] Idempotency
[ ] Rate-limit handling
[ ] Authentication
[ ] OTP
[ ] 2FA
[ ] Forgot Password
[ ] Reset Password
[ ] Security Questions where required
[ ] Password History support
[ ] Secure token storage
[ ] Biometric support where required
[ ] Permission management
[ ] Module management
[ ] Tenant management
[ ] Company context
[ ] Organization context
[ ] Localization
[ ] Bangla
[ ] English
[ ] Future language support
[ ] Offline support where required
[ ] Local persistence
[ ] Synchronization
[ ] Conflict resolution
[ ] Push notifications
[ ] Realtime communication
[ ] Centralized logging
[ ] Exception logging
[ ] OpenTelemetry
[ ] Crash/ANR monitoring
[ ] Performance monitoring
[ ] Unit tests
[ ] Integration tests
[ ] UI tests
[ ] Accessibility tests
[ ] Orientation tests
[ ] Adaptive layout tests
[ ] Offline tests
[ ] Network failure tests
[ ] Security tests
[ ] Build verified
[ ] Static analysis passed
[ ] CI/CD verified
[ ] No secrets committed
[ ] Documentation updated
[ ] .git preserved
[ ] Professional Git commit created
```

---

# 121. FINAL ARCHITECTURE

Shared architecture:

```text
                 ┌──────────────────────┐
                 │   Kotlin Mobile App  │
                 └──────────┬───────────┘
                            │
                    Presentation Layer
                            │
                    ViewModel / UI
                            │
                    Application Layer
                            │
                         Use Cases
                            │
                       Domain Layer
                            │
                     Repository APIs
                            │
                 ┌──────────┴───────────┐
                 │                      │
          Local Data              Remote Data
                 │                      │
          Room / SQLDelight       HTTP / gRPC
                 │                      │
                 └──────────┬───────────┘
                            │
                     Gateway / BFF
                            │
                       Backend APIs
```

Cross-platform structure:

```text
shared/
├── commonMain/
│   ├── domain/
│   ├── application/
│   ├── data/
│   ├── networking/
│   ├── validation/
│   ├── authentication/
│   ├── authorization/
│   ├── synchronization/
│   └── telemetry/
│
├── androidMain/
│   └── platform implementations
│
└── iosMain/
    └── platform implementations
```

Adaptive UI:

```text
             Available Window
                    │
        ┌───────────┼───────────┐
        │           │           │
     Compact      Medium      Expanded
        │           │           │
      Phone       Tablet      Large
      Portrait    Tablet      Tablet
      Landscape   Landscape   TV
                              Foldable
```

Communication:

```text
Mobile
  │
  ├── HTTP
  ├── gRPC
  └── Gateway / BFF
          │
          ├── YARP
          └── Ocelot
                  │
             Backend Services
```

Realtime:

```text
Backend Events
      │
      ▼
Realtime Adapter
      │
      ├── SignalR
      ├── WebSocket
      ├── SSE
      └── Push Notification
              │
              ▼
        Kotlin Mobile App
```

Observability:

```text
Mobile
  │
  ├── CorrelationId
  ├── TraceId
  ├── Errors
  └── Metrics
          │
          ▼
    OpenTelemetry
          │
          ├── Logs
          ├── Metrics
          └── Traces
```

---

# 122. REUSABILITY REQUIREMENT

The architecture must remain reusable across products such as:

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
Field Operations
```

The mobile application must remain a presentation/client platform.

Business rules must not become tightly coupled to Android UI.

---

# 123. FINAL RULE

The AI must prioritize:

```text
Correctness
Security
Maintainability
Responsive UX
Cross-platform compatibility
Observability
Testing
Performance
Documentation
```

over:

```text
Fastest possible implementation
Shortest code
Temporary hacks
Device-specific fixes
Fake implementations
Unnecessary abstractions
```

When uncertain about a minor implementation detail:

```text
Inspect the existing project.
Follow established conventions.
Choose the most maintainable enterprise solution.
Implement it.
Verify it.
Document it.
Commit it.
Continue.
```

Never stop unnecessarily.

Never delete `.git`.

Never rewrite Git history.

Never knowingly introduce technical debt.

Never declare a feature complete without verification.

# END OF KOTLIN MULTIPLATFORM & MOBILE ENTERPRISE ENGINEERING RULES
