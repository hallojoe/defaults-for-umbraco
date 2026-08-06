using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;

namespace Casko.DefaultsForUmbraco.Common.Http;

public static class HttpExtensions
{
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