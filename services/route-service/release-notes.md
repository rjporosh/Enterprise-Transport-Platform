# Release Notes — route-service (build-warning fix pass)

**Date**: 2026-08-19
**Branch**: `route-service`
**Build status**: NOT compile-verified (no dotnet SDK / no network in this
sandbox — see `ai-handover.md` for the exact commands to verify).

## Fixed
- **NU1608** — Pomelo.EntityFrameworkCore.MySql / EF Core 10 restore-time
  version-constraint warning. Suppression moved from item-level (only
  reached `RouteService.Infrastructure`) to project-level in all four
  `.csproj` files that transitively reference it, so it's actually
  suppressed everywhere NuGet reports it.
- **NU1903** — `Microsoft.OpenApi` 2.0.0 high-severity DoS advisory
  (GHSA-v5pm-xwqc-g5wc / CVE-2026-49451). Pinned to patched `2.7.5` in
  `RouteService.Api.csproj`.
- **NU1903** — `System.Security.Cryptography.Xml` high-severity DoS advisory
  (GHSA-23rf-6693-g89p / CVE-2026-50648). Bumped existing pin in
  `RouteService.Infrastructure.csproj` from `10.0.6` to patched `10.0.10`.

## Root cause summary
All three were NuGet restore-time audit/version-constraint diagnostics, not
application logic bugs — see `ai-handover.md` for the full root-cause
explanation of each. No `.cs` source files were changed in this pass; only
`.csproj` package references and warning suppressions.

## Known issues / not yet done
- **Not compile-verified.** This is the most important caveat — see
  "What is left" in `ai-handover.md` for the exact `dotnet restore` /
  `dotnet build` commands to run next, and what to do if new (CS-level)
  warnings turn up that this static pass couldn't have caught.
- Everything already tracked in `docs/ai-handover.md` and
  `docs/new-release-notes.md` (EF migrations, Booking Service sync
  consumer, configurable resilience policies, audit log IP population,
  gRPC generated code) is unchanged by this pass.

## Upgrade guide
- No breaking changes. Two transitive package versions were pinned higher
  (security patches only); no public API surface of route-service itself
  changed.
