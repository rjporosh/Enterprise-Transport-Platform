# Developer Guide — adding a CRUD feature

This is the canonical "how do I add something" doc for every backend
service in this platform (Auth, Booking, Bus, and whatever comes next).
Every service follows the same Clean Architecture + CQRS layering, so the
steps here are the same regardless of which service you're working in —
only the namespaces change.

Worked example in this guide: adding **`UpdateDepot`** to Bus Service (a
depot currently has Create + List, but no Update) — chosen because it's a
genuine, complete vertical slice touching all four layers, not a
simplified toy example.

## The layering, in one picture

```
Domain          — entities, business rules, domain events. Zero framework deps.
Application     — one Command/Query + Handler + Validator per feature (CQRS via MediatR).
Infrastructure  — EF Core, Redis, RabbitMQ, external I/O. Implements Application's interfaces.
Api             — minimal API endpoints. Maps HTTP <-> MediatR commands/queries. Thin.
```

A new feature is almost always: **one new/changed Domain method (if any) +
one new Application vertical slice + one new endpoint mapping.**
Infrastructure rarely changes for a new feature — it only changes when you
need a genuinely new capability (a new external dependency), which is the
uncommon case.

## Step 1 — Domain: does the aggregate need new behavior?

`Depot` currently has no way to change its own fields after creation — add
an intention-revealing method, never a public setter, in
`Domain/Entities/Depot.cs`:

```csharp
public void UpdateDetails(string name, string city, string? address)
{
    Name = name.Trim();
    City = city.Trim();
    Address = address?.Trim();
}
```

No domain event needed here — unlike `Bus.UpdateDetails` (which raises
`BusDetailsUpdatedDomainEvent` because Booking Service's replica cares
about bus field changes), nothing outside this service currently needs to
know when a depot's address changes. Add an event later if that becomes
true — don't add it speculatively.

If your feature is read-only, or the aggregate already has the behavior
you need, skip this step — that's the common case for a new Query.

## Step 2 — Application: Command + Validator + Handler

One folder per feature, under `Features/<Area>/<FeatureName>/`. Mirror the
most similar existing slice — here, `Features/Buses/UpdateBusDetails/` is
the closest match (an authenticated update against a simple aggregate).

**`Features/Depots/UpdateDepot/UpdateDepotCommand.cs`**
```csharp
using BusService.Application.Common.Models;
using MediatR;

namespace BusService.Application.Features.Depots.UpdateDepot;

public sealed record UpdateDepotCommand(Guid DepotId, string Name, string City, string? Address) : IRequest<DepotDto>;
```

**`UpdateDepotValidator.cs`**
```csharp
using FluentValidation;

namespace BusService.Application.Features.Depots.UpdateDepot;

public sealed class UpdateDepotValidator : AbstractValidator<UpdateDepotCommand>
{
    public UpdateDepotValidator()
    {
        RuleFor(x => x.DepotId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.City).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Address).MaximumLength(300).When(x => x.Address is not null);
    }
}
```

**`UpdateDepotHandler.cs`**
```csharp
using BusService.Application.Common.Interfaces;
using BusService.Application.Common.Models;
using BusService.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusService.Application.Features.Depots.UpdateDepot;

public sealed class UpdateDepotHandler : IRequestHandler<UpdateDepotCommand, DepotDto>
{
    private readonly IBusDbContext _context;

    public UpdateDepotHandler(IBusDbContext context) => _context = context;

    public async Task<DepotDto> Handle(UpdateDepotCommand request, CancellationToken cancellationToken)
    {
        var depot = await _context.Depots.FirstOrDefaultAsync(d => d.Id == request.DepotId, cancellationToken);
        if (depot is null) throw new DepotNotFoundException(request.DepotId);

        depot.UpdateDetails(request.Name, request.City, request.Address);
        await _context.SaveChangesAsync(cancellationToken);

        return new DepotDto(depot.Id, depot.Name, depot.City, depot.Address);
    }
}
```

**No new interfaces needed** — `IBusDbContext` already exists. You only
add a new interface under `Common/Interfaces/` (and implement it in
Infrastructure — Step 3) when the feature needs a genuinely new external
capability.

MediatR auto-discovers the handler via assembly scanning
(`Application/DependencyInjection.cs`) — no manual DI registration for the
handler or validator.

## Step 3 — Infrastructure (only if you added a new port in Step 2)

Not needed for this example. If you *did* add a new interface, implement
it under the matching Infrastructure subfolder and register it in
`Infrastructure/DependencyInjection.cs`.

## Step 4 — Api: map the endpoint

In `Api/Endpoints/BusEndpoints.cs`:

```csharp
depots.MapPut("/{depotId:guid}", UpdateDepotAsync)
    .WithName("UpdateDepot")
    .WithSummary("Update a depot's name, city, and address.")
    .Produces<DepotDto>(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status404NotFound)
    .RequireAuthorization(policy => policy.RequireRole("Admin"));
```

