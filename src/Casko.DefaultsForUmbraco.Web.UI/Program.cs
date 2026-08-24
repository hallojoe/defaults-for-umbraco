using Casko.DefaultsForUmbraco.Web.Startup;

var builder = WebApplication.CreateBuilder(args);
var startup = builder.ConfigureWebUiStartup();

builder.ConfigureUmbraco(startup);

var app = builder.Build();

await app.InitializeBlobMediaContainerAsync(startup.BlobsConnectionString);
app.UseWebUiMiddleware(startup);

await app.BootAndLogUmbracoAsync();

app.UseWebUiEndpoints(startup);
app.MapControllerRoute(
    name: "member-login",
    pattern: "member-login",
    defaults: new { controller = "MemberLogin", action = "Index" });
app.MapPingEndpoint(startup);

await app.RunAsync();
