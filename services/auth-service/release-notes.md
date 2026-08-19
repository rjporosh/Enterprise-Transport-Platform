# Release Notes — Auth Service Fix Pass

This mirrors what `GET /api/v1/auth/release-info` now reports — that
handler's `BugFixes`, `ChangedFeatures`, and `ConfigurationChanges` lists
were updated in the same commit as the fixes below so the SQA-facing API
and this file stay in sync. A copy of this file also lives at
`docs/new-release/release-notes.md`.

## Summary
Investigated "can't register/login" report and a browser console error from
Scalar's Try-It panel. Found and fixed one critical, high-confidence logic
bug (password reset was 100% broken for every user), closed a CORS gap that
was inconsistent with the rest of the platform, and diagnosed the reported
console error as a client-side browser-extension conflict, not an
auth-service bug. See `ai-handover.md` for full technical detail, what
still needs a real compiler to confirm, and the exact next command.

**This pass could not be compiled locally** — no `dotnet` SDK and no network
access in the working sandbox (confirmed: `apt-get install dotnet-sdk-8.0`
returns HTTP 403 on every package). Every fix below is from manual code
reading, matched against sibling services' established patterns. Treat
this as a diagnosed patch set to verify with a real build, not a
confirmed-green build.

## Fixed (round 2 — real build-warning fixes, from actual `dotnet build` output)
The user provided real `dotnet restore`/`dotnet build` output this round.
Fixed every warning it showed:
- **CS0618**: removed the obsolete Quartz `UseMicrosoftDependencyInjectionJobFactory()` call — it's already the default, zero behavior change. `DependencyInjection.cs`.
- **NU1608** (Pomelo.EntityFrameworkCore.MySql vs EF Core 10): was suppressed on the `PackageReference` itself but that never reached `AuthService.Api.csproj`'s own copy of the same whole-graph warning. Moved to a project-level `<NoWarn>` in both `.csproj` files so it's actually suppressed everywhere it fires.
- **NU1903, Microsoft.OpenApi 2.0.0** (GHSA-v5pm-xwqc-g5wc, high severity DoS): pinned directly to the patched 2.7.5, overriding the transitive version pulled in by `Microsoft.AspNetCore.OpenApi 10.0.0`.
- **NU1903 x4, System.Security.Cryptography.Xml 10.0.6** (GHSA-23rf-6693-g89p and 3 related IDs, the .NET July 2026 EncryptedXml DoS advisory): bumped the existing pin from 10.0.6 to the patched 10.0.10.

All four verified against their advisories via live web search this round (versions/CVEs from mid-late 2026, too recent to trust from memory). Full detail and exact diffs: `ai-handover.md`, "Turn 4."

## Fixed (round 1)

- **Critical: password reset was broken for 100% of requests, always.**
  `ResetPasswordHandler` looked up the reset token using
  `IPasswordHasher.Hash()` — PBKDF2 with a fresh random salt every call, by
  design, for password storage. But `ForgotPasswordHandler` stores the
  token hashed with `ITokenService.HashRefreshToken()` (deterministic
  SHA-256). The two hashes could never match, so every reset attempt threw
  `InvalidResetTokenException` regardless of a valid token. Fixed
  `ResetPasswordHandler` to hash the incoming token with
  `ITokenService.HashRefreshToken()`, matching how it was stored.
  File: `src/AuthService.Application/Features/Auth/ResetPassword/ResetPasswordHandler.cs`.

- **Missing CORS policy.** `auth-service` had no CORS configuration at all,
  while `booking-service` and `payment-service` both explicitly configure
  it for browser clients. Added the same `AllowConfiguredOrigins` policy
  (reads `Cors:AllowedOrigins` from config, matches booking-service's
  origin list: `http://localhost:4200`, `http://localhost:5173`) and wired
  `app.UseCors(...)` before `UseAuthentication()`/`UseAuthorization()`.
  Files: `src/AuthService.Api/Program.cs`, `src/AuthService.Api/appsettings.json`.
  Note: the documented frontend flow (nginx / `ng serve` proxy) is
  same-origin and didn't strictly need this to demo register/login — this
  closes a real inconsistency and protects any direct browser caller
  (Postman-from-browser, a future API gateway, mobile webviews).

## Diagnosed, not a code bug — root cause of the reported console error
```
scalar.js:16396 TypeError: Failed to execute 'fetch' on 'Window':
Cannot construct a Request with a Request object that has already been used.
    at window.fetch (injected-interceptor.js:1:2106)
    at async Object.sendRequest (scalar.js:29814:9)
```
`injected-interceptor.js` is a third-party browser extension monkey-patching
`window.fetch` before Scalar's own "Try it" panel gets to use it — the
error fires while *constructing* the `Request`, before anything leaves the
browser. This cannot be fixed from the API side. **Next step for whoever
hits this: retest in an incognito window with extensions disabled, or test
the same endpoint with curl/Postman.** If register/login still fails
outside the browser entirely, that's new information and a different bug —
see `ai-handover.md`, "Next command."

## Checked and found OK (not changed)
- Full request path for `/register` and `/login`: endpoint → validator →
  handler → `AuthDbContext` → migrations. No other hash/lookup mismatches
  found (checked OTP, security-answer, and refresh-token verification —
  all self-consistent, deterministic SHA-256 both ends).
- `RegisterHandler`/`LoginHandler`: role seeding, race-condition handling on
  concurrent registration, account lockout, timing-safe "user not found"
  vs "wrong password" — all read correctly.
- Migrations: `InitialCreate` + `AddSecurityAdminFeatures` reconcile
  cleanly against `AuthDbContextModelSnapshot.cs` (checked the `roles`
  table's `IsActive` column end-to-end — added in the second migration
  with a `false` default then backfilled to `true` via `UpdateData`, model
  snapshot agrees). Program.cs auto-applies migrations on startup in
  Development only, which is correctly enabled in docker-compose.
- All 6 `.csproj` files in the service: consistent `net10.0` targeting,
  every `ProjectReference` path resolves to a real file, no unusual
  package pins beyond what's already commented/justified in the files.
- Brace/paren balance across all `.cs` files under `services/auth-service`
  (heuristic only — not a substitute for a real compiler).

## Not fixed / left alone (out of scope per task instructions)
- `infrastructure/docker/docker-compose.yml` was **not** touched, even
  though `booking-service`'s entry there additionally sets
  `Cors__AllowedOrigins__0/1` as an explicit env override — that file is
  shared infrastructure outside `services/auth-service/`, and the
  appsettings.json default added here already covers the same origins, so
  auth-service's CORS fix works without touching it.
- No new tests were added, even though there was **zero existing test
  coverage** for the forgot-password → reset-password round trip (which is
  likely why this bug shipped unnoticed). Flagged for the next agent
  rather than added here, to stay within "fix, don't add features."

## Next for the next agent (or you) — see ai-handover.md for full detail
1. Get a working `dotnet` SDK + network, `dotnet build` the solution, fix
   whatever the compiler finds that this pass couldn't see.
2. `dotnet test` — especially add coverage for forgot-password →
   reset-password, since that's exactly the kind of bug a missing test let
   through.
3. Re-test register/login from an incognito browser window (extensions
   off) to rule the console-error report in or out as a real backend issue.
