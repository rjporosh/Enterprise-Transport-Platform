# Testing — Unit Tests

## Framework
- xUnit 2.9
- FluentAssertions 7.0
- Microsoft.EntityFrameworkCore.InMemory

## Location
`tests/RouteService.UnitTests/`

## Coverage
- Domain lifecycle rules (state transitions, soft delete)
- Handler success/failure paths
- Concurrency conflict handling
- Validation rules

## Running
```bash
dotnet test tests/RouteService.UnitTests
```

## Adding a new test
1. Create a class in the appropriate feature folder (e.g. `Routes/MyHandlerTests.cs`)
2. Use `TestRouteDbContext` with InMemory database
3. Use `FakeDateTimeProvider`, `FakeEventPublisher`, `FakeCurrentUser`
4. Assert with FluentAssertions

## Example
```csharp
[Fact]
public async Task Handle_WithValidData_ReturnsRouteDto()
{
    var handler = CreateHandler();
    var result = await handler.Handle(command, CancellationToken.None);
    result.IsSuccess.Should().BeTrue();
}
```
