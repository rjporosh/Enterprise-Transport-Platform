using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Platform.Gateway.Tests;

/// <summary>
/// Boots the real gateway (in-memory TestServer) plus a real downstream stub
/// that echoes the request it received, so tests can assert exactly what the
/// gateway forwards.
/// </summary>
public sealed class GatewayFixture : IAsyncLifetime
{
    private WebApplication _downstream = null!;
    private WebApplicationFactory<Program> _factory = null!;

    public HttpClient Client { get; private set; } = null!;
    public string DownstreamUrl { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        // ---- downstream stub: echoes method, path and received headers ----
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();
        _downstream = builder.Build();
        _downstream.MapMethods("/{**catch-all}", ["GET", "POST", "PUT", "DELETE"], (HttpContext ctx) => Results.Ok(new EchoResponse
        {
            Method = ctx.Request.Method,
            Path = ctx.Request.Path.Value ?? string.Empty,
            Headers = ctx.Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString(), StringComparer.OrdinalIgnoreCase)
        }));
        await _downstream.StartAsync();
        DownstreamUrl = _downstream.Urls.First();

        // ---- the gateway under test ----
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Development");
            b.UseSetting("Jwt:SigningKey", "test-signing-key-that-is-long-enough-32chars");
            // Every route/cluster points at the one stub for these tests.
            foreach (var cluster in new[] { "auth", "booking", "bus", "route", "payment", "notification", "ticketing" })
                b.UseSetting($"ReverseProxy:Clusters:{cluster}:Destinations:primary:Address", DownstreamUrl);
            // Take rate limiting out of the way of functional assertions.
            b.UseSetting("Gateway:RateLimiting:GlobalPermitLimit", "100000");
            b.UseSetting("Gateway:RateLimiting:AuthPermitLimit", "100000");
            b.UseSetting("Gateway:RateLimiting:PaymentPermitLimit", "100000");
        });

        Client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    public async Task DisposeAsync()
    {
        Client?.Dispose();
        if (_factory is not null) await _factory.DisposeAsync();
        if (_downstream is not null) await _downstream.DisposeAsync();
    }

    public async Task<EchoResponse> GetEchoAsync(HttpRequestMessage request)
    {
        var response = await Client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<EchoResponse>())!;
    }

    public sealed class EchoResponse
    {
        public string Method { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public Dictionary<string, string> Headers { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }
}

[CollectionDefinition(nameof(GatewayCollection))]
public sealed class GatewayCollection : ICollectionFixture<GatewayFixture>;
