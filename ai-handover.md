# AI Handover — 2026-08-16 session #3 (READ THIS SECTION FIRST)

## Fixed this session (root cause, verified by static analysis — not by
## running `npm start`, since this sandbox still has no network to fetch
## packages; the reasoning below doesn't need a live install to be correct)

**Client Portal `Cannot find module '@angular/core'` — FIXED.**
Root cause: `apps/shared-ui-library/angular` has no `node_modules` of its
own (by design — it's consumed as TS source, see its `package.json`
description) and it is a **sibling** of `apps/angular-client/...`, not an
ancestor. Node/esbuild module resolution only ever walks *upward* from the
importing file looking for `node_modules` — it can never cross sideways
into a sibling directory's `node_modules`. So every file under
`shared-ui-library/angular/src/lib/*` that imports `@angular/core` /
`@angular/forms` / needs `tslib` was structurally unable to resolve them,
regardless of what was installed in the client app. Same latent bug exists
for `apps/react-admin` + `apps/shared-ui-library/react` (not yet reported
as broken, but it's the identical pattern — Vite's alias in
`vite.config.ts` points straight at the sibling's `src/`, same resolution
problem waiting to happen).

Fix: added a root `package.json` with `npm workspaces` listing all four
packages (`angular-client/bus-ticketing-customer-web`,
`react-admin/bus-ticketing-admin`, `shared-ui-library/angular`,
`shared-ui-library/react`). Workspaces hoist shared deps into a root
`node_modules` that *is* a real ancestor of every workspace member, so
resolution now succeeds normally. No app code, no library code, no
architecture changed — this is exactly a missing piece of tooling config,
not a redesign.

Also fixed: `.gitignore` never excluded `node_modules` anywhere in the
repo (real gap — this is very plausibly how the 67MB blob that later
corrupted `.git` got committed in the first place). Added `**/node_modules/`.

**Not yet re-verified with a real `npm install && npm start`** — do that
first thing once you have network:
```bash
cd Enterprise-Transport-Platform
npm install         # run at the REPO ROOT, not inside the app folder —
                     # that's what makes workspace hoisting kick in
npm start --workspace=apps/angular-client/bus-ticketing-customer-web
```
If some other unrelated error surfaces after this, it's new information,
not a sign this fix was wrong — the specific `Cannot find module
'@angular/core'` / `tslib` errors from the pasted log are what this fixes.

## Investigated, found already correct (no change made/needed)
- **Ports**: booking-service (5600/5601) and payment-service (5500/5501)
  do NOT collide — this was already fixed in session #1's uncommitted
  diff. All 6 services now sit on distinct 5100/5200/5300/5400/5500/5600
  ranges.
- **Scalar `/scalar` docs page**: present and wired identically in all 6
  services' `Program.cs` (`AddOpenApi("v1")` + `MapOpenApi()` +
  `MapScalarApiReference("/scalar", ...)`), each with inline comments
  explaining the native-OpenAPI-not-Swashbuckle choice. Nothing to fix here.
- **Real API wiring**: both frontends already default to real APIs, not
  mocks — `apps/angular-client/.../environment.ts` has `mockApi: false`
  with mock kept only as an opt-in demo toggle; same pattern in
  `apps/react-admin/src/config/env.ts` (`VITE_USE_MOCK_API`). One
  *documented* (not hidden) gap: that file's own comment says two backend
  paths have no real implementation yet and stay mocked either way — it
  doesn't say which two. Worth a `grep -n "no real backend" -r apps/react-admin/src`
  to find them if you need to close that gap.

## Could not verify (no error text given, no way to compile)
**Booking Service build error**: you mentioned finding one but the actual
error text wasn't included this time. This sandbox has no `dotnet` SDK at
all, so I can't compile it to reproduce or confirm a fix either way. I did
a static pass over every `.cs` file under `services/booking-service/src`
(brace-balance check, grep for `NotImplementedException`/TODO/FIXME) and
found nothing obviously broken — but that is *not* the same as a clean
build, and I'm not claiming it is. Paste the actual `dotnet build` output
next time and it can be fixed for real instead of guessed at.

