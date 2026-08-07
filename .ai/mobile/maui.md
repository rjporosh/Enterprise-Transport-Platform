# .NET MAUI — Enterprise Cross-Platform Application Engineering Rules

## 1. PURPOSE

This document defines production-grade engineering rules for .NET MAUI applications.

It applies to:

* Android Phones
* Android Tablets
* Android Foldables
* iPhone
* iPad
* macOS
* Windows PCs
* Windows Tablets
* Large-screen devices
* Desktop-class applications
* TV-class applications where supported through the selected platform/integration strategy

This document extends:

```text
.ai/MASTER-RULE.md
.ai/AI_RULES.md
.ai/communication.md
.ai/observability.md
.ai/testing-and-performance.md
.ai/mobile/maui.md
```

The shared platform rules remain authoritative.

This document defines .NET MAUI-specific implementation standards.

---

# 2. CORE PRINCIPLE

This is NOT a demo application.

All MAUI applications must be:

```text
Production-ready
Enterprise-grade
Secure
Maintainable
Testable
Observable
Accessible
Responsive
Scalable
Localization-ready
Cross-platform
```

Never optimize the application for only one device.

The application must behave correctly across supported:

```text
Devices
Screen Sizes
Orientations
Operating Systems
Input Methods
Accessibility Settings
Network Conditions
```

---

# 3. VERSION POLICY

Always use the latest stable .NET / .NET MAUI ecosystem available at implementation time.

Do NOT permanently lock this document to an old framework version.

The AI must inspect:

```text
global.json
*.csproj
Directory.Build.props
Directory.Build.targets
Directory.Packages.props
NuGet.config
MauiProgram.cs
App.xaml
AppShell.xaml
Platform/
```

and determine the actual project version.

Prefer the latest stable supported version unless the project has an explicit compatibility requirement.

Do not downgrade merely to avoid fixing compatibility problems.

---

# 4. EXISTING PROJECT FIRST

Before making changes:

1. Inspect the complete project.
2. Understand existing architecture.
3. Inspect project configuration.
4. Inspect dependencies.
5. Inspect navigation.
6. Inspect authentication.
7. Inspect networking.
8. Inspect persistence.
9. Inspect platform-specific code.
10. Inspect tests.
11. Inspect CI/CD.
12. Inspect documentation.

Reuse existing good conventions.

Do not redesign the entire application unnecessarily.

Do not modify unrelated services or applications.

---

# 5. GIT SAFETY

This is NON-NEGOTIABLE.

If `.git` exists:

```text
NEVER delete .git.
NEVER reinitialize Git.
NEVER rewrite history.
NEVER force-push history.
NEVER delete existing branches.
```

unless explicitly authorized.

If `.git` does not exist:

```bash
git init
```

Then create the appropriate initial professional commit.

---

# 6. PROFESSIONAL GIT COMMITS

After every logical milestone:

```text
Implement
→ Build
→ Test
→ Review
→ Fix
→ Documentation
→ Git status
→ Commit
→ Continue
```

Use Conventional Commit style.

Examples:

```text
feat(auth): implement OTP authentication flow

feat(ui): implement adaptive dashboard layout

feat(sync): implement offline synchronization

feat(notification): implement push notification handling

feat(platform): add Windows platform support

feat(platform): add macOS platform support

fix(auth): handle expired refresh sessions

fix(ui): resolve tablet landscape layout issue

perf(ui): optimize large list rendering

test(ui): add adaptive layout tests

docs(maui): document cross-platform architecture

chore(build): update .NET MAUI toolchain
```

Do not create meaningless commits.

Do not stop after each commit waiting for approval.

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

Typical structure:

```text
src/
├── Domain/
├── Application/
├── Infrastructure/
├── Presentation/
├── Platforms/
└── Resources/
```

The exact structure must follow the existing project where practical.

---

# 8. CLEAN ARCHITECTURE

Use Clean Architecture where justified.

Recommended:

```text
UI
 ↓
ViewModel
 ↓
Use Case / Application Service
 ↓
Repository Interface
 ↓
Repository Implementation
 ↓
Data Source
```

Business rules must not depend directly on:

```text
MAUI UI
Page
XAML
Android
iOS
Windows
macOS
```

---

# 9. MVVM

