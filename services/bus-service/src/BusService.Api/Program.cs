using System.Net;
using System.Text;
using System.Threading.RateLimiting;
using BusService.Api.Diagnostics;
using BusService.Api.Endpoints;
using BusService.Api.Middleware;
using BusService.Api.Security;
using BusService.Application;
using BusService.Application.Common.Interfaces;
using BusService.Infrastructure;
using BusService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Scalar.AspNetCore;
using Serilog;

// Everything below is wrapped in one try/catch — see Api/Diagnostics/RuntimeErrorLogWriter.cs.
// The whole point: a crash before the DI container even finishes building
// (bad connection string, unreachable Postgres/Redis/RabbitMQ, port already
// in use) is exactly the kind of failure that is otherwise easy to miss in
// a scrolling terminal — this guarantees it also lands in
// logs/runtime-error-<dd-MM-yyyy-HH-mm-ss>.txt with a plain-English
// diagnosis, then rethrows so the process still exits non-zero as normal.
var contentRootForCrashHandler = Directory.GetCurrentDirectory();
try
{
    var builder = WebApplication.CreateBuilder(args);
    contentRootForCrashHandler = builder.Environment.ContentRootPath;

    // ---------- Serilog ----------
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Service", "bus-service")
        .Enrich.WithEnvironmentName()
        .WriteTo.Console(outputTemplate:
            "[{Timestamp:HH:mm:ss} {Level:u3}] ({CorrelationId}) {Message:lj} {Properties:j}{NewLine}{Exception}"));

    // ---------- Application / Infrastructure ----------
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    // ---------- Auth (validates JWTs issued by Auth Service — same signing key config) ----------
    var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "https://identity.bus-ticketing.local";
    var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "bus-ticketing-api";
    var jwtSigningKey = builder.Configuration["Jwt:SigningKey"] ?? "REPLACE_WITH_A_SECRET_AT_LEAST_32_CHARS_LONG_IN_PROD";

    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<ICurrentUser, CurrentUser>();

    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
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
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.OnRejected = async (context, token) =>
        {
            context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            await context.HttpContext.Response.WriteAsJsonAsync(new
            {
                type = "https://httpstatuses.io/429",
                title = "Too many requests.",
                status = StatusCodes.Status429TooManyRequests,
                traceId = context.HttpContext.TraceIdentifier,
                detail = "Rate limit exceeded. Please retry after the window resets."
            });
        };

        options.AddPolicy("bus-write", httpContext => RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.GetClientIpAddress() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

        options.AddPolicy("bus-read", httpContext => RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.GetClientIpAddress() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 10
            }));
    });

    // ---------- OpenAPI / Scalar ----------
    // Native minimal-API OpenAPI document generation + Scalar only — not Swashbuckle.
    // Swashbuckle and the framework's own OpenAPI.NET v2-based generator disagree on
    // the OpenApiDocument/OpenApiSchema shape on .NET 10, and Scalar's default
    // document route is the native generator's route — running Swashbuckle here would
    // reproduce the "Scalar loads but shows nothing" bug fixed elsewhere.
    builder.Services.AddOpenApi("v1", options =>
    {
        options.AddDocumentTransformer((document, context, cancellationToken) =>
        {
            document.Info.Title = "Bus Service API";
            document.Info.Version = "v1";
            document.Info.Description = "Fleet management — the canonical source of truth for buses and depots across the Enterprise Transport Platform.";

            document.Components ??= new Microsoft.OpenApi.OpenApiComponents();
            document.Components.SecuritySchemes ??= new Dictionary<string, Microsoft.OpenApi.IOpenApiSecurityScheme>();
            document.Components.SecuritySchemes["Bearer"] = new Microsoft.OpenApi.OpenApiSecurityScheme
            {
                Type = Microsoft.OpenApi.SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Paste an access token issued by Auth Service's /api/v1/auth/login."
            };

            return Task.CompletedTask;
        });
    });

    // ---------- Health checks ----------
    var healthChecks = builder.Services.AddHealthChecks();
    var dbProvider = (builder.Configuration["Database:Provider"] ?? "Postgres").Trim().ToLowerInvariant();
    var busDbConnectionString = builder.Configuration.GetConnectionString("BusDb");
    if (!string.IsNullOrWhiteSpace(busDbConnectionString) && dbProvider is "postgres" or "postgresql" or "npgsql")
        healthChecks.AddNpgSql(busDbConnectionString, name: "postgres");

    var redisConnectionString = builder.Configuration["Redis:ConnectionString"];
    if (!string.IsNullOrWhiteSpace(redisConnectionString))
        healthChecks.AddRedis(redisConnectionString, name: "redis");

    var rabbitHost = builder.Configuration["RabbitMq:HostName"];
    if (!string.IsNullOrWhiteSpace(rabbitHost))
    {
        var rabbitUser = builder.Configuration["RabbitMq:UserName"] ?? "guest";
        var rabbitPass = builder.Configuration["RabbitMq:Password"] ?? "guest";
        var rabbitPort = builder.Configuration["RabbitMq:Port"] ?? "5672";
        healthChecks.AddRabbitMQ(
            rabbitConnectionString: $"amqp://{rabbitUser}:{rabbitPass}@{rabbitHost}:{rabbitPort}",
            name: "rabbitmq");
    }

    // ---------- gRPC ----------
    builder.Services.AddGrpc(options =>
    {
        options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    });

    // ---------- OpenTelemetry ----------
    builder.Services.AddOpenTelemetry()
        .ConfigureResource(resource => resource.AddService(serviceName: "bus-service", serviceVersion: "1.0.0"))
        .WithTracing(tracing => tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddEntityFrameworkCoreInstrumentation()
            .AddOtlpExporter(otlp => otlp.Endpoint = new Uri(builder.Configuration["OpenTelemetry:OtlpEndpoint"] ?? "http://localhost:4317")))
        .WithMetrics(metrics => metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation()
            .AddMeter(BusService.Infrastructure.Observability.BusMetrics.MeterName)
            .AddPrometheusExporter());

    var app = builder.Build();

    // ---------- Middleware pipeline ----------
    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseSerilogRequestLogging();
    app.UseMiddleware<ExceptionHandlingMiddleware>();
    app.UseMiddleware<RequestContextMiddleware>();
    app.UseMiddleware<IdempotencyMiddleware>();
    app.UseRateLimiter();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference("/scalar", options =>
        {
            options.Title = "Bus Service API";
            options.Theme = ScalarTheme.Purple;
        });
    }

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapBusEndpoints();
    app.MapHealthChecks("/health");
    app.MapPrometheusScrapingEndpoint("/metrics");
    app.MapGrpcService<BusService.Api.Grpc.BusGrpcService>();

    // ---------- Dev-only auto-migrate ----------
    if (app.Environment.IsDevelopment())
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BusDbContext>();
        await db.Database.MigrateAsync();
    }

    app.Run();
}
catch (Exception ex)
{
    var logPath = RuntimeErrorLogWriter.Write(ex, contentRootForCrashHandler);
    Console.Error.WriteLine();
    Console.Error.WriteLine($"FATAL: Bus Service failed to start or crashed. Details saved to: {logPath}");
    Console.Error.WriteLine();
    throw;
}

// Exposed for WebApplicationFactory<Program> in BusService.IntegrationTests.
public partial class Program { }
