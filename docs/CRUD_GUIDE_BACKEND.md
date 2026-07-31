# Adding a new CRUD feature to a .NET service (vertical-slice pattern)

Worked example: adding **"Update passenger contact details on a booking"**
(`PATCH /api/v1/bookings/{id}/contact`) to the Booking Service. The same
7 steps apply to any new service built on this template, and to any new
CRUD operation on an existing aggregate.

## The rule

One feature = one folder under `Application/Features/<Aggregate>/<Verb+Noun>/`,
containing everything that feature needs: the request, its validator, its
handler, and its DTO. You should be able to delete a feature by deleting one
folder, never by hunting across shared `Services/`, `Repositories/`, `DTOs/`
folders.

## Step 1 — Does this need a new domain method?

If the change touches an invariant (can this happen in this state? does it
affect other data?), add a method to the aggregate — never mutate a public
setter from outside `Domain`. Public setters barely exist in this codebase
on purpose (see `Booking.cs` — everything is `private set` and mutated
through named methods like `Confirm()`, `Cancel()`).

```csharp
// Domain/Entities/Booking.cs
public void UpdateContactDetails(string phoneNumber, string email)
{
    if (Status is BookingStatus.Cancelled or BookingStatus.Refunded)
        throw new InvalidBookingStateException($"Cannot update contact details on a {Status} booking.");

    ContactPhoneNumber = phoneNumber;
    ContactEmail = email;
    // Raise(new BookingContactUpdatedDomainEvent(...)) if something downstream needs to react
}
```

Add the new columns via `BookingConfiguration.cs` and a new EF Core migration
(step 6).

## Step 2 — Create the feature folder

```
Application/Features/Bookings/UpdateContactDetails/
  UpdateContactDetailsCommand.cs
  UpdateContactDetailsValidator.cs
  UpdateContactDetailsHandler.cs
```

## Step 3 — The command (request contract)

```csharp
// UpdateContactDetailsCommand.cs
public sealed record UpdateContactDetailsCommand(
    Guid BookingId, Guid CustomerId, string PhoneNumber, string Email) : IRequest;
```

## Step 4 — The validator (runs automatically via the pipeline behavior)

```csharp
// UpdateContactDetailsValidator.cs
public sealed class UpdateContactDetailsValidator : AbstractValidator<UpdateContactDetailsCommand>
{
    public UpdateContactDetailsValidator()
    {
        RuleFor(x => x.BookingId).NotEmpty();
        RuleFor(x => x.PhoneNumber).NotEmpty().Matches(@"^\+?[0-9]{7,15}$");
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}
```

You don't need to register this anywhere — `AddApplication()` in
`Application/DependencyInjection.cs` scans the assembly for
`AbstractValidator<T>` implementations and `ValidationBehavior<,>` picks
them up automatically for any matching request.

## Step 5 — The handler

```csharp
// UpdateContactDetailsHandler.cs
public sealed class UpdateContactDetailsHandler : IRequestHandler<UpdateContactDetailsCommand>
{
    private readonly IBookingDbContext _context;

    public UpdateContactDetailsHandler(IBookingDbContext context) => _context = context;

    public async Task Handle(UpdateContactDetailsCommand request, CancellationToken cancellationToken)
    {
        var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.Id == request.BookingId, cancellationToken)
            ?? throw new BookingNotFoundException(request.BookingId);

        if (booking.CustomerId != request.CustomerId)
            throw new InvalidBookingStateException("You are not permitted to modify another customer's booking.");

        booking.UpdateContactDetails(request.PhoneNumber, request.Email);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
```

Follow the existing handlers' conventions: throw domain exceptions for
business rule violations (they're translated to the right HTTP status by
`ExceptionHandlingMiddleware.cs` automatically — check that file before
adding a new exception type, you probably don't need one), inject only the
ports you need, don't reach into `Infrastructure` directly.

## Step 6 — Wire the endpoint

```csharp
// Api/Endpoints/BookingsEndpoints.cs, inside MapBookingsEndpoints()
group.MapPatch("/{bookingId:guid}/contact", async (
        Guid bookingId, UpdateContactRequest request, ISender sender, CancellationToken ct) =>
    {
        await sender.Send(new UpdateContactDetailsCommand(bookingId, request.CustomerId, request.PhoneNumber, request.Email), ct);
        return Results.NoContent();
    })
    .WithName("UpdateBookingContact")
    .WithSummary("Update the contact phone/email on a booking.")
    .Produces(StatusCodes.Status204NoContent)
    .ProducesProblem(StatusCodes.Status404NotFound);
```

## Step 7 — Migration, tests, docs

```bash
dotnet ef migrations add AddBookingContactDetails \
  --project src/BookingService.Infrastructure --startup-project src/BookingService.Api
```

- Unit test the handler the way `CreateBookingHandlerTests.cs` does (EF
  InMemory + fakes for `ICacheService`/`IBookingMetrics` from
  `tests/BookingService.UnitTests/TestSupport/`).
- Add the request/response example to `docs/api/API_EXAMPLES.md`.
- Add a request to `postman/Bus-Ticketing-Booking-Service.postman_collection.json`.
- If it's a list endpoint, follow `docs/api/API_PAGINATION.md`'s contract.

## Applying this to a brand-new service (not just a new feature on Booking)

The folder skeleton for any new service (e.g. `services/route-service`)
should mirror `services/booking-service/src/`:
`{ServiceName}.Domain`, `.Application`, `.Infrastructure`, `.Api`, plus
`tests/{ServiceName}.UnitTests` and `.IntegrationTests`. Copy the `.csproj`
files from `booking-service` as a starting point (same target framework,
same package set minus anything Booking-specific), and copy
`Infrastructure/DependencyInjection.cs` + `Api/Program.cs` as templates for
wiring EF Core/Redis/RabbitMQ/OpenTelemetry/Serilog the same way — that
consistency is what makes `docs/OBSERVABILITY_GUIDE.md` apply to every
service without a service-specific version of that doc.