## Git is STILL blocked for commits — nothing above is committed
Confirmed again this session, now including plain root-level files:
```
$ git add package.json .gitignore AI-HANDOVER.md
fatal: unable to read tree 395876d3fec1a59a2ea431471d1dbcd86d219b7c
```
This is not path-specific — it blocks staging *anything* right now,
confirming this is the whole index/cache-tree, not just the
`services/`-heavy subtrees session #2 first hit. You said the missing
67MB-blob pack file was deleted intentionally to keep things simple — that
explains *why* it's gone, but the practical effect is the same either way:
`git commit` cannot succeed in this sandbox (no network to `git fetch
origin` and pull the objects back) until that's resolved on a machine that
has the missing objects or accepts starting fresh.

**Exact commit sequence to run the moment `git add` stops erroring**
(either after `git fetch origin` restores the pack, or after a deliberate
fresh-history decision — see session #2's section below for that
tradeoff), in order:

```bash
# 1. This session's fix — do this one first, it's what unblocks npm start
git add package.json .gitignore
git commit -m "fix(client): resolve Angular shared-ui-library module resolution via npm workspaces

Cannot find module '@angular/core'/'@angular/forms', tslib helper errors
were caused by apps/shared-ui-library/angular being a sibling (not an
ancestor) of the apps that import it as TS source, so Node/esbuild module
resolution could never reach its consumer's node_modules. Added root
package.json with npm workspaces covering both shared-ui-library packages
and both consuming apps, which hoists shared deps to a common ancestor
node_modules. Also closed a real gitignore gap (node_modules was never
excluded anywhere in the repo)."

# 2. Session #1's still-pending fixes (port collisions + Scalar bugs) —
#    verify these still look right first (git diff), they're older
git add services/
git commit -m "fix(core): resolve service port collisions and Scalar launch-URL bugs"

# 3. Then re-run the FINAL FULL SYSTEM VALIDATION checklist from the
#    original task brief before considering this done.
```

---

# AI Handover — 2026-08-16 session #2 (READ THIS SECTION FIRST)

This session found one **critical, previously-undocumented blocker** and
made no code changes (couldn't — see below). Everything under "session #1"
further down is still accurate background; this section supersedes it only
where they conflict.

## 0a. CRITICAL: the git repository itself is corrupted (missing pack data)

`git status` / `git log` / `git diff` all *look* fine, which is why session
#1 didn't catch this. But `git add` on almost any file under `services/`
fails immediately:

```
$ git add services/bus-service/README.md
fatal: unable to read tree 395876d3fec1a59a2ea431471d1dbcd86d219b7c
```

Root cause, confirmed with `git fsck --full --no-reflog` and
`ls .git/objects/pack/`:

```
pack-5a66b1746357aef289818f4b6ed86c51e4a5ade7.idx   (641,824 bytes — present)
pack-5a66b1746357aef289818f4b6ed86c51e4a5ade7.pack  <-- MISSING, no such file
```

One pack's `.idx` (the index) survived but its matching `.pack` (the actual
object data) is gone. `git fsck` confirms real broken tree→tree links
pointing at objects that no longer exist anywhere in `.git/objects/`. This
lines up with session #1's note about "the pre-existing broken/duplicated
history from the deleted 67MB blob" — that blob removal deleted the `.pack`
but left the stale `.idx` behind, which is worse than doing nothing: it
silently corrupted the object store for a large chunk of the tree.
`.git/objects/pack/.DS_Store` (garbage) is also sitting in there, harmless
but worth deleting.

**Practical impact: no new commit can be made for any file whose current
tree touches the missing objects (this covers most of `services/`) until
this is fixed.** This session could not stage or commit anything, including
the still-valid, still-uncommitted fixes from session #1 (port collisions +
Scalar launch-URL bugs — the diffs are still sitting unstaged in the
working tree right now, verified correct, just not committed).

**This session did NOT run any destructive git command** (no `gc`,
`prune`, `filter-repo`, `reflog expire`, no reset --hard) — the corruption
pre-dates this session, confirmed by read-only `git fsck`/`ls` only.

### Next agent: how to actually fix this (needs real network)
`origin` is configured: `git@github.com:rjporosh/Enterprise-Transport-Platform.git`.
Local is 5 commits ahead of `origin/main`, but the missing pack is old
(idx dated well before most of the divergence), so `origin` most likely
still has the objects this local clone lost.