```csharp
private static async Task<IResult> UpdateDepotAsync(Guid depotId, [FromBody] UpdateDepotRequest request, ISender sender, CancellationToken cancellationToken)
{
    var result = await sender.Send(new UpdateDepotCommand(depotId, request.Name, request.City, request.Address), cancellationToken);
    return Results.Ok(result);
}
```

```csharp
public sealed record UpdateDepotRequest(string Name, string City, string? Address);
```

## Step 5 — Tests

**Unit test** — mirror `RegisterBusHandlerTests`: spin up
`TestBusDbContext` (EF Core InMemory), seed a `Depot`, call the handler
directly, assert the state changed and the "not found" path throws.

**Integration test** — add a case to `BusApiTests.cs`: mint an Admin
token, `PUT /api/v1/depots/{id}`, assert `200` and that a follow-up
`GET /api/v1/depots` reflects the change.

## Step 6 — Migration

If you added or changed a mapped property (not the case in this example —
`UpdateDetails` only changes existing columns' *values*, not the schema),
generate a migration — see
[`docs/development/database-migrations.md`](database-migrations.md).

## Step 7 — Docs

Update the service's `docs/architecture/*-architecture.md` only if the
feature changes a design decision worth recording (a new domain event, a
new external dependency, a non-obvious trade-off). A routine CRUD
addition like this example usually doesn't need a doc update — most new
features won't.

## Checklist (copy into your PR description)

- [ ] Domain method added, if the feature needs new behavior/state (Step 1)
- [ ] Command/Query + Validator + Handler (Step 2)
- [ ] New port + Infrastructure implementation, if needed (Step 3)
- [ ] Endpoint mapped, request/response DTOs added (Step 4)
- [ ] Unit test for the handler (Step 5)
- [ ] Integration test for the endpoint (Step 5)
- [ ] Migration generated, if the schema changed (Step 6)
- [ ] Docs updated, if a design decision changed (Step 7)

## Conventions this platform has already learned the hard way

Every one of these was a real bug found and fixed in Auth, Booking, or Bus
Service this session — check new code against this list before it ships,
since these are exactly the mistakes that build cleanly and only fail at
runtime (or fail in a way the original bug report didn't even reach):

- **`Ignore()` any `AggregateRoot` collection/computed property in its EF
  Core configuration** — `DomainEvents` especially. EF Core's conventions
  will try to map it as a navigation property otherwise, and the failure
  ("entity type 'DomainEvent' requires a primary key") only surfaces the
  first time the model is actually built (a query, or `MigrateAsync()`),
  not at compile time.
- **Native OpenAPI (`Microsoft.AspNetCore.OpenApi` + `AddOpenApi()`/
  `MapOpenApi()`), never Swashbuckle.** Swashbuckle and the framework's own
  OpenAPI.NET v2-based generator disagree on the document shape on .NET 10,
  and Scalar's default document route only matches the native generator's
  — Swashbuckle produces a Scalar page that loads but shows zero endpoints.
- **`using Microsoft.Extensions.Diagnostics.Metrics;` for `IMeterFactory`**
  — it's there, not in `System.Diagnostics.Metrics`. A class library
  (`Microsoft.NET.Sdk`, not `.Sdk.Web`) gets none of the ASP.NET Core
  implicit usings, so this one is easy to miss.
- **`using Microsoft.AspNetCore.RateLimiting;`** for `AddRateLimiter`/
  `UseRateLimiter`/`RequireRateLimiting`, if you use rate limiting — also
  not in the implicit-usings set.
- **`AspNetCore.HealthChecks.Rabbitmq.v6`, not the mainline package** —
  the mainline 9.0.0 release requires a DI-resolved `IConnection`, which
  nothing in this platform's RabbitMQ setup provides (each service's
  `RabbitMqPublisher` holds its own private `Lazy<IConnection>`, not a DI
  service). The `.v6` package kept the original connection-string API.
- **Fully-qualify a Domain entity name that collides with an Application
  feature-folder name** — e.g. `AuthService.Domain.Entities.RefreshToken`
  vs the `AuthService.Application.Features.Auth.RefreshToken` namespace.
  C#'s enclosing-namespace lookup resolves the bare name to the sibling
  namespace, not the `using`-imported type, inside any file physically
  nested under the shared parent namespace.
- **Never `await` inside a `catch (...) when (...)` filter** (`CS7094`) —
  do the async check inside the `catch` body instead, with a conditional
  `throw;` to avoid masking unrelated exceptions.
- **Pin `OpenTelemetry.*` packages to a current release**, not whatever an
  early scaffold used — `OpenTelemetry.Api` 1.10.0–1.11.1 carries a real
  DoS advisory (GHSA-8785-wc3w-h8q6).
