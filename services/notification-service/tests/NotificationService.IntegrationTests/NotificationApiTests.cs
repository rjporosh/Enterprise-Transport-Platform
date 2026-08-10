using System;
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Xunit;

namespace NotificationService.IntegrationTests;

/// <summary>
/// End-to-end tests against a real Postgres + RabbitMQ (via Testcontainers)
/// and the actual ASP.NET pipeline (via WebApplicationFactory) — exercises
/// HTTP -> MediatR -> EF Core -> Postgres -> outbox -> RabbitMQ, which the
/// InMemory-backed unit tests in NotificationService.UnitTests cannot cover.
/// Smtp/Sms/Push are left pointed at their (unreachable, by default) config
/// values — these tests only assert on the HTTP-visible outcome (a
/// notification is accepted and persisted as Pending/Scheduled), not on an
/// actual email/SMS/push being delivered; verifying real provider delivery
/// is out of scope for an automated test and is a manual/staging-environment
/// concern (see docs/programmers-guide/notification-channels.md).
///
/// NOTE: requires a Docker daemon reachable from wherever `dotnet test` runs;
/// this sandbox has no Docker/network access to actually execute it, so
/// treat this file as the intended test — run it locally or in CI to verify
/// (same caveat as AuthService.IntegrationTests/AuthApiTests.cs).
/// </summary>
public sealed class NotificationApiTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("notification_service_test")
        .Build();

    private readonly RabbitMqContainer _rabbitMq = new RabbitMqBuilder()
        .WithImage("rabbitmq:3.13-management-alpine")
        .Build();

    private WebApplicationFactory<Program>? _factory;
    private HttpClient _client = default!;

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_postgres.StartAsync(), _rabbitMq.StartAsync());

        var rabbitConnectionString = _rabbitMq.GetConnectionString();
        var rabbitUri = new Uri(rabbitConnectionString);
        var rabbitUser = rabbitUri.UserInfo.Split(':')[0];
        var rabbitPass = rabbitUri.UserInfo.Split(':')[1];
        var rabbitHost = rabbitUri.Host;
        var rabbitPort = rabbitUri.Port;

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:NotificationDb", _postgres.GetConnectionString());
            builder.UseSetting("RabbitMq:HostName", rabbitHost);
            builder.UseSetting("RabbitMq:Port", rabbitPort.ToString());
            builder.UseSetting("RabbitMq:UserName", rabbitUser);
            builder.UseSetting("RabbitMq:Password", rabbitPass);
            builder.UseSetting("Jwt:SigningKey", "integration-test-signing-key-32-chars-minimum");
            builder.UseSetting("ASPNETCORE_ENVIRONMENT", "Development");
        });

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NotificationService.Infrastructure.Persistence.NotificationDbContext>();
        await db.Database.MigrateAsync();

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task SendNotification_WithValidBody_Returns201_AndPersistsAsPending()
    {
        var request = new
        {
            recipient = "jane@example.com",
            channel = "Email",
            subject = "Welcome",
            body = "Hello Jane, welcome aboard.",
            priority = "Normal",
            isTransactional = true
        };

        var response = await _client.PostAsJsonAsync("/api/v1/notifications", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<JsonElementWrapper>();
        body.Should().NotBeNull();
    }

    [Fact]
    public async Task SendNotification_WithNeitherTemplateNorBody_Returns400_WithValidationError()
    {
        var request = new { recipient = "jane@example.com", channel = "Email", isTransactional = true };

        var response = await _client.PostAsJsonAsync("/api/v1/notifications", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetNotificationById_ForUnknownId_Returns404()
    {
        var response = await _client.GetAsync($"/api/v1/notifications/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateThenCancelNotification_TransitionsToCancelled()
    {
        var sendResponse = await _client.PostAsJsonAsync("/api/v1/notifications", new
        {
            recipient = "jane@example.com",
            channel = "Email",
            subject = "Welcome",
            body = "Hello",
            priority = "Normal",
            scheduledForUtc = DateTimeOffset.UtcNow.AddHours(1), // scheduled, so the dispatch job won't race the assertion
            isTransactional = true
        });
        sendResponse.EnsureSuccessStatusCode();
        var created = await sendResponse.Content.ReadFromJsonAsync<SendResponseWrapper>();

        var cancelResponse = await _client.PostAsJsonAsync(
            $"/api/v1/notifications/{created!.Data!.NotificationId}/cancel", new { reason = "Integration test cleanup" });

        cancelResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var getResponse = await _client.GetFromJsonAsync<GetResponseWrapper>(
            $"/api/v1/notifications/{created.Data.NotificationId}");
        getResponse!.Data!.Status.Should().Be("Cancelled");
    }

    [Fact]
    public async Task HealthEndpoint_Returns200()
    {
        var response = await _client.GetAsync("/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    public async Task DisposeAsync()
    {
        if (_factory is not null) await _factory.DisposeAsync();
        await _postgres.DisposeAsync();
        await _rabbitMq.DisposeAsync();
    }

    private sealed record JsonElementWrapper(bool Success, string Message);
    private sealed record SendResponseWrapper(bool Success, SendDataWrapper? Data);
    private sealed record SendDataWrapper(Guid NotificationId, string Status);
    private sealed record GetResponseWrapper(bool Success, GetDataWrapper? Data);
    private sealed record GetDataWrapper(Guid Id, string Status);
}
