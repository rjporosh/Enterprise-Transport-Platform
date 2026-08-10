# Testing — Unit Tests

## Scope

Unit tests cover:
- Domain entity behavior (`BusTests.cs`)
- Handler logic (`RegisterBusHandlerTests.cs`, `ChangeBusStatusHandlerTests.cs`)
- Validators (implicitly via handler tests)

## Patterns

- **NSubstitute** for mocking interfaces (`IBusDbContext`, `ICacheService`, `ISender`, etc.).
- **FluentAssertions** for readable assertions.
- **In-memory database** is avoided; tests use fakes for repositories and context.

## Example: RegisterBusHandlerTests

```csharp
public sealed class RegisterBusHandlerTests
{
    [Fact]
    public async Task Handle_ValidCommand_ReturnsBusDto()
    {
        // Arrange
        var dbContext = Substitute.For<IBusDbContext>();
        var cache = Substitute.For<ICacheService>();
        var metrics = Substitute.For<IBusMetrics>();
        var publisher = Substitute.For<IEventPublisher>();
        var currentUser = new FakeCurrentUser();
        var dateTime = new FakeDateTimeProvider();
        var logger = Substitute.For<ILogger<RegisterBusHandler>>();

        var handler = new RegisterBusHandler(dbContext, cache, metrics, publisher, currentUser, dateTime, logger);

        var command = new RegisterBusCommand(
            operatorId: Guid.NewGuid(),
            plateNumber: "DHA-1234",
            busType: BusType.AcSleeper,
            totalSeats: 40,
            depotId: Guid.NewGuid(),
            manufacturer: "Volvo",
            model: "9600",
            yearOfManufacture: 2022,
            tenantId: null, companyId: null, organizationId: null
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.PlateNumber.Should().Be("DHA-1234");
        await dbContext.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
```

## Running

```bash
cd services/bus-service
dotnet test tests/BusService.UnitTests
```

## Coverage Goal

Target >80% line coverage for Application and Domain layers.
