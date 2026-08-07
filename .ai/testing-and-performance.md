# testing-and-performance.md

# Enterprise Testing & Performance Standard

## 1. Purpose

Every service must be production-tested before being considered complete.

Testing must cover:

```text
Unit Tests
Integration Tests
API Tests
Contract Tests
Load Tests
Stress Tests
Performance Tests
Resilience Tests
Security Tests where applicable
```

Performance testing must be reproducible and documented.

---

# 2. Test Structure

Each service should follow:

```text
tests/
├── Unit/
├── Integration/
├── API/
├── Contract/
└── load-test/
    ├── nbomber/
    ├── k6/
    └── jmeter/
```

Use the repository's existing naming conventions when they already exist.

Do not duplicate an existing test structure unnecessarily.

---

# 3. Testing Pyramid

Preferred order:

```text
                 E2E
                /   \
           Integration
             /       \
           API       Contract
             \       /
              Unit Tests
```

Performance testing is a separate layer:

```text
Functional Tests
       ↓
Performance Tests
       ↓
Load / Stress Tests
```

---

# 4. Unit Testing

Unit tests verify isolated business behavior.

Test:

```text
Domain Rules
Validators
Handlers
Services
Mappers
Result Pattern
Business Rules
Utility Logic
```

Unit tests should not require:

```text
PostgreSQL
RabbitMQ
Redis
External APIs
```

unless specifically testing infrastructure integration.

---

# 5. Unit Test Requirements

For important business logic cover:

```text
Happy Path
Invalid Input
Boundary Conditions
Null/Empty Values
Business Rule Violations
Concurrency-sensitive logic where applicable
Exception Paths
```

Tests must be deterministic.

Avoid tests that depend on:

```text
Current time
Random values
Network
External systems
```

unless controlled explicitly.

---

# 6. Integration Testing

Integration tests verify real component interaction.

Examples:

```text
Application
+
PostgreSQL

Application
+
Redis

Application
+
RabbitMQ
```

Use isolated test infrastructure where possible.

Docker containers are preferred when practical.

---

# 7. API Testing

API tests must verify:

```text
HTTP Status
Response Schema
Result Pattern
Validation
Authentication
Authorization
Pagination
Filtering
Sorting
Error Handling
CorrelationId
Localization
Rate Limiting where applicable
```

---

# 8. API Error Testing

Test:

```text
400
401
403
404
409
422 where applicable
429
500
502/503 where applicable
```

The exact status code must follow the platform's centralized error-handling standard.

---

# 9. Result Pattern Testing

Verify that multiple validation errors can be returned together.

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

Do not stop after the first validation error when the validation framework supports collecting multiple errors.

---

# 10. Authentication Tests

For authenticated services test:

```text
Valid Token
Expired Token
Invalid Token
Missing Token
Wrong Audience
Wrong Issuer
Insufficient Permission
Wrong Tenant
Disabled User
Disabled Organization
```

---

# 11. Authorization Tests

Verify:

```text
Role
Permission
Module
Tenant
Company
Organization
Resource ownership
```

Never test only the happy path.

Explicitly verify that unauthorized users cannot access protected resources.

---

# 12. Multi-Tenant Tests

For SaaS services verify:

```text
Tenant A cannot access Tenant B
Company A cannot access Company B
Organization A cannot access Organization B
```

Test both:

```text
Read
Write
Update
Delete
```

where applicable.

---

# 13. Database Tests

Test:

```text
CRUD
Constraints
Indexes
Transactions
Optimistic Concurrency
Soft Delete
Pagination
Filtering
Search
Migration
```

Verify database-generated errors are converted into the centralized error model.

---

# 14. Concurrency Tests

Test concurrent operations where applicable.

Examples:

```text
Two users update the same entity
Two requests use the same Idempotency-Key
Two consumers process the same event
Two workers execute the same job
```

Expected behavior must be deterministic and safe.

---

# 15. Idempotency Tests

For an idempotent operation:

```text
Request A
Idempotency-Key = X
```

followed by:

```text
Request B
Idempotency-Key = X
```

must not duplicate the business side effect.

Also test:

```text
Same key + different payload
```

and verify the request is rejected appropriately.

