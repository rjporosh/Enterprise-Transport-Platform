# AI Hanover Notes — PaymentService Build-Health Pass

> **Update (pass 3 — API documentation, Postman, DB scripts, interconnection):**
> New task: bring payment-service's API docs/Postman/DB-script hygiene up to
> the same standard as the other 5 services, verify auth-service token
> interconnection actually works, and design (not yet implement) the
> subscription/licensing/permission-limit system. Jump to
> "Pass 3 — what was covered" below; full detail is in
> `docs/new-release-notes/release-notes.md` (v1.4.0). Same sandbox
> constraint as before: no .NET SDK, no network — everything below was
> verified by reading the actual source (migrations, endpoint files, DTOs,
> appsettings across all 6 services), not by running `dotnet build`/`dotnet run`.
>
> **The most important thing found in this pass**: payment-service's
> `Jwt:Issuer`/`Jwt:Audience`/`Jwt:SigningKey` in `appsettings.json` and
> `appsettings.Development.json` did **not** match what auth-service
> actually issues (`https://identity.bus-ticketing.local` /
> `bus-ticketing-api`, confirmed by reading `JwtTokenService.cs` and
> auth-service's own `appsettings.json`, and cross-checked against
> booking-service/bus-service/route-service which all already use the
> correct values). This meant **every real access token from auth-service
> would have been rejected with 401 by payment-service** — the two
> services were not actually interconnected despite both compiling and
> running fine in isolation. Fixed — see below.

> **Update (pass 2):** The user ran the real build on their machine (macOS, .NET 10 SDK installed) and pasted the actual `dotnet restore` / `dotnet build` output — 10 restore warnings, up to 57 build warnings total, 0 errors. All of those specific warnings have now been fixed in source (see `release-notes.md` v1.3.2 for the full root-cause-by-root-cause breakdown). This sandbox still has no SDK/network, so **these fixes are unverified by an actual rebuild** — that is the one remaining step. Jump to "Exact next command to run" below.

## Task as given
"Make sure no regression, nothing broken, all APIs working nicely. Ensure 0 build warnings and 0 build errors. Include the `dotnet ef migrations add` and `dotnet ef database update` commands in `guide.md`. Return the zipped project."

## Root cause of why this handoff exists
**This sandbox has no .NET SDK installed (`dotnet: command not found`), and outbound network access is disabled (egress proxy blocks all hosts, including `dotnet.microsoft.com`/NuGet), so the SDK could not be installed either.** This means I could not run `dotnet restore`, `dotnet build`, `dotnet test`, or `dotnet ef migrations add` in this environment. Nothing here was skipped for convenience — it was structurally impossible in this sandbox.

## What was covered in this pass
1. Unzipped the project, stripped macOS junk (`__MACOSX/`, `.DS_Store`) and stale `obj/`/`bin/` build output that had been included in the archive.
2. Read all 7 `.csproj` files — package versions and project references all cross-reference consistently (net10.0 across the board, EF Core 10.0.0, matching provider packages).
3. Ran structural static checks across all 143 `.cs` files:
   - Brace `{}` balance per file — clean.
   - Paren `()` balance per file — clean.
   - Duplicate class/record/interface/enum names — the 4 hits found (`Program`, `InitialCreate`, `DependencyInjection`, `AddAgentPaymentMethod`) are all legitimate (different projects, or the EF migration + its `.Designer.cs` partial, which is correct EF Core pattern) — not collisions.
   - Namespace-vs-folder-path consistency across `src/` — clean.
4. Verified `IPaymentDbContext` (3 `DbSet<T>` members) is a strict, consistent subset of `PaymentDbContext`'s 4 `DbSet<T>` members.
5. Verified all 15 MediatR commands/queries in `PaymentService.Application` pair 1:1 with a handler of matching generic response type.
6. Verified `PaymentDbContextDesignTimeFactory : IDesignTimeDbContextFactory<PaymentDbContext>` exists and is wired to PostgreSQL — this is what makes `dotnet ef migrations add`/`database update` work.
7. Wrote root-level `guide.md` with the exact build/test/run/migration commands.
8. Updated `docs/programmers-guide/release-notes.md` with a `v1.3.1` entry documenting this pass.