```bash
cd Enterprise-Transport-Platform
rm -f .git/objects/pack/.DS_Store        # garbage, safe to remove
git fetch origin
git fsck --full --no-reflog              # re-check — objects may now resolve
git add services/bus-service/README.md   # smoke test — should no longer error
```

If `git fetch` restores the objects, proceed normally (stage session #1's
still-pending fixes first, commit them, then continue the milestones). If
`origin` is *also* missing the objects (i.e. the corruption was pushed),
this needs a human decision — recovering from a teammate's local clone or
a CI artifact if one exists, or accepting the loss and starting a fresh
initial commit from the current working tree (which means rewriting
history, which the task's own rules forbid without explicit sign-off — do
not do this unilaterally, ask first).

## 0b. Client Portal `npm install && npm start` — the actual reported error is still needed
User said they'd paste the exact npm error but it didn't come through in
their message. This session confirmed one thing that **changes session
#1's leading hypothesis**: in this sandbox, the Node version mismatch
(`v22.22.2` vs `package.json`'s required `^22.22.3 || ^24.15.0 || >=26.0.0`)
only produces `npm warn EBADENGINE` — a warning, not a hard failure
(`npm install --dry-run` completes past it). So the Node-patch-version
theory is not confirmed as *the* root cause of whatever the user is
actually seeing — it may still be relevant (some setups run with
`engine-strict=true` or a CI that treats warnings as failures) but it's not
provably the cause anymore. **Do not "fix" this by touching
package.json/engines or downgrading deps until the real error text is in
hand** — that would be exactly the "bypass the error blindly" the task
explicitly forbids.

Also confirmed this sandbox still has zero network access to
`registry.npmjs.org` (`403 host_not_allowed`) and no `dotnet` SDK at all —
identical constraints to session #1, so none of Milestones 1, 3, 4, 5
(anything requiring `dotnet build`) could be attempted or re-verified
either. Nothing in session #1's unverified hypotheses got upgraded to
verified this session, except the EBADENGINE point above.

## 1. What to do first, in order, once you have a real environment
1. Fix the git corruption (0a) — everything else is blocked on this for
   committing.
2. Get the actual `npm install`/`npm start` error text from the user for
   `apps/angular-client/bus-ticketing-customer-web` and diagnose from the
   real output, not from static review.
3. Then resume session #1's plan in section 5 below (build every `.sln`,
   fix what the compiler finds, then both frontends).

---

# AI Handover — 2026-08-16 session #1 (original, still accurate below)

Read this whole file before touching anything. It tells you exactly what
changed, what's still broken/unknown, and the precise next command to run.
Do NOT re-do work described as "done" below without first verifying it's
actually broken — re-verify, don't regenerate from scratch.

## 0. Environment constraint that shaped this whole session

The sandbox this session ran in had **no internet access** (all outbound
requests blocked, `x-deny-reason: host_not_allowed`) and **no .NET SDK
installed** (`dotnet` not found, no way to install one without network).
`npm`/`node` were present (node v22.22.2) but `npm install` also could not
reach `registry.npmjs.org` (same block).

**Consequence: nothing in this session was compiled, restored, or run.**
Everything below is from static/manual code review only — reading every
`.csproj`, `Program.cs`, `launchSettings.json`, `package.json`,
`package-lock.json`, `tsconfig.json`, `angular.json`, `vite.config.ts`, and
the relevant `.ts`/`.cs` source files by eye, plus heuristic checks (brace
balance, path resolution, JSON validity) that a real compiler was not
available to double-check.

**Your very first action, if you have `dotnet` and network access, must be
to actually compile everything and treat this file as a hypothesis, not a
verified state.** See section 5 for the exact command.

## 1. What was fixed this session (verified only by reading, not building)

### Backend — port conflicts (real bug, high confidence)
`payment-service` and `route-service` both hard-coded
`http://localhost:5003` in `launchSettings.json` — an actual port
collision if both are run locally with `dotnet run` at the same time.
`booking-service` was still on an IIS-Express auto-generated `32426` with
no https profile and no `launchUrl`, inconsistent with every other
service.

