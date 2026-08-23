namespace Casko.DefaultsForUmbraco.Aspire.AppHost;

internal static class UmbracoResourceExtensions
{
    public static UmbracoResources AddUmbracoResources(
        this IDistributedApplicationBuilder builder,
        DatabaseResources database,
        CacheResources cache,
        StorageResources storage,
        OpenAiResources openAi,
        NetworkResources network,
        IResourceBuilder<DashboardGroupResource> group,
        string distributedCacheProvider)
    {
        var cm = builder.AddUmbracoInstance(
            "cm",
            "Umbraco.Web.UI.SchedulingPublisher",
            "SchedulingPublisher",
            instanceName: null,
            database,
            storage,
            network,
            group,
            distributedCacheProvider,
            openAi);

        var cd = builder.AddUmbracoInstance(
            "cd",
            "Umbraco.Web.UI.Subscriber",
            "Subscriber",
            "cd-1",
            database,
            storage,
            network,
            group,
            distributedCacheProvider);

        var cdAlt = builder.AddUmbracoInstance(
            "cd-alt",
            "Umbraco.Web.UI.Subscriber.Alternative",
            "Subscriber",
            "cd-2",
            database,
            storage,
            network,
            group,
            distributedCacheProvider);

        if (distributedCacheProvider == "redis")
        {
            cm.WithReference(cache.Cache).WaitFor(cache.Cache);
            cd.WithReference(cache.Cache).WaitFor(cache.Cache);
            cdAlt.WithReference(cache.Cache).WaitFor(cache.Cache);
        }
        else
        {
            cm.WithEnvironment("ConnectionStrings__distributedCacheDbDSN", database.UmbracoDb);
            cd.WithEnvironment("ConnectionStrings__distributedCacheDbDSN", database.UmbracoDb);
            cdAlt.WithEnvironment("ConnectionStrings__distributedCacheDbDSN", database.UmbracoDb);
        }

        return new UmbracoResources(cm, cd, cdAlt);
    }

    private static IResourceBuilder<ProjectResource> AddUmbracoInstance(
        this IDistributedApplicationBuilder builder,
        string name,
        string launchProfileName,
        string serverRole,
        string? instanceName,
        DatabaseResources database,
        StorageResources storage,
        NetworkResources network,
        IResourceBuilder<DashboardGroupResource> group,
        string distributedCacheProvider,
        OpenAiResources? openAi = null)
    {
        var resource = builder
            .AddProject<Projects.Casko_DefaultsForUmbraco_Web_UI>(name, launchProfileName)
            .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
            .WithEnvironment("DOTNET_ENVIRONMENT", "Development")
            .WithEnvironment("UMBRACO_SERVER_ROLE", serverRole)
            .WithEnvironment("FORWARD_HEADERS_ENABLED", "true")
            .WithReference(database.UmbracoDb)
            .WithReference(storage.Blobs)
            .WithReference(network.Mailpit.GetEndpoint("smtp"))
            .WithEnvironment("Umbraco__CMS__Global__Smtp__From", "noreply@example.local")
            .WithEnvironment("Umbraco__CMS__Global__Smtp__Host", network.Mailpit.GetEndpoint("smtp").Property(EndpointProperty.Host))
            .WithEnvironment("Umbraco__CMS__Global__Smtp__Port", network.Mailpit.GetEndpoint("smtp").Property(EndpointProperty.Port))
            .WithEnvironment("Umbraco__CMS__Global__Smtp__SecureSocketOptions", "None")
            .WithEnvironment("Umbraco__CMS__Global__Smtp__DeliveryMethod", "Network")
            .WithEnvironment("Umbraco__CMS__Global__Smtp__Username", string.Empty)
            .WithEnvironment("Umbraco__CMS__Global__Smtp__Password", string.Empty)
            .WithEnvironment("CASKO_DISTRIBUTED_CACHE_PROVIDER", distributedCacheProvider)
            .WaitFor(database.UmbracoDb)
            .WaitFor(storage.Storage)
            .WithParentRelationship(group);

        if (instanceName is not null)
        {
            resource.WithEnvironment("CASKO_INSTANCE_NAME", instanceName);
        }

        if (openAi is not null)
        {
            resource
                .WithReference(openAi.OpenAi)
                .WithEnvironment("Umbraco__AI__OpenAI__ApiKey", openAi.OpenAi.Resource.Key);
        }

        return resource;
    }
}

internal sealed record UmbracoResources(
    IResourceBuilder<ProjectResource> Cm,
    IResourceBuilder<ProjectResource> Cd,
    IResourceBuilder<ProjectResource> CdAlt);