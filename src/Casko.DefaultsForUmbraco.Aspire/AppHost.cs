var builder = DistributedApplication.CreateBuilder(args);

var openai = builder.AddOpenAI("openai");
var chat = openai.AddModel("chat", "gpt-4o-mini");
var embeddings = openai.AddModel("embeddings", "text-embedding-3-small");


var sqlOnly = IsEnabled("CASKO_APPHOST_SQL_ONLY");

var sql = builder
    .AddSqlServer("sql", port: 11433)
    .WithImage("azure-sql-edge")
    .WithImageRegistry("mcr.microsoft.com")
    .WithDataVolume("defaults-for-umbraco-sql-data")
    .WithHostPort(11433)
    .WithDbGate();

if (sqlOnly)
{
    builder.Build().Run();
    return;
}

var distributedCacheProvider = GetDistributedCacheProvider();

var cache = builder
    .AddAzureManagedRedis("cache")
    .RunAsContainer(redis =>
    {
        redis.WithRedisInsight();
    });

var databaseName = "defaults-for-umbraco-v4-db";
var umbracoDb = sql
    .AddDatabase("umbracoDbDSN", databaseName)
    .WithCreationScript(GetUmbracoDatabaseCreationScript(databaseName));

var serviceBus = builder
    .AddAzureServiceBus("servicebus")
    .RunAsEmulator();

builder
    .CreateResourceBuilder(builder.Resources.OfType<ContainerResource>().Single(resource => resource.Name == "servicebus-mssql"))
    .WithImage("azure-sql-edge")
    .WithImageRegistry("mcr.microsoft.com");

var outboundEmail = serviceBus
    .AddServiceBusQueue("outbound-email")
    .WithProperties(queue =>
    {
        queue.MaxDeliveryCount = 5;
        queue.LockDuration = TimeSpan.FromMinutes(5);
        // The local Service Bus emulator supports a maximum default TTL of one hour.
        queue.DefaultMessageTimeToLive = TimeSpan.FromHours(1);
        queue.DeadLetteringOnMessageExpiration = true;
    });

var storage = builder
    .AddAzureStorage("storage")
    .RunAsEmulator(emulator => emulator
        .WithDataVolume("defaults-for-umbraco-azurite-data"));

var blobs = storage.AddBlobs("blobs");
var queues = storage.AddQueues("queues");
var tables = storage.AddTables("tables");

var mailpit = builder
    .AddContainer("mailpit", "axllent/mailpit")
    .WithEndpoint(targetPort: 1025, name: "smtp")
    .WithHttpEndpoint(targetPort: 8025, name: "ui");

var emailApi = builder
    .AddProject<Projects.Casko_DefaultsForUmbraco_EmailQueue_Api>("email-queue-api")
    .WithHttpEndpoint(name: "http")
    .WithReference(blobs)
    .WithReference(serviceBus)
    .WaitFor(blobs)
    .WaitFor(outboundEmail);

builder
    .AddProject<Projects.Casko_DefaultsForUmbraco_EmailQueue_Worker>("email-queue-worker")
    .WithReference(blobs)
    .WithReference(serviceBus)
    .WithReference(mailpit.GetEndpoint("smtp"))
    .WithEnvironment("Smtp__From", "noreply@example.local")
    .WithEnvironment("Smtp__Host", mailpit.GetEndpoint("smtp").Property(EndpointProperty.Host))
    .WithEnvironment("Smtp__Port", mailpit.GetEndpoint("smtp").Property(EndpointProperty.Port))
    .WithEnvironment("Smtp__UseStartTls", "false")
    .WaitFor(blobs)
    .WaitFor(outboundEmail)
    .WaitFor(mailpit);

builder
    .AddAzureFunctionsProject<Projects.Casko_DefaultsForUmbraco_Functions_Test>("functions-test")
    .WithHostStorage(storage)
    .WaitFor(storage);

