# TEMP and logs are per-instance

Each process needs its own temporary directory and log file name.

```json
"TEMP": "..\\aspnet\\temp\\subscriber",
"TMP": "..\\aspnet\\temp\\subscriber",
"Umbraco__CMS__Logging__FileNameFormat":
  "UmbracoTraceLog.Subscriber.{0}.json"
```
