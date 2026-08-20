var builder = DistributedApplication.CreateBuilder(args);
var sqlOnly = IsEnabled("CASKO_APPHOST_SQL_ONLY");

var sql = builder
    .AddSqlServer("sql", port: 11433)
    .WithImage("azure-sql-edge")
    .WithImageRegistry("mcr.microsoft.com")
    .WithDataVolume("defaults-for-umbraco-sql-data")
    .WithHostPort(11433);

if (sqlOnly)
{
    builder.Build().Run();
    return;
}

var databaseName = "defaults-for-umbraco-v3-db";
var umbracoDb = sql
    .AddDatabase("umbracoDbDSN", databaseName)
    .WithCreationScript(GetUmbracoDatabaseCreationScript(databaseName));

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
    .WithReference(mailpit.GetEndpoint("smtp"))
    .WithEnvironment("Umbraco__CMS__Global__Smtp__From", "noreply@example.local")
    .WithEnvironment("Umbraco__CMS__Global__Smtp__Host", mailpit.GetEndpoint("smtp").Property(EndpointProperty.Host))
    .WithEnvironment("Umbraco__CMS__Global__Smtp__Port", mailpit.GetEndpoint("smtp").Property(EndpointProperty.Port))
    .WithEnvironment("Umbraco__CMS__Global__Smtp__SecureSocketOptions", "None")
    .WithEnvironment("Umbraco__CMS__Global__Smtp__DeliveryMethod", "Network")
    .WithEnvironment("Umbraco__CMS__Global__Smtp__Username", string.Empty)
    .WithEnvironment("Umbraco__CMS__Global__Smtp__Password", string.Empty)
    .WithEnvironment("ConnectionStrings__distributedCacheDbDSN", umbracoDb)
    .WithEnvironment("ConnectionStrings__distributedCacheDbDSN", umbracoDb)
    .WaitFor(umbracoDb)
    .WaitFor(storage);

var cd = builder
    .AddProject<Projects.Casko_DefaultsForUmbraco_Web_UI>(
        "cd",
        launchProfileName: "Umbraco.Web.UI.Subscriber")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithEnvironment("DOTNET_ENVIRONMENT", "Development")
    .WithEnvironment("UMBRACO_SERVER_ROLE", "Subscriber")
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
    .WithEnvironment("ConnectionStrings__distributedCacheDbDSN", umbracoDb)
    .WaitFor(umbracoDb)
    .WaitFor(storage);

builder
    .AddProject<Projects.Casko_DefaultsForUmbraco_Yarp>(
        "yarp",
        launchProfileName: "LocalReverseProxy")
    .WithEnvironment(context =>
    {
        context.EnvironmentVariables["ReverseProxy__Clusters__cm__Destinations__default__Address"] = cm.GetEndpoint("https");
        context.EnvironmentVariables["ReverseProxy__Clusters__cd__Destinations__default__Address"] = cd.GetEndpoint("https");
    })
    .WithReference(cm)
    .WithReference(cd)
    .WaitFor(cm)
    .WaitFor(cd);

builder.Build().Run();

static bool IsEnabled(string environmentVariableName)
{
    var value = Environment.GetEnvironmentVariable(environmentVariableName);

    return bool.TryParse(value, out var enabled) && enabled;
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
