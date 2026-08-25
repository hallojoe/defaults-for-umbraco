# SQL Server in the AppHost

```csharp
var sql = builder
    .AddSqlServer("sql", port: 11433)
    .WithImage("azure-sql-edge")
    .WithDataVolume("defaults-for-umbraco-sql-data");
```

The database runs in a container and retains its data between runs.
