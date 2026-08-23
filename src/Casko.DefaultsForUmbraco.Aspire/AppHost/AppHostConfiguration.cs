internal static class AppHostConfiguration
{
    public static bool IsEnabled(string environmentVariableName) =>
        bool.TryParse(Environment.GetEnvironmentVariable(environmentVariableName), out var enabled) && enabled;

    public static string GetDistributedCacheProvider()
    {
        var provider = Environment.GetEnvironmentVariable("CASKO_DISTRIBUTED_CACHE_PROVIDER")?.Trim().ToLowerInvariant() ?? "redis";

        return provider switch
        {
            "redis" => provider,
            "sql" => provider,
            _ => throw new InvalidOperationException(
                "CASKO_DISTRIBUTED_CACHE_PROVIDER must be either 'sql' or 'redis'.")
        };
    }
}
