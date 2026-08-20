using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Casko.DefaultsForUmbraco.Web.OpenTelemetry;

/// <summary>
/// Configures telemetry exported to an OTLP endpoint, such as the .NET Aspire dashboard.
/// </summary>
public static class OpenTelemetryExtensions
{
    /// <summary>
    /// Adds tracing, metrics, and Serilog structured-log export when an OTLP endpoint is configured.
    /// </summary>
    public static WebApplicationBuilder AddOpenTelemetry(this WebApplicationBuilder builder)
    {
        var otlpEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
        var hasOtlpExporter = !string.IsNullOrWhiteSpace(otlpEndpoint);

        if (hasOtlpExporter)
        {
            AddSerilogOtlpSink(builder.Configuration, otlpEndpoint);
        }

        var openTelemetryBuilder = Microsoft.Extensions.DependencyInjection.OpenTelemetryServicesExtensions
            .AddOpenTelemetry(builder.Services)
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation())
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation());

        if (hasOtlpExporter)
        {
            openTelemetryBuilder
                .WithMetrics(metrics => metrics.AddOtlpExporter())
                .WithTracing(tracing => tracing.AddOtlpExporter());
        }

        return builder;
    }

    private static void AddSerilogOtlpSink(IConfigurationManager configuration, string? otlpEndpoint) =>
        configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Serilog:Using:0"] = "Serilog.Sinks.OpenTelemetry",
            ["Serilog:WriteTo:1:Name"] = "OpenTelemetry",
            ["Serilog:WriteTo:1:Args:Endpoint"] = otlpEndpoint
        });
}
