using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using QuestPDF.Infrastructure;
using Scalar.AspNetCore;
using Serilog;
using TicketingService.Api.Endpoints;
using TicketingService.Api.Middleware;
using TicketingService.Api.Security;
using TicketingService.Application;
using TicketingService.Application.Common.Interfaces;
using TicketingService.Infrastructure;
using TicketingService.Infrastructure.Persistence;

QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);
const string ServiceName = "ticketing-service";

builder.Host.UseSerilog((ctx, _, cfg) =>
{
    cfg.ReadFrom.Configuration(ctx.Configuration).Enrich.FromLogContext().Enrich.WithProperty("Service", ServiceName).WriteTo.Console();
    var seq = ctx.Configuration["Seq:ServerUrl"];
    if (!string.IsNullOrWhiteSpace(seq)) cfg.WriteTo.Seq(seq);
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

var jwt = builder.Configuration.GetSection("Jwt");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true, ValidateAudience = true, ValidateLifetime = true, ValidateIssuerSigningKey = true,
        ValidIssuer = jwt["Issuer"], ValidAudience = jwt["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["SigningKey"] ?? "dev-only-signing-key-change-me-32chars-minimum"))
    });
builder.Services.AddAuthorization();

builder.Services.AddOpenApi("v1");
builder.Services.AddCors(o => o.AddPolicy("Default", p =>
    p.WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>())
     .AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService(ServiceName))
    .WithTracing(t =>
    {
        t.AddAspNetCoreInstrumentation(o => o.RecordException = true).AddHttpClientInstrumentation().AddSource("Npgsql");
        var otlp = builder.Configuration["OpenTelemetry:OtlpEndpoint"];
        if (!string.IsNullOrWhiteSpace(otlp)) t.AddOtlpExporter(e => e.Endpoint = new Uri(otlp));
    })
    .WithMetrics(m => m.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation().AddRuntimeInstrumentation().AddPrometheusExporter());

var rmqUser = builder.Configuration["RabbitMq:UserName"] ?? "guest";
var rmqPass = builder.Configuration["RabbitMq:Password"] ?? "guest";
var rmqHost = builder.Configuration["RabbitMq:HostName"] ?? "localhost";
var rmqPort = builder.Configuration["RabbitMq:Port"] ?? "5672";
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("TicketingDb") ?? string.Empty, name: "postgres")
    .AddRabbitMQ(rabbitConnectionString: $"amqp://{rmqUser}:{rmqPass}@{rmqHost}:{rmqPort}", name: "rabbitmq");

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseSerilogRequestLogging();

app.MapOpenApi();
app.MapScalarApiReference("/scalar", o => o.WithTitle("Bus Ticketing — Ticketing Service").WithTheme(ScalarTheme.Purple));

app.UseCors("Default");
app.UseAuthentication();
app.UseAuthorization();

app.MapTicketsEndpoints();
app.MapTemplatesEndpoints();
app.MapHealthChecks("/health");
app.MapPrometheusScrapingEndpoint("/metrics");
app.MapGet("/api/v1/release", () => new { service = ServiceName, version = "1.0.0" }).AllowAnonymous();

try
{
    if (app.Environment.IsDevelopment())
    {
        using var scope = app.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<TicketingDbContext>().Database.MigrateAsync();
    }
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "ticketing-service failed to start");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}

public partial class Program { }
