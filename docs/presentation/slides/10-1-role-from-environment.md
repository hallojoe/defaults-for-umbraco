# The environment assigns the role

The role is supplied by Aspire, rather than hidden in application code.

```csharp
.WithEnvironment("UMBRACO_SERVER_ROLE", "SchedulingPublisher") // CM
.WithEnvironment("UMBRACO_SERVER_ROLE", "Subscriber")           // CD
```
