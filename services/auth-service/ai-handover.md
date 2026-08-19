# AI Handover — auth-service fix pass

## Environment note (read this first)
This pass ran in a sandbox with **no `dotnet` SDK and no network access**.
Confirmed by trying `apt-get install dotnet-sdk-8.0` — every package (even
plain Ubuntu archive packages, not just NuGet) came back `403 Forbidden`.
So: no `dotnet build`, no `dotnet ef migrations list`, no `dotnet test`, no
way to spin up Postgres/Redis/RabbitMQ and actually POST to `/register` or
`/login`. Everything below is from reading the source line by line and
cross-checking it against sibling services' equivalent code (booking-service,
payment-service) for pattern consistency. **Nothing here has been compiled
or run.** Treat this as a diagnosis + patch set to verify, not a
confirmed-green build. The previous agent session (see git log —
`fix(core): resolve service build warnings` and the deleted top-level
`AI-HANDOVER.md`) hit the identical sandbox limitation; this is not new.

## What was asked
1. Zero build warnings / zero build errors on auth-service.
2. Fix "can't register/login," including a browser console error pasted
   from Scalar's Try-It panel.
3. Confirm migrations are in order.
4. Don't touch anything outside auth-service. Professional git messages,
   one per logical fix. Update release-notes.md + this file with exact
   resume state if I ran out of budget (I did, once, mid-session — see
   "Session history" below).

## Session history (now three turns)
**Turn 3:** User reported "nothing fixed nothing touched" and asked again
for the same fix plus updates in `docs/new-release/release-notes.md`
specifically (mirroring the pattern seen in `route-service`/`payment-service`,
where `GET /api/v1/auth/release-info` — already implemented in this
service, see `GetReleaseInfoHandler.cs` — is the SQA-facing source of
truth). Verified via `git log` that turns 1–2's three commits
(`563e4226`, `64408341`, `7582c69b`) were never lost — they're present in
this working copy and in the delivered zip's `.git` history (confirmed by
re-extracting and running `git fsck` on it before delivery). Nothing was
actually un-fixed. What was missing: the *live* SQA release-info API
response (`GetReleaseInfoHandler.cs`) still had empty `BugFixes` /
`ChangedFeatures` lists — the fix existed in code but wasn't reflected
in the data that endpoint reports. Fixed that in this turn, and added
`docs/new-release/release-notes.md` at the exact path requested (a copy
of the existing root `release-notes.md`, kept in sync).

**If you're re-reading this because you think nothing was done: check
`git log --oneline -- services/auth-service` in the delivered zip.** You
should see commits `563e4226`, `64408341`, `7582c69b`, and this turn's
follow-up commits, in that repo's history, not just described in prose.

## Session history (turns 1–2)
**Turn 1:** Extracted the zip, confirmed no dotnet/network, read
Program.cs, DI (Application + Infrastructure), AuthEndpoints.cs,
RegisterHandler, LoginHandler, RefreshTokenHandler, LogoutHandler,
ChangePasswordHandler, ForgotPasswordHandler, ResetPasswordHandler,
OtpService + handlers, SecurityAnswerValidator + handlers, PasswordHasher,
JwtTokenService, all 6 `.csproj` files, both migrations +
AuthDbContextModelSnapshot.cs, RoleConfiguration, Dockerfile, docker-compose
entry, nginx.conf / vite proxy / Angular proxy.conf.json for the frontends.
Found the `ResetPasswordHandler` hash mismatch and fixed it. Ran out of
turn budget before finishing the CORS addition, before writing these docs,
and before any commit.

**Turn 2 (this one):** Verified the turn-1 fix was still present and
uncommitted (`git status` showed only the ResetPasswordHandler diff, as
expected — nothing lost). Finished the CORS policy in Program.cs +
appsettings.json, double-checked it against booking-service's exact
pattern, confirmed no test file references `ResetPasswordHandler`'s
constructor directly (so the new `ITokenService` parameter doesn't break
any test compile), wrote release-notes.md + this file, and will commit
next (see "Files changed" below for the exact commit plan).

## What was fixed

### 1. Password reset — critical logic bug, 100% failure rate
**File:** `src/AuthService.Application/Features/Auth/ResetPassword/ResetPasswordHandler.cs`

`ForgotPasswordHandler` (unchanged, was already correct):
```csharp
var tokenHash = _tokenService.HashRefreshToken(rawToken);   // deterministic SHA-256
var resetToken = new PasswordResetToken(..., tokenHash, ...);
_context.PasswordResetTokens.Add(resetToken);
```

`ResetPasswordHandler` (before this fix):
```csharp
var tokenHash = _passwordHasher.Hash(request.Token);   // PBKDF2, RANDOM SALT every call
var resetToken = await _context.PasswordResetTokens
    .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);
```

