using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Casko.DefaultsForUmbraco.Web.Configuration;

public static class WebApplicationBuilderExtensions
{
    public static WebApplicationBuilder AddEnvironmentThings(this WebApplicationBuilder builder)
    {
        builder.AddDefaultForwardHeaders();
        builder.AddEnvironmentThingsJsonFiles();
        builder.AddDistributedCacheThings();
        
        return builder;
    }

    public static WebApplicationBuilder AddDistributedCacheThings(this WebApplicationBuilder builder)
    {
        if (Environment.GetEnvironmentVariable(CommonConstants.SingleServerRoleName) == "true")
        {
            return builder;
        }

        var provider = builder.Configuration["CASKO_DISTRIBUTED_CACHE_PROVIDER"]?.Trim().ToLowerInvariant() ?? "sql";

        switch (provider)
        {
            case "redis":
                builder.Services.AddHybridCache();
                builder.AddRedisDistributedCache("cache");
                break;

            case "sql":
                var distributedCacheDbDsn = builder.Configuration.GetConnectionString("distributedCacheDbDSN");

                if (!string.IsNullOrWhiteSpace(distributedCacheDbDsn))
                {
                    builder.Services.AddHybridCache();
                    builder.Services.AddDistributedSqlServerCache(options =>
                    {
                        options.ConnectionString = distributedCacheDbDsn;
                        options.SchemaName = "dbo";
                        options.TableName = "DistributedCache";
                    });
                }

                break;

            default:
                throw new InvalidOperationException(
                    "CASKO_DISTRIBUTED_CACHE_PROVIDER must be either 'sql' or 'redis'.");
        }

        return builder;
    }

    public static WebApplicationBuilder AddEnvironmentThingsJsonFiles(this WebApplicationBuilder builder)
    {
        var serverRole = Environment.GetEnvironmentVariable(CommonConstants.UmbracoServerRoleEnvironmentVariableName);
        
        if(Environment.GetEnvironmentVariable(CommonConstants.EnableForwardHeadersEnvironmentVariableName)?.Equals("true", StringComparison.OrdinalIgnoreCase) is true) 
        {
            builder.Configuration
                .AddJsonFile($"appsettings.Hosts.json", optional: true, reloadOnChange: true);
        }

        builder.Configuration
            .AddJsonFile($"appsettings.{serverRole}.json", optional: true, reloadOnChange: true);
        
        if (builder.Environment.IsDevelopment())
        {
            if (string.IsNullOrWhiteSpace(serverRole))
            {
                builder.Configuration.AddEnvironmentVariables();

                return builder;
            }

            builder.Configuration
                .AddJsonFile($"appsettings.Development.{serverRole}.json", optional: false, reloadOnChange: true);
        }

        // The role-specific JSON files are intentionally loaded after the default providers.
        // Re-add environment variables so Aspire references override their local development fallbacks.
        builder.Configuration.AddEnvironmentVariables();

        return builder;
    }
 
    public static WebApplication UseDefaultForwardHeaders(this WebApplication webApplication)
    {
        if (!ShouldAddForwardHeaders())
        {
            return webApplication;
        }
        
        webApplication.UseForwardedHeaders();
       
        return webApplication;
    }
    
    public static WebApplicationBuilder AddDefaultForwardHeaders(this WebApplicationBuilder builder)
    {
        if (!ShouldAddForwardHeaders())
        {
            return builder;
        }

        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders =
                ForwardedHeaders.XForwardedFor |
                ForwardedHeaders.XForwardedHost |
                ForwardedHeaders.XForwardedProto;

            options.KnownProxies.Add(IPAddress.Loopback);
            options.KnownProxies.Add(IPAddress.IPv6Loopback);
        });

        return builder;
    }

    public static bool ShouldAddForwardHeaders()
    {
        return Environment.GetEnvironmentVariable(CommonConstants.EnableForwardHeadersEnvironmentVariableName)?
            .Equals("true", StringComparison.OrdinalIgnoreCase) is true;
    }
}    
