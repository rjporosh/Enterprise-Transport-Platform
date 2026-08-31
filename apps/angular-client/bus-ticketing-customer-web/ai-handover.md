# AI Handover — Customer Client (Angular) — 2026-08-20 frontend wiring pass

Scope of this pass: wire this app's real API calls to the actual backend
contracts (no backend code touched, per instructions). This file covers
the customer client specifically; see the root `ai-handover.md` for
whole-repo history and `apps/react-admin/bus-ticketing-admin/ai-handover.md`
for the admin console.

## What's genuinely wired to real APIs now (verified by reading the
## actual C# endpoint/DTO source, not by trusting existing code comments)

- **Auth** (`POST /auth/login`, `POST /auth/register`, `GET /auth/me`) —
  `AuthService`/`AuthStore` now match auth-service's real
  `TokenPairResponse`/`UserDto` contracts. Previously the code assumed a
  single-call `{accessToken, user:{fullName, customerId, email}}` shape
  that auth-service has never returned, and the register form sent a
  single `fullName` field where the backend requires `firstName`/
  `lastName` separately. Login/register now do: call → persist tokens
  (including refresh token, previously dropped) → `GET /auth/me` →
  assemble the `AuthUser` the rest of the app renders. If the `/me` call
  fails, the session is not considered established. Fixed in
  `src/app/core/auth/{auth.model.ts,auth.store.ts,services/auth.service.ts}`
  and both auth pages.
- **Booking create + cancel now use the real signed-in customer id.**
  `BookingStore.confirm()` and the "My bookings" cancel flow were
  sending a hardcoded placeholder guid (`00000000-...-000000000001`) —
  confirmed by the code's own "placeholder until Auth is built" comment
  — instead of the actual logged-in user's id, which now exists since
  the auth fix above. A booking attempt while signed out now fails
  cleanly with a message instead of silently booking as the wrong
  customer. Fixed in `src/app/state/booking/booking.store.ts` and
  `src/app/features/profile/{pages/my-bookings-page,services/my-bookings.service}.ts`.
- Trip search (`GET /trips/search`) and booking create/get
  (`POST /bookings`, `GET /bookings/{id}`) were already correct against
  `TripsEndpoints.cs`/`BookingsEndpoints.cs` — verified, untouched.

## Known gaps — real, not something frontend wiring can close

Both are already handled by an honest mock fallback (see the doc
comment at the top of `src/app/core/interceptors/mock-api.interceptor.ts`
for the single source of truth) rather than left to 404 or given a fake
backend:

1. **`GET /bookings/mine` — no such endpoint exists.** booking-service
   only has get-by-id and cancel; there is no "list my bookings"
   endpoint anywhere. The My Bookings page reads from the same mock
   fixtures used in full-mock mode instead of breaking.
2. **Payment confirmation** — payment-service's `CreatePayment` command
   requires a `TenantId` (its data model is built for a multi-tenant
   B2B/agent context — see `PaymentService.Application.Features.Payments.CreatePayment.CreatePaymentCommand`)
   that has no source anywhere in this consumer app's identity: the JWT
   auth-service issues carries no tenant claim at all (checked
   `JwtTokenService.cs` directly). This is a genuine cross-service
   contract gap between a B2C customer flow and a B2B-shaped payment
   service, not a frontend bug — closing it means either auth-service
   issuing a tenant claim or payment-service accepting a default/retail
   tenant, both backend changes out of this pass's scope. The payment
   page continues to run on the mock fixture rather than send a
   fabricated `TenantId`.

## How to test this against the real backend

1. Start the stack (`docker compose -f infrastructure/docker/docker-compose.yml up -d`,
   or run each service locally with `scripts/dotnet-run.sh` — see root
   README for both paths).
2. `apps/angular-client/bus-ticketing-customer-web/src/environments/environment.ts`
   already has `mockApi: false` by default — no change needed.
3. `ng serve` (proxy.conf.json already points every prefix at the right
   local-dev port per service's `launchSettings.json`).
4. Register a new account on the register page (now sends real
   `firstName`/`lastName`), then log in — the header should show the
   real name that came back from `GET /auth/me`, not a demo placeholder.
5. Search a trip, book it, confirm the booking now carries your real
   customer id (check `bookings` table / booking confirmation screen).
6. My Bookings and Payment screens will show mock data by design — see
   the gaps above; this is not a regression to chase.

Flip `mockApi: true` for a fully offline click-through with no backend
at all.

## Exact next-step command for whoever picks this up

Nothing was left mid-edit — every change above is committed. To verify
compile-cleanliness (not possible in this sandbox: no network, so
`node_modules` was never installed here — all fixes were done by
reading `.ts` source against the real C# DTOs directly, not by running a
build):

```bash
cd apps/angular-client/bus-ticketing-customer-web
npm install
ng serve      # walk through register -> login -> search -> book -> cancel
```

If the build surfaces a type error, it is new information (this sandbox
could not typecheck), not evidence the reasoning above was wrong — fix
it in place; nothing here needs to be redesigned.

Not yet attempted in this pass, in priority order if the backend gaps
above get closed: (1) real payment confirm once tenant resolution
exists, (2) real "my bookings" list once that endpoint exists.

---

## 2026-08-31 update

The repo-root **`docs/API-GAPS.md`** is now the single source of truth for which
endpoints are real / unsafe / mock-only / missing across all 6 services — read it
instead of trusting the comment in `mock-api.interceptor.ts`. The full
frontend + backend gap picture and the fix order are in
`docs/PRODUCTION-GAP-ANALYSIS.md` and `docs/PRODUCTION-MILESTONES.md`. This app's
audit findings (no token refresh, no OTP UI, no i18n, no tests, payment page is
simulated) are in `release-notes.md` next to this file.
