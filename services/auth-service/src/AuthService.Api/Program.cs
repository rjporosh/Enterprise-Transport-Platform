using System.Text;
using System.Threading.RateLimiting;
using AuthService.Api.Endpoints;
using AuthService.Api.Middleware;
using AuthService.Api.Security;
using AuthService.Application;
using AuthService.Application.Common.Interfaces;
using AuthService.Infrastructure;
using AuthService.Infrastructure.Persistence;
using AuthService.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ---------- Serilog ----------
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Service", "auth-service")
    .Enrich.WithEnvironmentName()
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] ({CorrelationId}) {Message:lj} {Properties:j}{NewLine}{Exception}"));

// ---------- Application / Infrastructure ----------
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// ---------- Auth ----------
var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization();

// ---------- Rate limiting ----------
// A stricter policy on the write-heavy auth endpoints (register/login/refresh)
// than the platform gateway's general limit — these are exactly the
// endpoints credential-stuffing and account-enumeration attacks target.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("auth-write", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: httpContext.GetClientIpAddress() ?? "unknown",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        }));
});

// ---------- OpenAPI / Scalar ----------
// Swashbuckle is deliberately NOT used here: on .NET 10, Swashbuckle (still
// built against the OpenAPI.NET v1 object model) and the framework's own
// Microsoft.AspNetCore.OpenApi (OpenAPI.NET v2) disagree on the shape of
// OpenApiDocument/OpenApiSchema. They didn't crash at startup here (unlike
// the near-identical bug in Booking Service — see that service's Program.cs
// comment), but Scalar's default document route is /openapi/{documentName}.json,
// which is the NATIVE generator's route, not Swashbuckle's /swagger/v1/swagger.json
// — so Scalar loaded with zero operations shown, silently, instead of
// erroring. Native OpenAPI + Scalar only, matching Booking Service.
builder.Services.AddOpenApi("v1", options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info.Title = "Auth Service API";
        document.Info.Version = "v1";
        document.Info.Description = "Identity, authentication, and account-security audit trail for the Enterprise Transport Platform.";

        // .NET 10's native OpenAPI generator uses OpenAPI.NET v2, where
        // Components.SecuritySchemes is IDictionary<string, IOpenApiSecurityScheme>
        // (interface-typed) and is null until first touched — unlike Swashbuckle's
        // AddSecurityDefinition, there's no service-collection-level shortcut for
        // this, it has to happen inside a document transformer. This declares the
        // scheme so Scalar's Authentication panel offers a place to paste a token;
        // it deliberately does NOT also try to attach a formal per-operation
        // OpenApiReference-based security requirement — that specific API shape
        // changed between .NET 9 and 10 in ways not fully stable to hand-write
        // without a compiler to check against, and Scalar's "paste token here"
        // UX works fine without it.
        document.Components ??= new Microsoft.OpenApi.OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, Microsoft.OpenApi.IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["Bearer"] = new Microsoft.OpenApi.OpenApiSecurityScheme
        {
            Type = Microsoft.OpenApi.SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Paste the access token returned by /api/v1/auth/login or /register."
        };

        return Task.CompletedTask;
    });
});

// ---------- Health checks ----------
var healthChecks = builder.Services.AddHealthChecks();
var dbProvider = (builder.Configuration["Database:Provider"] ?? "Postgres").Trim().ToLowerInvariant();
var authDbConnectionString = builder.Configuration.GetConnectionString("AuthDb");
if (!string.IsNullOrWhiteSpace(authDbConnectionString) && dbProvider is "postgres" or "postgresql" or "npgsql")
{
    // Health-check packages that ship a matching probe for SqlServer/MySql
    // are wired the same way when Database:Provider is switched — see
    // docs/development/how-to-check-observability-stack.md.
    healthChecks.AddNpgSql(authDbConnectionString, name: "postgres");
}
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

// ---------- OpenTelemetry ----------
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(serviceName: "auth-service", serviceVersion: "1.0.0"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddEntityFrameworkCoreInstrumentation()
        .AddOtlpExporter(otlp => otlp.Endpoint = new Uri(builder.Configuration["OpenTelemetry:OtlpEndpoint"] ?? "http://localhost:4317")))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddMeter(AuthService.Infrastructure.Observability.AuthMetrics.MeterName)
        .AddPrometheusExporter());

var app = builder.Build();

// ---------- Middleware pipeline ----------
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseSerilogRequestLogging();
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();                          // -> /openapi/v1.json
    app.MapScalarApiReference("/scalar", options =>
    {
        options.Title = "Auth Service API";
        options.Theme = ScalarTheme.Purple;
    });
}

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();
app.MapHealthChecks("/health");
app.MapPrometheusScrapingEndpoint("/metrics");

// ---------- Dev-only auto-migrate ----------
// Never do this in a real prod deployment — migrations belong in the CI/CD
// pipeline (see infrastructure/cicd) so a schema change is reviewable and
// rollback-able independent of an app restart. Convenience only for local
// docker-compose / first-run dev.
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    await db.Database.MigrateAsync();
}

app.Run();

// Exposed for WebApplicationFactory<Program> in AuthService.IntegrationTests.
public partial class Program { }
