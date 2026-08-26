using Casko.DefaultsForUmbraco.Web.Configuration;
using Casko.DefaultsForUmbraco.Web.OpenTelemetry;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Casko.DefaultsForUmbraco.Web.Startup;

internal static class WebUiStartupConfiguration
{
    public static WebUiStartup ConfigureWebUiStartup(this WebApplicationBuilder builder)
    {
        builder.Configuration
            .AddJsonFile("appsettings.XmlSitemapsForUmbraco.json", optional: true, reloadOnChange: true)
            .AddJsonFile("appsettings.RobotsTxtForUmbraco.json", optional: true, reloadOnChange: true)
            .AddJsonFile("appsettings.Development.Email.json", optional: true, reloadOnChange: true);

        if (builder.Environment.IsDevelopment())
        {
            builder.Configuration.AddJsonFile(
                "appsettings.Development.HttpHeadersForUmbraco.json",
                optional: false,
                reloadOnChange: true);
        }

        builder.AddEnvironmentThings();
        builder.AddOpenTelemetry();

        var blobsConnectionString = builder.Configuration.GetConnectionString("blobs");
        if (!string.IsNullOrWhiteSpace(blobsConnectionString))
        {
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Umbraco:Storage:AzureBlob:Media:ConnectionString"] = blobsConnectionString
            });
        }

        var umbracoServerRole = Environment.GetEnvironmentVariable(CommonConstants.UmbracoServerRoleEnvironmentVariableName)
            ?? CommonConstants.SingleServerRoleName;

        ArgumentException.ThrowIfNullOrWhiteSpace(umbracoServerRole);

        var useBackoffice = !umbracoServerRole.Equals(
            CommonConstants.SubscriberServerRoleName,
            StringComparison.OrdinalIgnoreCase);
        var useMemberLogin = !umbracoServerRole.Equals(
            CommonConstants.SchedulingPublisherServerRoleName,
            StringComparison.OrdinalIgnoreCase);
        var useNemLogin3ExternalLogin =
            Environment.GetEnvironmentVariable("NEMLOGIN_3_ENABLED")?.Equals("true", StringComparison.OrdinalIgnoreCase) is true;

        if (useNemLogin3ExternalLogin && builder.Environment.IsDevelopment())
        {
            builder.Configuration
                .AddJsonFile("appsettings.Development.NemLogin3.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.Development.{umbracoServerRole}.NemLogin3.json", optional: true, reloadOnChange: true);
        }

        return new WebUiStartup(
            umbracoServerRole,
            useBackoffice,
            useMemberLogin,
            useNemLogin3ExternalLogin,
            blobsConnectionString);
    }
}

internal sealed record WebUiStartup(
    string UmbracoServerRole,
    bool UseBackoffice,
    bool UseMemberLogin,
    bool UseNemLogin3ExternalLogin,
    string? BlobsConnectionString);
