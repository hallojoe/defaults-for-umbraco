using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using Casko.DefaultsForUmbraco.EmailQueue.Worker.Infrastructure.BlobStorage;
using Casko.DefaultsForUmbraco.EmailQueue.Worker.Infrastructure.Smtp;
using Casko.DefaultsForUmbraco.EmailQueue.Worker.Models;
using Casko.DefaultsForUmbraco.EmailQueue.Worker.Services;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

var builder = Host.CreateApplicationBuilder(args);

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
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation())
    .WithTracing(tracing => tracing
        .AddHttpClientInstrumentation()
        .AddSource("Azure.Core.Http", "Azure.Messaging.ServiceBus"));

if (hasOtlpExporter)
{
    telemetry
        .WithMetrics(metrics => metrics.AddOtlpExporter())
        .WithTracing(tracing => tracing.AddOtlpExporter());
}

builder.Services.AddOptions<EmailQueueOptions>()
    .BindConfiguration(EmailQueueOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddOptions<EmailWorkerOptions>()
    .BindConfiguration(EmailWorkerOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddOptions<SmtpOptions>()
    .BindConfiguration(SmtpOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddSingleton(_ => new BlobServiceClient(
    builder.Configuration.GetConnectionString("blobs")
    ?? throw new InvalidOperationException("Connection string 'blobs' is required.")));
builder.Services.AddSingleton(_ => new ServiceBusClient(
    builder.Configuration.GetConnectionString("servicebus")
    ?? throw new InvalidOperationException("Connection string 'servicebus' is required.")));
builder.Services.AddSingleton<IEmailPayloadReader, BlobEmailPayloadReader>();
builder.Services.AddSingleton<IDeliveryReceiptStore, BlobDeliveryReceiptStore>();
builder.Services.AddSingleton<IEmailSender, MailKitEmailSender>();
builder.Services.AddHostedService<EmailDeliveryWorker>();

await builder.Build().RunAsync();
