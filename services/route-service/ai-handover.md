# AI Handover — route-service, 0-build-warnings pass

## Environment note (read this first)
This pass ran in a sandbox with **no `dotnet` SDK and no network access**.
Confirmed by trying `apt-get install dotnet-sdk-10.0` — every package (even
plain Ubuntu archive packages, not just NuGet) came back `403 Forbidden`, and
`curl` to any external host is blocked by the network allowlist. So: no
`dotnet restore`, no `dotnet build`, no `dotnet test`. Everything below is
from reading the source and every `.csproj` line by line, plus web-searching
the two NuGet security advisories to confirm exact patched versions. **This
has not been compiled.** Treat it as a diagnosis + patch set to verify, not
a confirmed-green build.

This is not a new limitation for this repo — the identical sandbox
limitation hit the `auth-service` fix pass (see
`services/auth-service/ai-handover.md`, commit `72f1190d`), and the same
remediation pattern proved out there once the user pasted real build output
in a later turn. This pass applies that **already-verified** pattern to
route-service, since both services share the same dependency-graph shape
(Pomelo on MySQL, SqlServer's XML key-store dependency, ASP.NET Core OpenAPI
generation) — so the fixes below are high-confidence even without a local
compile.

## What was asked
1. New branch `route-service`, scoped to `services/route-service/` only.
2. Zero build warnings / zero build errors on route-service.
3. Professional git commit messages, one per logical fix.
4. If the session runs out of budget: stop, update this file and
   `release-notes.md` with what was covered, what was fixed, root cause,
   what's left, and the exact command for the next agent (or the user) to
   continue and finish.

**This session ended before a real build could be run** (no SDK/network in
this sandbox) — this document is written for that "stop here" case.

## What is fixed (root cause + fix, per item)

### 1. NU1608 — Pomelo.EntityFrameworkCore.MySql 9.0.0 vs EF Core 10.0.0
**Root cause:** Pomelo 9.0.0 declares a dependency constraint on
`Microsoft.EntityFrameworkCore.Relational` 9.x, but this repo resolves EF
Core 10.0.0 everywhere. NuGet reports this every time it restores, because
it's a whole-graph diagnostic — it fires against *any* project that pulls
`RouteService.Infrastructure` into its graph, not just the project that
references Pomelo directly. The existing `NoWarn="NU1608"` on the Pomelo
`PackageReference` in `RouteService.Infrastructure.csproj` only suppressed
it locally in that one project; `RouteService.Api`, `RouteService.UnitTests`,
and `RouteService.IntegrationTests` all reference Infrastructure
transitively and would still show the warning.

**Fix:** Added `<NoWarn>$(NoWarn);NU1608</NoWarn>` at the `PropertyGroup`
level in all four `.csproj` files that transitively touch Infrastructure:
`RouteService.Infrastructure.csproj`, `RouteService.Api.csproj`,
`RouteService.UnitTests.csproj`, `RouteService.IntegrationTests.csproj`.
This is a deliberate, documented suppression of a known upstream-blocked
issue (Pomelo has not shipped an EF Core 10-compatible release yet) — not a
bug in this codebase. Every suppression has an inline `<!-- -->` comment
explaining why.

### 2. NU1903 — Microsoft.OpenApi 2.0.0 (GHSA-v5pm-xwqc-g5wc / CVE-2026-49451)
**Root cause:** `Microsoft.AspNetCore.OpenApi 10.0.0` (referenced directly in
`RouteService.Api.csproj`) transitively pulls in `Microsoft.OpenApi 2.0.0`,
which has a high-severity (CVSS 7.5) advisory: a small OpenAPI document with
a circular schema reference can crash the process via stack overflow during
parsing. Confirmed via web search against the published GitHub Security
Advisory — fixed in `2.7.5` for the 2.x line (3.x line fixed in `3.5.4`, but
that's a breaking major bump this project doesn't otherwise need).

**Fix:** Added a direct `<PackageReference Include="Microsoft.OpenApi"
Version="2.7.5" />` in `RouteService.Api.csproj`. NuGet resolves the highest
version requested across the graph, so this direct pin overrides the
vulnerable transitive one.

