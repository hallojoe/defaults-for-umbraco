using Casko.NemLogin3.Web.Configuration;
using Microsoft.AspNetCore.Builder;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddNemLogin3Web(builder.Configuration, builder.Environment);

var app = builder.Build();

app.UseNemLogin3Web();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

await app.RunAsync();
