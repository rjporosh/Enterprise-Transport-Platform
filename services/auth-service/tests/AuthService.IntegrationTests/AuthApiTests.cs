using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Testcontainers.Redis;
using Xunit;

namespace AuthService.IntegrationTests;

/// <summary>
/// End-to-end test against a real Postgres + RabbitMQ + Redis, spun up via
/// Testcontainers, and the actual ASP.NET pipeline via WebApplicationFactory.
/// Exercises the full register -&gt; login -&gt; refresh -&gt; reuse-detection -&gt;
/// logout lifecycle against real HTTP + real SQL, which is exactly the layer
/// the InMemory-backed unit tests cannot cover.
///
/// NOTE: requires a Docker daemon reachable from wherever `dotnet test` runs;
/// this sandbox has no Docker/network access to actually execute it, so
/// treat this file as the intended test — run it locally or in CI to verify.
/// </summary>
public sealed class AuthApiTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("auth_service_test")
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
            builder.UseSetting("ConnectionStrings:AuthDb", _postgres.GetConnectionString());
            builder.UseSetting("RabbitMq:HostName", _rabbitMq.Hostname);
            builder.UseSetting("RabbitMq:Port", _rabbitMq.GetMappedPublicPort(5672).ToString());
            builder.UseSetting("Redis:ConnectionString", _redis.GetConnectionString());
            builder.UseSetting("Jwt:SigningKey", "integration-test-signing-key-32-chars-minimum");
        });

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Register_WithNewEmail_Returns200_AndTokenPair()
    {
        var email = $"{Guid.NewGuid():N}@example.com";

        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email,
            password = "correct-horse-battery-staple",
            firstName = "Integration",
            lastName = "Test",
            phoneNumber = (string?)null
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("accessToken").GetString().Should().NotBeNullOrEmpty();
        body.GetProperty("refreshToken").GetString().Should().NotBeNullOrEmpty();
        body.GetProperty("roles").EnumerateArray().Select(r => r.GetString()).Should().Contain("Customer");
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsConflict()
    {
        var email = $"{Guid.NewGuid():N}@example.com";
        var payload = new { email, password = "correct-horse-battery-staple", firstName = "A", lastName = "B", phoneNumber = (string?)null };

        await _client.PostAsJsonAsync("/api/v1/auth/register", payload);
        var second = await _client.PostAsJsonAsync("/api/v1/auth/register", payload);

        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task FullLifecycle_Register_Login_Refresh_Logout_ThenRefreshAgainFails()
    {
        var email = $"{Guid.NewGuid():N}@example.com";
        var password = "correct-horse-battery-staple";

        await _client.PostAsJsonAsync("/api/v1/auth/register", new { email, password, firstName = "A", lastName = "B", phoneNumber = (string?)null });

        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", new { email, password });
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var loginBody = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        var refreshToken = loginBody.GetProperty("refreshToken").GetString();

        var refreshResponse = await _client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken });
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var newRefreshToken = (await refreshResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("refreshToken").GetString();

        var logoutResponse = await _client.PostAsJsonAsync("/api/v1/auth/logout", new { refreshToken = newRefreshToken });
        logoutResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // The original (already-rotated) token is a reuse signal and must
        // now be rejected too, per the token-family revocation rule.
        var reuseResponse = await _client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken });
        reuseResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCurrentUser_WithoutAuthToken_Returns401()
    {
        var response = await _client.GetAsync("/api/v1/auth/me");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401_AndDoesNotRevealWhetherEmailExists()
    {
        var knownEmail = $"{Guid.NewGuid():N}@example.com";
        await _client.PostAsJsonAsync("/api/v1/auth/register", new { email = knownEmail, password = "correct-horse-battery-staple", firstName = "A", lastName = "B", phoneNumber = (string?)null });

        var wrongPasswordResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", new { email = knownEmail, password = "totally-wrong" });
        var unknownEmailResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", new { email = $"{Guid.NewGuid():N}@example.com", password = "totally-wrong" });

        wrongPasswordResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        unknownEmailResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var wrongPasswordBody = await wrongPasswordResponse.Content.ReadAsStringAsync();
        var unknownEmailBody = await unknownEmailResponse.Content.ReadAsStringAsync();
        // Same ProblemDetails "title" either way — never let a client distinguish the two cases.
        JsonDocument.Parse(wrongPasswordBody).RootElement.GetProperty("title").GetString()
            .Should().Be(JsonDocument.Parse(unknownEmailBody).RootElement.GetProperty("title").GetString());
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        if (_factory is not null) await _factory.DisposeAsync();
        await Task.WhenAll(_postgres.DisposeAsync().AsTask(), _rabbitMq.DisposeAsync().AsTask(), _redis.DisposeAsync().AsTask());
    }
}
