using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using Casko.DefaultsForUmbraco.EmailQueue.Api.Infrastructure.BlobStorage;
using Casko.DefaultsForUmbraco.EmailQueue.Api.Infrastructure.ServiceBus;
using Casko.DefaultsForUmbraco.EmailQueue.Api.Models;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

var hasOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);
if (hasOtlpExporter)
{
    builder.Logging.AddOpenTelemetry(logging =>
    {
        logging.IncludeFormattedMessage = true;
        logging.IncludeScopes = true;
        logging.ParseStateValues = true;
        logging.AddOtlpExporter();
    });
}

var telemetry = builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation())
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddSource("Azure.Core.Http", "Azure.Messaging.ServiceBus"));

if (hasOtlpExporter)
{
    telemetry
        .WithMetrics(metrics => metrics.AddOtlpExporter())
        .WithTracing(tracing => tracing.AddOtlpExporter());
}

builder.Services.AddControllers();
builder.Services.AddOptions<EmailQueueOptions>()
    .BindConfiguration(EmailQueueOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddSingleton(_ => new BlobServiceClient(
    builder.Configuration.GetConnectionString("blobs")
    ?? throw new InvalidOperationException("Connection string 'blobs' is required.")));
builder.Services.AddSingleton(_ => new ServiceBusClient(
    builder.Configuration.GetConnectionString("servicebus")
    ?? throw new InvalidOperationException("Connection string 'servicebus' is required.")));
builder.Services.AddSingleton<IEmailPayloadStore, BlobEmailPayloadStore>();
builder.Services.AddSingleton<IEmailRequestedPublisher, ServiceBusEmailRequestedPublisher>();

var app = builder.Build();
app.MapControllers();
app.Run();

public partial class Program;
