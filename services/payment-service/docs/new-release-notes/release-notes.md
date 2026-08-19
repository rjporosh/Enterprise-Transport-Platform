# Payment Service — Release Notes (mirror)

> This file mirrors the latest entry from
> `services/payment-service/docs/programmers-guide/release-notes.md`
> (the service's canonical, full-history release notes file) at the path
> requested for this task. Full version history (v1.0.0 through v1.3.x)
> lives only in the canonical file to avoid two divergent histories —
> this file is refreshed with the newest entry each time a task
> specifically asks for `docs/new-release-notes/release-notes.md`.

## v1.4.0 - API Documentation, Postman Suite, DB Scripts, and Verified Auth Interconnection

**Task context:** bring payment-service's Scalar/OpenAPI docs, Postman
collection, and DB scripts up to parity with the other 5 services in this
platform; verify (not assume) that auth-service-issued tokens actually work
against payment-service; add a root-level 6-service migration guide; and
produce a design ADR for subscription licensing / module permissions /
per-user configurable rate limits. Full detail and rationale for every
change is in `ai-hanover.md`; this entry is the changelog summary.

### Fixed — cross-service authentication was silently broken

- **`Jwt:Issuer` / `Jwt:Audience` / `Jwt:SigningKey`** in both
  `appsettings.json` and `appsettings.Development.json` did not match what
  auth-service actually issues (confirmed by reading `JwtTokenService.cs`
  and cross-checking against auth-service/booking-service/bus-service/
  route-service's own `appsettings.json`, which all agree with each other).
  **Effect before this fix: every real login token from auth-service would
  have been rejected with 401 by payment-service.** Corrected to
  `Issuer: https://identity.bus-ticketing.local`,
  `Audience: bus-ticketing-api`,
  `SigningKey: REPLACE_WITH_A_SECRET_AT_LEAST_32_CHARS_LONG_IN_PROD`
  (matching the platform-wide placeholder convention — replace with a real
  secret from a vault in production, same as every other service).
- **`/api/v1/payments/search`** had no `.RequireAuthorization()` or
  `.RequireRateLimiting("PaymentPolicy")` — the only endpoint group in
  `PaymentEndpoints.cs` missing both, meaning it was callable
  unauthenticated despite returning `CustomerId`/`TenantId`/`Amount` per
  result. Fixed to match every other group in the file. (The `/api/v1/
  webhooks/{providerName}` group remains intentionally unauthenticated —
  see the code comment added there — payment providers can't present a
  platform JWT, and it's protected by per-provider signature verification.)

### Added — API documentation

- `Program.cs`'s OpenAPI registration upgraded from a bare `AddOpenApi("v1")`
  to a document transformer matching auth/booking/bus/route-service: sets
  `Info.Title`/`Description`, and registers a `Bearer` security scheme so
  Scalar's `/scalar` UI has a place to paste a token (previously it had
  none — functionally usable but undocumented compared to the other 5
  services).

### Added — Postman collection (`docs/scripts/postman/`)

- New `payment-service.postman-collection.json` +
  `payment-service.postman_environment.json`, replacing the old
  `docs/programmers-guide/postman-collection.json` (104 lines, missing most
  endpoints, no environment, no scripts — removed, pointer left in its
  place).
- Collection-level pre-request script auto-logs-in against auth-service and
  refreshes the token when it's missing/near expiry — no manual token
  copy-paste needed to exercise the whole collection.
- Every real endpoint covered (9 Payments + 5 AgentPaymentMethods +
  Webhook + 2 Health), each with an example request body matched
  field-for-field against the real C# command/query records, and a
  per-request test script asserting status codes and chaining IDs
  (`payment_id`, `refund_id`, `payment_method_id`) forward.
- **Caveat, stated plainly: not yet run against a live stack.** JSON was
  validated for correctness and every field/enum was checked against
  source, but no Postman/Newman execution happened in this pass (no SDK/
  network in this sandbox — see `ai-hanover.md`). Treat the first real run
  as this collection's actual test.

### Added — DB scripts (`docs/db-scripts/2026/August/19-08-2026/`)

- `schema-scripts.sql` — snapshot of the two real EF migrations
  (`InitialCreate`, `AddAgentPaymentMethod`), for review/DBA sign-off. Not
  the apply mechanism (`dotnet ef database update` still is) — file
  documents how to regenerate it properly once someone has the SDK.
- `triggers-scripts.sql` — optional/supplemental `UpdatedAtUtc` touch
  triggers + a refund-does-not-exceed-payment guard trigger (defense in
  depth alongside the existing application-layer check).
- `functions-script.sql` — optional ops/reporting functions: available-
  refund calculator (mirrors `PaymentDto.AvailableRefundAmount`), outbox
  dead-letter counter, outbox purge helper.
- None of these three files have been executed against a real Postgres
  instance in this pass.

### Added — platform-level docs (repo root, affect all 6 services)

- `/guide.md` — the exact `dotnet ef migrations add "dd-mm-yy-name"` /
  `dotnet ef database update` commands for all 6 services from one place,
  a DbContext/schema/port reference table, and what actually wires the 6
  services together at runtime.
- `/docs/adr/0009-subscription-licensing-and-module-rate-limits.md` —
  design proposal (not implemented) for subscription/license enforcement,
  module-based access, and configurable per-user/day/month request and
  resource limits, built on top of auth-service's existing
  Module/Permission/Role entities rather than a parallel system.

### Not done in this pass (see `ai-hanover.md` "Exact next command")

- No enforcement code for ADR 0009 exists yet — design only.
- The Postman collection has not been run live.
- `notification-service` has the same class of Jwt Issuer/Audience mismatch
  just fixed here, in a different service — out of scope for a
  payment-service-only task, flagged for follow-up.
