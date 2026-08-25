# Configuration by environment and role

The same application needs different configuration depending on where and how it runs.

Think in layers:

- appsettings.json
- appsettings.Development.json
- role-specific configuration
- Development + role-specific configuration
- environment variables supplied by Aspire

Aspire describes the instance. Umbraco configuration describes how that instance behaves.
