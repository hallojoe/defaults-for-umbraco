namespace Casko.DefaultsForUmbraco.Aspire.AppHost;

#pragma warning disable ASPIREJAVASCRIPT001

internal static class AstroResourceExtensions
{
    public static void AddAstroResource(this IDistributedApplicationBuilder builder)
    {
        builder
            .AddViteApp("frontend", "../Casko.DefaultsForUmbraco.Astro.UI", runScriptName: "dev")
            .PublishAsStaticWebsite()
            .WithExternalHttpEndpoints();
    }
}