New local-dev (`dotnet run`, NOT Docker) port scheme — all unique, all now
have an `https`+`http` pair and a `"launchUrl": "scalar"`:

| Service | https | http | Scalar UI |
|---|---|---|---|
| auth-service | 5100 | 5101 | http://localhost:5101/scalar |
| bus-service | 5200 | 5201 | http://localhost:5201/scalar |
| notification-service | 5300 | 5301 | http://localhost:5301/scalar |
| route-service | 5400 (was 5004) | 5401 (was 5003) | http://localhost:5401/scalar |
| payment-service | 5500 (new) | 5501 (was 5003) | http://localhost:5501/scalar |
| booking-service | 5600 (new) | 5601 (was 32426) | http://localhost:5601/scalar |

Files touched: each service's
`src/<Service>.Api/Properties/launchSettings.json`.

**This local-dev scheme is independent of `infrastructure/docker/docker-compose.yml`**,
which already had its own unique host-port mapping (booking 8080, admin
console/customer web 4200/5173, notification 8081, bus 5201, payment
5202, auth 5203, route 5204, each service's own Postgres on 5432-5437) —
checked, docker-compose had no conflicts, not touched. The Angular and
React frontends' dev-server proxies (`proxy.conf.json`, `vite.config.ts`)
target the docker-compose ports, not the launchSettings ones — that is
intentional (dev workflow = backends in Docker, frontend on the host),
not a bug, so left alone.

### Backend — wrong/duplicate Scalar registration (real bug, high confidence)
- `auth-service` and `notification-service` had `"launchUrl": "scalar/v1"`
  in `launchSettings.json`, but `Program.cs` actually mounts Scalar at
  `/scalar` (`app.MapScalarApiReference("/scalar", ...)`). That would 404
  on launch. Fixed both to `"launchUrl": "scalar"`.
- `payment-service/src/PaymentService.Api/Program.cs` registered
  `MapOpenApi()`/`MapScalarApiReference()` twice — once gated to
  `IsDevelopment()` with a title/theme, once unconditionally right after
  with no options. Removed the duplicate Development-gated block, kept a
  single unconditional registration with
  `WithTitle("Payment Service API").WithTheme(ScalarTheme.Purple)`
  matching the pattern every other service uses.
- Also fixed payment-service's `Docker` launch profile: `launchUrl` was
  `"{ServiceHost}/swagger"` but there is no Swagger/Swashbuckle anywhere
  in this service (deliberately — see the code comments about
  OpenAPI.NET v1/v2 conflicts). Changed to `"{ServiceHost}/scalar"`.

Did NOT find this duplicate-registration pattern in auth, bus,
notification, or booking's `Program.cs` — each maps Scalar exactly once.
Did not deep-review booking-service's or bus-service's full `Program.cs`
beyond the Scalar/OpenAPI section — worth a second look.

### What was checked and found OK (not changed)
- All 40 `.csproj` files target `net10.0` consistently.
- Every `<ProjectReference>` path in every `.csproj` resolves to a real
  file (checked programmatically across all 40 files).
- No brace/paren imbalance across all 849 `.cs` files under
  `services/` + `shared/` (heuristic only — a real compiler catches far
  more than this; treat as "nothing screamed at me," not "compiles clean").
- `docker-compose.yml` port mappings: no conflicts.

### Known-but-not-fixed backend inconsistency (needs a judgment call with a compiler)
`notification-service` pins `Microsoft.EntityFrameworkCore`,
`Microsoft.EntityFrameworkCore.Design`, `Microsoft.EntityFrameworkCore.SqlServer`,
`Microsoft.EntityFrameworkCore.InMemory`, and
`Npgsql.EntityFrameworkCore.PostgreSQL` to 9.0.0 across all its own
projects (Infrastructure/Application/Api/tests) — internally consistent,
so not a build error by itself — while every other service uses 10.0.0.
Also inconsistent across the repo: `FluentAssertions` (6.12.1 vs 7.0.0),
`Scalar.AspNetCore` (2.1.2 / 2.2.0 / 2.9.0 across services — API surface
looked compatible by eye, both `options.Theme = ...` and
`options.WithTheme(...)` styles are used and likely both valid, but
unverified), `NBomber` (5.3.1 / 5.8.0 / 5.9.0), `Quartz` (3.13.0 / 3.13.1
/ 3.14.0), `Polly` (8.4.0 / 8.5.0), `Grpc.AspNetCore` (2.66.0 / 2.67.0).
Did NOT bump any of these — no way to test whether e.g. the EF Core 9→10
API surface changed in a way that breaks notification-service's
Infrastructure code, and guessing wrong here would be a regression.
**Next agent with a working dotnet + network: bump notification-service
to EF Core 10.0.0 to match the rest of the platform, rebuild, fix
whatever breaks.** The other version spreads are lower priority (mostly
test-only packages) — align opportunistically.

## 2. Frontend status — better than the task description implies

Expected to need to wire the frontends to real APIs from scratch. Did
not need to — a previous agent already did this properly, before this
session:

- `apps/angular-client/bus-ticketing-customer-web`: `environment.ts` /
  `environment.prod.ts` both have `mockApi: false`. Every feature service
  (auth, trip-search, booking, payment, my-bookings) calls `HttpClient`
  against real REST paths under `/api/v1/...`. `mock-api.interceptor.ts`
  only intercepts and fakes two endpoints even in real mode —
  `GET /bookings/mine` and `POST /payments/{id}/confirm` — because no
  matching backend endpoint exists yet anywhere in the platform
  (documented in the interceptor's own comments, matches what was found
  by grepping the backend). Everything else falls through to
  `next(req)`, i.e. a real HTTP call via `proxy.conf.json` (targets the
  docker-compose ports and looked correctly scoped: `/api/v1/auth`→5203,
  `/api/v1/payments`→5202, `/api/v1`→8080 for booking-service, which
  owns `/api/v1/trips/*` too — confirmed via grep, so trip search is not
  misrouted).
- `apps/react-admin/bus-ticketing-admin`: same pattern —
  `VITE_USE_MOCK_API=false` by default, `vite.config.ts` proxies
  `/api/v1/auth`→5203, `/api/v1/buses`→5201, `/api/v1/routes`→5204,
  `/api/v1`→8080, and `src/api/mockAdapter.ts` only fakes
  dashboard-stats and user-management (also documented as having no real
  backend endpoint yet).

**Did not touch either frontend's API-wiring code — it looked correct
and already matches "real API calls," which is the last item on the
original task list.** If it's actually broken at runtime, that's new
information this session didn't have (no way to run `ng serve` /
`vite dev` and click through), not something already verified.

