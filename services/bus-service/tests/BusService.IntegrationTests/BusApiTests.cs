using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.IdentityModel.Tokens;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Testcontainers.Redis;
using Xunit;

namespace BusService.IntegrationTests;

/// <summary>
/// End-to-end against real Postgres + RabbitMQ + Redis via Testcontainers,
/// same pattern as Auth/Booking Service's equivalent. Since Bus Service has
/// no login endpoint of its own (it validates tokens Auth Service issues),
/// this test mints its own JWT locally using the same signing key/issuer/
/// audience the test host is configured with — standing in for a real
/// Auth Service token for the purposes of exercising authorization here.
///
/// NOTE: requires a Docker daemon; this could not be executed in the
/// sandbox this was written in (no Docker/network access) — run it
/// locally or in CI to verify.
/// </summary>
public sealed class BusApiTests : IAsyncLifetime
{
    private const string TestSigningKey = "integration-test-signing-key-32-chars-minimum";
    private const string TestIssuer = "https://identity.bus-ticketing.local";
    private const string TestAudience = "bus-ticketing-api";

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

    private static string MintToken(params string[] roles)
    {
        var claims = new List<Claim> { new(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()) };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(TestIssuer, TestAudience, claims, expires: DateTime.UtcNow.AddHours(1), signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    [Fact]
    public async Task Health_ReturnsHealthy()
    {
        var response = await _client.GetAsync("/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RegisterBus_WithoutToken_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/buses", new { });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RegisterBus_WithCustomerRole_Returns403()
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", MintToken("Customer"));

        var response = await _client.PostAsJsonAsync("/api/v1/buses", new { });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task FullLifecycle_CreateDepot_RegisterBus_ChangeStatus_GetBus()
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", MintToken("Admin"));

        var depotResponse = await _client.PostAsJsonAsync("/api/v1/depots", new { name = "Central Depot", city = "Dhaka", address = (string?)null });
        depotResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var depotId = (await depotResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var plateNumber = $"TEST-{Guid.NewGuid():N}"[..15];
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/buses", new
        {
            operatorId = Guid.NewGuid(),
            plateNumber,
            busType = "AcSleeper",
            totalSeats = 40,
            depotId,
            manufacturer = "Volvo",
            model = "9600",
            yearOfManufacture = 2022
        });
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var busId = (await registerResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var statusResponse = await _client.PostAsJsonAsync($"/api/v1/buses/{busId}/status", new { newStatus = "UnderMaintenance" });
        statusResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var getResponse = await _client.GetAsync($"/api/v1/buses/{busId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await getResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("status").GetString().Should().Be("UnderMaintenance");
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        if (_factory is not null) await _factory.DisposeAsync();
        await Task.WhenAll(_postgres.DisposeAsync().AsTask(), _rabbitMq.DisposeAsync().AsTask(), _redis.DisposeAsync().AsTask());
    }
}
