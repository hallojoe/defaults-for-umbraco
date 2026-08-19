# Casko.DefaultsForUmbraco.Yarp

This project is the local reverse proxy used when running the solution as separate Umbraco server roles.

It uses YARP to expose friendly development hostnames on HTTPS port 4443 and forward traffic to the local Kestrel ports used by `Casko.DefaultsForUmbraco.Web.UI`.

## Routes

The development proxy configuration lives in `appsettings.Development.json`.

| Public URL | Role | Destination |
| ---------- | ---- | ----------- |
| `https://cm.dev.localhost:4443` | Scheduling publisher / backoffice | `https://localhost:64101/` |
| `https://cd.dev.localhost:4443` | Subscriber / content delivery | `https://localhost:44101/` |

The proxy itself listens on:

```text
https://localhost:4443
```

## Running

From this project directory:

```powershell
dotnet run --launch-profile LocalReverseProxy
```

The proxy should be started alongside the corresponding `Casko.DefaultsForUmbraco.Web.UI` launch profiles:

```powershell
dotnet run --project ../Casko.DefaultsForUmbraco.Web.UI --launch-profile Umbraco.Web.UI.SchedulingPublisher
dotnet run --project ../Casko.DefaultsForUmbraco.Web.UI --launch-profile Umbraco.Web.UI.Subscriber
```

Once both Web UI profiles and the proxy are running, use:

```text
https://cm.dev.localhost:4443/umbraco/
https://cd.dev.localhost:4443/
```

## Certificates and hostnames

The setup relies on the ASP.NET Core development certificate. Trust it once per machine:

```powershell
dotnet dev-certs https --trust
```

Hostnames ending in `.localhost` normally resolve to loopback automatically. If your environment does not resolve them, add these entries to `C:\Windows\System32\drivers\etc\hosts`:

```text
127.0.0.1 cm.dev.localhost
127.0.0.1 cd.dev.localhost
```

## Forwarded headers

The proxied `Web.UI` profiles set:

```text
FORWARD_HEADERS_ENABLED=true
```

That allows `Casko.DefaultsForUmbraco.Common` to register and apply ASP.NET Core forwarded header middleware. This matters because Umbraco and ASP.NET Core authentication need to see the public host and scheme when generating redirects.

Without forwarded headers, the backend can generate URLs for the internal destination, such as `https://localhost:64101`, instead of the public proxy host, such as `https://cm.dev.localhost:4443`.

## Umbraco host settings

When forwarded headers are enabled, `Casko.DefaultsForUmbraco.Web.UI` also loads `appsettings.Hosts.json`.

That file sets the public Umbraco URLs:

```json
{
  "Umbraco": {
    "CMS": {
      "Security": {
        "BackOfficeHost": "https://cm.dev.localhost:4443"
      },
      "WebRouting": {
        "UmbracoApplicationUrl": "https://cd.dev.localhost:4443/"
      }
    }
  }
}
```

The role-specific appsettings files can keep direct localhost URLs for non-proxied runs, while `appsettings.Hosts.json` supplies the public proxy URLs for the split-role development setup.

## Project shape

`Program.cs` intentionally stays small:

```csharp
builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

app.MapReverseProxy();
```

Most behavior should stay in configuration unless the solution needs custom proxy transforms or diagnostics.