`PasswordHasher.Hash()` (`src/AuthService.Infrastructure/Security/PasswordHasher.cs`)
generates a new random 16-byte salt on every call — it's designed for
one-way user-password storage, not for producing a repeatable lookup key.
Calling it twice on the identical input string produces two different
output strings. So the token hash computed at reset time could **never**
equal the hash stored at forgot-password time, for any token, ever. Every
`/reset-password` call would hit `resetToken is null` and throw
`InvalidResetTokenException`, indistinguishable from "your link expired."

**Fix:** changed the lookup to `_tokenService.HashRefreshToken(request.Token)`
— the same deterministic SHA-256 method used to produce the stored hash.
Added `ITokenService` as a constructor dependency (already registered as a
singleton in `AuthService.Infrastructure/DependencyInjection.cs`, so no DI
wiring change needed). `IPasswordHasher` is still used, correctly, later in
the same method to hash the *new* password for storage — that usage was
never wrong.

**Confidence:** high. This isn't a maybe — `Rfc2898DeriveBytes.Pbkdf2` with
`RandomNumberGenerator.GetBytes(16)` for the salt on every call is
definitionally non-reproducible. No test exercised this path (see "Gaps"
below), which is exactly how it shipped unnoticed.

**Please verify:** once you have a real `dotnet test` or a live DB, run
forgot-password → grab the token → reset-password → login with the new
password, end to end. I could not.

### 2. CORS — missing entirely, added for platform consistency
**Files:** `src/AuthService.Api/Program.cs`, `src/AuthService.Api/appsettings.json`

Before this fix, `auth-service` had zero mentions of CORS anywhere in
`src/`. `booking-service/src/BookingService.Api/Program.cs` and
`payment-service/src/PaymentService.Api/Program.cs` both configure it:

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowConfiguredOrigins", policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
        policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod();
    });
});
// ...
app.UseCors("AllowConfiguredOrigins");
```

Added the identical pattern to auth-service, `UseCors` placed before
`UseRateLimiter()`/`UseAuthentication()`/`UseAuthorization()` (CORS must run
before those for preflight `OPTIONS` requests to get a response at all).
`appsettings.json` now has:
```json
"Cors": { "AllowedOrigins": [ "http://localhost:4200", "http://localhost:5173" ] }
```
matching booking-service's exact origin list (Angular `ng serve` default
port 4200, Vite/React default 5173 — confirmed against
`apps/angular-client/.../package.json` and
`apps/react-admin/.../vite.config.ts`).

**Important caveat — this is very likely NOT what caused your reported
error.** I traced the actual frontend request path:
- `ng serve` dev mode → `proxy.conf.json` proxies `/api/v1/auth` → same
  origin as the Angular dev server → no CORS involved.
- Production/docker-compose → `nginx.conf` in the customer-web container
  reverse-proxies `/api/v1/auth/` → `auth-service:8080` server-side → the
  *browser* only ever talks to the same origin (port 4200) → no CORS
  involved either.

So in both documented flows, the browser never makes a cross-origin request
directly to auth-service — CORS wouldn't have blocked register/login in
either. I added it anyway because (a) it's a real, confirmed inconsistency
with two sibling services that clearly consider it necessary, (b) it's
additive and can't break the proxied flows, and (c) it protects any caller
that isn't going through the documented proxy (Postman "Send from browser"
mode, a mobile webview, a future API gateway, someone testing the raw
`:5203`/`:5101` port directly). It's a legitimate hardening fix, not a
misdiagnosed root cause — see below for what the actual root cause of your
specific error most likely is.

### 3. NOT a code fix — diagnosis of the pasted console error
```
scalar.js:16396 TypeError: Failed to execute 'fetch' on 'Window':
Cannot construct a Request with a Request object that has already been used.
    at window.fetch (injected-interceptor.js:1:2106)
    at async Object.sendRequest (scalar.js:29814:9)
    at async O (scalar.js:29850:13)
