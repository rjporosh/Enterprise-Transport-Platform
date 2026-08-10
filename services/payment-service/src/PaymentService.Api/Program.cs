using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using PaymentService.Api.Endpoints;
using PaymentService.Api.Middleware;
using PaymentService.Application;
using PaymentService.Infrastructure;
using PaymentService.Infrastructure.Persistence;
using Scalar.AspNetCore;
using Serilog;
using StackExchange.Redis;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Service", "PaymentService")
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

try
{
    builder.Services.AddApplication();
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<PaymentService.Application.Common.Interfaces.ICurrentUser, PaymentService.Api.Security.CurrentUser>();
    builder.Services.AddInfrastructure(builder.Configuration);

    builder.Services.AddCors();

    builder.Services.AddAuthentication("Bearer")
        .AddJwtBearer("Bearer", options =>
        {
            var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "http://localhost:5001";
            var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "payment-service";
            var jwtSigningKey = builder.Configuration["Jwt:SigningKey"] ?? "super_secret_development_key_please_change_in_production";

            options.RequireHttpsMetadata = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtIssuer,
                ValidateAudience = true,
                ValidAudience = jwtAudience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSigningKey)),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30)
            };
        });

    builder.Services.AddAuthorization();

    builder.Services.AddRateLimiter(options =>
    {
        options.AddPolicy("PaymentPolicy", context =>
        {
            return RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: context.User.Identity?.Name ?? context.Request.Headers.Host.ToString(),
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 100,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                });
        });
    });

    builder.Services.AddOpenApi("v1");

    var defaultConnection = builder.Configuration.GetConnectionString("DefaultConnection");
    var healthChecks = builder.Services.AddHealthChecks();
    if (!string.IsNullOrWhiteSpace(defaultConnection))
    {
        healthChecks.AddNpgSql(defaultConnection, name: "postgres");
    }

    var redisConnection = builder.Configuration["Redis:ConnectionString"];
    if (!string.IsNullOrWhiteSpace(redisConnection))
    {
        healthChecks.AddRedis(redisConnection, name: "redis");
    }

    var rabbitHost = builder.Configuration["RabbitMQ:HostName"];
    if (!string.IsNullOrWhiteSpace(rabbitHost))
    {
        var rabbitUser = builder.Configuration["RabbitMQ:UserName"] ?? "guest";
        var rabbitPass = builder.Configuration["RabbitMQ:Password"] ?? "guest";
        var rabbitPort = builder.Configuration["RabbitMQ:Port"] ?? "5672";
        healthChecks.AddRabbitMQ(
            rabbitConnectionString: $"amqp://{rabbitUser}:{rabbitPass}@{rabbitHost}:{rabbitPort}/",
            name: "rabbitmq",
            timeout: TimeSpan.FromSeconds(5));
    }

    builder.Services.AddOpenTelemetry()
        .WithTracing(tracing =>
        {
            tracing.AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddSource("PaymentService")
                .SetSampler(new AlwaysOnSampler())
                .AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri(builder.Configuration["OpenTelemetry:Endpoint"] ?? "http://localhost:4317");
                });
        })
        .WithMetrics(metrics =>
        {
            metrics.AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddMeter("PaymentService")
                .AddPrometheusExporter();
        });

    var app = builder.Build();

    app.UseMiddleware<PaymentService.Api.Middleware.CorrelationIdMiddleware>();
    app.UseMiddleware<PaymentService.Api.Middleware.RequestContextMiddleware>();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference("/scalar", options =>
        {
            options.WithTitle("Payment Service API")
                .WithTheme(ScalarTheme.Purple);
        });
    }

    app.UseCors(policy =>
    {
        policy.WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });

    app.UseAuthentication();
    app.UseAuthorization();
    app.UseRateLimiter();

    app.UseMiddleware<PaymentService.Api.Middleware.ExceptionHandlingMiddleware>();

    app.MapOpenApi();
    app.MapScalarApiReference();

    app.MapPaymentEndpoints();
    app.MapAgentPaymentMethodEndpoints();

    app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
    app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = r => r.Name != "postgres" });

    app.MapPrometheusScrapingEndpoint();

    if (app.Environment.IsDevelopment())
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    var logger = app.Services.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Program>>();
    logger.LogInformation("Starting Payment Service on {Environment}", app.Environment.EnvironmentName);

    await app.RunAsync();

    return 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    throw;
}

public partial class Program { }