---

# 16. Communication Tests

Test each configured communication mechanism independently.

### HTTP

Test:

```text
Success
Timeout
4xx
5xx
Retry
Circuit Breaker
Correlation
Trace propagation
```

### gRPC

Test:

```text
Success
Timeout
Cancellation
Metadata
Error handling
Correlation
Trace propagation
```

### RabbitMQ

Test:

```text
Publish
Consume
Duplicate event
Retry
Dead Letter
Outbox
Inbox
Correlation
Trace propagation
```

---

# 17. Resilience Tests

Verify:

```text
Retry
Timeout
Circuit Breaker
Rate Limiting
Fallback where applicable
```

Do not retry permanent failures.

---

# 18. Retry Test

Simulate:

```text
Transient Failure
↓
Retry
↓
Success
```

and:

```text
Persistent Failure
↓
Retry limit
↓
Controlled failure
```

Verify that retries do not create duplicate side effects.

---

# 19. Circuit Breaker Test

Verify:

```text
Healthy
↓
Repeated failures
↓
Circuit Opens
↓
Requests rejected quickly
↓
Recovery period
↓
Half Open
↓
Successful request
↓
Circuit Closed
```

---

# 20. RabbitMQ Tests

Verify:

```text
Producer
Consumer
Acknowledgement
Retry
Duplicate delivery
Dead Letter Queue
Outbox
Inbox
```

Never mark RabbitMQ integration as verified if RabbitMQ was not actually available.

---

# 21. Quartz / Background Job Tests

Every important background job should test:

```text
Normal execution
Failure
Retry
Cancellation
Duplicate execution
Concurrency
Misfire behavior where applicable
```

Jobs must be safe to execute more than once.

---

# 22. Contract Testing

For shared APIs/events/gRPC contracts:

Verify:

```text
Producer contract
Consumer expectations
Schema compatibility
Version compatibility
Required fields
Optional fields
```

Prefer additive contract changes.

---

# 23. Test Data

Test data must be:

* Deterministic
* Isolated
* Reproducible
* Safe
* Easy to reset

Never use real production secrets or customer data.

---

# 24. Test Configuration

Separate:

```text
appsettings.Test.json
```

or the repository's established test configuration mechanism.

Never use production credentials.

---

# 25. Test Isolation

Tests should not depend on execution order.

Bad:

```text
Test A creates record
↓
Test B assumes record exists
```

Good:

```text
Test B creates its own required state
```

---

# 26. Performance Testing

Performance testing is mandatory for production-critical services.

Use:

```text
NBomber
k6
JMeter
```

for different purposes.

---

# 27. NBomber

Use NBomber for:

* .NET-centric load testing
* Programmatic scenarios
* Service-level performance testing
* High-throughput test scenarios
* Custom .NET integration

Recommended structure:

```text
tests/load-test/nbomber/
```

---

# 28. k6

Use k6 for:

* HTTP load testing
* API load testing
* Stress testing
* Spike testing
* Threshold-based CI testing

Recommended structure:

```text
tests/load-test/k6/
```

---

# 29. JMeter

Use JMeter for:

* API performance testing
* Complex request workflows
* Parameterized scenarios
* Enterprise performance test plans
* Performance reporting

Recommended structure:

```text
tests/load-test/jmeter/
```

---

# 30. Why Three Tools

Do not duplicate the exact same test blindly.

Use the tools according to their strengths.

```text
NBomber
→ .NET/service-centric load testing

k6
→ Scriptable API load/stress testing

JMeter
→ Enterprise performance scenarios and test plans
```

A service may use all three when the project requires independent verification.

---

# 31. Load Test

Load testing verifies expected production traffic.

Example:

```text
Normal expected traffic
+
Peak expected traffic
```

Measure:

```text
Requests/sec
Latency
Error Rate
CPU
Memory
Database
Network
Queue
```

---

# 32. Stress Test

Stress testing intentionally exceeds expected capacity.

Example:

```text
100 RPS
200 RPS
500 RPS
1000 RPS
...
```

until the service reaches a controlled degradation point.

Record:

```text
Maximum sustainable throughput
Failure point
Latency degradation
Error rate
Recovery behavior
```