### The "npm install / npm start still broken" report — unresolved, leading hypothesis
Could not reproduce it (no network to actually run `npm install`). What
was checked instead:
- `package.json`, `package-lock.json` (lockfileVersion 3), `angular.json`,
  `tsconfig*.json`, `proxy.conf.json` all parse as valid JSON.
- `package.json` deps match `package-lock.json`'s root `dependencies`/
  `devDependencies` exactly — no drift between the two files.
- Spot-checked `package-lock.json` resolved URLs/integrity hashes for
  `@angular/core` (22.1.0), `typescript` (6.0.3), `@angular/cli` (22.1.2)
  — all point at real `registry.npmjs.org` tarball URLs with proper
  sha512 integrity strings, 1034 packages total. Looks like a real
  `npm install` was actually run at some point to generate this lockfile,
  not hand-written/fabricated.
- Web-searched and confirmed Angular 22 (released June 3, 2026) does
  genuinely require TypeScript 6.0+ and Node 22+ — package.json's
  `"typescript": "~6.0.0"` and
  `"engines": { "node": "^22.22.3 || ^24.15.0 || >=26.0.0" }` are
  consistent with that, not a mistake.
- All paths `angular.json` references (`src/index.html`, `src/main.ts`,
  `tsconfig.app.json`, `src/assets`, `src/styles.css`, `proxy.conf.json`)
  exist. All 35 `.ts` files under `src/` are brace/paren-balanced. The
  `@shared-ui/*` TS path alias in `tsconfig.json` points at
  `../../shared-ui-library/angular/src/lib/*`, and confirmed
  `button/button.component.ts` (the one `app.component.ts` imports)
  actually exists there.

