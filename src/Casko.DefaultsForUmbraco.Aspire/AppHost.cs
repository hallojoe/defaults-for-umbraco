var builder = DistributedApplication.CreateBuilder(args);

var groups = builder.AddDashboardGroups();
var openAi = builder.AddOpenAiResources();
var database = builder.AddDatabaseResources(groups.Database);

if (AppHostConfiguration.IsEnabled("CASKO_APPHOST_SQL_ONLY"))
{
    builder.Build().Run();
    return;
}

var cache = builder.AddCacheResources(groups.Caching);
var storage = builder.AddStorageResources();
var network = builder.AddNetworkResources(groups.Network);
var umbraco = builder.AddUmbracoResources(
    database,
    cache,
    storage,
    openAi,
    network,
    groups.Umbraco,
    AppHostConfiguration.GetDistributedCacheProvider());

builder.AddYarpResource(umbraco, groups.Network);

builder.Build().Run();
