# Release Notes — Build Fix Pass

## Summary
Fixes the build error and all NuGet security-advisory warnings reported by
`dotnet build` on `BookingService.Api`. See `ai-handover.md` for full technical
detail, caveats, and next steps — **this pass could not be compiled locally**
(no `dotnet` SDK / no network in the working sandbox), so treat it as a patch to
verify, not a confirmed-green build.

## Fixed
- **Build error (CS7036, `Program.cs:59`):** `.AddNpgsql()` in the OpenTelemetry
  tracing setup was resolving to the wrong extension method (EF Core's
  `AddNpgsql<TContext>(IServiceCollection, string connectionString, ...)` instead
  of the Npgsql.OpenTelemetry tracing one). Replaced with `.AddSource("Npgsql")`
  and removed the now-unused `Npgsql.OpenTelemetry` package reference.
- **NU1903 (Microsoft.OpenApi 2.0.0, GHSA-v5pm-xwqc-g5wc):** pinned to `2.7.5` in
  `BookingService.Api.csproj`.
- **NU1903 ×8 (System.Security.Cryptography.Xml 9.0.0, CVE-2026-50648 and its
  sibling GHSA IDs):** pinned to `10.0.10` in
  `BookingService.Infrastructure.csproj`.

## Added
- `.gitignore` for standard .NET build output and IDE files.
- `ai-handover.md` — full diagnosis, what's fixed, what's open, next commands.

## Not yet done (see `ai-handover.md`)
- Build has not been run/verified (no SDK in this environment).
- No EF Core migrations exist in the repo yet — `InitialCreate` needs to be
  generated and applied before the API will serve real requests.
- No local infra (docker-compose) was found/verified for Postgres/RabbitMQ/
  Redis/Seq.
