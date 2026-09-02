using Casko.DefaultsForUmbraco.Web.Configuration;
using Casko.NemLogin3ForUmbraco.Configuration;
using Casko.SyncExtensionsForUmbraco.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;

namespace Casko.DefaultsForUmbraco.Web.Startup;

internal static class UmbracoComposition
{
    public static void ConfigureUmbraco(this WebApplicationBuilder builder, WebUiStartup startup)
    {
        var umbracoBuilder = builder.CreateUmbracoBuilder()
            .AddServerRole(startup.UmbracoServerRole)
            .AddBackOffice()
            .AddWebsite()
            .AddDeliveryApi()
            .AddComposers()
            .AddAzureBlobMediaFileSystem()
            .AddAzureBlobImageSharpCache();

        if (startup.UseNemLogin3ExternalLogin && builder.Environment.IsDevelopment())
        {
            if (startup.UseMemberLogin)
            {
                umbracoBuilder.AddNemLogin3MemberLogin(builder.Environment);
            }

            if (startup.UseBackoffice)
            {
                umbracoBuilder.AddNemLogin3BackOfficeLogin(builder.Environment);
            }
        }

        umbracoBuilder.Build();
    }
}
