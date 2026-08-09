using Casko.DefaultsForUmbraco.NemLogin3.Configuration;
using Casko.DefaultsForUmbraco.Common;
using Casko.DefaultsForUmbraco.Common.Configuration;
using Casko.DefaultsForUmbraco.Common.Http;

var webApplicationBuilder = WebApplication.CreateBuilder(args);

webApplicationBuilder.AddSpecializedEnvironment();

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
    .AddComposers();

if (useNemLogin3ExternalLogin && webApplicationBuilder.Environment.IsDevelopment())
{
    if (useMemberLogin)
    {
        umbracoBuilder.AddNemLogin3MemberLogin(webApplicationBuilder.Environment);
    }

    if (useBackOfficeLogin)
    {
        umbracoBuilder.AddNemLogin3BackOfficeLogin(webApplicationBuilder.Environment);
    }
}

umbracoBuilder.Build();

var webApplication = webApplicationBuilder.Build();

if (webApplicationBuilder.Environment.IsDevelopment())
{
    webApplication.UseDefaultForwardHeaders();
}

await webApplication.BootUmbracoAsync();

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


webApplication.MapGet("/ping", () => Results.Ok(new
{
    Status = "OK",
    Environment = webApplication.Environment.EnvironmentName,
    ServerRole = umbracoServerRole,
    Timestamp = DateTimeOffset.UtcNow
}));

await webApplication.RunAsync();
