using Casko.DefaultsForUmbraco.Web.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Casko.DefaultsForUmbraco.Web.Configuration;

public static class ConfigurationExtensions
{
    public static WebApplicationBuilder AddSpecializedEnvironment(this WebApplicationBuilder builder)
    {
        builder.AddDefaultForwardHeaders();
        builder.AddSpecializedEnvironmentJsonFiles();
        builder.AddSpecializedDistributedCache();
        
        return builder;
    }

    public static WebApplicationBuilder AddSpecializedDistributedCache(this WebApplicationBuilder builder)
    {
        if (Environment.GetEnvironmentVariable(CommonConstants.SingleServerRoleName) == "true")
        {
            return builder;
        }

        var distributedCacheDbDsn =
            builder.Configuration.GetConnectionString("distributedCacheDbDSN");

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

        return builder;
    }

    public static WebApplicationBuilder AddSpecializedEnvironmentJsonFiles(this WebApplicationBuilder builder)
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
                return builder;
            }

            builder.Configuration
                .AddJsonFile($"appsettings.Development.{serverRole}.json", optional: true, reloadOnChange: true);
        }

        return builder;
    }
    
}