Prefer MVVM for MAUI applications.

Typical flow:

```text
Page
 ↓
ViewModel
 ↓
Command
 ↓
Use Case
 ↓
Repository
 ↓
API / Database
```

Do not put business logic inside:

```text
.xaml
.xaml.cs
```

except for genuinely UI-specific behavior.

---

# 10. COMMUNITY TOOLKIT

Use .NET MAUI Community Toolkit where it provides genuine value.

Potential capabilities include:

```text
Behaviors
Converters
Popups
Animations
Essentials helpers
```

Do not add libraries merely because they are popular.

Inspect the existing project before introducing dependencies.

---

# 11. RESPONSIVE DESIGN — HARD REQUIREMENT

A screen must NEVER be designed around a single phone resolution.

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
Desktop
Large Monitor
TV-class display
```

No:

```text
Clipping
Overlap
Off-screen controls
Broken navigation
Unreadable text
Overflow
Broken dialogs
Broken forms
```

is acceptable.

---

# 12. PLATFORM MATRIX

Design for:

```text
Android
iOS
iPadOS
macOS
Windows
```

where supported by the project's MAUI target configuration.

TV support is platform-dependent.

Do not claim that standard MAUI automatically provides native support for every Smart TV platform.

If TV support is required:

```text
Use an appropriate supported platform/integration strategy.
```

---

# 13. PHONE

Phone layouts should prioritize:

```text
Readable Content
Thumb Reach
Simple Navigation
Single-column Forms
Compact Lists
Bottom Navigation where appropriate
```

Do not blindly apply desktop layouts to phones.

---

# 14. TABLET

Tablet layouts should exploit additional space.

Use:

```text
Navigation Rail
Side Navigation
Two-pane layouts
Master-detail
Multi-column forms
Expanded tables
Adaptive grids
```

where appropriate.

Do not simply stretch the phone UI.

---

# 15. FOLDABLE

Support:

```text
Fold
Unfold
Posture Changes
Rotation
Window Resize
Multi-window
```

Do not assume a fixed display size.

---

# 16. WINDOWS

Windows applications must support:

```text
Resizable Window
Minimum Window Size
Maximum Useful Layout
Keyboard
Mouse
Touch where applicable
```

The UI must remain usable when the window is resized.

Test:

```text
Small Window
Normal Window
Large Window
Maximized Window
```

---

# 17. MACOS

macOS applications must support:

```text
Resizable Windows
Keyboard
Mouse / Trackpad
Menus where appropriate
Desktop navigation patterns
Large displays
```

Do not simply ship a phone UI inside a Mac window.

---

# 18. IPAD

iPad layouts must support:

```text
Portrait
Landscape
Split View where applicable
Resizable layouts
Keyboard
Pointer
Large-screen navigation
```

---

# 19. TV-CLASS APPLICATIONS

TV interfaces are fundamentally different from touch interfaces.

Where TV support is implemented, require:

```text
Remote Control
D-pad
Focus Navigation
Large Typography
Large Controls
Landscape
High Visibility
Focus Indicators
```

Every actionable element must be reachable without touch.

---

# 20. ORIENTATION

All applicable screens must support:

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

Do not lose user input during rotation.

---

# 21. ADAPTIVE UI

Prefer available-space-based layouts.

Do NOT create:

```csharp
if (width == 390)
{
    ...
}
```

Prefer:

```text
Window Size
Device Idiom
Display Density
Available Space
Adaptive Layout
```

Use device-specific conditions only when genuinely necessary.

---

# 22. RESPONSIVE BREAKPOINTS

Use conceptual breakpoints such as:

```text
Compact
Medium
Expanded
```

rather than hardcoding device models.

Example:

```text
Compact
→ Phone layout

Medium
→ Tablet / small desktop layout

