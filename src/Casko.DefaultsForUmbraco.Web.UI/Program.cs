using Casko.DefaultsForUmbraco.Web;
using Casko.DefaultsForUmbraco.Web.Configuration;
using Casko.DefaultsForUmbraco.Web.Http;
using Casko.DefaultsForUmbraco.Web.OpenTelemetry;
//using Casko.RobotsTxtForUmbraco.Delivery.Configuration;
using Azure.Storage.Blobs;
using Umbraco.Cms.Core.Sync;

//using Casko.NemLogin3ForUmbraco.Configuration;

var webApplicationBuilder = WebApplication.CreateBuilder(args);

webApplicationBuilder.Configuration
    .AddJsonFile("appsettings.XmlSitemapsForUmbraco.json", optional: true, reloadOnChange: true);

webApplicationBuilder.Configuration
    .AddJsonFile("appsettings.Development.Email.json", optional: true, reloadOnChange: true);

if (webApplicationBuilder.Environment.IsDevelopment())
{
    webApplicationBuilder.Configuration
        .AddJsonFile("appsettings.Development.HttpHeadersForUmbraco.json", optional: false, reloadOnChange: true);
}

webApplicationBuilder.AddSpecializedEnvironment();
webApplicationBuilder.AddOpenTelemetry();

var blobsConnectionString = webApplicationBuilder.Configuration.GetConnectionString("blobs");
if (!string.IsNullOrWhiteSpace(blobsConnectionString))
{
    webApplicationBuilder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["Umbraco:Storage:AzureBlob:Media:ConnectionString"] = blobsConnectionString
    });
}

var umbracoServerRole = Environment.GetEnvironmentVariable(CommonConstants.UmbracoServerRoleEnvironmentVariableName) 
                        ?? CommonConstants.SingleServerRoleName;
ArgumentException.ThrowIfNullOrWhiteSpace(umbracoServerRole);

var useBackoffice =
    umbracoServerRole?.Equals(CommonConstants.SubscriberServerRoleName, StringComparison.OrdinalIgnoreCase) is false;

var useMemberLogin =
    umbracoServerRole?.Equals(CommonConstants.SchedulingPublisherServerRoleName, StringComparison.OrdinalIgnoreCase) is false;

var useBackOfficeLogin = useBackoffice;

var useNemLogin3ExternalLogin =
    Environment.GetEnvironmentVariable("NEMLOGIN_3_ENABLED")?.Equals("true", StringComparison.OrdinalIgnoreCase) is true;

if (useNemLogin3ExternalLogin && webApplicationBuilder.Environment.IsDevelopment())
{
    webApplicationBuilder.Configuration
        .AddJsonFile("appsettings.Development.NemLogin3.json", optional: true, reloadOnChange: true)
        .AddJsonFile($"appsettings.Development.{umbracoServerRole}.NemLogin3.json", optional: true, reloadOnChange: true);
}

var umbracoBuilder = webApplicationBuilder.CreateUmbracoBuilder()
    .AddServerRole(umbracoServerRole!)
    .AddBackOffice()
    .AddWebsite()
    .AddDeliveryApi()
    // .AddRobotsTxtDeliveryApi()
    .AddComposers()
    .AddAzureBlobMediaFileSystem()
    .AddAzureBlobImageSharpCache();

if (useNemLogin3ExternalLogin && webApplicationBuilder.Environment.IsDevelopment())
{
    if (useMemberLogin)
    {
//        umbracoBuilder.AddNemLogin3MemberLogin(webApplicationBuilder.Environment);
    }

    if (useBackOfficeLogin)
    {
//        umbracoBuilder.AddNemLogin3BackOfficeLogin(webApplicationBuilder.Environment);
    }
}

umbracoBuilder.Build();

var webApplication = webApplicationBuilder.Build();

// Aspire exposes the Azurite Blob service but does not create Umbraco's media
// container. Provision it here so both CM and CD can safely start against a new
// emulator; CreateIfNotExistsAsync is idempotent when they start concurrently.
if (!string.IsNullOrWhiteSpace(blobsConnectionString))
{
    var mediaContainerName = webApplication.Configuration["Umbraco:Storage:AzureBlob:Media:ContainerName"];
    ArgumentException.ThrowIfNullOrWhiteSpace(mediaContainerName);

    var blobServiceClient = new BlobServiceClient(blobsConnectionString);
    await blobServiceClient
        .GetBlobContainerClient(mediaContainerName)
        .CreateIfNotExistsAsync();
}

if (webApplicationBuilder.Environment.IsDevelopment())
{
    webApplication.UseDefaultForwardHeaders();
}

await webApplication.BootUmbracoAsync();

var assignedServerRole = webApplication.Services
    .GetRequiredService<IServerRoleAccessor>()
    .CurrentServerRole;

webApplication.Logger.LogInformation("Server role is {umbracoServerRole}", assignedServerRole);

if (webApplication.Environment.IsDevelopment())
{
    webApplication.Logger.LogInformation("ASP.NET temp folder : {aspNetTempFolder}", Path.GetTempPath());
}

webApplication
    .UseUmbraco()
    .WithMiddleware(u =>
    {
        if (useBackoffice)
        {
            u.UseBackOffice();
        }
        u.UseWebsite();
    })
    .WithEndpoints(u =>
    {
        if (useBackoffice)
        {
            u.UseBackOfficeEndpoints();
        }
        u.UseWebsiteEndpoints();
    });


webApplication.MapGet("/ping", (ILoggerFactory loggerFactory, IServerRoleAccessor umbracoServerRoleAccessor) =>
{
    loggerFactory
        .CreateLogger("Casko.DefaultsForUmbraco.Web.UI.Ping")
        .LogInformation(
            "Ping request received for server role {ServerRole} in environment {Environment}",
            umbracoServerRole,
            webApplication.Environment.EnvironmentName);

    return Results.Ok(new
    {
        Status = "OK",
        Environment = webApplication.Environment.EnvironmentName,
        ClaimedServerRole = umbracoServerRole,
        AssignedServerRole = umbracoServerRoleAccessor.CurrentServerRole.ToString(),
        Timestamp = DateTimeOffset.UtcNow
    });
});

await webApplication.RunAsync();
