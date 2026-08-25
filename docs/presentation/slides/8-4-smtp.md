# Mailpit in the AppHost

```csharp
builder.AddContainer("mailpit", "axllent/mailpit")
    .WithEndpoint(targetPort: 1025, name: "smtp")
    .WithHttpEndpoint(targetPort: 8025, name: "ui");
```

The SMTP endpoint is injected into each Umbraco instance.