---

# 33. Spike Test

Simulate sudden traffic increases.

Example:

```text
50 RPS
   ↓
500 RPS
```

Verify:

```text
Rate limiting
Connection pools
Thread pools
Database capacity
Queue behavior
Circuit breakers
```

---

# 34. Soak Test

For long-running services:

Run sustained traffic for an extended period.

Look for:

```text
Memory leaks
Connection leaks
Thread exhaustion
Queue growth
Database connection exhaustion
Increasing latency
Log volume problems
```

---

# 35. Performance Metrics

At minimum measure:

```text
RPS
Average latency
P50
P90
P95
P99
Max latency
Error rate
Timeout rate
CPU
Memory
Database latency
Database connections
Queue depth
```

---

# 36. Performance Thresholds

Every important performance test should define thresholds.

Example:

```text
HTTP success rate >= 99%
P95 < 500ms
P99 < 1000ms
Error rate < 1%
```

These are examples only.

Actual thresholds must be based on the service's requirements and environment.

Do not invent production SLAs and present them as official requirements.

---

# 37. k6 Threshold Example

Conceptually:

```javascript
export const options = {
  thresholds: {
    http_req_failed: ['rate<0.01'],
    http_req_duration: ['p(95)<500'],
  }
};
```

Actual thresholds must be adjusted for the service.

---

# 38. NBomber Measurements

Record:

```text
Ok
Fail
RPS
Latency
Min
Mean
Max
P50
P75
P90
P95
P99
```

Where supported by the selected NBomber version.

---

# 39. JMeter Results

JMeter performance reports should capture:

```text
Throughput
Average Response Time
Median
90th percentile
95th percentile
99th percentile
Error %
Active Threads
```

---

# 40. Performance Test Environment

Never run heavy load/stress tests against production.

Use:

```text
Development
Testing
Staging
Dedicated Performance Environment
```

Prefer an environment whose infrastructure resembles production.

---

# 41. Performance Test Documentation

Every performance test directory must contain an instruction file.

Example:

```text
tests/load-test/
└── README.md
```

It must explain:

```text
Prerequisites
Environment setup
How to run NBomber
How to run k6
How to run JMeter
Configuration
Target URL
Test data
Load profile
Expected result
Where results are stored
How to interpret results
Troubleshooting
```

---

# 42. Example Commands

Commands must match the actual repository.

Examples only:

### .NET / NBomber

```bash
dotnet run --project tests/load-test/nbomber
```

### k6

```bash
k6 run tests/load-test/k6/api-load.js
```

### JMeter

```bash
jmeter -n \
  -t tests/load-test/jmeter/api-test.jmx \
  -l results.jtl
```

Do not document commands that have not been verified against the repository.

---

# 43. Performance Results

Results should be stored outside source-controlled generated output unless the project explicitly requires committing them.

Example:

```text
test-results/
├── nbomber/
├── k6/
└── jmeter/
```

Do not commit huge generated performance artifacts by default.

---

# 44. Baseline Performance

For important services establish a baseline.

Record:

```text
Version
Environment
CPU
Memory
Database
Traffic
RPS
P95
P99
Error Rate
```

Future changes can be compared against the baseline.

---

# 45. Performance Regression

A performance regression occurs when a new version materially worsens established performance.

Check:

```text
Latency
Throughput
Error Rate
Memory
CPU
Database
```

If regression is detected:

```text
Measure
↓
Profile
↓
Identify bottleneck
↓
Optimize
↓
Re-run benchmark
```

---

# 46. Database Performance

Inspect:

```text
Slow Queries
Missing Indexes
N+1 Queries
Unnecessary Includes
Large Result Sets
Connection Pool
Transaction Duration
Locking
```

Use the project's query logging mechanism.

Do not optimize queries without measuring when practical.

---

# 47. API Performance

Check:

```text
Serialization
Validation
Database
External Calls
Caching
Network
Compression
Pagination
```

Never solve database inefficiency merely by adding caching without understanding the underlying issue.

---

# 48. Redis Performance

Where Redis is used:

Test:

```text
Cache hit
Cache miss
Expiration
Serialization
Connection failure
Latency
```

