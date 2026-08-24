using Casko.DefaultsForUmbraco.Web.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
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
        if (startup.UmbracoServerRole.Equals(
                CommonConstants.SubscriberServerRoleName,
                StringComparison.OrdinalIgnoreCase))
        {
            application.UseMemberLoginRedirect();
        }

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

    private static void UseMemberLoginRedirect(this WebApplication application)
    {
        application.Use(async (context, next) =>
        {
            await next(context);

            if (context.Response.HasStarted ||
                context.User.Identity?.IsAuthenticated is true ||
                context.Response.StatusCode != StatusCodes.Status302Found ||
                !context.Response.Headers.TryGetValue("Location", out var location))
            {
                return;
            }

            var redirectUri = location.ToString();
            if (!TryGetLocalReturnUrl(context, redirectUri, out var returnUrl))
            {
                return;
            }

            context.Response.Headers.Location = QueryHelpers.AddQueryString("/member-login", "returnUrl", returnUrl);
        });
    }

    private static bool TryGetLocalReturnUrl(HttpContext context, string redirectUri, out string returnUrl)
    {
        returnUrl = string.Empty;

        if (!Uri.TryCreate(redirectUri, UriKind.RelativeOrAbsolute, out var uri) ||
            (uri.IsAbsoluteUri && !uri.Host.Equals(context.Request.Host.Host, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        var queryStart = redirectUri.IndexOf('?');
        var queryString = uri.IsAbsoluteUri
            ? uri.Query
            : queryStart >= 0 ? redirectUri[queryStart..] : string.Empty;
        if (string.IsNullOrEmpty(queryString))
        {
            return false;
        }

        var query = QueryHelpers.ParseQuery(queryString);
        if (!query.TryGetValue("returnUrl", out var requestedUrl) ||
            string.IsNullOrWhiteSpace(requestedUrl))
        {
            return false;
        }

        var value = requestedUrl.ToString();
        if (!value.StartsWith("/", StringComparison.Ordinal) || value.StartsWith("//", StringComparison.Ordinal))
        {
            return false;
        }

        returnUrl = value;
        return true;
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
