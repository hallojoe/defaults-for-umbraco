using Aspire.Hosting.ApplicationModel;

internal static class NetworkResourceExtensions
{
    public static NetworkResources AddNetworkResources(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<DashboardGroupResource> group)
    {
        var mailpit = builder
            .AddContainer("mailpit", "axllent/mailpit")
            .WithEndpoint(targetPort: 1025, name: "smtp")
            .WithHttpEndpoint(targetPort: 8025, name: "ui")
            .WithParentRelationship(group);

        return new NetworkResources(mailpit);
    }
}

internal sealed record NetworkResources(IResourceBuilder<ContainerResource> Mailpit);