## What is explicitly NOT verified (left for the next agent / you)
- **Actual compilation.** Static review cannot catch: NuGet version/restore conflicts (e.g. transitive package downgrades — note `Pomelo.EntityFrameworkCore.MySql 9.0.0` pinned against `Microsoft.EntityFrameworkCore 10.0.0` with `NoWarn="NU1608"` already suppressing a known version-range warning there — worth double-checking this doesn't hide a real incompatibility once you can actually restore), analyzer/nullable-reference warnings (`CS86xx`, `CS8600`-series, `CA*`, `IDE*` rules — this repo has `<Nullable>enable</Nullable>` everywhere, which is a common source of build *warnings* even when there are zero *errors*), or EF Core model-building-time errors.
- **Runtime API behavior.** No endpoint was exercised; "all APIs working nicely" is unverified beyond the codebase being internally consistent on paper.
- **Test suite pass/fail.** Not run.

## Exact next command to run (pick up here)

```bash
cd payment-service
dotnet restore PaymentService.sln
dotnet build PaymentService.sln -c Release 2>&1 | tee /tmp/build.log
grep -E "warning|error" /tmp/build.log
```

- If the grep prints nothing → 0 warnings / 0 errors confirmed. Run `dotnet test PaymentService.sln -c Release` to confirm the test suite still passes (the Bkash/Nagad null-check change and the `NoOpLogger` constraint change are both in test-adjacent or provider code — worth confirming `WebhookSignatureVerificationTests` and any Bkash/Nagad unit tests still pass), then the task is done.
- If any warning still prints, it's most likely one of these:
  - A **different** NU1903 CVE than the ones fixed here, disclosed after this pass — re-run `dotnet list package --vulnerable --include-transitive` and repeat the "pin to patched version" pattern used in v1.3.2.
  - `NU1608` still appearing for a project outside `src/`/`tests/` (e.g. `performance-tests/`) if it sits outside the directory tree `Directory.Build.props` auto-imports from — move/copy the props file up a level, or add a project-local one.
  - A genuinely new warning introduced by one of this pass's edits — check the specific file/line the compiler reports first; don't assume it's unrelated.
- Then run the migration commands exactly as documented in `guide.md` against a real Postgres instance to confirm `dotnet ef migrations add`/`database update` actually execute (not run in this sandbox, same SDK/network reason).

## Pass 3 — what was covered

Task as given: bring payment-service's Scalar/OpenAPI docs, Postman
collection (with environment + pre/post-request scripts for every
endpoint), and DB scripts up to parity with the other 5 services; verify
real interconnection with auth-service's tokens; add a root `guide.md`
with the 6-service migration workflow; and produce an ADR for a
subscription-licensing / module-permission / configurable-rate-limit
system, without implementing that system yet.

1. **Fixed the auth-service token interconnection bug** described above —
   `Issuer`/`Audience`/`SigningKey` in both `appsettings.json` and
   `appsettings.Development.json` now match the platform standard used by
   auth/booking/bus/route-service.
2. **Fixed a real authorization gap**: `PaymentEndpoints.cs`'s
   `/api/v1/payments/search` group had no `.RequireAuthorization()` or
   `.RequireRateLimiting()` — every other endpoint group in the file has
   both. This meant payment search (which returns `CustomerId`, `TenantId`,
   `Amount`, etc.) was callable without a token. Fixed to match the rest of
   the file. The webhook group intentionally remains unauthenticated (see
   the code comment added there) — providers can't present a platform JWT,
   and it's protected by per-provider signature verification instead.
3. **Upgraded `Program.cs`'s OpenAPI/Scalar registration** from a bare
   `AddOpenApi("v1")` (no title, no description, no way to paste a token
   into Scalar's Authorication panel) to the same document-transformer
   pattern already used by auth-service/booking-service/bus-service/
   route-service — proper title, description, and a `Bearer` security
   scheme registered so Scalar's UI offers a token field.
4. **Rebuilt the Postman collection from scratch** at
   `docs/scripts/postman/payment-service.postman-collection.json` +
   `payment-service.postman_environment.json`, matching the
   `docs/scripts/postman/` convention already used by auth-service,
   bus-service, and route-service (the old `docs/programmers-guide/
   postman-collection.json`, 104 lines, covered a fraction of the real
   endpoints and had no environment or scripts, has been removed — a
   `.moved.md` pointer is left in its place). The new collection:
   - Has a **collection-level pre-request script** that auto-logs-in
     against auth-service (`POST /api/v1/auth/login`) whenever
     `access_token` is missing/expired, and stores the resulting
     `accessToken`/`refreshToken`/`accessTokenExpiresAtUtc` back into the
     environment — no manual token-copy-pasting required to run the
     collection end to end.
   - Has a **collection-level test script** (status/latency sanity checks
     + a console warning that points at the Jwt config if a 401 slips
     through).
   - Covers every real endpoint in `PaymentEndpoints.cs` and
     `AgentPaymentMethodEndpoints.cs` (Create/Get/List/Process/Confirm/
     Fail/Cancel/Refund/Search payments; Add/List/GetDefault/Verify/
     SetDefault agent payment methods; the bKash webhook; both health
     probes), each with a real example body matched field-for-field
     against the actual C# command records (verified by reading
     `CreatePaymentCommand`, `ConfirmPaymentCommand`, etc. — not guessed),
     and a per-request test script that asserts status code and chains
     IDs (`payment_id`, `refund_id`, `payment_method_id`) into the
     environment for the next request.
   - **Not verified by an actual Postman/Newman run** — no network egress
     to `getpostman.com`/Newman in this sandbox. The JSON was validated as
     syntactically correct (`json.load` round-trip) and every field name/
     enum value was cross-checked against source, but nobody has actually
     fired these requests at a running payment-service + auth-service yet.
     **Next step: run it against a live stack and fix whatever the first
     real 400/401/500 reveals.**
5. **Added `docs/db-scripts/2026/August/19-08-2026/{schema-scripts.sql,
   triggers-scripts.sql,functions-script.sql}`**:
   - `schema-scripts.sql` is a hand-derived-but-faithful snapshot of the
     two real EF migrations (`InitialCreate`, `AddAgentPaymentMethod`) —
     every table/column/index/FK in it was copied from the migration
     `.cs` files, not invented. It is explicitly documented as a
     **snapshot for review**, not the actual apply mechanism (`dotnet ef
     database update` remains that) — the file says how to regenerate it
     properly with `dotnet ef migrations script` once someone has the SDK.
   - `triggers-scripts.sql` and `functions-script.sql` are clearly labeled
     **supplemental/optional DBA scripts**, not part of EF migration
     history and not depended on by the application. They add an
     `UpdatedAtUtc` touch-trigger (defense in depth — the app already sets
     this itself) and a refund-does-not-exceed-payment guard trigger
     (mirrors `RefundPaymentHandler`'s own check as a second DB-level line
     of defense), plus three ops/reporting functions (available-refund
     calculator matching `PaymentDto.AvailableRefundAmount`, an outbox
     dead-letter counter, and an outbox purge helper). **None of these
     have been run against a real Postgres instance** — same SDK/network
     constraint as everything else in this hand-over chain.
6. **Added root-level `/guide.md`** (repo root, not `services/payment-service/
   guide.md` which already existed and is unrelated) with the exact
   `dotnet ef migrations add "dd-mm-yy-name"` / `dotnet ef database update`
   commands for **all 6 services**, a DbContext/schema/port reference
   table, and an explanation of what actually wires the services together
   (shared JWT config, the gateway, RabbitMQ).
7. **Added `docs/adr/0009-subscription-licensing-and-module-rate-limits.md`**
   at the repo root — a **design proposal, not an implementation** — for
   the licensing/subscription/module-permission/per-user rate-and-quota
   system requested. Grounded in what already exists (auth-service already
   has `Module`/`Permission`/`Role`/`RolePermission`/`ModulePermission`
   entities and Admin CRUD endpoints for them — verified by reading
   `AuthService.Domain.Entities` and `AuthService.Application.Features.
   Admin`), proposing `Subscription`/`Plan`/`PermissionLimit`/
   `UserPermissionOverride` on top of that plus a Redis-backed shared
   rate/quota-check library used by all 6 services, rather than either a
   synchronous auth-service call per request or six independently
   duplicated implementations. Explicitly flags the open product decisions
   (grace-period behavior, token revocation staleness, plan-editing UX)
   that need the platform owner's input before implementation.

## What is explicitly NOT done — full scope was far larger than one pass

The original task asked for: full per-service module/permission
enforcement wired end-to-end, per-user configurable request/day/month
limits actually enforced in code, subscription expiry actually gating
API access, and this done with verified, live-tested Postman runs. **None
of the enforcement code exists yet** — pass 3 designed it (ADR 0009) and
fixed the two concrete bugs found along the way (JWT mismatch, missing
auth on search), but implementing `Subscription`/`Plan`/`PermissionLimit`/
`UserPermissionOverride`, the Redis-backed limiter library, and wiring it
into all 6 services' endpoints is unstarted. This was a deliberate
stopping point, not an oversight — see "Exact next command for the next
agent" below.

## Exact next command for the next agent (pass 3 continuation)

1. **Verify what this pass could not**: get the .NET 10 SDK + a running
   Postgres/Redis/RabbitMQ (`infrastructure/docker/docker-compose.yml` has
   them), then:
   ```bash
   cd services/payment-service
   dotnet restore PaymentService.sln
   dotnet build PaymentService.sln -c Release
   dotnet ef database update --project src/PaymentService.Infrastructure --startup-project src/PaymentService.Api
   dotnet run --project src/PaymentService.Api
   ```
   In a second terminal, do the same for `services/auth-service`, then
   import `services/payment-service/docs/scripts/postman/payment-service
   .postman-collection.json` + the `.postman_environment.json` into
   Postman (or run via `newman run ... -e ...`) and fix whatever the first
   real failure reveals — the collection was built from source, not from a
   live run, so treat the first execution as the real test of this pass.
2. **Then implement ADR 0009** (`docs/adr/0009-subscription-licensing-and-
   module-rate-limits.md`), starting with the auth-service entities/
   migration it lists, following the root `/guide.md` migration workflow.
3. **Also worth a follow-up, found but out of scope for "just
   payment-service"**: notification-service's `Jwt:Issuer`/`Jwt:Audience`
   (`bus-ticketing-auth-service` / `bus-ticketing-platform`) don't match
   the `https://identity.bus-ticketing.local` / `bus-ticketing-api`
   standard either — same class of bug just fixed here, in a different
   service. Not touched in this pass since the task scope was payment-service
   only.

## Files touched this pass (cumulative, both rounds)
- Added: `guide.md`, `ai-hanover.md`, `Directory.Build.props` (all at repo root)
- Edited: `docs/programmers-guide/release-notes.md` (v1.3.1 + v1.3.2 entries)
- Edited (round 2 — real warning fixes): `src/PaymentService.Infrastructure/Providers/BkashPaymentProvider.cs`, `src/PaymentService.Infrastructure/Providers/NagadPaymentProvider.cs`, `tests/PaymentService.UnitTests/Providers/WebhookSignatureVerificationTests.cs`, `src/PaymentService.Api/Endpoints/PaymentEndpoints.cs`, `src/PaymentService.Api/Endpoints/AgentPaymentMethodEndpoints.cs`, `src/PaymentService.Infrastructure/PaymentService.Infrastructure.csproj`, `src/PaymentService.Api/PaymentService.Api.csproj`, `tests/PaymentService.IntegrationTests/PaymentService.IntegrationTests.csproj`
- No other `.cs`/`.csproj` files were changed in rounds 1-2. Round 1 (static-only) made no source changes; round 2's changes are listed above with full root-cause detail in `release-notes.md`.

**Pass 3 (this update):**
- Edited: `src/PaymentService.Api/appsettings.json`, `src/PaymentService.Api/appsettings.Development.json` (Jwt Issuer/Audience/SigningKey fix)
- Edited: `src/PaymentService.Api/Program.cs` (OpenAPI/Scalar document transformer + Bearer scheme)
- Edited: `src/PaymentService.Api/Endpoints/PaymentEndpoints.cs` (added `.RequireAuthorization()`/`.RequireRateLimiting()` to the search group)
- Added: `docs/scripts/postman/payment-service.postman-collection.json`, `docs/scripts/postman/payment-service.postman_environment.json`
- Removed: `docs/programmers-guide/postman-collection.json` (stale/incomplete) → replaced with `docs/programmers-guide/postman-collection.json.moved.md` pointer
- Added: `docs/db-scripts/2026/August/19-08-2026/{schema-scripts.sql,triggers-scripts.sql,functions-script.sql}`
- Added (repo root, not this service): `/guide.md`, `/docs/adr/0009-subscription-licensing-and-module-rate-limits.md`
- Added: `docs/new-release-notes/release-notes.md` (mirrors the v1.4.0 entry also added to `docs/programmers-guide/release-notes.md`)
