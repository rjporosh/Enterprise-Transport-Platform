using System.Diagnostics.Metrics;
using BookingService.Application.Common.Interfaces;

namespace BookingService.Infrastructure.Observability;

/// <summary>
/// Custom business metrics exposed on the same Meter name that Program.cs
/// registers with OpenTelemetry ("BookingService"), so these show up
/// alongside the built-in ASP.NET Core/runtime metrics in Prometheus/Grafana.
/// </summary>
public sealed class BookingMetrics : IBookingMetrics
{
    public const string MeterName = "BookingService";

    private readonly Counter<long> _bookingsCreated;
    private readonly Counter<long> _bookingsCancelled;
    private readonly Counter<long> _seatConflicts;
    private readonly Histogram<double> _bookingValue;

    public BookingMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);

        _bookingsCreated = meter.CreateCounter<long>(
            "bookings_created_total", unit: "{booking}", description: "Number of bookings successfully created.");
        _bookingsCancelled = meter.CreateCounter<long>(
            "bookings_cancelled_total", unit: "{booking}", description: "Number of bookings cancelled.");
        _seatConflicts = meter.CreateCounter<long>(
            "booking_seat_conflicts_total", unit: "{conflict}", description: "Number of 409s caused by concurrent seat holds — the metric that proves the concurrency control is doing its job under real load.");
        _bookingValue = meter.CreateHistogram<double>(
            "booking_value", unit: "BDT", description: "Distribution of booking total amounts.");
    }

    public void RecordBookingCreated(decimal amount, string currency)
    {
        _bookingsCreated.Add(1, new KeyValuePair<string, object?>("currency", currency));
        _bookingValue.Record((double)amount, new KeyValuePair<string, object?>("currency", currency));
    }

    public void RecordBookingCancelled() => _bookingsCancelled.Add(1);

    public void RecordSeatConflict() => _seatConflicts.Add(1);
}
