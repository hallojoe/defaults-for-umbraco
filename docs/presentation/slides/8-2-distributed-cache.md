# Redis in the AppHost

```csharp
var cache = builder
    .AddAzureManagedRedis("cache")
    .RunAsContainer(redis => redis.WithRedisInsight());

cm.WithReference(cache.Cache).WaitFor(cache.Cache);
cd.WithReference(cache.Cache).WaitFor(cache.Cache);
```

One shared cache connects CM and CD instances.
