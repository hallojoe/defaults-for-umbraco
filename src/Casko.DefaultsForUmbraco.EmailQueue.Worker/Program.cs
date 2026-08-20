using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using Casko.DefaultsForUmbraco.EmailQueue.Worker.Infrastructure.BlobStorage;
using Casko.DefaultsForUmbraco.EmailQueue.Worker.Infrastructure.ServiceBus;
using Casko.DefaultsForUmbraco.EmailQueue.Worker.Infrastructure.Smtp;
using Casko.DefaultsForUmbraco.EmailQueue.Worker.Models;
using Casko.DefaultsForUmbraco.EmailQueue.Worker.Services;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

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
