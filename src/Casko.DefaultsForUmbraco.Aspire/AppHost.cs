using Casko.DefaultsForUmbraco.Aspire.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

//var openAi = builder.AddOpenAiResources();
var database = builder.AddDatabaseResources();

if (AppHostConfiguration.IsEnabled("CASKO_APPHOST_SQL_ONLY"))
{
    builder.Build().Run();
    return;
}

var cache = builder.AddCacheResources();
var storage = builder.AddStorageResources();
var network = builder.AddNetworkResources();
var umbraco = builder.AddUmbracoResources(
    database,
    cache,
    storage,
    null,
    network,
    AppHostConfiguration.GetDistributedCacheProvider());

builder.AddYarpResource(umbraco);
builder.AddAstroResource();

builder.Build().Run();
