# AI Hanover Notes — PaymentService Build-Health Pass

> **Update (pass 2):** The user ran the real build on their machine (macOS, .NET 10 SDK installed) and pasted the actual `dotnet restore` / `dotnet build` output — 10 restore warnings, up to 57 build warnings total, 0 errors. All of those specific warnings have now been fixed in source (see `release-notes.md` v1.3.2 for the full root-cause-by-root-cause breakdown). This sandbox still has no SDK/network, so **these fixes are unverified by an actual rebuild** — that is the one remaining step. Jump to "Exact next command to run" below.

## Task as given
"Make sure no regression, nothing broken, all APIs working nicely. Ensure 0 build warnings and 0 build errors. Include the `dotnet ef migrations add` and `dotnet ef database update` commands in `guide.md`. Return the zipped project."

## Root cause of why this handoff exists
**This sandbox has no .NET SDK installed (`dotnet: command not found`), and outbound network access is disabled (egress proxy blocks all hosts, including `dotnet.microsoft.com`/NuGet), so the SDK could not be installed either.** This means I could not run `dotnet restore`, `dotnet build`, `dotnet test`, or `dotnet ef migrations add` in this environment. Nothing here was skipped for convenience — it was structurally impossible in this sandbox.

## What was covered in this pass
1. Unzipped the project, stripped macOS junk (`__MACOSX/`, `.DS_Store`) and stale `obj/`/`bin/` build output that had been included in the archive.
2. Read all 7 `.csproj` files — package versions and project references all cross-reference consistently (net10.0 across the board, EF Core 10.0.0, matching provider packages).
3. Ran structural static checks across all 143 `.cs` files:
   - Brace `{}` balance per file — clean.
   - Paren `()` balance per file — clean.
   - Duplicate class/record/interface/enum names — the 4 hits found (`Program`, `InitialCreate`, `DependencyInjection`, `AddAgentPaymentMethod`) are all legitimate (different projects, or the EF migration + its `.Designer.cs` partial, which is correct EF Core pattern) — not collisions.
   - Namespace-vs-folder-path consistency across `src/` — clean.
4. Verified `IPaymentDbContext` (3 `DbSet<T>` members) is a strict, consistent subset of `PaymentDbContext`'s 4 `DbSet<T>` members.
5. Verified all 15 MediatR commands/queries in `PaymentService.Application` pair 1:1 with a handler of matching generic response type.
6. Verified `PaymentDbContextDesignTimeFactory : IDesignTimeDbContextFactory<PaymentDbContext>` exists and is wired to PostgreSQL — this is what makes `dotnet ef migrations add`/`database update` work.
7. Wrote root-level `guide.md` with the exact build/test/run/migration commands.
8. Updated `docs/programmers-guide/release-notes.md` with a `v1.3.1` entry documenting this pass.

## What is explicitly NOT verified (left for the next agent / you)
- **Actual compilation.** Static review cannot catch: NuGet version/restore conflicts (e.g. transitive package downgrades — note `Pomelo.EntityFrameworkCore.MySql 9.0.0` pinned against `Microsoft.EntityFrameworkCore 10.0.0` with `NoWarn="NU1608"` already suppressing a known version-range warning there — worth double-checking this doesn't hide a real incompatibility once you can actually restore), analyzer/nullable-reference warnings (`CS86xx`, `CS8600`-series, `CA*`, `IDE*` rules — this repo has `<Nullable>enable</Nullable>` everywhere, which is a common source of build *warnings* even when there are zero *errors*), or EF Core model-building-time errors.
- **Runtime API behavior.** No endpoint was exercised; "all APIs working nicely" is unverified beyond the codebase being internally consistent on paper.
- **Test suite pass/fail.** Not run.

## Exact next command to run (pick up here)

```bash
cd payment-service
dotnet restore PaymentService.sln
dotnet build PaymentService.sln -c Release 2>&1 | tee /tmp/build.log
grep -E "warning|error" /tmp/build.log
```

- If the grep prints nothing → 0 warnings / 0 errors confirmed. Run `dotnet test PaymentService.sln -c Release` to confirm the test suite still passes (the Bkash/Nagad null-check change and the `NoOpLogger` constraint change are both in test-adjacent or provider code — worth confirming `WebhookSignatureVerificationTests` and any Bkash/Nagad unit tests still pass), then the task is done.
- If any warning still prints, it's most likely one of these:
  - A **different** NU1903 CVE than the ones fixed here, disclosed after this pass — re-run `dotnet list package --vulnerable --include-transitive` and repeat the "pin to patched version" pattern used in v1.3.2.
  - `NU1608` still appearing for a project outside `src/`/`tests/` (e.g. `performance-tests/`) if it sits outside the directory tree `Directory.Build.props` auto-imports from — move/copy the props file up a level, or add a project-local one.
  - A genuinely new warning introduced by one of this pass's edits — check the specific file/line the compiler reports first; don't assume it's unrelated.
- Then run the migration commands exactly as documented in `guide.md` against a real Postgres instance to confirm `dotnet ef migrations add`/`database update` actually execute (not run in this sandbox, same SDK/network reason).

## Files touched this pass (cumulative, both rounds)
- Added: `guide.md`, `ai-hanover.md`, `Directory.Build.props` (all at repo root)
- Edited: `docs/programmers-guide/release-notes.md` (v1.3.1 + v1.3.2 entries)
- Edited (round 2 — real warning fixes): `src/PaymentService.Infrastructure/Providers/BkashPaymentProvider.cs`, `src/PaymentService.Infrastructure/Providers/NagadPaymentProvider.cs`, `tests/PaymentService.UnitTests/Providers/WebhookSignatureVerificationTests.cs`, `src/PaymentService.Api/Endpoints/PaymentEndpoints.cs`, `src/PaymentService.Api/Endpoints/AgentPaymentMethodEndpoints.cs`, `src/PaymentService.Infrastructure/PaymentService.Infrastructure.csproj`, `src/PaymentService.Api/PaymentService.Api.csproj`, `tests/PaymentService.IntegrationTests/PaymentService.IntegrationTests.csproj`
- No other `.cs`/`.csproj` files were changed. Round 1 (static-only) made no source changes; round 2's changes are listed above with full root-cause detail in `release-notes.md`.
