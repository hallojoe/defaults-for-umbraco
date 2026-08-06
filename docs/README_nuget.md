# Robots.txt for Umbraco

Robots.txt for Umbraco publishes a host-aware `/robots.txt` from configuration and Umbraco content rules.

## Configuration

```json
{
  "RobotsTxt": {
    "Enabled": true,
    "RewritesEnabled": true,
    "ExcludingUrlPropertyAlias": "umbracoNaviHide",
    "ExcludingUrlPropertyValue": "1",
    "IncludedCultures": [ "en", "da" ],
    "ExcludedCultures": [],
    "UseDeliveryApiAccessPolicy": false,
    "SitemapUrls": [ "https://example.com/sitemap.xml" ],
    "Groups": [
      {
        "UserAgents": [ "*" ],
        "Disallow": [ "/private" ],
        "Allow": []
      }
    ],
    "Storage": {
      "RefreshStaleAfterSeconds": 3600,
      "BackgroundJob": {
        "Enabled": true,
        "IntervalSeconds": 3600
      }
    }
  }
}
```

Content matching `ExcludingUrlPropertyAlias` and `ExcludingUrlPropertyValue` is added as `Disallow` entries for each routed URL on the current request host.