The service must remain safe when Redis is unavailable if the cache is non-critical.

---

# 49. External Dependency Performance

For:

```text
Payment providers
SMS
Email
Push
Third-party APIs
```

measure:

```text
Latency
Timeout
Retry
Failure rate
Circuit breaker behavior
```

Do not allow slow external dependencies to consume unlimited application resources.

---

# 50. Cancellation Testing

Every long-running operation should respect:

```text
CancellationToken
```

Test cancellation where applicable.

Examples:

```text
HTTP request cancelled
gRPC call cancelled
Background job stopped
Consumer shutdown
Database operation cancelled
```

---

# 51. Graceful Shutdown Testing

Verify:

```text
Application receives shutdown
↓
Stops accepting new work
↓
Finishes/abandons work safely
↓
Stops consumers/workers
↓
Closes connections
↓
Exits
```

Do not abruptly terminate critical background work.

---

# 52. Memory Testing

Monitor for:

```text
Memory growth
Large object allocation
Unbounded collections
Caching without expiration
Unreleased resources
Connection leaks
```

Soak tests are especially useful for identifying these problems.

---

# 53. Thread/Concurrency Testing

Check for:

```text
Thread starvation
Deadlocks
Race conditions
Lock contention
Connection exhaustion
```

Do not assume asynchronous code automatically eliminates concurrency problems.

---

# 54. Security Performance

Security mechanisms must not be bypassed merely for performance.

Do not disable:

```text
Authentication
Authorization
Tenant validation
Rate limiting
Audit logging
Input validation
```

to make benchmarks faster.

---

# 55. Test Logging

Test execution should produce enough information to identify failures.

Include:

```text
Test Name
Timestamp
Environment
Service Version
CorrelationId where applicable
Failure
Expected
Actual
```

Do not flood logs with unnecessary data during high-load tests.

---

# 56. Test Reports

A performance test report should include:

```text
Test Name
Date
Version
Environment
Scenario
Virtual Users
Duration
RPS
P50
P90
P95
P99
Error Rate
CPU
Memory
Database
Queue
Result
Observations
Recommendations
```

---

# 57. CI Performance Testing

Do not run massive stress tests on every pull request.

Recommended:

```text
Pull Request
→ Unit + Integration + API

Main Branch
→ Selected Performance Smoke Test

Scheduled Pipeline
→ Full Load Test

Release Candidate
→ Full Load + Stress + Soak
```

Adapt according to infrastructure cost.

---

# 58. Test Failure Classification

When a test fails classify it as:

```text
CODE_FAILURE
TEST_FAILURE
ENVIRONMENT_FAILURE
DEPENDENCY_FAILURE
DATA_FAILURE
PERFORMANCE_REGRESSION
CONFIGURATION_FAILURE
```

Do not modify production code until the failure category is understood.

---

# 59. Performance Failure Diagnosis

When performance degrades:

```text
1. Confirm reproducibility
2. Compare baseline
3. Check application metrics
4. Check database metrics
5. Check external dependencies
6. Check CPU/memory
7. Check logs/traces
8. Identify bottleneck
9. Fix
10. Re-run
```

---

# 60. Definition of Done

A service is not considered production-ready until applicable tests have been addressed:

```text
[ ] Unit tests
[ ] Integration tests
[ ] API tests
[ ] Contract tests
[ ] Authentication tests
[ ] Authorization tests
[ ] Tenant isolation tests
[ ] Error handling tests
[ ] Idempotency tests
[ ] Concurrency tests
[ ] Communication tests
[ ] Resilience tests
[ ] Background job tests
[ ] RabbitMQ tests
[ ] Database tests
[ ] Load tests
[ ] Stress tests
[ ] Performance tests
[ ] Documentation
```

Items that are not applicable must be explicitly documented.

---

# 61. Final Testing Principle

Testing is not:

```text
"Does it work once?"
```

Testing means:

```text
Does it work?
Does it fail safely?
Does it recover?
Does it scale?
Does it remain correct under concurrency?
Does it preserve tenant isolation?
Does it remain observable?
Can another developer reproduce the result?
```

The goal is not merely passing tests.

The goal is confidence in production behavior.

# END OF testing-and-performance.md
