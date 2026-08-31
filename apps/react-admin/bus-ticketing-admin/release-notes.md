# Release Notes — Admin Console (React)

## 2026-08-31 — Production-readiness audit (no code changed)

A read-only whole-repo audit ran this date. Findings relevant to this app are in
`docs/PRODUCTION-GAP-ANALYSIS.md` and `docs/API-GAPS.md` at the repo root
(`API-GAPS.md` is now the single source of truth for endpoint gaps, superseding
the top-of-file comment in `src/api/mockAdapter.ts`):

- **Still mock-only** (no backend endpoint): `GET /dashboard/stats`, `GET /users`,
  `GET /bookings` (list), `GET /trips` (list). Tracked in milestones M1/M2.
- **No token refresh** — `admin_refresh_token` is stored but never used; sessions
  drop at the 15-minute access-token expiry (milestone M1).
- **No i18n** — hardcoded English only (milestone M10).
- **No tests** — no test files or test runner configured.
- **No correlation-id header** on requests.
- **Stale code comment**: `src/modules/buses/api/buses.api.ts:37` refers to
  `src/modules/operators` as "UI scaffolding" — that directory does not exist.
- First-admin bootstrapping still requires the manual SQL step (or the env-gated
  `DevAdminBootstrapper`) — see this folder's `ai-handover.md`.

No fix was applied in the audit pass; these are tracked in
`docs/PRODUCTION-MILESTONES.md`.

## 2026-08-20 — Real API wiring pass

**Fixed**
- Login and session now use auth-service's actual token/profile contract
  (`POST /auth/login` + `GET /auth/me`) instead of an assumed shape that
  never matched the real API — login against the real backend now
  genuinely works end-to-end, including refresh-token persistence
  (previously silently dropped).
- Signed-in accounts without the `Admin` role now get an explicit
  in-app warning instead of a confusing failure on the first admin
  screen (every `/api/v1/admin/*` route requires that role server-side).
- Buses list (`GET /buses`) now correctly unwraps bus-service's
  `Result<T>` response envelope and maps its real status values — this
  was previously reading from the wrong place in the response and would
  not have shown data at all against the real backend.
- Routes list (`GET /routes`) now resolves real origin/destination stop
  ids to city names (via `GET /stops`) and correctly parses the
  backend's duration format, instead of showing ids/`NaN`.
- Dev-server proxy (`vite.config.ts`) and the production nginx config
  now route every prefix this app calls (including `/admin`, `/stops`,
  and several others that had no route at all before) to the correct
  service on the correct port.

**Known limitations (documented, not silently broken)**
- User management, dashboard KPIs, and the plain bookings/trips list
  views have no backend endpoint anywhere yet — these screens continue
  to run on mock data until a real endpoint exists (see `ai-handover.md`
  for the exact list and why).
- The Buses table shows an operator id, not an operator name — no
  operator-directory service exists yet.
- The Routes table's "Active trips" column always reads 0 — no service
  aggregates a live trip count against a route.

**How to test**
See `ai-handover.md` in this folder — in particular the section on
creating a working Admin login, since the real database has no seeded
admin account and no self-serve way to create one.

---
*Earlier history: this is the first dated release-notes entry for this
app. Prior wiring work (initial proxy setup, module scaffolding, mock
adapter) is recorded in git history and the root-level `ai-handover.md`.*
