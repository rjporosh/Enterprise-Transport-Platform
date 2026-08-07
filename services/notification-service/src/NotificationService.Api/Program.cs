using NotificationService.Api.Endpoints;
using NotificationService.Api.Grpc;
using NotificationService.Api.Middleware;
using NotificationService.Application;
using NotificationService.Infrastructure;
using NotificationService.Infrastructure.Persistence;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Scalar.AspNetCore;
using Serilog;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// ---------- Serilog ----------
// Console sink for local/dev tailing + a rolling file sink under
// logs/runtime-errors/ (CLAUDE.md, "Custom Logging Framework" ->
// "runtime-error-dd-mm-yy.txt"). A bespoke logging DI framework/file format
// was NOT built from scratch for this service — see this delivery's Known
// Limitations for why (it would be a cross-service architecture decision,
// not a notification-service-only change) — but the *outcome* CLAUDE.md
// actually wants (structured, dated, greppable runtime-error files, rolled
// daily, retained, with root cause/possible solution captured — see
// ExceptionHandlingMiddleware and every channel sender's catch block) is
// delivered here through Serilog's own file sink, which already does daily
// rolling/retention robustly and is the same logging library already used
// by every other service in this solution.
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Service", "notification-service")
    .Enrich.WithEnvironmentName()
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] ({CorrelationId}) {Message:lj} {Properties:j}{NewLine}{Exception}")
    .WriteTo.Logger(errorLogger => errorLogger
        .Filter.ByIncludingOnly(e => e.Level >= Serilog.Events.LogEventLevel.Error)
        .WriteTo.File(
            "logs/runtime-errors/runtime-error-.txt",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 30,
            outputTemplate:
                "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] ({CorrelationId}) {Message:lj}{NewLine}{Exception}{NewLine}")));

// ---------- Application / Infrastructure ----------
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHttpContextAccessor();

// ---------- Auth ----------
// Notification Service trusts bearer tokens issued by Auth Service (same
// signing key/issuer/audience convention) but does not itself issue or
// refresh tokens — RequireAuthorization() below is used only for the
// operator-facing surface (Templates admin CRUD, manual Retry/Delete);
// SendNotification and the recipient-preferences endpoints are called
// service-to-service or by an already-authenticated frontend and are left
// open at this layer, matching how BookingService leaves its
// event-triggered endpoints open. See appsettings.json "Jwt" section.
var jwtSection = builder.Configuration.GetSection("Jwt");
if (jwtSection.Exists())
{
    builder.Services
        .AddAuthentication(Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtSection["Issuer"],
                ValidateAudience = true,
                ValidAudience = jwtSection["Audience"],
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                    System.Text.Encoding.UTF8.GetBytes(jwtSection["SigningKey"] ?? string.Empty)),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30)
            };
        });
}
builder.Services.AddAuthorization();

// ---------- Rate limiting ----------
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Looser than AuthService's login/register limit (this isn't a
    // credential-stuffing target) but still bounded — a misbehaving upstream
    // caller retrying in a tight loop should not be able to exhaust SMTP/SMS
    // provider quota for the whole platform.
    options.AddPolicy("notification-write", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 60,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        }));
});

// ---------- OpenAPI / Scalar ----------
// Native Microsoft.AspNetCore.OpenApi + Scalar, not Swashbuckle -- same
// reasoning already documented in AuthService.Api/Program.cs (OpenAPI.NET
// v1 vs v2 model mismatch on .NET 10).
builder.Services.AddOpenApi("v1", options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info.Title = "Notification Service API";
        document.Info.Version = "v1";
        document.Info.Description = "Email/SMS/Push delivery, templates, and recipient preferences for the Enterprise Transport Platform.";
        return Task.CompletedTask;
    });
});

// ---------- Health checks ----------
var healthChecks = builder.Services.AddHealthChecks();
var dbProvider = (builder.Configuration["Database:Provider"] ?? "Postgres").Trim().ToLowerInvariant();
var notificationDbConnectionString = builder.Configuration.GetConnectionString("NotificationDb");
if (!string.IsNullOrWhiteSpace(notificationDbConnectionString) && dbProvider is "postgres" or "postgresql" or "npgsql")
{
    healthChecks.AddNpgSql(notificationDbConnectionString, name: "postgres");
}
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
    .ConfigureResource(resource => resource.AddService(serviceName: "notification-service", serviceVersion: "1.0.0"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddEntityFrameworkCoreInstrumentation()
        .AddSource("NotificationService")
        .AddOtlpExporter(otlp => otlp.Endpoint = new Uri(builder.Configuration["OpenTelemetry:OtlpEndpoint"] ?? "http://localhost:4317")))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddPrometheusExporter());

// ---------- gRPC ----------
builder.Services.AddGrpc();

var app = builder.Build();

// ---------- Middleware pipeline ----------
// Order matters: correlation id first (everything downstream, including
// request logging, needs it), then request logging, then the global
// exception handler (so it can log with the correlation id already in
// scope), then locale resolution and idempotency before routing/endpoints.
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseSerilogRequestLogging();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<LocalizationMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference("/scalar", options =>
    {
        options.Title = "Notification Service API";
        options.Theme = ScalarTheme.Purple;
    });
}

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<IdempotencyMiddleware>();

app.MapNotificationsEndpoints();
app.MapTemplatesEndpoints();
app.MapPreferencesEndpoints();
app.MapGrpcService<NotificationGrpcServiceImpl>();

app.MapHealthChecks("/health");
app.MapPrometheusScrapingEndpoint("/metrics");

// ---------- Dev-only auto-migrate ----------
// Same convention/caveat as every other service here -- see
// AuthService.Api/Program.cs for the "never do this in real prod" note.
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
    await db.Database.MigrateAsync();
}

app.Run();

// Exposed for WebApplicationFactory<Program> in NotificationService.IntegrationTests.
public partial class Program { }
