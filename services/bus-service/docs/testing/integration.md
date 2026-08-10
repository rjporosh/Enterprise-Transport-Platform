# Testing — Integration Tests

## Scope

Integration tests exercise the full HTTP pipeline (and gRPC where applicable) against real infrastructure via Testcontainers.

## Stack

- **Microsoft.AspNetCore.Mvc.Testing** (`WebApplicationFactory<Program>`)
- **Testcontainers.PostgreSql**, **Testcontainers.RabbitMq**, **Testcontainers.Redis**
- **FluentAssertions** for assertions
- **xunit** as the test framework

## Example: BusApiTests

```csharp
public sealed class BusApiTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder().WithImage("postgres:16-alpine").WithDatabase("bus_service_test").Build();
    private readonly RabbitMqContainer _rabbitMq = new RabbitMqBuilder().WithImage("rabbitmq:3.13-management-alpine").Build();
    private readonly RedisContainer _redis = new RedisBuilder().WithImage("redis:7.4-alpine").Build();

    private WebApplicationFactory<Program>? _factory;
    private HttpClient _client = default!;

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_postgres.StartAsync(), _rabbitMq.StartAsync(), _redis.StartAsync());

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:BusDb", _postgres.GetConnectionString());
            builder.UseSetting("RabbitMq:HostName", _rabbitMq.Hostname);
            builder.UseSetting("RabbitMq:Port", _rabbitMq.GetMappedPublicPort(5672).ToString());
            builder.UseSetting("Redis:ConnectionString", _redis.GetConnectionString());
            builder.UseSetting("Jwt:SigningKey", TestSigningKey);
            builder.UseSetting("Jwt:Issuer", TestIssuer);
            builder.UseSetting("Jwt:Audience", TestAudience);
        });

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task FullLifecycle_CreateDepot_RegisterBus_ChangeStatus_GetBus()
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", MintToken("Admin"));

        var depotResponse = await _client.PostAsJsonAsync("/api/v1/depots", new { name = "Central", city = "Dhaka", address = (string?)null });
        depotResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var depotId = (await depotResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var busResponse = await _client.PostAsJsonAsync("/api/v1/buses", new { /* ... */ });
        busResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var busId = (await busResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        await _client.PostAsJsonAsync($"/api/v1/buses/{busId}/status", new { newStatus = "UnderMaintenance" });
        var getResponse = await _client.GetAsync($"/api/v1/buses/{busId}");
        (await getResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("status").GetString().Should().Be("UnderMaintenance");
    }
}
```

## Running

```bash
cd services/bus-service
dotnet test tests/BusService.IntegrationTests
```

> **Note**: Requires Docker.
