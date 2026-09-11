using Aspire.Hosting.Azure;

namespace Casko.DefaultsForUmbraco.Aspire.AppHost;

internal static class CacheResourceExtensions
{
    public static CacheResources AddCacheResources(this IDistributedApplicationBuilder builder)
    {
        var cache = builder
            .AddAzureManagedRedis("cache")
            .RunAsContainer(redis => redis.WithRedisInsight());

        return new CacheResources(cache);
    }
}

internal sealed record CacheResources(IResourceBuilder<AzureManagedRedisResource> Cache);
