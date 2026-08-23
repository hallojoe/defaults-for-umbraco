using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;

internal static class CacheResourceExtensions
{
    public static CacheResources AddCacheResources(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<DashboardGroupResource> group)
    {
        var cache = builder
            .AddAzureManagedRedis("cache")
            .RunAsContainer(redis => redis.WithRedisInsight())
            .WithParentRelationship(group);

        builder
            .CreateResourceBuilder(builder.Resources.OfType<ContainerResource>().Single(resource => resource.Name == "redisinsight"))
            .WithParentRelationship(group);

        return new CacheResources(cache);
    }
}

internal sealed record CacheResources(IResourceBuilder<AzureManagedRedisResource> Cache);
