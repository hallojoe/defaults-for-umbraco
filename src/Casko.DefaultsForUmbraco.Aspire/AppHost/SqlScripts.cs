internal static class SqlScripts
{
    public static string GetUmbracoDatabaseCreationScript(string databaseName) =>
        """
        IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = N'{databaseName}')
        BEGIN
            CREATE DATABASE [{databaseName}];
        END
        GO

        USE [{databaseName}];
        GO

        IF OBJECT_ID(N'[dbo].[DistributedCache]', N'U') IS NULL
        BEGIN
            CREATE TABLE [dbo].[DistributedCache](
                [Id] nvarchar(449) COLLATE SQL_Latin1_General_CP1_CS_AS NOT NULL,
                [Value] varbinary(max) NOT NULL,
                [ExpiresAtTime] datetimeoffset NOT NULL,
                [SlidingExpirationInSeconds] bigint NULL,
                [AbsoluteExpiration] datetimeoffset NULL,
                CONSTRAINT [PK_DistributedCache] PRIMARY KEY CLUSTERED ([Id] ASC)
            );

            CREATE NONCLUSTERED INDEX [IX_DistributedCache_ExpiresAtTime]
                ON [dbo].[DistributedCache]([ExpiresAtTime] ASC);
        END
        ELSE IF NOT EXISTS (
            SELECT 1
            FROM sys.indexes
            WHERE name = N'IX_DistributedCache_ExpiresAtTime'
                AND object_id = OBJECT_ID(N'[dbo].[DistributedCache]', N'U')
        )
        BEGIN
            CREATE NONCLUSTERED INDEX [IX_DistributedCache_ExpiresAtTime]
                ON [dbo].[DistributedCache]([ExpiresAtTime] ASC);
        END
        """.Replace("{databaseName}", databaseName);
}
