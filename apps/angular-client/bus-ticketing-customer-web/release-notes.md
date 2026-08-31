# Release Notes — Customer Client (Angular)

## 2026-08-31 — Production-readiness audit (no code changed)

A read-only whole-repo audit ran this date. Findings relevant to this app,
recorded in `docs/PRODUCTION-GAP-ANALYSIS.md` and `docs/API-GAPS.md` at the repo
root (the latter is now the single source of truth for endpoint gaps):

- **Payment page is a simulated card form** hitting a mock-only
  `POST /payments/{id}/confirm` — it never contacts the real payment-service and
  no real charge occurs. Closing this needs a backend tenant-claim + safe confirm
  (milestones M1 + M3).
- **My Bookings** still reads from the in-app mock (`mock-api.interceptor.ts`) —
  `GET /bookings/mine` does not exist server-side (milestone M2).
- **No token refresh** — the app stores `refresh_token` but never uses it;
  `/auth/refresh` exists and works. Sessions drop at the 15-minute access-token
  expiry (milestone M1).
- **No OTP UI** — auth-service exposes `/auth/otp/request` + `/auth/otp/verify`
  with en/bn messages; the login flow is password-only (milestone M1).
- **No i18n** — every string is hardcoded English; no `@angular/localize` /
  ngx-translate (milestone M10).
- **No tests** — no `*.spec.ts` files exist.
- **No correlation-id header** is sent on any request.

No fix was applied in the audit pass; these are tracked in
`docs/PRODUCTION-MILESTONES.md`.

## 2026-08-20 — Real API wiring pass

**Fixed**
- Login and registration now use auth-service's actual token/profile
  contract (`POST /auth/login`, `POST /auth/register` + `GET /auth/me`)
  instead of an assumed shape that never matched the real API — both
  now genuinely work end-to-end against the real backend, including
  refresh-token persistence (previously silently dropped).
- The register form now sends first/last name separately, matching the
  backend requirement, while keeping a single "Full name" field in the
  UI for a simpler sign-up experience.
- Booking creation and cancellation now use the real signed-in
  customer's id instead of a hardcoded placeholder — a real bug that
  would have booked and cancelled everything under one fixed fake
  customer regardless of who was actually logged in.

**Known limitations (documented, not silently broken)**
- "My Bookings" list has no backend endpoint yet (booking-service has
  no list-my-bookings route) — this screen runs on mock data by design.
- Payment confirmation has no backend path yet for this app's identity
  model (payment-service requires a tenant id this app's JWT doesn't
  carry) — this screen also runs on mock data by design. See
  `ai-handover.md` for the full technical reason.

**How to test**
See `ai-handover.md` in this folder for exact steps, including which
screens are expected to show mock data and why.

---
*Earlier history: this is the first dated release-notes entry for this
app. Prior wiring work (initial proxy setup, feature scaffolding, mock
interceptor) is recorded in git history and the root-level
`ai-handover.md`.*
