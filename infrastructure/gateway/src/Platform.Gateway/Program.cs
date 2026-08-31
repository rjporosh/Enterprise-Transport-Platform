using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Platform.Common.Correlation;
using Platform.Common.DependencyInjection;
using Platform.Gateway.RateLimiting;
using Serilog;
using Yarp.ReverseProxy.Transforms;

var builder = WebApplication.CreateBuilder(args);

// --------------------------------------------------------------------------
// Serilog
// --------------------------------------------------------------------------
builder.Host.UseSerilog((context, _, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Service", "api-gateway")
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] ({CorrelationId}) {Message:lj} {Properties:j}{NewLine}{Exception}"));

// --------------------------------------------------------------------------
// Request size + timeouts (Kestrel). Per-route body caps can be tightened later.
// --------------------------------------------------------------------------
var maxBodyBytes = builder.Configuration.GetValue<long?>("Gateway:MaxRequestBodyBytes") ?? 10 * 1024 * 1024;
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = maxBodyBytes;
    options.Limits.KeepAliveTimeout = TimeSpan.FromSeconds(
        builder.Configuration.GetValue<int?>("Gateway:KeepAliveTimeoutSeconds") ?? 120);
    options.AddServerHeader = false;
});

// --------------------------------------------------------------------------
// Forwarded headers — the gateway is the trust boundary. By default it trusts
// only the socket peer (no LB in local dev). In an environment with an ingress
// / L7 load balancer, set Gateway:ForwardedHeaders:Enabled=true and list the
// ingress IPs in KnownProxies so X-Forwarded-For is honoured from THEM only.
// --------------------------------------------------------------------------
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    var fhSection = builder.Configuration.GetSection("Gateway:ForwardedHeaders");
    if (!fhSection.GetValue("Enabled", false))
    {
        options.ForwardedHeaders = ForwardedHeaders.None;
        return;
    }

    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
    foreach (var proxy in fhSection.GetSection("KnownProxies").Get<string[]>() ?? [])
    {
        if (System.Net.IPAddress.TryParse(proxy, out var ip))
            options.KnownProxies.Add(ip);
    }
    options.ForwardLimit = fhSection.GetValue<int?>("ForwardLimit") ?? 1;
});

// --------------------------------------------------------------------------
// Authentication boundary. The gateway VALIDATES the token so it can (a) resolve
// the tenant claim for TenantHeaderHygiene and (b) partition rate limits per
// user/tenant. It does NOT reject anonymous requests — per-endpoint
// authorization stays in each service. The bearer token is forwarded untouched.
// --------------------------------------------------------------------------
var jwt = builder.Configuration.GetSection("Jwt");
var signingKey = jwt["SigningKey"];

// The gateway reads JWT claims for tenant-context propagation and rate-limit
// partitioning. In Production it MUST validate the signature, so a signing key
// is mandatory — fail fast rather than silently trusting unvalidated claims
// (P0-12). Development/Testing run with the shared dev key from
// appsettings.Development.json.
if (builder.Environment.IsProduction() && string.IsNullOrWhiteSpace(signingKey))
{
    throw new InvalidOperationException(
        "Jwt:SigningKey is not configured. The API gateway requires it in Production so it can " +
        "validate tokens before using their claims. Set the environment variable Jwt__SigningKey " +
        "(and Jwt__Issuer / Jwt__Audience) — see docs/programmers-guide/gateway.md.");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = builder.Configuration.GetValue("Jwt:RequireHttpsMetadata", false);
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = !string.IsNullOrWhiteSpace(jwt["Issuer"]),
            ValidIssuer = jwt["Issuer"],
            ValidateAudience = !string.IsNullOrWhiteSpace(jwt["Audience"]),
            ValidAudience = jwt["Audience"],
            ValidateIssuerSigningKey = !string.IsNullOrWhiteSpace(signingKey),
            IssuerSigningKey = string.IsNullOrWhiteSpace(signingKey)
                ? null
                : new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
        // A bad/expired token must not 401 at the gateway — downstream decides.
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = ctx => { ctx.NoResult(); return Task.CompletedTask; },
            OnChallenge = ctx => { ctx.HandleResponse(); return Task.CompletedTask; }
        };
    });
builder.Services.AddAuthorization();

// --------------------------------------------------------------------------
// Rate limiting (edge backstop — see GatewayRateLimiterPolicies).
// --------------------------------------------------------------------------
builder.Services.AddGatewayRateLimiting(builder.Configuration);

// --------------------------------------------------------------------------
// YARP reverse proxy — routes & clusters from configuration only. Internal
// service addresses come from ReverseProxy:Clusters:*:Destinations (overridable
// by environment variables, e.g. ReverseProxy__Clusters__auth__Destinations__primary__Address).
// The gateway adds NO business logic — only transforms for context propagation.
// --------------------------------------------------------------------------
builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddTransforms(transformBuilderContext =>
    {
        // Guarantee the correlation id reaches the destination on the standard
        // header, whatever the client sent (CorrelationIdMiddleware has already
        // normalised HttpContext.Request.Headers by the time YARP forwards).
        transformBuilderContext.AddRequestTransform(async transform =>
        {
            var correlationId = transform.HttpContext.Items[CorrelationIdMiddleware.HttpContextItemKey] as string;
            if (!string.IsNullOrEmpty(correlationId))
            {
                transform.ProxyRequest.Headers.Remove(Platform.SharedKernel.Correlation.PlatformHeaders.CorrelationId);
                transform.ProxyRequest.Headers.TryAddWithoutValidation(
                    Platform.SharedKernel.Correlation.PlatformHeaders.CorrelationId, correlationId);
            }

            transform.ProxyRequest.Headers.Remove(Platform.SharedKernel.Correlation.PlatformHeaders.ForwardedByGateway);
            transform.ProxyRequest.Headers.TryAddWithoutValidation(
                Platform.SharedKernel.Correlation.PlatformHeaders.ForwardedByGateway, "1");

            await ValueTask.CompletedTask;
        });
    });

// --------------------------------------------------------------------------
// Health checks + OpenTelemetry
// --------------------------------------------------------------------------
builder.Services.AddHealthChecks();

builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService(serviceName: "api-gateway", serviceVersion: "0.1.0"))
    .WithTracing(t => t
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter(o => o.Endpoint = new Uri(
            builder.Configuration["OpenTelemetry:OtlpEndpoint"] ?? "http://localhost:4317")))
    .WithMetrics(m => m
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddPrometheusExporter());

var app = builder.Build();

// --------------------------------------------------------------------------
// Middleware pipeline — ORDER MATTERS.
// --------------------------------------------------------------------------
app.UseForwardedHeaders();

// 1. Correlation id + security headers (correlation FIRST so exception logs carry it).
app.UsePlatformEdge();

app.UseSerilogRequestLogging();

// 2. Authentication so the tenant claim + user id are available to steps 3/4.
app.UseAuthentication();
app.UseAuthorization();

// 3. Strip client-supplied tenant headers; re-inject from the validated claim.
app.UseTenantHeaderHygiene();

// 4. Edge rate-limit backstop.
app.UseRateLimiter();

// --------------------------------------------------------------------------
// Endpoints
// --------------------------------------------------------------------------
app.MapGet("/", () => Results.Ok(new
{
    service = "api-gateway",
    status = "ok",
    docs = "docs/programmers-guide/gateway.md"
}));

app.MapHealthChecks("/health");
app.MapPrometheusScrapingEndpoint("/metrics");

app.MapReverseProxy();

app.Run();

/// <summary>Exposed for WebApplicationFactory&lt;Program&gt; in Platform.Gateway.Tests.</summary>
public partial class Program;
