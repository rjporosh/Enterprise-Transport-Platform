using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using PaymentService.Application.Common.Interfaces;
using PaymentService.Application.Features.Payments.CreatePayment;
using PaymentService.Domain.Enums;
using PaymentService.Infrastructure.Messaging;
using PaymentService.Infrastructure.Persistence;
using Xunit;

namespace PaymentService.IntegrationTests;

public sealed class PaymentApiTests : IAsyncLifetime
{
    private const string TestSigningKey = "integration-test-signing-key-32-chars-minimum";
    private const string TestIssuer = "https://identity.payment-service.local";
    private const string TestAudience = "payment-service";
    private SqliteConnection? _sharedConnection;

    private WebApplicationFactory<Program>? _factory;
    private HttpClient _client = default!;

    public async Task InitializeAsync()
    {
        _sharedConnection = new SqliteConnection("DataSource=:memory:");
        await _sharedConnection.OpenAsync();

        _factory = new TestWebApplicationFactory(_sharedConnection);
        _client = _factory.CreateClient();
    }

        private sealed class TestWebApplicationFactory : WebApplicationFactory<Program>
        {
            private readonly SqliteConnection _connection;

            public TestWebApplicationFactory(SqliteConnection connection)
            {
                _connection = connection;
            }

            protected override void ConfigureWebHost(IWebHostBuilder builder)
            {
                builder.UseSetting(WebHostDefaults.EnvironmentKey, "Testing");
                builder.UseSetting("ConnectionStrings:DefaultConnection", "DataSource=:memory:");
                builder.UseSetting("Database:Provider", "sqlite");
                builder.UseSetting("Redis:ConnectionString", "localhost:6379");
                builder.UseSetting("RabbitMQ:HostName", "localhost");
                builder.UseSetting("RabbitMQ:Port", "5672");
                builder.UseSetting("Jwt:Issuer", TestIssuer);
                builder.UseSetting("Jwt:Audience", TestAudience);
                builder.UseSetting("Jwt:SigningKey", TestSigningKey);

                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IMessageBusPublisher));
                    if (descriptor is not null)
                        services.Remove(descriptor);

                    descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ICacheService));
                    if (descriptor is not null)
                        services.Remove(descriptor);

                    descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IPaymentMetrics));
                    if (descriptor is not null)
                        services.Remove(descriptor);

                    descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<PaymentDbContext>));
                    if (descriptor is not null)
                        services.Remove(descriptor);

                    services.AddSingleton<IMessageBusPublisher, TestMessageBusPublisher>();
                    services.AddSingleton<ICacheService, TestCacheService>();
                    services.AddSingleton<IPaymentMetrics, TestPaymentMetrics>();

                    services.AddDbContext<PaymentDbContext>(options =>
                    {
                        options.UseSqlite(_connection)
                            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
                    });

                    services.AddScoped<IPaymentDbContext>(sp => sp.GetRequiredService<PaymentDbContext>());

                    var sp = services.BuildServiceProvider();
                    using var scope = sp.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
                    db.Database.EnsureCreated();
                });
            }
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
        if (_sharedConnection is not null) await _sharedConnection.DisposeAsync();
    }

    private sealed class TestMessageBusPublisher : IMessageBusPublisher
    {
        public Task PublishAsync(string routingKey, string payload, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class TestCacheService : ICacheService
    {
        public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
        {
            return Task.FromResult<T?>(null);
        }

        public Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken cancellationToken = default) where T : class
        {
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class TestPaymentMetrics : IPaymentMetrics
    {
        public void RecordPaymentCreated(string paymentMethod, string status) { }
        public void RecordPaymentSucceeded(string paymentMethod, decimal amount, string currency) { }
        public void RecordPaymentFailed(string paymentMethod, string? failureCode) { }
        public void RecordRefundCreated(string currency, decimal amount) { }
        public void RecordRefundSucceeded(string currency, decimal amount) { }
        public void RecordProviderLatency(string provider, double milliseconds) { }
        public void RecordIdempotencyConflict() { }
        public void RecordCircuitBreakerOpened(string provider) { }
    }
}
