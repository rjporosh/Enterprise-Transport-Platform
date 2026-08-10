using System.Text;
using Microsoft.AspNetCore.RateLimiting;
using RouteService.Api.Diagnostics;
using RouteService.Api.Endpoints;
using RouteService.Api.Middleware;
using RouteService.Api.Security;
using RouteService.Application;
using RouteService.Application.Common.Interfaces;
using RouteService.Infrastructure;
using RouteService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Scalar.AspNetCore;
using Serilog;

var contentRootForCrashHandler = Directory.GetCurrentDirectory();
try
{
    var builder = WebApplication.CreateBuilder(args);
    contentRootForCrashHandler = builder.Environment.ContentRootPath;

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Service", "route-service")
        .Enrich.WithEnvironmentName()
        .WriteTo.Console(outputTemplate:
            "[{Timestamp:HH:mm:ss} {Level:u3}] ({CorrelationId}) {Message:lj} {Properties:j}{NewLine}{Exception}"));

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

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
        options.AddFixedWindowLimiter("write", configure =>
        {
            configure.Window = TimeSpan.FromSeconds(10);
            configure.PermitLimit = 20;
        });
    });

    builder.Services.AddOpenApi("v1", options =>
    {
        options.AddDocumentTransformer((document, context, cancellationToken) =>
        {
            document.Info.Title = "Route Service API";
            document.Info.Version = "v1";
            document.Info.Description = "Route and schedule management for the Enterprise Transport Platform.";

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

    var healthChecks = builder.Services.AddHealthChecks();
    var dbProvider = (builder.Configuration["Database:Provider"] ?? "Postgres").Trim().ToLowerInvariant();
    var routeDbConnectionString = builder.Configuration.GetConnectionString("RouteDb");
    if (!string.IsNullOrWhiteSpace(routeDbConnectionString) && dbProvider is "postgres" or "postgresql" or "npgsql")
        healthChecks.AddNpgSql(routeDbConnectionString, name: "postgres");

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

    builder.Services.AddGrpc(options =>
    {
        options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    });

    builder.Services.AddOpenTelemetry()
        .ConfigureResource(resource => resource.AddService(serviceName: "route-service", serviceVersion: "1.0.0"))
        .WithTracing(tracing => tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddEntityFrameworkCoreInstrumentation()
            .AddOtlpExporter(otlp => otlp.Endpoint = new Uri(builder.Configuration["OpenTelemetry:OtlpEndpoint"] ?? "http://localhost:4317")))
        .WithMetrics(metrics => metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation()
            .AddMeter(RouteService.Infrastructure.Observability.RouteMetrics.MeterName)
            .AddPrometheusExporter());

    var app = builder.Build();

    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseSerilogRequestLogging();
    app.UseMiddleware<ExceptionHandlingMiddleware>();
    app.UseMiddleware<RequestContextMiddleware>();
    app.UseMiddleware<RouteService.Api.Middleware.LocalizationMiddleware>();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference("/scalar", options =>
        {
            options.Title = "Route Service API";
            options.Theme = ScalarTheme.Purple;
        });
    }

    app.UseAuthentication();
    app.UseAuthorization();

    app.UseRateLimiter();

    app.MapRouteEndpoints();
    app.MapReleaseEndpoints();
    app.MapHealthChecks("/health");
    app.MapPrometheusScrapingEndpoint("/metrics");
    app.MapGrpcService<RouteService.Api.Grpc.RouteGrpcServiceImpl>();

    if (app.Environment.IsDevelopment())
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RouteDbContext>();
        await db.Database.MigrateAsync();
    }

    app.Run();
}
catch (Exception ex)
{
    var logPath = RuntimeErrorLogWriter.Write(ex, contentRootForCrashHandler);
    Console.Error.WriteLine();
    Console.Error.WriteLine($"FATAL: Route Service failed to start or crashed. Details saved to: {logPath}");
    Console.Error.WriteLine();
    throw;
}

public partial class Program { }