Expanded
→ Large tablet / desktop layout
```

---

# 23. SINGLE SCREEN REQUIREMENT

A logical screen should remain reusable across supported form factors.

Example:

```text
Dashboard
```

should adapt between:

```text
Phone
Tablet
Windows
macOS
```

without maintaining unnecessary duplicate implementations.

Separate platform implementations are allowed when interaction models genuinely differ.

---

# 24. RESPONSIVE TYPOGRAPHY

Support:

```text
Dynamic Font Scaling
Accessibility Text Scaling
Different DPI
Different Screen Sizes
Long Text
Bangla Text
English Text
Future Languages
```

Never assume English text length.

---

# 25. RESPONSIVE FORMS

Phone:

```text
Single Column
```

Tablet:

```text
Two Columns where appropriate
```

Desktop:

```text
Multi-column where useful
```

TV:

```text
Large Controls
Focus Navigation
```

No overlapping controls.

---

# 26. RESPONSIVE TABLES

Phone:

```text
Cards
Compact Lists
Horizontal scrolling only when justified
```

Tablet/Desktop:

```text
Expanded Grid
Columns
Sorting
Filtering
Pagination
```

TV:

```text
Large readable rows
Focus navigation
```

Never force desktop-sized tables into phone screens.

---

# 27. NAVIGATION

Navigation must be:

```text
Predictable
Restorable
Accessible
Deep-linkable where required
Responsive
Platform-aware
```

Use appropriate patterns:

```text
Shell
Navigation
Flyout
Tabbed Navigation
NavigationPage
Custom Navigation
```

according to the project architecture.

---

# 28. BACK NAVIGATION

Android:

```text
Back Gesture
Back Button
System Navigation
```

iOS:

```text
Navigation Stack
Swipe Back where applicable
```

Windows/macOS:

```text
Window Navigation
Keyboard Navigation
```

TV:

```text
Back / Remote Navigation
```

Never break native navigation expectations.

---

# 29. STATE MANAGEMENT

Critical state must survive appropriate lifecycle events.

Consider:

```text
Rotation
Backgrounding
Foregrounding
Process Recreation
Navigation
```

Do not keep critical data only inside page memory.

---

# 30. MVVM STATE

Represent state explicitly:

```text
Loading
Success
Empty
Error
Refreshing
Saving
Deleting
Uploading
```

Avoid large collections of unrelated Boolean properties.

---

# 31. ASYNC PROGRAMMING

Use modern async/await.

Avoid blocking:

```csharp
.Result
.Wait()
```

on UI threads.

Avoid unnecessary fire-and-forget tasks.

Use cancellation tokens where appropriate.

---

# 32. CANCELLATION

Long-running operations should support:

```text
CancellationToken
```

Examples:

```text
Search
Upload
Download
API Request
Synchronization
Large Processing
```

---

# 33. NETWORKING

Never call APIs directly from UI pages.

Preferred:

```text
Page
 ↓
ViewModel
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

---

# 34. HTTP

Use the existing project HTTP abstraction.

Possible technology:

```text
HttpClient
IHttpClientFactory
Refit
Other approved client
```

Do not introduce multiple HTTP frameworks unnecessarily.

---

# 35. GRPC

Where required, support:

```text
gRPC
```

behind an application-level abstraction where practical.

Do not spread gRPC-specific types throughout the domain layer.

---

# 36. API GATEWAY

Mobile applications must remain compatible with:

```text
YARP
Ocelot
BFF
API Gateway
```

Typical:

```text
MAUI
 ↓
Gateway / BFF
 ↓
Backend Services
```

The client must not depend on internal microservice topology.

---

# 37. COMMUNICATION ABSTRACTION

Use abstraction where useful:

```text
Application
 ↓
ICommunicationProvider
 ↓
Provider
```

Possible providers:

```text
HTTP
gRPC
Gateway
BFF
Realtime
```

Changing communication provider should not require rewriting business logic.

Do not create unnecessary abstractions.

---

# 38. EVENT-DRIVEN COMMUNICATION

MAUI clients should not directly connect to:

```text
RabbitMQ
Kafka
Internal Brokers
```

Backend events should reach clients through:

```text
Backend Event
 ↓
Realtime Adapter
 ↓
SignalR / WebSocket / SSE / Push
 ↓
MAUI
```

---

# 39. REALTIME

Where required:

```text
SignalR
WebSocket
SSE
Push Notification
```

Potential use cases:

```text
Booking Updates
Transport Tracking
Payment Status
Notifications
Operational Alerts
Live Dashboard
```

---

# 40. PUSH NOTIFICATIONS

Support where required:

```text
Android Push
iOS Push
Notification Actions
Deep Links
Unread Counts
Notification State
```

Do not make business-critical state dependent solely on notification delivery.

