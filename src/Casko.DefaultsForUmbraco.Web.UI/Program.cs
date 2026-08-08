using Casko.DefaultsForUmbraco.NemLogin3.Configuration;
using Casko.DefaultsForUmbraco.Common;
using Casko.DefaultsForUmbraco.Common.Configuration;
using Casko.DefaultsForUmbraco.Common.Http;

var webApplicationBuilder = WebApplication.CreateBuilder(args);

webApplicationBuilder.AddSpecializedEnvironment();

var umbracoServerRole = Environment.GetEnvironmentVariable(CommonConstants.UmbracoServerRoleEnvironmentVariableName) 
                        ?? CommonConstants.SingleServerRoleName;
var useBackoffice =
    umbracoServerRole?.Equals(CommonConstants.SubscriberServerRoleName, StringComparison.OrdinalIgnoreCase) is false;

var directory = webApplicationBuilder.Environment.WebRootPath;
var directory2 = webApplicationBuilder.Environment.WebRootPath;

var umbracoBuilder = webApplicationBuilder.CreateUmbracoBuilder()
    .AddServerRole(umbracoServerRole)
    .AddBackOffice()
    .AddWebsite()
    .AddDeliveryApi()
    .AddComposers();

if (webApplicationBuilder.Environment.IsDevelopment())
{
    umbracoBuilder.AddNemLogin3MemberLogin(webApplicationBuilder.Environment);
}

umbracoBuilder.Build();

var webApplication = webApplicationBuilder.Build();

webApplication.UseDefaultForwardHeaders();

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