var cm = builder
    .AddProject<Projects.Casko_DefaultsForUmbraco_Web_UI>(
        "cm",
        launchProfileName: "Umbraco.Web.UI.SchedulingPublisher")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithEnvironment("DOTNET_ENVIRONMENT", "Development")
    .WithEnvironment("UMBRACO_SERVER_ROLE", "SchedulingPublisher")
    .WithEnvironment("FORWARD_HEADERS_ENABLED", "true")
    .WithReference(umbracoDb)
    .WithReference(blobs)
    .WithReference(queues)
    .WithReference(tables)
    .WithReference(openai)
    .WithEnvironment("Umbraco__AI__OpenAI__ApiKey", openai.Resource.Key)
    .WithReference(mailpit.GetEndpoint("smtp"))
    .WithEnvironment("Umbraco__CMS__Global__Smtp__From", "noreply@example.local")
    .WithEnvironment("Umbraco__CMS__Global__Smtp__Host", mailpit.GetEndpoint("smtp").Property(EndpointProperty.Host))
    .WithEnvironment("Umbraco__CMS__Global__Smtp__Port", mailpit.GetEndpoint("smtp").Property(EndpointProperty.Port))
    .WithEnvironment("Umbraco__CMS__Global__Smtp__SecureSocketOptions", "None")
    .WithEnvironment("Umbraco__CMS__Global__Smtp__DeliveryMethod", "Network")
    .WithEnvironment("Umbraco__CMS__Global__Smtp__Username", string.Empty)
    .WithEnvironment("Umbraco__CMS__Global__Smtp__Password", string.Empty)
    .WithEnvironment("CASKO_DISTRIBUTED_CACHE_PROVIDER", distributedCacheProvider)
    .WaitFor(umbracoDb)
    .WaitFor(storage);

var cd = builder
    .AddProject<Projects.Casko_DefaultsForUmbraco_Web_UI>(
        "cd",
        launchProfileName: "Umbraco.Web.UI.Subscriber")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithEnvironment("DOTNET_ENVIRONMENT", "Development")
    .WithEnvironment("UMBRACO_SERVER_ROLE", "Subscriber")
    .WithEnvironment("CASKO_INSTANCE_NAME", "cd-1")
    .WithEnvironment("FORWARD_HEADERS_ENABLED", "true")
    .WithReference(umbracoDb)
    .WithReference(blobs)
    .WithReference(queues)
    .WithReference(tables)
    .WithReference(mailpit.GetEndpoint("smtp"))
    .WithEnvironment("Umbraco__CMS__Global__Smtp__From", "noreply@example.local")
    .WithEnvironment("Umbraco__CMS__Global__Smtp__Host", mailpit.GetEndpoint("smtp").Property(EndpointProperty.Host))
    .WithEnvironment("Umbraco__CMS__Global__Smtp__Port", mailpit.GetEndpoint("smtp").Property(EndpointProperty.Port))
    .WithEnvironment("Umbraco__CMS__Global__Smtp__SecureSocketOptions", "None")
    .WithEnvironment("Umbraco__CMS__Global__Smtp__DeliveryMethod", "Network")
    .WithEnvironment("Umbraco__CMS__Global__Smtp__Username", string.Empty)
    .WithEnvironment("Umbraco__CMS__Global__Smtp__Password", string.Empty)
    .WithEnvironment("CASKO_DISTRIBUTED_CACHE_PROVIDER", distributedCacheProvider)
    .WaitFor(umbracoDb)
    .WaitFor(storage);

var cdAlt = builder
    .AddProject<Projects.Casko_DefaultsForUmbraco_Web_UI>(
        "cd-alt",
        launchProfileName: "Umbraco.Web.UI.Subscriber.Alternative")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithEnvironment("DOTNET_ENVIRONMENT", "Development")
    .WithEnvironment("UMBRACO_SERVER_ROLE", "Subscriber")
    .WithEnvironment("CASKO_INSTANCE_NAME", "cd-2")
    .WithEnvironment("FORWARD_HEADERS_ENABLED", "true")
    .WithReference(umbracoDb)
    .WithReference(blobs)
    .WithReference(queues)
    .WithReference(tables)
    .WithReference(mailpit.GetEndpoint("smtp"))
    .WithEnvironment("Umbraco__CMS__Global__Smtp__From", "noreply@example.local")
    .WithEnvironment("Umbraco__CMS__Global__Smtp__Host", mailpit.GetEndpoint("smtp").Property(EndpointProperty.Host))
    .WithEnvironment("Umbraco__CMS__Global__Smtp__Port", mailpit.GetEndpoint("smtp").Property(EndpointProperty.Port))
    .WithEnvironment("Umbraco__CMS__Global__Smtp__SecureSocketOptions", "None")
    .WithEnvironment("Umbraco__CMS__Global__Smtp__DeliveryMethod", "Network")
    .WithEnvironment("Umbraco__CMS__Global__Smtp__Username", string.Empty)
    .WithEnvironment("Umbraco__CMS__Global__Smtp__Password", string.Empty)
    .WithEnvironment("CASKO_DISTRIBUTED_CACHE_PROVIDER", distributedCacheProvider)
    .WaitFor(umbracoDb)
    .WaitFor(storage);

