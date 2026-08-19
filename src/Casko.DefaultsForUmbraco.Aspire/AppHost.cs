var builder = DistributedApplication.CreateBuilder(args);

var sql = builder
    .AddSqlServer("sql", port: 11433)
    .WithImage("azure-sql-edge")
    .WithImageRegistry("mcr.microsoft.com")
    .WithDataVolume("defaults-for-umbraco-sql-data")
    .WithHostPort(11433);

var umbracoDb = sql
    .AddDatabase("umbracoDbDSN", "defaults-for-umbraco-db")
    .WithCreationScript(GetUmbracoDatabaseCreationScript());

var cm = builder
    .AddProject<Projects.Casko_DefaultsForUmbraco_Web_UI>(
        "cm",
        launchProfileName: "Umbraco.Web.UI.SchedulingPublisher")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithEnvironment("DOTNET_ENVIRONMENT", "Development")
    .WithEnvironment("UMBRACO_SERVER_ROLE", "SchedulingPublisher")
    .WithEnvironment("FORWARD_HEADERS_ENABLED", "true")
    .WithReference(umbracoDb)
    .WithEnvironment("ConnectionStrings__distributedCacheDbDSN", umbracoDb)
    .WaitFor(umbracoDb);

var cd = builder
    .AddProject<Projects.Casko_DefaultsForUmbraco_Web_UI>(
        "cd",
        launchProfileName: "Umbraco.Web.UI.Subscriber")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithEnvironment("DOTNET_ENVIRONMENT", "Development")
    .WithEnvironment("UMBRACO_SERVER_ROLE", "Subscriber")
    .WithEnvironment("FORWARD_HEADERS_ENABLED", "true")
    .WithReference(umbracoDb)
    .WithEnvironment("ConnectionStrings__distributedCacheDbDSN", umbracoDb)
    .WaitFor(umbracoDb);

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

static string GetUmbracoDatabaseCreationScript() =>
    """
    IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = N'defaults-for-umbraco-db')
    BEGIN
        CREATE DATABASE [defaults-for-umbraco-db];
    END
    GO

    USE [defaults-for-umbraco-db];
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
    """;
