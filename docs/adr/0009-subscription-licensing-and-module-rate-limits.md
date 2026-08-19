# 0009 — Subscription Licensing, Module Permissions, and Configurable Rate Limits

- Status: **Proposed** (design only — not yet implemented in code)
- Date: 2026-08-19
- Author: AI engineering pass, payment-service documentation task
- Deciders: platform owner (product/business decision), backend leads for auth-service and payment-service

## Context

The platform owner wants to run this system as a licensed, subscription-based
product for tenant businesses, with fine control over:

1. **Module-based access** — an admin/tenant-owner can grant or revoke access
   to whole feature modules (e.g. "Payments", "Route Management") per role or
   per user.
2. **Per-request/per-action permission checks** — not just "can this user hit
   this module" but "can this specific user perform this specific action"
   (e.g. create a payment vs. only view one).
3. **Configurable usage limits per user**, of at least three kinds:
   - **Count-based** ("this user can create at most 2 sub-users").
   - **Rate-based** ("this user can call this API at most 5 times per day").
   - **Time-window-based** ("this permission/module is only usable for N
     days, or only during certain hours, or resets monthly").
4. **Subscription enforcement** — once a licensed period or quota is
   exhausted, access is denied until the tenant renews/upgrades ("buys it
   from me again").

This ADR records the target design and **where it plugs into what already
exists**, so the next implementer isn't starting from zero and doesn't
accidentally build a second, competing permission system.

### What already exists today (verified in code, not assumed)

auth-service already has a working RBAC foundation:

- `User` ↔ `Role` (`UserRole` join entity)
- `Role` ↔ `Permission` (`RolePermission` join entity)
- `Module` ↔ `Permission` (`ModulePermission` join entity — a permission
  belongs to a module)
- Admin CRUD endpoints for all of the above already exist under
  `AuthService.Application.Features.Admin.{Modules,Roles,Permissions}`.
- JWTs issued by auth-service currently carry `sub`, `email`, `jti`, `iat`,
  `first_name`, `last_name`, and one `role` claim per assigned role (see
  `JwtTokenService.GenerateAccessToken`). They do **not** currently carry
  permission or module claims, or tenant/subscription state.
- Every downstream service (including payment-service, as of this pass)
  validates the same JWT via `AddJwtBearer` and reads roles via
  `ClaimTypes.Role` / `HttpContext.User.IsInRole(...)`. Payment-service's
  `RequireRateLimiting("PaymentPolicy")` is a **flat, unconfigurable**
  100 req/min-per-user fixed-window limiter — the same for every user
  regardless of plan.

So: the module/permission *shape* already exists. What's missing is (a) a
per-user/per-tenant **quota and time-window layer** on top of it, and
(b) a **subscription/license** record that gates the whole thing, and (c)
propagating enough of that into the JWT (or a fast lookup) that every
downstream service — payment-service included — can enforce it without an
auth-service round-trip on every request.

## Decision

Extend the existing auth-service RBAC model with three new concepts,
owned by auth-service (the platform's single source of truth for identity
and access), and have every downstream service (payment-service included)
enforce limits locally using claims + a shared cache, not a synchronous
call to auth-service per request.

### 1. Subscription / License (new, tenant-level)

A new `Subscription` entity in auth-service:

```
Subscription
  Id, TenantId, PlanId, Status (Active/Expired/Suspended/Cancelled),
  StartsAtUtc, EndsAtUtc, BillingCycle (Monthly/Annual/None),
  MaxSubUsers (nullable int),      -- "can create at most N more users"
  RenewedAtUtc, GracePeriodEndsUtc
```

A `Plan` entity defines which `Module`s and which per-permission limits
(see below) a plan includes, so the platform owner can compose plans
("Starter", "Pro", "Enterprise") out of the modules/permissions that
already exist, rather than hard-coding limits per tenant.

When `EndsAtUtc` (plus any `GracePeriodEndsUtc`) passes, or `Status` is
not `Active`, auth-service stops issuing new access tokens for that
tenant's users beyond a short-lived read-only grace token (implementation
detail left to the implementer — options: refuse `/login` and `/refresh`
outright, or issue a token with a `sub_status=expired` claim that every
service treats as "read-only" — this is a product decision, not purely
technical, and should be confirmed with the platform owner before coding).

### 2. Per-permission limits (new, extends `Permission`/`RolePermission`)

Rather than a single flat rate limit, each **grant** of a permission to a
role (or, for overrides, directly to a user) carries its own limit
configuration:

```
PermissionLimit
  Id, RolePermissionId (nullable), UserPermissionOverrideId (nullable),
  LimitType (RequestCount | ResourceCount),
  MaxValue (int),                  -- e.g. 5, or 2
  Window (PerDay | PerMonth | PerLifetime | Unlimited),
  ResetAtUtc (nullable — computed from Window, exposed so a client can show "resets in X")
```

This single shape covers every example the platform owner gave:
- *"hit an API 5 times max per day"* → `LimitType=RequestCount, MaxValue=5, Window=PerDay`.
- *"create at most 2 more users"* → `LimitType=ResourceCount, MaxValue=2, Window=PerLifetime`, attached to a `users.create` permission.
- *"available for N days"* → covered by the subscription's `EndsAtUtc`, or by a `UserPermissionOverride.ExpiresAtUtc` for a one-off grant to a single user (e.g. a temporary elevated permission).
- *"resets monthly"* → `Window=PerMonth`, `ResetAtUtc` recalculated on each successful check.

A **`UserPermissionOverride`** table lets an admin grant/revoke or
re-limit a specific permission for a specific user without creating a new
role — this is what "I should be able to give permission for days or
day-time limit" maps to concretely: an override row with its own
`PermissionLimit` and optional `ExpiresAtUtc`/`ActiveHoursStart`/
`ActiveHoursEnd`.

### 3. Enforcement point: a shared library, not a shared service call

Counting "5 calls today" or "2 sub-users created" **cannot** be done by
re-validating a JWT alone — it needs shared, fast, cross-request state.
Two extremes were considered and rejected:

- **Rejected: auth-service call on every request.** Correct, but adds a
  synchronous network hop + auth-service load to every single API call on
  every service — unacceptable latency/availability coupling for something
  as hot-path as payment creation.
- **Rejected: duplicate the limit logic separately in each of the 6
  services.** Guarantees drift and inconsistent enforcement (exactly the
  kind of bug this pass just found and fixed with the JWT
  issuer/audience mismatch — six independent copies of security-relevant
  config *will* drift).

**Chosen approach:** a new shared library,
`shared/shared-kernel/RateLimiting` (alongside the existing
`shared/shared-kernel`, `shared/common`, `shared/contracts` projects
already in this repo), referenced by every service's `.Api` project:

1. auth-service embeds the user's **currently-active permission +
   limit set** as a compact claim in the access token at login/refresh
   time (e.g. a `perms` claim: a short, versioned, signed blob — not the
   full limit config, just `{permissionCode: limitId}` pairs — the actual
   `PermissionLimit` rows are cached, not put in the JWT, to keep tokens
   small).
2. Each service's `.Api` project registers a
   `RequirePermissionLimit("payments.create")` endpoint filter (same
   pattern as the existing `.RequireRateLimiting("PaymentPolicy")` calls
   in `PaymentEndpoints.cs` / `AgentPaymentMethodEndpoints.cs` — this
   slots in next to them, it doesn't replace the transport-level rate
   limiter, which stays as a DoS backstop).
3. The filter checks/increments a counter in **Redis** (already a
   dependency of payment-service — see `Redis:ConnectionString` in
   `appsettings.json` and `PaymentService.Infrastructure/Caching`) keyed
   `limit:{userId}:{permissionCode}:{windowBucket}`, using `INCR` +
   `EXPIRE`, which is atomic and cheap. This is the same pattern
   `RateLimitPartition.GetFixedWindowLimiter` already uses conceptually
   in `Program.cs`, just promoted to a cross-service, config-driven,
   Redis-backed version instead of per-process in-memory.
4. Every service still independently validates the JWT signature/issuer/
   audience exactly as today — no new trust dependency between services
   beyond the JWT itself and the shared Redis instance.

This means payment-service's existing `RequireRateLimiting("PaymentPolicy")`
stays exactly as-is (flat 100/min DoS backstop, unrelated to billing), and
a **new**, separate, plan-aware limiter is what enforces the product's
subscription limits.

## Consequences

**Positive**
- Builds on existing, tested auth-service entities (`Module`, `Permission`,
  `Role`) instead of a parallel system — smaller diff, less to review.
- One enforcement library shared by all 6 services means the
  issuer/audience-mismatch class of bug (fixed in this pass for
  payment-service) can't recur here — there's one place the check lives.
- Redis-backed counters mean no synchronous cross-service call on the hot
  path, and the same Redis instance payment-service already depends on can
  be reused (or a dedicated instance, if isolation is preferred — that's
  an infra decision, not an architectural one).

**Negative / open questions for the platform owner (not decided by this ADR)**
- **Token staleness**: if an admin revokes a permission mid-token-lifetime
  (access tokens currently live 15 minutes per
  `AccessTokenLifetimeMinutes: 15`), the revocation won't take effect until
  the next refresh unless a revocation-check is added. Given the sensitivity
  of billing-relevant limits, a short-TTL Redis-side "is this grant still
  active" check (not just trusting the JWT claim) is recommended, at the
  cost of one Redis lookup per gated call.
- **Plan-editing UX** (who defines plans, how they're versioned when a
  tenant's plan changes mid-cycle) is a product/business decision, not
  covered here.
- **Grace-period behavior on expiry** (hard cutoff vs. read-only grace
  window) needs a product decision before implementation, as noted above.
- **Billing/payment for the subscription itself** — whether tenant
  subscription billing runs through this same payment-service or a
  separate billing flow — is out of scope for this ADR and should be its
  own decision record once scoped.

## Implementation status

**Not implemented.** This ADR is the design hand-off requested alongside
the payment-service documentation work in this pass. Suggested
implementation order for whoever picks this up:
1. `Subscription`/`Plan`/`PermissionLimit`/`UserPermissionOverride` entities
   + EF migration in auth-service (use the root `guide.md` migration
   commands for `auth-service`).
2. `perms` claim emission in `JwtTokenService`.
3. The shared `RateLimiting` library + Redis-backed filter.
4. Wire payment-service's `PaymentEndpoints.cs` /
   `AgentPaymentMethodEndpoints.cs` as the first consumer (smallest surface
   area — 2 endpoint files, already inventoried in this pass), then roll
   out to the other 5 services.
