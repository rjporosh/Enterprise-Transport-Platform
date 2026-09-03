using System.Text;
using BookingService.Api.Diagnostics;
using BookingService.Api.Endpoints;
using BookingService.Api.Middleware;
using BookingService.Api.Security;
using BookingService.Application;
using BookingService.Application.Common.Interfaces;
using BookingService.Infrastructure;
using BookingService.Infrastructure.Observability;
using BookingService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
const string ServiceName = "booking-service";

// --- Logging ---------------------------------------------------------------
// Console (always) + Seq (structured, queryable log store — see infrastructure/docker).
builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Service", ServiceName)
        .WriteTo.Console();

    var seqUrl = context.Configuration["Seq:ServerUrl"];
    if (!string.IsNullOrWhiteSpace(seqUrl))
        configuration.WriteTo.Seq(seqUrl);
});

// --- Application services ---------------------------------------------------
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// --- Observability: OpenTelemetry traces + metrics --------------------------
// Traces export over OTLP to Jaeger. Metrics are scraped by Prometheus from
// /metrics and visualized in Grafana (see infrastructure/monitoring and
// docs/OBSERVABILITY_GUIDE.md for exact queries to run against each tool).
//
// IMPORTANT: this block only touches TracerProviderBuilder/MeterProviderBuilder
// extension methods (Add***Instrumentation / AddOtlpExporter / AddPrometheusExporter).
// Health checks (AddNpgSql/AddRabbitMQ/AddRedis) belong ONLY in the
// AddHealthChecks() block further down — mixing them in here previously
// caused the "builds but won't start" bug reported after the .NET 10 upgrade,
// since those are IHealthChecksBuilder extensions, not tracing/metrics ones.
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(
        serviceName: ServiceName,
        serviceVersion: typeof(Program).Assembly.GetName().Version?.ToString() ?? "0.1.0"))
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation(options => options.RecordException = true)
            .AddHttpClientInstrumentation()
            // Npgsql emits its own ActivitySource named "Npgsql" (see
            // https://www.npgsql.org/doc/diagnostics/tracing.html). Subscribing
            // via AddSource is enough to capture it and avoids depending on the
            // Npgsql.OpenTelemetry package's .AddNpgsql() extension, whose name
            // collides at this call site with
            // NpgsqlServiceCollectionExtensions.AddNpgsql<TContext>(IServiceCollection, ...)
            // from Npgsql.EntityFrameworkCore.PostgreSQL (brought into scope via
            // the SDK's implicit "Microsoft.Extensions.DependencyInjection" using) —
            // that collision is what produced CS7036 here.
            .AddSource("Npgsql");

        // Parameterless overload resolves IConnectionMultiplexer from DI at
        // startup (OpenTelemetry.Instrumentation.StackExchangeRedis). If your
        // installed package version only exposes the instance-based
        // AddRedisInstrumentation(IConnectionMultiplexer) overload, resolve it
        // via a temporary provider instead:
        //   using var sp = builder.Services.BuildServiceProvider();
        //   tracing.AddRedisInstrumentation(sp.GetRequiredService<IConnectionMultiplexer>());
        tracing.AddRedisInstrumentation();

        var otlpEndpoint = builder.Configuration["OpenTelemetry:OtlpEndpoint"];
        if (!string.IsNullOrWhiteSpace(otlpEndpoint))
            tracing.AddOtlpExporter(otlp => otlp.Endpoint = new Uri(otlpEndpoint));
    })
    .WithMetrics(metrics => metrics
        .AddMeter(BookingMetrics.MeterName)
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddPrometheusExporter());

// --- AuthN/AuthZ -------------------------------------------------------------
// Booking Service trusts JWTs already validated by the API Gateway (Ocelot),
// but validates them again here defense-in-depth — a service should never
// assume its only caller is the gateway.
var jwtSection = builder.Configuration.GetSection("Jwt");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidAudience = jwtSection["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["SigningKey"] ?? "dev-only-signing-key-change-me-32chars"))
        };
    });
builder.Services.AddAuthorization();

// Caller identity from the validated JWT (customer id, tenant, contact, roles).
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

// --- API documentation: native OpenAPI + Scalar only -------------------------
// Swashbuckle/Swagger is deliberately NOT used here: on .NET 10, Swashbuckle
// (still built against the OpenAPI.NET v1 object model) and the framework's
// own Microsoft.AspNetCore.OpenApi (now on OpenAPI.NET v2) disagree on the
// shape of OpenApiDocument/OpenApiSchema — trying to register both throws at
// startup, which is almost certainly the "builds fine, crashes on run" you
// hit. Scalar renders the native /openapi/v1.json document directly, so it
// doesn't need Swashbuckle at all. If you specifically need swagger.json for
// an external tool, generate it via `dotnet build` + the
// Microsoft.Extensions.ApiDescription.Server tooling instead of adding
// Swashbuckle back in.
builder.Services.AddOpenApi("v1");

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowConfiguredOrigins", policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
        policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod();
    });
});

// --- Health checks (surfaced at /health; each dependency also feeds Grafana via its own exporter) ---
var rabbitMqUser = builder.Configuration["RabbitMq:UserName"] ?? "guest";
var rabbitMqPass = builder.Configuration["RabbitMq:Password"] ?? "guest";
var rabbitMqHost = builder.Configuration["RabbitMq:HostName"] ?? "localhost";
var rabbitMqPort = builder.Configuration["RabbitMq:Port"] ?? "5672";

builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("BookingDb") ?? string.Empty, name: "postgres")
    .AddRabbitMQ(
        rabbitConnectionString: $"amqp://{rabbitMqUser}:{rabbitMqPass}@{rabbitMqHost}:{rabbitMqPort}",
        name: "rabbitmq")
    .AddRedis(builder.Configuration["Redis:ConnectionString"] ?? "localhost:6379", name: "redis");

var app = builder.Build();

// --- Middleware pipeline -----------------------------------------------
// Correlation first so the exception handler (and every log line / query
// log) can record the correlation id; exception handler next so it wraps
// auth, routing and the endpoints.
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseSerilogRequestLogging();

app.MapOpenApi();                          // -> /openapi/v1.json
app.MapScalarApiReference("/scalar", options =>
{
    options.WithTitle("Bus Ticketing — Booking Service");
    options.WithTheme(ScalarTheme.Purple);
});

app.UseCors("AllowConfiguredOrigins");
app.UseAuthentication();
app.UseAuthorization();

app.MapTripsEndpoints();
app.MapBookingsEndpoints();
app.MapHealthChecks("/health");
app.MapPrometheusScrapingEndpoint("/metrics");

// Applies pending EF Core migrations on startup in dev/demo environments.
// REQUIRES migrations to already exist (dotnet ef migrations add InitialCreate)
// — see docs/RUNBOOK.md step 1. If no migration files exist yet, this call
// silently does nothing and every request will fail with "relation does not
// exist", which is the other very likely cause of "builds fine, doesn't
// actually work end-to-end".
try
{
    if (app.Environment.IsDevelopment())
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
        await db.Database.MigrateAsync();
    }

    app.Run();
}
catch (Exception ex)
{
    // Anything that kills startup (unreachable Postgres, missing migration,
    // port in use) is written to logs/runtime-errors/ with a diagnosed root
    // cause + suggested fix before the process exits non-zero.
    var path = RuntimeErrorLogWriter.Write(ex, app.Environment.ContentRootPath, app.Environment.EnvironmentName);
    Log.Fatal(ex, "booking-service failed to start. Diagnostic written to {Path}", path);
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}

// Exposed for WebApplicationFactory-based integration tests.
public partial class Program { }
