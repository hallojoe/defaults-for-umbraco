# Working agreement

This repository provides a local Umbraco setup with separate content-management and public-site roles, coordinated by .NET Aspire.

## Where to make changes

- `src/Casko.DefaultsForUmbraco.Aspire` defines the local environment. Its `AppHost` folder separates database, cache, storage, network, Umbraco, and traffic-entry-point resources.
- `src/Casko.DefaultsForUmbraco.Web.UI` is the runnable Umbraco website.
- `src/Casko.DefaultsForUmbraco.Yarp` routes the local content-management and public website addresses.
- `src/Casko.DefaultsForUmbraco.Web` contains shared website configuration and behavior.

## Keep the documentation honest

Update `README.md`, `INSTALL.md`, or `RUN.md` whenever a user-facing local setup, address, dependency, or service changes. The AppHost source is the authority for what starts locally.

## Safe changes

- Preserve the content-management site and both public-site instances unless the task explicitly changes the topology.
- Keep local endpoints and environment-variable names aligned with the AppHost and launch profiles.
- Do not overwrite unrelated changes in a dirty working tree.
- Prefer the smallest relevant validation. For project changes, use `dotnet build src/Casko.DefaultsForUmbraco.slnx` when no running process has locked an output file.

## Common commands

```powershell
dotnet build src/Casko.DefaultsForUmbraco.slnx
dotnet run --project src/Casko.DefaultsForUmbraco.Aspire
```
