# AI Handover — booking-service build fix

## Environment note (read this first)
This pass was done in a sandbox with **no `dotnet` SDK and no network access for
`dotnet build`/`dotnet restore`**. All fixes below are based on reading the source,
the original build log you provided, and researching the referenced NuGet
advisories/APIs on the web. **Nothing here has been compiled.** The very first
thing the next agent (or you) should do is run a real build — see "Next command"
below — and treat this file as a diagnosis + patch set to verify, not a
guarantee.

## What was fixed

### 1. Build error — CS7036 in `Program.cs(59,14)`
Original code:
```csharp
tracing
    .AddAspNetCoreInstrumentation(options => options.RecordException = true)
    .AddHttpClientInstrumentation()
    .AddNpgsql();
```
`BookingService.Api.csproj` referenced the `Npgsql.OpenTelemetry` package (Npgsql's
own tracing package) to get `.AddNpgsql()` on `TracerProviderBuilder`. At this call
site the compiler was instead resolving `NpgsqlServiceCollectionExtensions
.AddNpgsql<TContext>(IServiceCollection, string?, ...)` — the **EF Core**
registration helper from `Npgsql.EntityFrameworkCore.PostgreSQL`, which requires a
`connectionString` argument that wasn't supplied. Same method name, two different
packages, two different receiver types.

**Fix applied:**
- Removed the `Npgsql.OpenTelemetry` package reference entirely.
- Replaced `.AddNpgsql()` in `Program.cs` with `.AddSource("Npgsql")`, which
  subscribes the tracer to Npgsql's own `"Npgsql"` `ActivitySource` directly (this
  is the documented zero-extra-package way to get Npgsql traces — see
  https://www.npgsql.org/doc/diagnostics/tracing.html and
  https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/issues/906).
  This sidesteps the name collision completely and needs no package at all.

**⚠️ Please verify:** I could not compile this. If `dotnet build` still reports an
ambiguity or a missing-package error on this line, the fallback is to fully
qualify the call instead, e.g.:
```csharp
tracing.AddSource("Npgsql"); // already unambiguous — should just work
```
or, if you'd rather keep using the Npgsql.OpenTelemetry package's richer
`.AddNpgsql(Action<NpgsqlTracingOptionsBuilder>)` overload, fully qualify it by
class name once you've confirmed the exact namespace/class from the installed
package (I found the source file at
`github.com/npgsql/npgsql/blob/main/src/Npgsql.OpenTelemetry/TracerProviderBuilderExtensions.cs`
but couldn't fetch its contents to confirm the exact namespace).

### 2. NU1903 warnings — `Microsoft.OpenApi` 2.0.0 (GHSA-v5pm-xwqc-g5wc / CVE-2026-49451)
Stack-overflow DoS on circular schema refs. Pulled in transitively by
`Microsoft.AspNetCore.OpenApi 10.0.0`. **Fix:** added a direct
`<PackageReference Include="Microsoft.OpenApi" Version="2.7.5" />` in
`BookingService.Api.csproj` (patched version per the GitHub advisory; direct refs
win over transitive under NuGet's nearest-wins rule).

### 3. NU1903 warnings (×8, one per GHSA ID) — `System.Security.Cryptography.Xml` 9.0.0
All eight advisories (`23rf-6693-g89p`, `37gx-xxp4-5rgx`, `6588-8gv4-xfgh`,
`8q5v-6pqq-x66h`, `cvvh-rhrc-wg4q`, `g8r8-53c2-pm3f`, `mmjf-rqrv-855v`,
`w3x6-4m5h-cxqf`) are the same underlying XML-encryption DoS
(CVE-2026-50648), fixed together per version line: 8.0.4 / 9.0.18 / **10.0.10**.
**Fix:** added `<PackageReference Include="System.Security.Cryptography.Xml"
Version="10.0.10" />` directly in `BookingService.Infrastructure.csproj` (it was
resolving to 9.0.0 transitively despite the project targeting net10.0).

### 4. Repo hygiene
- Added `.gitignore` (no `bin`/`obj`/IDE cruft was previously excluded).
- Initialized git and made commits — see `git log`.

## What's still open / needs the next agent's attention

1. **Run a real build.** Nothing above was compiled — see "Next command."
2. **No EF Core migrations exist yet** (`find . -path "*Migrations*"` returns
   nothing). `Program.cs` already calls `db.Database.MigrateAsync()` on startup in
   Development, but with zero migrations that's a no-op and every request will
   fail with "relation does not exist" (this is called out in the existing code
   comment above that block). You need to create the initial migration.
3. **No `docker-compose.yml` was found in this archive** for Postgres/RabbitMQ/
   Redis/Seq — check `README.md` (not fully read this pass) for whether local
   infra is expected to be spun up some other way, or whether one needs to be
   added.
4. Double-check whether `AspNetCore.HealthChecks.NpgSql` 9.0.0 and
   `AspNetCore.HealthChecks.Rabbitmq.v6` 9.0.0 have their own advisories — they
   weren't flagged in the original log, but that log is now stale after the
   package changes above; a fresh `dotnet restore` will re-audit everything.
5. **NuGetAudit may resurface new/different advisories** on the versions I pinned
   (2.7.5 / 10.0.10) if newer patches have shipped since — re-check
   `dotnet list package --vulnerable --include-transitive` after restore.

## Next command (run this first)

From the repo root:

```bash
# 1. Restore + build, confirm 0 errors / 0 warnings
dotnet restore
dotnet build /warnaserror

# 2. If step 1 is clean, install/confirm the EF Core tool, then create the
#    initial migration and apply it (uses the "BookingDb" connection string
#    from appsettings / user-secrets / env)
dotnet tool install --global dotnet-ef   # skip if already installed
cd src/BookingService.Api
dotnet ef migrations add InitialCreate --project ../BookingService.Infrastructure --startup-project .
dotnet ef database update --project ../BookingService.Infrastructure --startup-project .

# 3. Run it
dotnet run --project .
```

If `dotnet build` still fails on the `AddSource`/`AddNpgsql` line or reports new
NU1903 warnings, fix forward from there — this file plus the git commit messages
should give full context on what was already tried and why.