---

# 41. CORRELATION ID

Every request should support:

```text
CorrelationId
TraceId
RequestId
```

Typical:

```text
MAUI
 ↓
Gateway
 ↓
Service
 ↓
Database
```

The request must remain traceable across the system.

---

# 42. RESULT PATTERN

Use the backend's standardized Result Pattern.

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

Display all actionable validation errors.

Do not display only the first error if multiple errors are provided.

---

# 43. CENTRALIZED ERROR HANDLING

Handle centrally:

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
Timeout
Network Failure
Serialization Failure
Unknown Failure
```

Never duplicate identical error handling in every page.

---

# 44. GRACEFUL USER MESSAGES

Users must never see:

```text
Stack Trace
SQL
Exception Type
Internal File Path
Internal Server Details
```

Instead display a safe localized message.

Developer diagnostics belong in logs.

---

# 45. RETRY POLICY

Retry only transient failures:

```text
Timeout
Temporary Network Failure
503
504
```

Do not retry automatically:

```text
400
401
403
404
409
422
```

Never blindly retry financial or destructive operations.

---

# 46. IDEMPOTENCY

Use:

```text
Idempotency-Key
```

for operations where duplicate execution is dangerous:

```text
Payment
Booking
Ticket Purchase
Order
Refund
Financial Transaction
```

---

# 47. RATE LIMITING

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

Never create uncontrolled retry loops.

---

# 48. AUTHENTICATION

Support the centralized authentication system:

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

# 49. TOKEN STORAGE

Never store sensitive tokens in plain preferences.

Use platform-secure storage.

Possible mechanisms:

```text
Android Keystore
iOS Keychain
Windows secure storage
macOS secure storage
MAUI SecureStorage
```

where appropriate.

---

# 50. TWO-FACTOR AUTHENTICATION

Support:

```text
OTP
Authenticator
SMS
Email
Biometric verification where appropriate
```

Never log:

```text
Password
OTP
Access Token
Refresh Token
Security Answer
```

---

# 51. BIOMETRICS

Support where required:

```text
Fingerprint
Face Authentication
Device Biometrics
```

Biometric authentication should unlock or authorize secure application operations.

Backend authorization remains authoritative.

---

# 52. SESSION MANAGEMENT

Typical:

```text
API → 401
 ↓
Refresh Token
 ↓
Retry safe request
 ↓
Refresh fails
 ↓
Clear Session
 ↓
Navigate to Login
```

Prevent infinite refresh loops.

---

# 53. AUTHORIZATION

Support:

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
payments.read
payments.create
payments.refund
reports.export
```

Client-side permissions control UX.

Backend permissions enforce security.

---

# 54. MULTI-TENANCY

Where applicable:

```text
TenantId
CompanyId
OrganizationId
BranchId
```

must be handled consistently.

Never trust arbitrary tenant identifiers supplied by the user.

---

# 55. TENANT SWITCHING

When tenant/company changes:

```text
Select Tenant
 ↓
Update Context
 ↓
Refresh Permissions
 ↓
Clear/Invalidate Tenant Data
 ↓
Reload Data
```

Never show stale data belonging to another tenant.

---

# 56. LOCALIZATION

Minimum:

```text
English
Bangla
```

Architecture must support future languages.

Never hardcode user-facing strings.

---

# 57. LANGUAGE SELECTION

Support:

```text
Device Language
User Preference
Tenant Preference
Explicit Selection
```

according to requirements.

Fallback:

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

# 58. INTERNATIONALIZATION

Support:

```text
Bangla
English
Future Languages
Long Text
Date Formats
Time Formats
Number Formats
Currency
RTL where required
```

Never assume English layout.

---

# 59. DATE / TIME

Clearly distinguish:

```text
UTC
Server Time
Tenant Time Zone
Device Time Zone
```

Do not silently convert timestamps.

---

# 60. CURRENCY

Use:

```text
Currency Code
Locale
Tenant Configuration
```

Never hardcode currency symbols throughout the application.

---

# 61. OFFLINE-FIRST

Where business requirements require offline capability:

```text
UI
 ↓
Use Case
 ↓
Repository
 ↓
Local Database
 ↓
Sync Engine
 ↓
Remote API
```

---

