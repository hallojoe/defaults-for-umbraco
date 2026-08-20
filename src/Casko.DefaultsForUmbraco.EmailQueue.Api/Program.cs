using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using Casko.DefaultsForUmbraco.EmailQueue.Api.Infrastructure.BlobStorage;
using Casko.DefaultsForUmbraco.EmailQueue.Api.Infrastructure.ServiceBus;
using Casko.DefaultsForUmbraco.EmailQueue.Api.Models;

var builder = WebApplication.CreateBuilder(args);

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
