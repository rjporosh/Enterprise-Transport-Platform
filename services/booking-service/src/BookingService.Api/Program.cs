using System.Text;
using BookingService.Api.Endpoints;
using BookingService.Api.Middleware;
using BookingService.Application;
using BookingService.Infrastructure;
using BookingService.Infrastructure.Observability;
using BookingService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Scalar.AspNetCore;
using Serilog;
using StackExchange.Redis;

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
// Traces export over OTLP to Jaeger (accepts OTLP natively since 1.35+, see
// docker-compose). Metrics are scraped by Prometheus from /metrics and
// visualized in Grafana (dashboards provisioned in infrastructure/monitoring).
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(
        serviceName: ServiceName,
        serviceVersion: typeof(Program).Assembly.GetName().Version?.ToString() ?? "0.1.0"))
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation(options => options.RecordException = true)
            .AddHttpClientInstrumentation()
            .AddNpgsql()
            // Parameterless overload resolves IConnectionMultiplexer from DI at
            // startup (OpenTelemetry.Instrumentation.StackExchangeRedis, DI-aware
            // registration). If your installed package version only exposes the
            // instance-based AddRedisInstrumentation(IConnectionMultiplexer)
            // overload, resolve it here instead:
            //   var muxer = builder.Services.BuildServiceProvider().GetRequiredService<IConnectionMultiplexer>();
            //   tracing.AddRedisInstrumentation(muxer);
            .AddRedisInstrumentation();

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

// --- API documentation: Swagger (classic) + native OpenAPI/Scalar (modern) --
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Bus Ticketing — Booking Service",
        Version = "v1",
        Description = "Owns trip search, seat holds, booking lifecycle and the booking outbox. " +
                       "Every endpoint below has a filled-in example — hit **Authorize**, paste a " +
                       "bearer token, then **Try it out**.",
        Contact = new OpenApiContact { Name = "Bus Ticketing Platform Team" }
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste just the raw JWT (no 'Bearer ' prefix — Swashbuckle adds it)."
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});

// Native Microsoft.AspNetCore.OpenApi document — this is what Scalar renders.
builder.Services.AddOpenApi("v1", options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Info.Title = "Bus Ticketing — Booking Service";
        document.Info.Version = "v1";
        document.Info.Description = "Trip search, seat holds, booking lifecycle. See /scalar for the interactive reference.";

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Paste a raw JWT access token."
        };
        return Task.CompletedTask;
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowConfiguredOrigins", policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
        policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod();
    });
});

// --- Health checks (surfaced at /health; each dependency also feeds Grafana via its own exporter) ---
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("BookingDb") ?? string.Empty, name: "postgres")
    .AddRabbitMQ(name: "rabbitmq")
    .AddRedis(builder.Configuration["Redis:ConnectionString"] ?? "localhost:6379", name: "redis");

var app = builder.Build();

// --- Middleware pipeline -----------------------------------------------
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseSerilogRequestLogging();

app.MapOpenApi(); // -> /openapi/v1.json, consumed by Scalar below
app.MapScalarApiReference("/scalar", options =>
{
    options
        .WithTitle("Bus Ticketing — Booking Service")
        .WithTheme(ScalarTheme.Purple)
        .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
        .WithPreferredScheme("Bearer");
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Booking Service v1");
        options.DocumentTitle = "Bus Ticketing — Booking Service";
    });
}

app.UseCors("AllowConfiguredOrigins");
app.UseAuthentication();
app.UseAuthorization();

app.MapTripsEndpoints();
app.MapBookingsEndpoints();
app.MapHealthChecks("/health");
app.MapPrometheusScrapingEndpoint("/metrics");

// Applies pending EF Core migrations on startup in dev/demo environments.
// In production this is a deliberate no-op — migrations are applied via the
// CI/CD pipeline's dedicated migration step, never by an app instance racing
// other replicas on boot.
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
    await db.Database.MigrateAsync();
}

app.Run();

// Exposed for WebApplicationFactory-based integration tests.
public partial class Program { }
