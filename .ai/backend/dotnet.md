# .NET Backend Engineering Rules

## 1. Purpose

This document defines stack-specific engineering rules for:

* .NET
* ASP.NET Core
* ASP.NET Core Web API
* .NET Worker Services
* Entity Framework Core
* gRPC
* SignalR where applicable

These rules complement:

```text
.ai/MASTER-RULE.md
.ai/AI_RULES.md
.ai/communication.md
.ai/observability.md
.ai/testing-and-performance.md
```

Do not duplicate those documents here.

---

# 2. .NET VERSION POLICY

## Mandatory Target

This project family targets:

```text
.NET 10+
```

.NET 8 or older must NOT be introduced for new development.

When starting or extending a project, determine the latest **stable/supported .NET version available at implementation time**.

Preferred decision:

```text
Latest stable/LTS .NET version
        ↓
Compatible with project ecosystem
        ↓
Use that version
```

Do NOT automatically select an old framework merely because an older version is familiar.

---

# 3. VERSION SELECTION RULE

When implementing a new .NET project or upgrading an existing project:

1. Detect the currently targeted .NET version.
2. Determine the latest stable/supported .NET version available.
3. Check compatibility with:

   * ASP.NET Core
   * EF Core
   * NuGet packages
   * Database providers
   * Authentication packages
   * gRPC packages
   * OpenTelemetry packages
   * Docker base images
   * CI/CD runners
   * Build tooling
4. Prefer the latest stable/supported version when compatibility is confirmed.
5. Do not downgrade to .NET 8 or older.
6. Do not introduce preview/RC versions into production unless explicitly requested.
7. Do not perform a major framework upgrade merely for cosmetic reasons when working on an existing production service.
8. If a major upgrade is necessary, verify the entire dependency chain before changing it.

---

# 4. EXISTING PROJECT VERSION

If the repository already targets:

```text
.NET 10
```

continue using it.

If the repository targets a version newer than .NET 10:

```text
Use the newer supported version.
```

If the repository targets .NET 8 or older:

```text
Do NOT silently downgrade or preserve the old version for new development.
```

Instead:

* determine whether migration to the current supported .NET version is required;
* assess compatibility;
* upgrade only when within the requested scope or when required by the task;
* never perform a risky framework migration without understanding its impact.

---

# 5. NEVER USE END-OF-LIFE FRAMEWORKS

Do not intentionally introduce:

```text
.NET 8 or older
End-of-life .NET versions
Unsupported ASP.NET Core versions
Unsupported EF Core versions
```

unless the repository explicitly requires legacy compatibility for a documented reason.

If legacy compatibility is required, document it under:

```text
Known Limitations
```

---

# 6. PACKAGE VERSION POLICY

.NET package versions should normally align with the selected .NET ecosystem.

For example:

```text
.NET version
    ↓
Compatible ASP.NET Core version
    ↓
Compatible EF Core version
    ↓
Compatible database provider
    ↓
Compatible supporting packages
```

Avoid mixing incompatible major versions.

Before adding or upgrading packages, verify:

```text
Target Framework
Package Compatibility
Transitive Dependencies
Known Vulnerabilities
Runtime Compatibility
```

---

# 7. LATEST DOES NOT MEAN PREVIEW

"Latest" means:

```text
Latest stable/supported production-ready release
```

It does NOT mean:

```text
Preview
Alpha
Beta
RC
Nightly
Experimental
```

unless explicitly requested.

Production systems must prefer stable releases.

---

# 8. FRAMEWORK UPGRADE SAFETY

Before upgrading .NET:

```text
Inspect
 ↓
Check compatibility
 ↓
Upgrade
 ↓
Restore
 ↓
Build
 ↓
Run tests
 ↓
Run integration tests
 ↓
Run migrations if applicable
 ↓
Run API verification
 ↓
Review breaking changes
```

Never assume that changing:

```xml
<TargetFramework>...</TargetFramework>
```

is sufficient.

Check the entire application ecosystem.

---

# 9. DO NOT UPGRADE UNRELATED SERVICES

If a multi-service repository contains:

```text
Notification
Payment
Bus
Route
Auth
Gateway
```

and only Notification is being implemented, do not upgrade every service to the latest .NET version automatically.

Keep changes scoped.

A framework upgrade affecting multiple services requires explicit architectural consideration.

---

# 10. VERSION-AWARE AI BEHAVIOR

When working on this repository, do not blindly copy version numbers from this document.

The AI must inspect the actual environment.

Use:

```bash
dotnet --version
dotnet --list-sdks
dotnet --list-runtimes
```

and inspect:

```text
*.csproj
global.json
Directory.Build.props
Directory.Packages.props
NuGet.config
Dockerfile
CI/CD configuration
```

when present.

The repository configuration is the source of truth for the currently selected version.

---

# 11. DEFAULT DECISION

When no explicit framework version is specified:

```text
Choose the latest stable/supported .NET version
that is compatible with the project's architecture and dependency ecosystem.
```

Never choose .NET 8 merely because:

```text
"It is familiar"
"It is commonly used"
"The example uses .NET 8"
```

---

# 12. FINAL PRINCIPLE

The project should remain modern without becoming unstable.

Therefore:

```text
Latest Stable
+
Supported
+
Compatible
+
Production Ready
=
Preferred .NET Version
```

Not:

```text
Oldest Familiar Version
```

and not:

```text
Newest Preview Version
```

# END OF .NET VERSION POLICY