### 3. NU1903 — System.Security.Cryptography.Xml 10.0.6 (GHSA-23rf-6693-g89p / CVE-2026-50648, plus 4 related advisory IDs for the same underlying issue)
**Root cause:** `RouteService.Infrastructure.csproj` already had a direct
pin on this package (pulled in transitively by
`Microsoft.EntityFrameworkCore.SqlServer` → `Microsoft.Data.SqlClient`, for
Always Encrypted XML key-store support) at `10.0.6`. That version is inside
the vulnerable range (`>=10.0.0, <=10.0.9`) for a high-severity (CVSS 7.5)
EncryptedXml denial-of-service advisory published July 2026. Confirmed via
web search against the GitHub Security Advisory — patched version is
`10.0.10`.

**Fix:** Bumped the existing pin from `10.0.6` to `10.0.10`.

## What was checked and found clean (no fix needed)
- `grep`'d the entire `services/route-service` tree for `Obsolete`,
  `async void`, `TODO`/`FIXME`/`HACK`, and `#pragma warning` — nothing
  outside the two auto-generated EF Core migration designer files (which
  legitimately use `#pragma warning disable 612, 618` around obsolete-API
  scaffolding, a standard EF Core code-gen pattern, not a hand-written
  suppression).
- Route-service does **not** use Quartz (unlike auth-service, where a
  genuine `CS0618` obsolete-API call was found and removed in commit
  `330f51ff`) — so that specific fix does not apply here. Re-checked by
  grepping for `Quartz` across all `.cs` and `.csproj` files: no matches.
- Skimmed `RouteService.Infrastructure/DependencyInjection.cs` and
  `RouteService.Api/Program.cs` top-to-bottom for other obsolete-API calls
  or obviously-wrong patterns (nullable misuse, unused variables): nothing
  stood out on a static read.
- All four `.csproj` files already had `<Nullable>enable</Nullable>` and
  `<ImplicitUsings>enable</ImplicitUsings>` set consistently — no
  nullable-context inconsistency between projects that would itself cause
  warnings.

**Caveat on all of the above:** these are static-reading conclusions, not
compiler output. A real `dotnet build` may surface CS-level warnings
(nullable-reference warnings, unused usings, unused variables, etc.) that
simply can't be found by eye in a 140-file codebase. Don't treat "nothing
stood out" as "confirmed zero warnings."

## What is left
1. **Get a real build — this is the single highest-value next step.**
   Nothing above has been compiled. Exact commands once you have `dotnet`
   SDK + network access:
   ```bash
   cd services/route-service
   dotnet restore
   dotnet build -c Release
   ```
   If it reports **new** warnings beyond the three fixed above, they're
   almost certainly CS-level (nullable, unused-using, unused-variable, or
   similar) that this static pass couldn't detect. Fix each on its own
   commit, same pattern as this pass and as the auth-service pass before
   it: root cause in the commit body, not just "fix warning."
2. **Confirm the NU1608/NU1903 fixes actually resolve, don't just suppress
   silently-broken behavior.** In particular, verify `Microsoft.OpenApi
   2.7.5` doesn't introduce a breaking API surface change against
   `Microsoft.AspNetCore.OpenApi 10.0.0`'s expectations — re-run
   `dotnet build` and exercise the `/openapi/v1.json` and `/scalar`
   endpoints locally.
3. **Re-run `dotnet list package --vulnerable`** (or equivalent) once
   network access is available, in case there are other advisories not
   caught by this pass's targeted grep-for-known-issues approach.
4. Everything already listed as a known gap in `docs/ai-handover.md`
   (pre-existing, not part of this pass) still applies: no generated EF
   Core migration for the target provider beyond the initial one, no
   Booking Service sync consumer, resilience policies not yet
   configurable, audit log IP/correlation-ID population incomplete, gRPC
   generated code not yet compiled via `dotnet grpc` tooling.

## Files changed this session
```
services/route-service/src/RouteService.Infrastructure/RouteService.Infrastructure.csproj
services/route-service/src/RouteService.Api/RouteService.Api.csproj
services/route-service/tests/RouteService.UnitTests/RouteService.UnitTests.csproj
services/route-service/tests/RouteService.IntegrationTests/RouteService.IntegrationTests.csproj
services/route-service/release-notes.md   (new, this pass)
services/route-service/ai-handover.md     (new, this file)
```
Nothing outside `services/route-service/` was touched. Branch `route-service`
was created from `main` at commit `b7639212` before any change was made.