# 62. LOCAL DATABASE

Use the project's chosen storage solution.

Potential options:

```text
SQLite
EF Core SQLite
sqlite-net
Realm
Other approved storage
```

Do not introduce multiple persistence frameworks without justification.

---

# 63. CACHE

Define:

```text
Cache Duration
Invalidation
Synchronization
Sensitive Data Handling
Maximum Size
```

Do not cache sensitive data indefinitely.

---

# 64. OFFLINE QUEUE

For safe operations:

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
Complete
```

Financial operations require special idempotency handling.

---

# 65. CONFLICT RESOLUTION

When local/server state differs:

```text
Detect
 ↓
Classify
 ↓
Resolve
 ↓
Notify User if necessary
 ↓
Persist
```

Never silently overwrite important user data.

---

# 66. BACKGROUND PROCESSING

Use platform-appropriate mechanisms.

Android:

```text
WorkManager where appropriate
```

iOS:

```text
Platform-supported background execution
```

Windows/macOS:

```text
Platform-appropriate background mechanisms
```

Never assume unrestricted background execution.

---

# 67. BACKGROUND RETRY

Background jobs should support:

```text
Retry
Backoff
Cancellation
Network Constraints
Failure Handling
```

Avoid endless retry loops.

---

# 68. DEVICE APIs

Platform-specific capabilities should be isolated where practical:

```text
Camera
GPS
Bluetooth
NFC
Biometrics
Files
Contacts
Notifications
```

Use dependency injection/platform abstractions.

---

# 69. PERMISSIONS

Request only required permissions.

Handle:

```text
Granted
Denied
Restricted
Permanently Denied
```

Provide a graceful recovery path.

Never repeatedly ask for a denied permission without explaining why it is needed.

---

# 70. LOCATION

If required:

```text
Minimum Permission
Purpose Explanation
Permission Denial
Approximate Location
Background Restrictions
```

Never collect location without business justification.

---

# 71. ACCESSIBILITY

Support:

```text
Screen Readers
TalkBack
VoiceOver
Keyboard
Mouse
Touch
Large Text
High Contrast
Focus Navigation
```

where applicable.

---

# 72. TV ACCESSIBILITY

TV-class applications must support:

```text
D-pad
Remote
Visible Focus
Logical Focus Order
Large Text
Large Controls
```

where the target platform supports it.

---

# 73. WEBVIEW

If WebView is required:

```text
Restrict Navigation
Validate URLs
Avoid Secrets
Avoid Untrusted Content
Control JavaScript
```

Do not use WebView as a shortcut for the entire application.

---

# 74. DEEP LINKS

Deep links must:

```text
Validate Input
Check Authentication
Check Authorization
Check Tenant Context
Handle Expired Sessions
Prevent Open Redirects
```

Never trust deep-link parameters.

---

# 75. LOGGING

Centralized logs should contain useful diagnostic context:

```text
Timestamp
Application
Version
Platform
Device
OS
Screen
Entry Point
Endpoint
Method
File
File Location where available
Line Number where available
Exception
Root Cause
Possible Solution
CorrelationId
TraceId
```

Never log:

```text
Password
OTP
Tokens
Security Answers
Private Keys
Secrets
Sensitive Personal Data
```

---

# 76. EXCEPTION LOGGING

Centralized exception handling must provide enough diagnostic information to locate failures.

Include where available:

```text
Entry Point
Endpoint
Screen
Method Name
File Name
File Location
Line Number
Root Cause
Exact Exception Message
Possible Solution
Best Practice
CorrelationId
TraceId
Timestamp
```

Never expose internal diagnostics to end users.

---

# 77. OBSERVABILITY

Use where supported:

```text
OpenTelemetry
Centralized Logging
Metrics
Distributed Tracing
```

Potential backend visualization:

```text
Jaeger
Prometheus
Grafana
Seq
Kibana
Graylog
```

Mobile telemetry must not collect unnecessary personal information.

---

# 78. PERFORMANCE

Monitor:

```text
Startup Time
Screen Load
API Latency
Rendering
Memory
CPU
Battery
Network Usage
Database Operations
Crash Rate
```

---

# 79. MEMORY

Avoid:

```text
Large In-memory Collections
Unbounded Caches
Event Handler Leaks
Long-lived References
Large Images
```

Dispose resources correctly.

---

# 80. IMAGE PERFORMANCE

Use:

```text
Lazy Loading
Caching
Correct Resolution
Compression
Thumbnailing
```

Never load full-resolution images unnecessarily.

---

# 81. NETWORK PERFORMANCE

Minimize:

```text
API Requests
Payload Size
Repeated Calls
Polling
```

Prefer:

```text
Pagination
Caching
Compression
Batching
Realtime Updates where justified
```

---

# 82. BATTERY

Avoid:

```text
Continuous Polling
Unnecessary GPS
Unnecessary Background Tasks
Wake Locks
High-frequency synchronization
```

Use platform scheduling.

---

# 83. SEARCH

Support:

```text
Debouncing
Cancellation
Server-side Search
Pagination
Loading
Empty
Error
```

Do not download massive datasets to search locally unless explicitly justified.

---

# 84. PAGINATION

Use:

```text
Page
Page Size
Cursor where supported
Total Count where appropriate
```

Never load millions of records into memory.

---

# 85. FORM VALIDATION

Validate:

```text
Client
+
Server
```

Client validation improves UX.

Server validation remains authoritative.

---

# 86. MULTIPLE ERRORS

If backend returns:

```json
{
  "errors": [
    {},
    {},
    {}
  ]
}
```

display/map all actionable errors.

Do not hide useful validation failures.

---

# 87. LOADING STATES

Every important asynchronous operation should have:

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

# 88. EMPTY STATES

Support:

```text
No Data
No Search Results
No Permission
Offline
Failure
```

Never leave a screen blank without context.

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

# 90. DEVICE TEST MATRIX

Test representative:

```text
Small Android Phone
Large Android Phone
Android Tablet
Foldable
iPhone
iPad
Windows PC
Windows Tablet
macOS
Large Monitor
TV-class platform where applicable
```

Test:

```text
Portrait
Landscape
```

where supported.

---

# 91. UI TESTING

Critical flows should be automated:

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
Tenant Switching
Permissions
Logout
```