if (distributedCacheProvider == "redis")
{
    cm.WithReference(cache).WaitFor(cache);
    cd.WithReference(cache).WaitFor(cache);
    cdAlt.WithReference(cache).WaitFor(cache);
}
else
{
    cm.WithEnvironment("ConnectionStrings__distributedCacheDbDSN", umbracoDb);
    cd.WithEnvironment("ConnectionStrings__distributedCacheDbDSN", umbracoDb);
    cdAlt.WithEnvironment("ConnectionStrings__distributedCacheDbDSN", umbracoDb);
}

builder
    .AddProject<Projects.Casko_DefaultsForUmbraco_Yarp>(
        "yarp",
        launchProfileName: "LocalReverseProxy")
    .WithEnvironment(context =>
    {
        context.EnvironmentVariables["ReverseProxy__Clusters__cm__Destinations__default__Address"] = cm.GetEndpoint("https");
        context.EnvironmentVariables["ReverseProxy__Clusters__cd__Destinations__subscriber-1__Address"] = cd.GetEndpoint("https");
        context.EnvironmentVariables["ReverseProxy__Clusters__cd__Destinations__subscriber-2__Address"] = cdAlt.GetEndpoint("https");
    })
    .WithReference(cm)
    .WithReference(cd)
    .WithReference(cdAlt)
    .WaitFor(cm)
    .WaitFor(cd)
    .WaitFor(cdAlt);

builder.Build().Run();

static bool IsEnabled(string environmentVariableName)
{
    var value = Environment.GetEnvironmentVariable(environmentVariableName);

    return bool.TryParse(value, out var enabled) && enabled;
}

static string GetDistributedCacheProvider()
{
    var provider = Environment.GetEnvironmentVariable("CASKO_DISTRIBUTED_CACHE_PROVIDER")?.Trim().ToLowerInvariant() ?? "redis";

    return provider switch
    {
        "redis" => provider,
        "sql" => provider,
        _ => throw new InvalidOperationException(
            "CASKO_DISTRIBUTED_CACHE_PROVIDER must be either 'sql' or 'redis'.")
    };
}

static string GetUmbracoDatabaseCreationScript(string databaseName) =>
    """
    IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = N'{databaseName}')
    BEGIN
        CREATE DATABASE [{databaseName}];
    END
    GO

    USE [{databaseName}];
    GO

    IF OBJECT_ID(N'[dbo].[DistributedCache]', N'U') IS NULL
    BEGIN
        CREATE TABLE [dbo].[DistributedCache](
            [Id] nvarchar(449) COLLATE SQL_Latin1_General_CP1_CS_AS NOT NULL,
            [Value] varbinary(max) NOT NULL,
            [ExpiresAtTime] datetimeoffset NOT NULL,
            [SlidingExpirationInSeconds] bigint NULL,
            [AbsoluteExpiration] datetimeoffset NULL,
            CONSTRAINT [PK_DistributedCache] PRIMARY KEY CLUSTERED ([Id] ASC)
        );

        CREATE NONCLUSTERED INDEX [IX_DistributedCache_ExpiresAtTime]
            ON [dbo].[DistributedCache]([ExpiresAtTime] ASC);
    END
    ELSE IF NOT EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE name = N'IX_DistributedCache_ExpiresAtTime'
            AND object_id = OBJECT_ID(N'[dbo].[DistributedCache]', N'U')
    )
    BEGIN
        CREATE NONCLUSTERED INDEX [IX_DistributedCache_ExpiresAtTime]
            ON [dbo].[DistributedCache]([ExpiresAtTime] ASC);
    END
    """.Replace("{databaseName}", databaseName);
