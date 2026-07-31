using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Testcontainers.Redis;
using Xunit;

namespace BookingService.IntegrationTests;

/// <summary>
/// End-to-end test against a real Postgres + RabbitMQ, spun up via
/// Testcontainers, and the actual ASP.NET pipeline via WebApplicationFactory.
/// This is the layer that would have caught anything the unit tests'
/// InMemory provider couldn't — e.g. the xmin concurrency mapping, jsonb
/// serialization, real SQL translation of the seat-count subquery.
///
/// NOTE: requires a Docker daemon reachable from wherever `dotnet test` runs;
/// this sandbox has no Docker/network access to actually execute it, so
/// treat this file as the intended test — run it locally or in CI to verify.
/// </summary>
public sealed class BookingApiTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("booking_service_test")
        .Build();

    private readonly RabbitMqContainer _rabbitMq = new RabbitMqBuilder()
        .WithImage("rabbitmq:3.13-management-alpine")
        .Build();

    private readonly RedisContainer _redis = new RedisBuilder()
        .WithImage("redis:7.4-alpine")
        .Build();

    private WebApplicationFactory<Program>? _factory;
    private HttpClient _client = default!;

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_postgres.StartAsync(), _rabbitMq.StartAsync(), _redis.StartAsync());

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:BookingDb", _postgres.GetConnectionString());
            builder.UseSetting("RabbitMq:HostName", _rabbitMq.Hostname);
            builder.UseSetting("RabbitMq:Port", _rabbitMq.GetMappedPublicPort(5672).ToString());
            builder.UseSetting("Redis:ConnectionString", _redis.GetConnectionString());
        });

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task SearchTrips_WithNoMatchingTrips_ReturnsEmptyPagedResult()
    {
        var response = await _client.GetAsync(
            $"/api/v1/trips/search?origin=Dhaka&destination=Sylhet&date={DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)):yyyy-MM-dd}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("totalCount").GetInt32().Should().Be(0);
    }

    [Fact]
    public async Task CreateBooking_WithoutAuthToken_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/bookings", new
        {
            tripId = Guid.NewGuid(),
            customerId = Guid.NewGuid(),
            passengers = Array.Empty<object>()
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        if (_factory is not null) await _factory.DisposeAsync();
        await Task.WhenAll(_postgres.DisposeAsync().AsTask(), _rabbitMq.DisposeAsync().AsTask(), _redis.DisposeAsync().AsTask());
    }
}
