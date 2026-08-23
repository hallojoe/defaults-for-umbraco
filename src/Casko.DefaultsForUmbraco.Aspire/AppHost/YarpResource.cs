namespace Casko.DefaultsForUmbraco.Aspire.AppHost;

internal static class YarpResourceExtensions
{
    public static void AddYarpResource(
        this IDistributedApplicationBuilder builder,
        UmbracoResources umbraco,
        IResourceBuilder<DashboardGroupResource> group)
    {
        builder
            .AddProject<Projects.Casko_DefaultsForUmbraco_Yarp>(
                "yarp",
                launchProfileName: "LocalReverseProxy")
            .WithEnvironment(context =>
            {
                context.EnvironmentVariables["ReverseProxy__Clusters__cm__Destinations__default__Address"] = umbraco.Cm.GetEndpoint("https");
                context.EnvironmentVariables["ReverseProxy__Clusters__cd__Destinations__subscriber-1__Address"] = umbraco.Cd.GetEndpoint("http");
                context.EnvironmentVariables["ReverseProxy__Clusters__cd__Destinations__subscriber-2__Address"] = umbraco.CdAlt.GetEndpoint("http");
            })
            .WithReference(umbraco.Cm)
            .WithReference(umbraco.Cd)
            .WithReference(umbraco.CdAlt)
            .WaitFor(umbraco.Cm)
            .WaitFor(umbraco.Cd)
            .WaitFor(umbraco.CdAlt)
            .WithParentRelationship(group);
    }
}