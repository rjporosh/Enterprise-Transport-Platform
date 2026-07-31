namespace BookingService.Application.Common.Interfaces;

/// <summary>
/// Domain-level metrics, recorded via System.Text.Diagnostics.Metrics in the
/// implementation and scraped by Prometheus / visualized in Grafana. Kept as
/// an abstraction here so Application has zero dependency on
/// System.Diagnostics.Metrics or OpenTelemetry directly.
/// </summary>
public interface IBookingMetrics
{
    void RecordBookingCreated(decimal amount, string currency);
    void RecordBookingCancelled();
    void RecordSeatConflict();
}
