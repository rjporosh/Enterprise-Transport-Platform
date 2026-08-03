namespace AuthService.Application.Common.Interfaces;

/// <summary>
/// Domain-level metrics recorded via System.Diagnostics.Metrics in
/// Infrastructure and scraped by Prometheus / visualized in Grafana.
/// </summary>
public interface IAuthMetrics
{
    void RecordRegistration();
    void RecordLoginSuccess();
    void RecordLoginFailure();
    void RecordAccountLockout();
    void RecordTokenRefresh();
}