---

# 92. ACCESSIBILITY TESTING

Test:

```text
Small Text
Large Text
Screen Reader
Keyboard
Mouse
Touch
TV Remote
Focus
High Contrast
```

where applicable.

---

# 93. NETWORK FAILURE TESTING

Test:

```text
No Internet
Slow Internet
Intermittent Internet
Timeout
DNS Failure
500
503
429
Expired Token
```

The application must fail gracefully.

---

# 94. OFFLINE TESTING

Where supported:

```text
Read Offline
Create Offline
Update Offline
Queue
Reconnect
Synchronize
Conflict
Failed Sync
Retry
```

---

# 95. BUILD VERIFICATION

Before declaring a milestone complete:

```text
dotnet restore
dotnet build
dotnet test
```

Use the project's actual configuration and target frameworks.

Never invent commands when project-specific commands already exist.

---

# 96. STATIC ANALYSIS

Use configured tools such as:

```text
Roslyn Analyzers
.NET Analyzers
Sonar analyzers where configured
StyleCop where configured
```

Do not introduce redundant tooling unnecessarily.

---

# 97. DEPENDENCY MANAGEMENT

Before adding a NuGet package:

```text
Check Existing Packages
Check Compatibility
Check Security
Check Maintenance
Check License
Check Size
Check Platform Support
```

Avoid dependency bloat.

---

# 98. SECRETS

Never commit:

```text
Passwords
API Keys
Private Keys
Certificates
Tokens
Cloud Credentials
Signing Credentials
```

Use secure configuration and CI/CD secrets.

---

# 99. PLATFORM CONFIGURATION

Separate:

```text
Development
Testing
Staging
Production
```

configuration.

Never embed production secrets inside the app.

Remember:

**Client applications can be reverse engineered.**

---

# 100. CI/CD

CI should verify where applicable:

```text
Restore
Build
Lint / Static Analysis
Unit Tests
Integration Tests
UI Tests
Security Checks
Artifact Generation
```

Release pipeline should securely manage:

```text
Signing
Versioning
Environment Configuration
Artifact Publishing
```

---

# 101. RELEASE MANAGEMENT

Maintain consistent:

```text
Version
Build Number
Release Channel
Environment
```

