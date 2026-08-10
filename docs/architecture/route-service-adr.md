# Architecture Decision Records — Route Service

## ADR-001: Stop Soft-Delete Without Global Query Filter

**Date:** 2026-08-10
**Status:** Accepted

### Context

EF Core global query filters (`HasQueryFilter`) automatically apply a
predicate to every query for an entity. For soft-delete on `Stop`, this
would be `!IsDeleted`. However, `RouteStop` has a **required**
navigation to `Stop` (`StopId` is non-nullable `Guid`). EF Core warns:

> Entity 'Stop' has a global query filter defined and is the required end
> of a relationship with the entity 'RouteStop'.

This creates a shadow FK (`StopId1`) in the model and an empty migration
when scaffolding, because EF Core resolves the conflict by adding an
optional duplicate FK.

### Decision

Remove the global query filter from `StopConfiguration`. Handlers filter
`!s.IsDeleted` explicitly in their LINQ queries.

### Consequences

- **Positive:** No shadow FKs, clean migrations, predictable model.
- **Negative:** Every stop query must remember the `!IsDeleted` filter.
  Mitigated by having all queries go through handlers that already do this.
- **Neutral:** `Route` and `Schedule` also lack global query filters for
  the same reason; soft-delete is explicit everywhere.

---

## ADR-002: Polly Resilience Without AddPolicyHandler

**Date:** 2026-08-10
**Status:** Accepted

### Context

The platform's `ai-handover.md` §4 documents that `Polly.Extensions.Http`
8.5.0 is unavailable in the offline restore cache (nearest is 3.0.0). The
`AddPolicyHandler` extension lives in that package.

### Decision

Do not attach Polly policies via `IHttpClientBuilder.AddPolicyHandler`.
Instead, wrap `HttpCommunicationService` methods with policies inline, or
register policies as named `IHttpClient` instances with a custom
`DelegatingHandler`.

### Consequences

- **Positive:** No missing-package restore errors.
- **Negative:** Less declarative DI setup. Acceptable for this service's
  current single HTTP client.
