using System.Text;
using BookingService.Api.Endpoints;
using BookingService.Api.Middleware;
using BookingService.Application;
using BookingService.Infrastructure;
using BookingService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// --- Logging -----------------------------------------------------------
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Service", "booking-service")
    .WriteTo.Console());

// --- Application services ------------------------------------------------
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// --- AuthN/AuthZ ---------------------------------------------------------
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

// --- API surface -----------------------------------------------------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Bus Ticketing — Booking Service",
        Version = "v1",
        Description = "Owns trip search, seat holds, booking lifecycle and the booking outbox."
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

builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("BookingDb") ?? string.Empty, name: "postgres")
    .AddRabbitMQ(name: "rabbitmq");

var app = builder.Build();

// --- Middleware pipeline -----------------------------------------------
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowConfiguredOrigins");
app.UseAuthentication();
app.UseAuthorization();

app.MapTripsEndpoints();
app.MapBookingsEndpoints();
app.MapHealthChecks("/health");

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