Do not manually update versions in multiple unrelated files.

---

# 102. CRASH / ANR / HANG MONITORING

Monitor production failures:

```text
Crash
Unhandled Exception
ANR
Startup Failure
Application Hang
Critical API Failure
```

where supported by the observability platform.

---

# 103. API CONTRACT COMPATIBILITY

The MAUI application must consume standardized backend contracts.

Backend contracts should remain reusable by:

```text
Angular
React
MAUI
Kotlin
```

Do not create unnecessarily MAUI-specific backend APIs.

---

# 104. DESIGN SYSTEM

Maintain reusable components:

```text
Button
Input
Card
Dialog
Navigation
List
Table
Form
Loading
Error
Empty State
Notification
```

Components must support responsive layouts.

---

# 105. NO PIXEL HACKING

Do not solve responsive issues through:

```text
Magic Numbers
Negative Margins
Device Model Checks
Excessive Absolute Positioning
Hardcoded Screen Widths
```

Fix the layout architecture.

---

# 106. RESPONSIVE QUALITY GATE

A UI milestone is incomplete until verified against:

```text
[ ] Phone portrait
[ ] Phone landscape
[ ] Tablet portrait
[ ] Tablet landscape
[ ] Foldable
[ ] iPad
[ ] Windows resize
[ ] macOS resize
[ ] Large display
[ ] TV-class target where applicable
[ ] Dynamic text
[ ] Bangla text
[ ] Long text
[ ] Loading
[ ] Empty
[ ] Error
[ ] Offline where applicable
```

---

# 107. PERFORMANCE QUALITY GATE

Before committing:

```text
[ ] Startup acceptable
[ ] Screen transitions acceptable
[ ] No obvious memory leaks
[ ] No unnecessary API calls
[ ] Large lists optimized
[ ] Images optimized
[ ] Background work controlled
[ ] Network usage reasonable
```

---

# 108. SECURITY QUALITY GATE

Before committing:

```text
[ ] No secrets in source
[ ] No tokens in logs
[ ] Secure storage
[ ] Authentication centralized
[ ] Authorization respected
[ ] Tenant isolation respected
[ ] Deep links validated
[ ] WebView reviewed
[ ] Sensitive data protected
[ ] Network security reviewed
```

---

# 109. DOCUMENTATION

Maintain:

```text
docs/programmers-guide/
```

Include:

```text
MAUI Architecture
Project Structure
Platform Setup
Android Setup
iOS Setup
iPad Setup
Windows Setup
macOS Setup
TV Strategy
Adaptive UI
Responsive Design
Navigation
Authentication
Authorization
Localization
Offline Mode
Synchronization
API Communication
Error Handling
Observability
Testing
Performance
Release
Troubleshooting
```

Documentation must be concise and developer-friendly.

---

# 110. TROUBLESHOOTING

Document:

```text
.NET SDK Failure
MAUI Workload Failure
NuGet Failure
Android Build Failure
iOS Build Failure
macOS Build Failure
Windows Build Failure
Signing Failure
Provisioning Failure
Network Failure
Authentication Failure
Push Notification Failure
Responsive Layout Failure
Orientation Failure
TV Focus Failure
```

For each known issue include:

```text
Problem
Root Cause
Exact Location
Possible Solution
Recommended Practice
```

---

# 111. MILESTONE WORKFLOW

For every logical feature:

