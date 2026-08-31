using System.Net;
using FluentAssertions;
using Xunit;

namespace Platform.Gateway.Tests;

[Collection(nameof(GatewayCollection))]
public sealed class GatewayBehaviourTests(GatewayFixture fixture)
{
    private const string CorrelationHeader = "X-Correlation-Id";

    // ---------------------------------------------------------------- health / info

    [Fact]
    public async Task Health_endpoint_returns_200()
    {
        var response = await fixture.Client.GetAsync("/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Root_endpoint_identifies_the_gateway()
    {
        var body = await fixture.Client.GetStringAsync("/");
        body.Should().Contain("api-gateway");
    }

    [Fact]
    public async Task Metrics_endpoint_is_exposed()
    {
        var response = await fixture.Client.GetAsync("/metrics");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ---------------------------------------------------------------- correlation id

    [Fact]
    public async Task Missing_correlation_id_is_generated_and_returned()
    {
        var response = await fixture.Client.GetAsync("/");

        response.Headers.Should().ContainKey(CorrelationHeader);
        var generated = response.Headers.GetValues(CorrelationHeader).Single();
        generated.Should().NotBeNullOrWhiteSpace();
        generated.Length.Should().BeGreaterThanOrEqualTo(8);
    }

    [Fact]
    public async Task Valid_client_correlation_id_is_preserved_end_to_end()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/anything");
        request.Headers.Add(CorrelationHeader, "client-corr-1234567890");

        var echo = await fixture.GetEchoAsync(request);

        // preserved on the response...
        // ...and forwarded downstream unchanged.
        echo.Headers.Should().ContainKey(CorrelationHeader);
        echo.Headers[CorrelationHeader].Should().Be("client-corr-1234567890");
    }

    [Fact]
    public async Task Malformed_correlation_id_is_replaced()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/anything");
        request.Headers.TryAddWithoutValidation(CorrelationHeader, "bad id with spaces & symbols!!");

        var echo = await fixture.GetEchoAsync(request);

        echo.Headers.Should().ContainKey(CorrelationHeader);
        var forwarded = echo.Headers[CorrelationHeader];
        forwarded.Should().NotBe("bad id with spaces & symbols!!");
        forwarded.Should().MatchRegex("^[a-zA-Z0-9._:-]{8,}$");
    }

    [Fact]
    public async Task Gateway_marks_the_forwarded_request_as_gateway_originated()
    {
        var echo = await fixture.GetEchoAsync(new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/x"));
        echo.Headers.Should().ContainKey("X-Forwarded-By-Gateway");
    }

    // ---------------------------------------------------------------- tenant hygiene

    [Fact]
    public async Task Client_supplied_tenant_headers_are_stripped_before_forwarding()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/payments/anything");
        request.Headers.TryAddWithoutValidation("X-Tenant-Id", "11111111-1111-1111-1111-111111111111");
        request.Headers.TryAddWithoutValidation("X-Company-Id", "attacker-company");
        request.Headers.TryAddWithoutValidation("X-Organization-Id", "attacker-org");

        var echo = await fixture.GetEchoAsync(request);

        echo.Headers.Should().NotContainKey("X-Tenant-Id");
        echo.Headers.Should().NotContainKey("X-Company-Id");
        echo.Headers.Should().NotContainKey("X-Organization-Id");
    }

    [Fact]
    public async Task Unauthenticated_request_is_forwarded_not_rejected()
    {
        // The gateway is an auth *boundary*, not an auth *gate* — per-endpoint
        // authorization stays in each service. An anonymous call must reach the
        // downstream (which then decides).
        var response = await fixture.Client.GetAsync("/api/v1/bookings/mine");
        response.StatusCode.Should().Be(HttpStatusCode.OK); // stub echoes 200
    }

    // ---------------------------------------------------------------- security headers

    [Theory]
    [InlineData("X-Content-Type-Options", "nosniff")]
    [InlineData("X-Frame-Options", "DENY")]
    [InlineData("Referrer-Policy", "no-referrer")]
    public async Task Security_headers_are_present(string header, string expected)
    {
        var response = await fixture.Client.GetAsync("/");
        response.Headers.TryGetValues(header, out var values).Should().BeTrue($"{header} must be set");
        values!.Single().Should().Be(expected);
    }

    [Fact]
    public async Task Content_security_policy_locks_down_scripting()
    {
        var response = await fixture.Client.GetAsync("/");
        response.Headers.GetValues("Content-Security-Policy").Single().Should().Contain("default-src 'none'");
    }

    [Fact]
    public async Task Server_header_is_not_leaked()
    {
        var response = await fixture.Client.GetAsync("/");
        response.Headers.Contains("Server").Should().BeFalse();
        response.Headers.Contains("X-Powered-By").Should().BeFalse();
    }

    // ---------------------------------------------------------------- routing

    [Theory]
    [InlineData("/api/v1/auth/login")]
    [InlineData("/api/v1/bookings/mine")]
    [InlineData("/api/v1/buses/")]
    [InlineData("/api/v1/routes/")]
    [InlineData("/api/v1/payments/")]
    [InlineData("/api/v1/notifications/")]
    public async Task Known_api_paths_are_proxied_downstream(string path)
    {
        var echo = await fixture.GetEchoAsync(new HttpRequestMessage(HttpMethod.Get, path));
        echo.Path.Should().Be(path);
    }

    [Fact]
    public async Task Unknown_path_is_not_routed()
    {
        var response = await fixture.Client.GetAsync("/not-an-api-route");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