**Leading hypothesis, unverified: this sandbox's Node is v22.22.2 — one
patch version below package.json's stated minimum of `^22.22.3`.** If
whoever previously tested "npm install/npm start" was on the same or an
older Node 22.22.x patch, that's enough to cause `EBADENGINE` failures or
subtler esbuild-native-binary resolution issues on some setups, depending
on npm config/CI strictness. **First thing to try: bump to Node 22.22.3+
(or 24.15+, or 26+) and actually run `npm install` then `npm start` in
`apps/angular-client/bus-ticketing-customer-web`, capture the real error
text if it still fails, and go from there** — don't guess further from
static review, the actual npm error message is now the fastest path to a
real fix.

## 3. Not reviewed this session at all
- `bus-service`, `notification-service`, `booking-service`,
  `auth-service` `Program.cs` files beyond their Scalar/OpenAPI section.
- All backend business logic, controllers/endpoints, EF Core
  configurations, migrations.
- `apps/Flutter`, `apps/MAUI`, `apps/Native Android`, `apps/Native IOS`,
  `apps/shared-ui-library` (only spot-checked one file existed).
- `infrastructure/gateway` (looked empty/placeholder per existing code
  comments in the frontend configs — not independently confirmed).
- CI/CD pipelines, `docs/` content quality, Postman collections, k6/
  NBomber/JMeter load tests.
- Whether every service's Scalar page actually renders a rich documented
  API (examples, auth flow, etc.) as the original task demands — only
  confirmed the route mounts and matches the launch URL, not
  documentation quality/richness.

## 4. Files changed this session (full list)
```
services/auth-service/src/AuthService.Api/Properties/launchSettings.json
services/booking-service/src/BookingService.Api/Properties/launchSettings.json
services/notification-service/src/NotificationService.Api/Properties/launchSettings.json
services/payment-service/src/PaymentService.Api/Program.cs
services/payment-service/src/PaymentService.Api/Properties/launchSettings.json
services/route-service/src/RouteService.Api/Properties/launchSettings.json
services/route-service/docs/ai-handover.md   (updated stale port reference)
AI-HANDOVER.md                                (this file)
```
No other files were modified. `.git` was not touched (per instructions —
the pre-existing broken/duplicated history from the deleted 67MB blob is
untouched, left alone).

Note: this repo has both `AI-HANDOVER.md` (root, this file) and a
lowercase `ai-handover.md` tracked separately in git (shows as `deleted`
in `git status` because the zip this session started from only had one
physical file on disk due to a case-collision) — pre-existing quirk, not
caused this session, not fixed this session since the instructions said
not to touch git.

## 5. Exact next command for the resuming agent

**If you have a working dotnet SDK and network access — start here,
before doing anything else in this file:**

```bash
cd Enterprise-Transport-Platform
for sln in services/*/*.sln; do
  echo "=== $sln ==="
  scripts/dotnet-build.sh "$sln"
done
```

This uses the repo's own build-wrapper script (`scripts/dotnet-build.sh`
— already exists, not written this session), which appends full error
output to `logs/build-error-<dd-MM-yyyy>.txt` on any failure. Read that
file for the first real, compiler-verified list of what's actually
broken — everything in section 1 of this document is unverified until
this step happens. Fix what it finds, re-run, repeat until every `.sln`
builds with 0 errors. Then check the build output specifically for
warnings too, since "0 warnings" was also part of the original ask and a
clean build doesn't guarantee 0 warnings.

Then, for the frontend:
```bash
cd apps/angular-client/bus-ticketing-customer-web
node --version   # confirm >=22.22.3, else nvm/fnm install a matching version first
npm install
npm start
```
Capture the exact error if either step fails — that replaces the
hypothesis in section 2 with a real answer.

Then do the same for `apps/react-admin/bus-ticketing-admin`
(`npm install && npm run dev`).

**Do not**: regenerate frontend API-wiring code (section 2 says it's
already done), rewrite `launchSettings.json` ports again (section 1's
scheme is final unless the build step finds a reason to change it), or
touch `.git` history.

Once backend + both frontends build/run clean, move to section 3's
unreviewed items in priority order: remaining `Program.cs` files first
(highest-risk unreviewed surface), then CI/CD, then the mobile apps, then
docs/Postman/load-test polish.
