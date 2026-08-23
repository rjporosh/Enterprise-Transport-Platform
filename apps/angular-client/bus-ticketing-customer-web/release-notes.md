# Release Notes — Customer Client (Angular)

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
