# Run the local environment

Make sure Docker Desktop or Podman is running, then open a terminal in `src/Casko.DefaultsForUmbraco.Aspire` and run:

```powershell
dotnet run --project src/Casko.DefaultsForUmbraco.Aspire
```

If you have Aspire CLI installed then:

```powershell
aspire start
```


```mermaid
sequenceDiagram
    participant You
    participant Docker as Docker Desktop
    participant Terminal
    participant Aspire as Aspire AppHost
    participant Dashboard as Aspire dashboard

    You->>Docker: Confirm it is running
    You->>Terminal: Run the Aspire project
    Terminal->>Aspire: Start local environment
    Aspire->>Dashboard: Open dashboard
    loop While services are starting
        Dashboard-->>You: Show each service status
    end
    Dashboard-->>You: Sites are ready to use
    You->>Dashboard: Open the site or tool you need
    You->>Terminal: Press Ctrl+C when finished
    Terminal->>Aspire: Stop local environment
```

Your browser opens the Aspire dashboard. The first start may take a few minutes while local services and container images are prepared.

When the dashboard shows the sites as running, open:

| What you want to do | Address |
| --- | --- |
| Manage content in Umbraco | `https://cm.dev.localhost:4443/umbraco/` |
| View the public website | `https://cd.dev.localhost:4443/` |
| View test emails | Open the `mailpit` **ui** link in the Aspire dashboard |
| Inspect the local database | Open the `dbgate` link in the Aspire dashboard |

Press `Ctrl+C` in the terminal to stop the environment.

## If the website addresses do not open

See HOSTS.md

## Useful options

Use the database-backed cache instead of the default Redis cache:

```powershell
$env:CASKO_DISTRIBUTED_CACHE_PROVIDER = "sql"
dotnet run --project src/Casko.DefaultsForUmbraco.Aspire
```

Start only the local database and its viewer:

```powershell
dotnet run --project src/Casko.DefaultsForUmbraco.Aspire --launch-profile sql-only
```
