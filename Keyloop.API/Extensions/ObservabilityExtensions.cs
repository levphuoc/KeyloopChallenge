using Keyloop.Application.Diagnostics;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Keyloop.API.Extensions;

public static class ObservabilityExtensions
{
    public static IServiceCollection AddApiObservability(
        this IServiceCollection services, IConfiguration configuration)
    {
        var consoleExporter = configuration.GetValue("Observability:ConsoleExporter", false);

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService("Keyloop.API"))
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation();

                if (consoleExporter)
                    tracing.AddConsoleExporter();
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddMeter(SchedulerDiagnostics.MeterName);

                if (consoleExporter)
                    metrics.AddConsoleExporter();
            });

        return services;
    }
}
