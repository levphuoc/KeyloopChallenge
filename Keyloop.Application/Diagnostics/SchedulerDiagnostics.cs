using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Keyloop.Application.Diagnostics;

/// <summary>
/// Shared telemetry names for the scheduling flow. Keeping them in the
/// application layer lets the infrastructure and API compose the same signals.
/// </summary>
public static class SchedulerDiagnostics
{
    public const string ActivitySourceName = "Keyloop.Scheduler";
    public const string MeterName = "Keyloop.Scheduler";

    public static readonly ActivitySource ActivitySource = new(ActivitySourceName, "1.0.0");
    public static readonly Meter Meter = new(MeterName, "1.0.0");

    public static readonly Counter<long> BookingsCreated =
        Meter.CreateCounter<long>("scheduler.bookings.created", description: "Confirmed appointments.");

    public static readonly Counter<long> BookingsRejected =
        Meter.CreateCounter<long>("scheduler.bookings.rejected", description: "Rejected booking requests by reason.");

    public static readonly Histogram<double> BookingDuration =
        Meter.CreateHistogram<double>("scheduler.booking.duration", unit: "ms", description: "Booking request duration.");
}
