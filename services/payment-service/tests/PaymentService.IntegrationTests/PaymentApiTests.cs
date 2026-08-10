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
using PaymentService.Application.Features.Payments.CreatePayment;
using PaymentService.Domain.Enums;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Testcontainers.Redis;
using Xunit;

namespace PaymentService.IntegrationTests;

public sealed class PaymentApiTests : IAsyncLifetime
{
    private const string TestSigningKey = "integration-test-signing-key-32-chars-minimum";
    private const string TestIssuer = "https://identity.payment-service.local";
    private const string TestAudience = "payment-service";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder().WithImage("postgres:16-alpine").WithDatabase("payment_service_test").Build();
    private readonly RabbitMqContainer _rabbitMq = new RabbitMqBuilder().WithImage("rabbitmq:3.13-management-alpine").Build();
    private readonly RedisContainer _redis = new RedisBuilder().WithImage("redis:7.4-alpine").Build();

    private WebApplicationFactory<Program>? _factory;
    private HttpClient _client = default!;

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_postgres.StartAsync(), _rabbitMq.StartAsync(), _redis.StartAsync());

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ASPNETCORE_ENVIRONMENT", "Development");
            builder.UseSetting("ConnectionStrings:DefaultConnection", _postgres.GetConnectionString());
            builder.UseSetting("Redis:ConnectionString", _redis.GetConnectionString());
            builder.UseSetting("RabbitMQ:HostName", _rabbitMq.Hostname);
            builder.UseSetting("RabbitMQ:Port", _rabbitMq.GetMappedPublicPort(5672).ToString());
            builder.UseSetting("Jwt:Issuer", TestIssuer);
            builder.UseSetting("Jwt:Audience", TestAudience);
            builder.UseSetting("Jwt:SigningKey", TestSigningKey);
        });

        _client = _factory.CreateClient();
    }

    private static string MintToken(params string[] roles)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()),
            new("tenant_id", "tenant-001"),
            new(ClaimTypes.NameIdentifier, "user-001")
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(TestIssuer, TestAudience, claims, expires: DateTime.UtcNow.AddHours(1), signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    [Fact]
    public async Task CreatePayment_WithValidData_ReturnsCreated()
    {
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", MintToken("Admin"));

        var command = new CreatePaymentCommand(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            Guid.Parse("44444444-4444-4444-4444-444444444444"),
            "ORDER-INT-001",
            PaymentMethodType.Card,
            150.00m,
            "USD",
            "idem-int-001",
            3.00m,
            7.50m,
            null,
            30);

        var json = System.Text.Json.JsonSerializer.Serialize(command);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/api/v1/payments", content);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreatePayment_WithoutAuth_ReturnsUnauthorized()
    {
        var command = new CreatePaymentCommand(
            Guid.NewGuid(), null, null, Guid.NewGuid(), "ORDER-001",
            PaymentMethodType.Card, 100m, "USD", "idem-001", null, null, null, null);

        var json = System.Text.Json.JsonSerializer.Serialize(command);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/api/v1/payments", content);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        if (_factory is not null) await _factory.DisposeAsync();
        await Task.WhenAll(_postgres.DisposeAsync().AsTask(), _rabbitMq.DisposeAsync().AsTask(), _redis.DisposeAsync().AsTask());
    }
}