```
Reading this stack bottom-to-top: Scalar's own "Try it" button
(`scalar.js`, `sendRequest`) calls `window.fetch(...)`. But `window.fetch`
has been overwritten by `injected-interceptor.js` — that filename pattern
is not part of Scalar or any package in this repo; it's the signature of a
browser extension injecting a content script into every page (ad blocker,
privacy tool, a corporate proxy/DLP agent, a "CORS unblock" extension, a
request-logging devtool, etc.). That interceptor's wrapper is internally
reusing a `Request` object across what looks like a retry/logging path,
which the Fetch API spec forbids once the `Request`'s body stream has been
read — hence "already been used." **This throws before any bytes leave the
browser.** It cannot be caused by, or fixed in, `auth-service`'s C# code —
there's nothing server-side to patch for a client-side extension conflict.

**What to actually do about it:**
1. Open the Scalar page (`/scalar`) in an Incognito/Private window with all
   extensions disabled, or in a browser profile with no extensions, and
   retry Register/Login there.
2. If it works there → confirmed extension conflict, not a backend bug.
   Identify which extension (disable them one at a time) if you want it
   fixed permanently, or just always test APIs from a clean profile.
3. If it still fails there too → that's new information this session
   didn't have, and now the *actual* error (likely a real HTTP status +
   body from the API, not a client-side TypeError) is the next thing to
   read. Test the same request with `curl` to rule the browser out
   entirely:
   ```bash
   curl -i -X POST http://localhost:5203/api/v1/auth/register \
     -H "Content-Type: application/json" \
     -d '{"email":"test@example.com","password":"SomeStrongPassw0rd!","firstName":"Test","lastName":"User"}'
   ```
   (port 5203 = docker-compose; use 5101 for local `dotnet run`.)

## Migrations — checked, found consistent
Two migrations: `20260802071041_InitialCreate` and
`20260810134211_AddSecurityAdminFeatures`. Traced the `roles` table
specifically end-to-end since it's the one register/login actually depends
on (the `Customer` role must exist and be active or `RegisterHandler`
throws `InvalidOperationException`):
- `InitialCreate` creates `roles` (Id, Name, Description only — no
  `IsActive` column yet) and seeds the three well-known roles via
  `InsertData` with matching columns.
- `AddSecurityAdminFeatures` adds the `IsActive` column
  (`AddColumn<bool>(..., defaultValue: false)`), then immediately
  backfills all three seeded rows to `true` via three `UpdateData` calls.
- `AuthDbContextModelSnapshot.cs` agrees with the end state (`IsActive`
  present, `HasData` block shows all three roles with `IsActive = true`).
- `RoleConfiguration.cs` (the live `IEntityTypeConfiguration<Role>`) also
  seeds the same three roles with `IsActive = true` directly — this is
  EF's designer-time `HasData` mechanism and is consistent with what the
  migrations produce at runtime; not a duplicate/conflicting seed.

This is correctly engineered, not a bug. **I could not confirm it actually
*applies* cleanly** (no Postgres, no `dotnet ef database update`) — that
still needs a real run. `Program.cs` already auto-applies migrations on
startup when `ASPNETCORE_ENVIRONMENT=Development` (`await
db.Database.MigrateAsync()`), and docker-compose sets that env var for
auth-service, so a `docker compose up` should apply them automatically —
untested by me.

## Also checked, found OK (see release-notes.md for the summary list)
Full list of what was read and why it's not suspected: OTP hash path
(SHA-256 both ends, self-consistent), security-answer hash path (same),
refresh-token rotation/reuse-detection logic, account lockout logic,
timing-safe login failure path, all `.csproj` package references and
`ProjectReference` paths, Dockerfile, brace/paren balance across the
service's `.cs` files (heuristic).

## Gaps / next steps for the next agent (or you)

1. **Get a real build.** This is the single highest-value next step —
   everything above is "nothing screamed at me while reading it," not
   "compiles clean." Exact command once you have `dotnet` + network:
   ```bash
   cd services/auth-service
   dotnet restore
   dotnet build -c Release
   ```
   Fix whatever it reports. Given the density and care already visible in
   this codebase (see the extensive package-version rationale comments in
   every `.csproj`), I'd expect this to be close to clean already, but I
   cannot promise zero warnings without having run it.

2. **Add test coverage for forgot-password → reset-password.** There is
   currently **no test at all** for this flow (`grep -rl
   "ResetPasswordHandler" tests/` returns nothing). That's exactly the kind
   of gap that let the bug in fix #1 ship unnoticed. A unit test for
   `ResetPasswordHandler` (mock `ITokenService`/`IAuthDbContext`, assert it
   looks up by `HashRefreshToken`, not `IPasswordHasher.Hash`) or an
   integration test hitting `/forgot-password` then `/reset-password`
   end-to-end would both have caught this immediately.

3. **Re-verify the console-error report** using the incognito-window /
   curl steps in section 3 above, and report back what you get.

4. **If you want full CORS parity with booking-service**, it also sets
   `Cors__AllowedOrigins__0`/`__1` as explicit env vars in
   `infrastructure/docker/docker-compose.yml`'s `auth-service:` block.
   I deliberately did **not** touch that file (outside `services/auth-service/`,
   out of scope per the task instructions) — the `appsettings.json` default
   added here already covers the same two origins, so functionally nothing
   is missing, this would just be for exact-pattern parity if you want it.

## Files changed this session
```
services/auth-service/src/AuthService.Application/Features/Auth/ResetPassword/ResetPasswordHandler.cs
services/auth-service/src/AuthService.Api/Program.cs
services/auth-service/src/AuthService.Api/appsettings.json
services/auth-service/release-notes.md   (new)
services/auth-service/ai-handover.md     (new, this file)
```
Nothing outside `services/auth-service/` was touched. The pre-existing
uncommitted deletion of the top-level `ai-handover.md` (present before this
session started, visible in `git status` from the very first command run)
was left exactly as found — not staged, not restored, not part of any
commit made here.
