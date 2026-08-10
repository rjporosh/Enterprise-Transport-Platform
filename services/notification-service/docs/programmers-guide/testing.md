# Testing

## Unit Tests

Located in `tests/NotificationService.UnitTests/`.

- Use xUnit + FluentAssertions
- Use EF Core InMemory for handler tests that need a DbContext
- Use hand-written fakes for dependencies (`FakeDateTimeProvider`, `FakeEventPublisher`, `FakeTemplateRenderer`)

### Pattern

```csharp
public class MyHandlerTests : IDisposable
{
    private readonly TestNotificationDbContext _context;
    private readonly FakeEventPublisher _eventPublisher = new();
    private readonly FakeDateTimeProvider _clock = new();

    public MyHandlerTests()
    {
        var options = new DbContextOptionsBuilder<TestNotificationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new TestNotificationDbContext(options);
    }

    [Fact]
    public async Task Handle_ValidInput_ReturnsSuccess()
    {
        var handler = new MyHandler(_context, _eventPublisher, _clock);
        var result = await handler.Handle(/* ... */);
        result.IsSuccess.Should().BeTrue();
    }

    public void Dispose() => _context.Dispose();
}
```

## Integration Tests

Located in `tests/NotificationService.IntegrationTests/`.

- Use Testcontainers (PostgreSQL + RabbitMQ)
- Use `WebApplicationFactory<Program>` to test the full HTTP pipeline
- Apply migrations in `InitializeAsync`

## Load Tests

See `tests/load-test/README.md`.

## Running Tests

```bash
# Unit tests only
dotnet test tests/NotificationService.UnitTests

# All tests (requires Docker for integration tests)
dotnet test
```

## Coverage

Focus on:
- Domain state machine transitions
- Validation (all-errors collection)
- Handler happy paths and error paths
- Template rendering with Scriban
- Tenant isolation (if applicable)
- Idempotency behavior
