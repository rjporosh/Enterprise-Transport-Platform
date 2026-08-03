using System.Diagnostics.Metrics;
using AuthService.Application.Common.Interfaces;
using Microsoft.Extensions.Diagnostics.Metrics;

namespace AuthService.Infrastructure.Observability;

/// <summary>
/// Custom business metrics on the "AuthService" meter, registered with
/// OpenTelemetry in Program.cs so they show up in Prometheus/Grafana
/// alongside the built-in ASP.NET Core/runtime metrics.
/// </summary>
public sealed class AuthMetrics : IAuthMetrics
{
    public const string MeterName = "AuthService";

    private readonly Counter<long> _registrations;
    private readonly Counter<long> _loginSuccesses;
    private readonly Counter<long> _loginFailures;
    private readonly Counter<long> _lockouts;
    private readonly Counter<long> _tokenRefreshes;

    public AuthMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);

        _registrations = meter.CreateCounter<long>("auth_registrations_total", unit: "{user}", description: "Number of accounts successfully registered.");
        _loginSuccesses = meter.CreateCounter<long>("auth_login_success_total", unit: "{login}", description: "Number of successful sign-ins.");
        _loginFailures = meter.CreateCounter<long>("auth_login_failure_total", unit: "{login}", description: "Number of failed sign-in attempts (bad password or unknown email).");
        _lockouts = meter.CreateCounter<long>("auth_account_lockouts_total", unit: "{lockout}", description: "Number of accounts locked out due to repeated failed sign-ins.");
        _tokenRefreshes = meter.CreateCounter<long>("auth_token_refresh_total", unit: "{refresh}", description: "Number of successful refresh-token rotations.");
    }

    public void RecordRegistration() => _registrations.Add(1);
    public void RecordLoginSuccess() => _loginSuccesses.Add(1);
    public void RecordLoginFailure() => _loginFailures.Add(1);
    public void RecordAccountLockout() => _lockouts.Add(1);
    public void RecordTokenRefresh() => _tokenRefreshes.Add(1);
}
