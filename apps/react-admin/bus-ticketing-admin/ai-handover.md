# AI Handover — Admin Console (React) — 2026-08-20 frontend wiring pass

Scope of this pass: wire this app's real API calls to the actual backend
contracts (no backend code touched, per instructions). This file covers
the admin console specifically; see the root `ai-handover.md` for
whole-repo history and `apps/angular-client/bus-ticketing-customer-web/ai-handover.md`
for the customer client.

## What's genuinely wired to real APIs now (verified by reading the actual
## C# endpoint/DTO source, not by trusting existing code comments)

- **Auth** (`GET/POST /auth/*`) — `login()` + `me()` now match auth-service's
  real `TokenPairResponse`/`UserDto` contracts exactly. Previously the
  code assumed a single-call `{accessToken, user}` shape that
  auth-service has never returned — login would have "succeeded" over
  the wire but the app had nothing valid to read the display name/role
  from. Fixed in `src/modules/auth/{models/auth.model.ts,api/auth.api.ts,AuthContext.tsx}`.
- **Buses** (`GET /buses`) — bus-service wraps every response in a
  `Result<T>{success,message,value}` envelope; the admin code was
  reading the paged list off the response root, so this would have
  rendered an empty table (or thrown) the moment mock mode was switched
  off. Also mapped `BusDto.status`'s real enum
  (`Active/UnderMaintenance/Retired`) to the UI's
  (`Active/Maintenance/Suspended`) instead of assuming the strings
  matched. Fixed in `src/modules/buses/api/buses.api.ts`.
- **Routes** (`GET /routes`, `GET /stops`) — `RouteDto` carries stop
  *ids*, not city name strings, and its duration is a .NET `TimeSpan`
  serialized as `"hh:mm:ss"`, not a minutes integer. Added a single
  batched `GET /stops` call per page load to resolve origin/destination
  names, and a real `TimeSpan` parser. Fixed in
  `src/modules/routes/api/routes.api.ts`.
- **Dev-server & production proxying** — `vite.config.ts`'s dev proxy had
  stale ports for auth/routes and no route at all for `/admin`, `/stops`,
  `/schedules`, `/depots`, `/agents`, `/webhooks`, `/notifications`,
  `/recipients`, `/templates` (everything fell through to one catch-all).
  Repointed every prefix at its real `launchSettings.json` port. The
  equivalent gap in the production `nginx.conf` (`/stops`, `/admin`
  falling through to booking-service) is also fixed.
- Bookings `GET /bookings/{id}` and `POST /bookings/{id}/cancel` were
  already correct — verified against `BookingsEndpoints.cs`, untouched.

## Known gaps — real, not something frontend wiring can close

These are documented in code comments at the point they matter
(`src/api/mockAdapter.ts`'s top-of-file doc comment is the single source
of truth — read that first if this file and the code ever disagree) and
kept on the mock fallback rather than left to 404 or fabricated:

1. **`GET /users` — no user-management/listing backend exists anywhere.**
   auth-service has `/api/v1/admin/roles`, `/permissions`, `/modules`
   (grant/revoke on an existing user by id) but nothing that lists users.
2. **`GET /dashboard/stats` — no aggregation endpoint exists.** No
   service anywhere computes cross-service KPIs.
3. **`GET /bookings` and `GET /trips` (the plain, paginated *list*
   views)** — booking-service only has get-by-id/cancel for bookings and
   `/trips/search` (needs origin+destination+date — a different shape
   entirely from this admin screen's list-with-status-filter), not a
   bare list of either.
4. **Bus → operator display name.** `BusDto` only carries `operatorId`
   (a guid). There is no operator-directory service — `src/modules/operators`
   is UI scaffolding with nothing behind it. The Buses table shows the
   raw id.
5. **Route → "Active trips" count.** route-service has no reference to
   booking-service's `Trip` entity, so nothing can compute a live count.
   Shown as `0` with a code comment, not a fabricated number.

None of these were invented a workaround with fake data — per the
project's own instruction ("do not create mock APIs or fake data" —
found already written into `mockAdapter.ts` from an earlier pass), they
keep answering from the existing mock fixtures so the screens don't
break, until a real endpoint exists.

## How to actually test this against the real backend — including the
## "there is no admin in the real DB" problem

There is no seed/bootstrap Admin account anywhere in auth-service (no
seed SQL, no startup seeding code beyond the three **roles** —
`Customer`/`Operator`/`Admin` — which do exist by default; see
`services/auth-service/src/AuthService.Infrastructure/Persistence/Configurations/RoleConfiguration.cs`).
Every `/api/v1/admin/*` endpoint that could grant the Admin role itself
requires an existing Admin-role JWT — a genuine chicken-and-egg gap in
the backend, not something this frontend pass can wire around.

**Concrete way to get a working Admin login today, without touching
backend code:**

1. Start the stack (`docker compose -f infrastructure/docker/docker-compose.yml up -d`,
   or run each service locally with `scripts/dotnet-run.sh` — see root
   README for both paths).
2. Register a normal account through **the customer client**
   (`apps/angular-client/bus-ticketing-customer-web`, register page) —
   or `POST http://localhost:5101/api/v1/auth/register` directly. This
   creates a `users` row and auto-assigns the `Customer` role.
3. Promote that user to Admin directly in Postgres (there is no
   self-serve path — this is the one manual step the real DB requires):
   ```bash
   docker exec -it bus-ticketing-postgres-auth \
     psql -U auth_svc -d auth_service
   ```
   ```sql
   -- Admin role's id is seeded as a fixed constant — see RoleConfiguration.cs
   INSERT INTO auth.user_roles (user_id, role_id, assigned_at_utc)
   SELECT id, '33333333-3333-3333-3333-333333333333', now()
   FROM auth.users
   WHERE email = 'the-email-you-registered-with@example.com';
   ```
4. Log into this admin console with that email/password. `AuthContext`
   now surfaces an explicit warning banner if the logged-in account's
   roles don't include `Admin` (rather than a confusing 401 on the first
   real screen) — after step 3 that warning should not appear.
5. Buses and Routes screens should now show real data seeded by
   bus-service/route-service's own startup seeding (check each
   service's `Program.cs`/migrations if either table is empty — that's a
   backend data question, not a frontend one).

Full-mock mode (`VITE_USE_MOCK_API=true` in `.env`) still works
end-to-end with no backend at all, for a quick click-through without any
of the above.

## Exact next-step command for whoever picks this up

Nothing was left mid-edit — every change above is committed. To verify
compile-cleanliness (not possible in this sandbox: no network, so
`node_modules` was never installed here — all fixes were done by
reading `.ts`/`.tsx` source against the real C# DTOs directly, not by
running a build):

```bash
cd apps/react-admin/bus-ticketing-admin
npm install
npm run build      # or: npm run dev, then walk through login -> buses -> routes
```

If the build surfaces a type error, it is new information (this sandbox
could not typecheck), not evidence the reasoning above was wrong — fix
it in place and keep going; nothing here needs to be redesigned.

Not yet attempted in this pass, in priority order if more time is
available: (1) `users` module against a real backend once one exists,
(2) `dashboard` stats once an aggregation endpoint exists, (3) an
operator-directory lookup for the Buses table once one exists.
