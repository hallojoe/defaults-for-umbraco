namespace Casko.DefaultsForUmbraco.Aspire.AppHost;

internal static class DatabaseResourceExtensions
{
    public static DatabaseResources AddDatabaseResources(this IDistributedApplicationBuilder builder)
    {
        var sql = builder
            .AddSqlServer("sql", port: 11433)
            .WithImageTag("2022-latest")
            .WithImageRegistry("mcr.microsoft.com")
            .WithDataVolume("defaults-for-umbraco-sql-data")
            .WithHostPort(11433)
            .WithDbGate();
        

        var umbracoDb = sql
            .AddDatabase("umbracoDbDSN", "defaults-for-umbraco-v7-db")
            .WithCreationScript(SqlScripts.GetUmbracoDatabaseCreationScript("defaults-for-umbraco-v7-db"));

        var automateDb = sql
            .AddDatabase("umbracoAutomateDbDSN", "defaults-for-umbraco-automate-db")
            .WithCreationScript(SqlScripts.GetAutomateDatabaseCreationScript("defaults-for-umbraco-automate-db"));

        return new DatabaseResources(sql, umbracoDb, automateDb);
    }
}

internal sealed record DatabaseResources(
    IResourceBuilder<SqlServerServerResource> Sql,
    IResourceBuilder<SqlServerDatabaseResource> UmbracoDb,
    IResourceBuilder<SqlServerDatabaseResource> AutomateDb);
