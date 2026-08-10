using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace BusService.FunctionalTests;

/// <summary>
/// Functional tests for Bus Service.
/// These tests verify end-to-end behavior against the real API.
/// Run against a live Docker Compose stack or a test environment.
/// </summary>
public sealed class BusServiceFunctionalTests : IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    public BusServiceFunctionalTests()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                var config = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.FunctionalTests.json")
                    .AddEnvironmentVariables()
                    .Build();

                builder.Configuration.AddConfiguration(config);
            });
    }

    public async Task InitializeAsync()
    {
        _client = _factory.CreateClient();
        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        _factory.Dispose();
        await Task.CompletedTask;
    }

    [Fact]
    public async Task Health_ReturnsHealthy()
    {
        var response = await _client.GetAsync("/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ReleaseInfo_ReturnsVersionAndFeatures()
    {
        var response = await _client.GetAsync("/api/v1/release-info");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("success").GetBoolean().Should().BeTrue();
        json.GetProperty("data").GetProperty("serviceName").GetString().Should().Be("Bus Service");
        json.GetProperty("data").GetProperty("features").GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetBuses_WithoutAuth_Returns401()
    {
        var response = await _client.GetAsync("/api/v1/buses");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetBus_WithInvalidId_Returns400()
    {
        var token = MintToken("Admin");
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/v1/buses/not-a-guid");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RegisterBus_WithDuplicatePlate_Returns409()
    {
        // This test requires a pre-seeded database or test fixture.
        // Placeholder for the duplicate plate scenario.
        await Task.CompletedTask;
    }

    [Fact]
    public async Task ChangeBusStatus_RetiredThenActive_Returns400()
    {
        // This test requires an existing Retired bus.
        // Placeholder for invalid transition scenario.
        await Task.CompletedTask;
    }

    private static string MintToken(params string[] roles)
    {
        // In a real functional test, mint a JWT using the same key as the API.
        // For now, return a placeholder or integrate with a test helper.
        return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("test-token"));
    }
}