```text
1. Inspect
2. Plan
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

Minor decisions should be made automatically using:

```text
Existing Project Conventions
Enterprise Best Practices
Security
Maintainability
Compatibility
```

Ask for approval only if the change materially affects:

```text
Shared Architecture
Shared Contracts
Shared Database
Other Services
Security Architecture
Public APIs
Existing Production Behavior
```

---

# 112. NO UNNECESSARY QUESTIONS

Do not stop for:

```text
Naming preference
Minor implementation choice
Routine library choice
Small UI decision
Obvious enterprise convention
```

Make the best compatible decision and continue.

---

# 113. NO FAKE IMPLEMENTATIONS

Never leave production functionality as:

```text
TODO
NotImplementedException
Fake API
Fake Repository
Dummy Authentication
Fake Payment
Hardcoded Production Data
```

unless explicitly documented as a temporary development stub.

---

# 114. DO NOT DELETE EXISTING FUNCTIONALITY

Never remove working features simply to simplify implementation.

If replacement is required:

```text
Implement
→ Test
→ Migrate
→ Verify
→ Remove safely
```

---

# 115. FINAL PRODUCTION CHECKLIST

```text
[ ] Latest supported .NET / MAUI ecosystem
[ ] Existing project inspected
[ ] Clean Architecture where appropriate
[ ] MVVM
[ ] Dependency Injection
[ ] Android
[ ] iOS
[ ] iPadOS
[ ] macOS
[ ] Windows
[ ] Tablet
[ ] Foldable
[ ] Large Screen
[ ] TV strategy where applicable
[ ] Portrait
[ ] Landscape
[ ] Adaptive Layout
[ ] Responsive Typography
[ ] Accessibility
[ ] Keyboard
[ ] Mouse
[ ] Touch
[ ] Remote / D-pad where applicable
[ ] Centralized Navigation
[ ] State Restoration
[ ] HTTP
[ ] gRPC where required
[ ] YARP compatibility
[ ] Ocelot compatibility
[ ] BFF compatibility
[ ] CorrelationId
[ ] TraceId
[ ] Result Pattern
[ ] Centralized Errors
[ ] Multi-error support
[ ] Retry
[ ] Timeout
[ ] Idempotency
[ ] Rate-limit handling
[ ] Authentication
[ ] OTP
[ ] 2FA
[ ] Forgot Password
[ ] Reset Password
[ ] Security Questions
[ ] Password History
[ ] Secure Token Storage
[ ] Biometrics
[ ] Authorization
[ ] Permission Management
[ ] Module Management
[ ] Multi-tenancy
[ ] Company Context
[ ] Organization Context
[ ] Branch Context
[ ] Localization
[ ] Bangla
[ ] English
[ ] Future Language Support
[ ] Offline Support where required
[ ] Local Persistence
[ ] Synchronization
[ ] Conflict Resolution
[ ] Push Notifications
[ ] Realtime Communication
[ ] Centralized Logging
[ ] Exception Logging
[ ] OpenTelemetry
[ ] Crash Monitoring
[ ] Performance Monitoring
[ ] Unit Tests
[ ] Integration Tests
[ ] UI Tests
[ ] Accessibility Tests
[ ] Orientation Tests
[ ] Responsive Tests
[ ] Offline Tests
[ ] Network Failure Tests
[ ] Security Tests
[ ] Build Verified
[ ] Static Analysis
[ ] CI/CD
[ ] No Secrets
[ ] Documentation Updated
[ ] .git Preserved
[ ] Professional Git Commit
```

---

# 116. REUSABILITY

The MAUI architecture should be reusable across:

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

Business logic must remain independent of individual platform UI implementations.

---

# 117. FINAL ARCHITECTURE

```text
                         MAUI Application
                                │
                    ┌───────────┴───────────┐
                    │                       │
              Presentation             Platform
                    │                       │
                ViewModel              Android
                    │                   iOS/iPadOS
               Application             Windows
                    │                   macOS
                 Use Cases                TV*
                    │
                  Domain
                    │
              Repository APIs
                    │
             ┌──────┴──────┐
             │             │
          Local         Remote
             │             │
         SQLite/etc.   HTTP/gRPC
                           │
                       Gateway/BFF
                           │
                     Backend Services
```

TV support is platform/integration dependent and must be implemented only where the selected MAUI/platform stack supports it appropriately.

---

# 118. FINAL RULE

The AI must prioritize:

```text
Correctness
Security
Maintainability
Responsive UX
Cross-platform Compatibility
Accessibility
Observability
Testing
Performance
Documentation
```

over:

```text
Fastest implementation
Shortest code
Temporary hacks
Device-specific fixes
Fake implementations
Unnecessary abstractions
```

When a minor decision is unclear:

```text
Inspect the existing project.
Follow existing conventions.
Choose the latest compatible stable technology.
Choose the simplest enterprise-grade solution.
Implement.
Build.
Test.
Review.
Document.
Commit.
Continue.
```

Never delete `.git`.

Never rewrite Git history.

Never knowingly introduce technical debt.

Never declare a feature complete without verification.

# END OF .NET MAUI ENTERPRISE ENGINEERING RULES
