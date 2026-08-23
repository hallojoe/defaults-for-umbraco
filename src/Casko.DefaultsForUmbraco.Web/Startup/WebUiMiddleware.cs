using Casko.DefaultsForUmbraco.Web.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Sync;

namespace Casko.DefaultsForUmbraco.Web.Startup;

internal static class WebUiMiddleware
{
    public static void UseWebUiMiddleware(this WebApplication application, WebUiStartup startup)
    {
        if (application.Environment.IsDevelopment())
        {
            application.UseDefaultForwardHeaders();
        }

        var instanceName = Environment.GetEnvironmentVariable("CASKO_INSTANCE_NAME");
        if (!string.IsNullOrWhiteSpace(instanceName))
        {
            application.Use(async (context, next) =>
            {
                context.Response.OnStarting(() =>
                {
                    context.Response.Headers["X-Casko-Instance"] = instanceName;
                    return Task.CompletedTask;
                });

                await next(context);
            });
        }
    }

    public static async Task BootAndLogUmbracoAsync(this WebApplication application)
    {
        await application.BootUmbracoAsync();

        var assignedServerRole = application.Services
            .GetRequiredService<IServerRoleAccessor>()
            .CurrentServerRole;

        application.Logger.LogInformation("Server role is {umbracoServerRole}", assignedServerRole);

        if (application.Environment.IsDevelopment())
        {
            application.Logger.LogInformation("ASP.NET temp folder : {aspNetTempFolder}", Path.GetTempPath());
        }
    }

    public static void UseWebUiEndpoints(this WebApplication application, WebUiStartup startup)
    {
        application
            .UseUmbraco()
            .WithMiddleware(u =>
            {
                if (startup.UseBackoffice)
                {
                    u.UseBackOffice();
                }

                u.UseWebsite();
            })
            .WithEndpoints(u =>
            {
                if (startup.UseBackoffice)
                {
                    u.UseBackOfficeEndpoints();
                }

                u.UseWebsiteEndpoints();
            });
    }

    public static void MapPingEndpoint(this WebApplication application, WebUiStartup startup)
    {
        application.MapGet("/ping", (ILoggerFactory loggerFactory, IServerRoleAccessor umbracoServerRoleAccessor) =>
        {
            loggerFactory
                .CreateLogger("Casko.DefaultsForUmbraco.Web.UI.Ping")
                .LogInformation(
                    "Ping request received for server role {ServerRole} in environment {Environment}",
                    startup.UmbracoServerRole,
                    application.Environment.EnvironmentName);

            return Results.Ok(new
            {
                Status = "OK",
                Environment = application.Environment.EnvironmentName,
                ClaimedServerRole = startup.UmbracoServerRole,
                AssignedServerRole = umbracoServerRoleAccessor.CurrentServerRole.ToString(),
                Timestamp = DateTimeOffset.UtcNow
            });
        });
    }
}
