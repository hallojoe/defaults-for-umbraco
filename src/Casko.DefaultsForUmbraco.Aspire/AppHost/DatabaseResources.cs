using Aspire.Hosting.ApplicationModel;

internal static class DatabaseResourceExtensions
{
    public static DatabaseResources AddDatabaseResources(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<DashboardGroupResource> group)
    {
        var sql = builder
            .AddSqlServer("sql", port: 11433)
            .WithImage("azure-sql-edge")
            .WithImageRegistry("mcr.microsoft.com")
            .WithDataVolume("defaults-for-umbraco-sql-data")
            .WithHostPort(11433)
            .WithDbGate()
            .WithParentRelationship(group);

        builder
            .CreateResourceBuilder(builder.Resources.OfType<ContainerResource>().Single(resource => resource.Name == "dbgate"))
            .WithParentRelationship(group);

        var umbracoDb = sql
            .AddDatabase("umbracoDbDSN", "defaults-for-umbraco-v4-db")
            .WithCreationScript(SqlScripts.GetUmbracoDatabaseCreationScript("defaults-for-umbraco-v4-db"));

        return new DatabaseResources(sql, umbracoDb);
    }
}

internal sealed record DatabaseResources(
    IResourceBuilder<SqlServerServerResource> Sql,
    IResourceBuilder<SqlServerDatabaseResource> UmbracoDb);
