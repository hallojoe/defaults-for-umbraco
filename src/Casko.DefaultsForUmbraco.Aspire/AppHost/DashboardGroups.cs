using Aspire.Hosting.ApplicationModel;

internal static class DashboardGroupExtensions
{
    public static DashboardGroups AddDashboardGroups(this IDistributedApplicationBuilder builder) => new(
        builder.AddResource(new DashboardGroupResource("database")),
        builder.AddResource(new DashboardGroupResource("caching")),
        builder.AddResource(new DashboardGroupResource("umbraco")),
        builder.AddResource(new DashboardGroupResource("network")));
}

internal sealed record DashboardGroups(
    IResourceBuilder<DashboardGroupResource> Database,
    IResourceBuilder<DashboardGroupResource> Caching,
    IResourceBuilder<DashboardGroupResource> Umbraco,
    IResourceBuilder<DashboardGroupResource> Network);

internal sealed class DashboardGroupResource(string name) : Resource(name), IResourceWithoutLifetime;
