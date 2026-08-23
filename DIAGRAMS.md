# Casko Defaults for Umbraco diagrams

These diagrams describe the local Aspire environment and the relationship between its Umbraco server roles and shared services.

## Publishing flow

```mermaid
flowchart LR
    Editors[Content editors] --> CM[Content management]
    Visitors[Website visitors] --> CD[Public website]
    CM --> Shared[Shared content, media, cache and database]
    CD --> Shared
```

## Server roles and routing

```mermaid
flowchart TB
    AppHost[.NET Aspire AppHost]

    subgraph Sites[Umbraco servers]
        CM[Content management]
        CD1[Public website 1]
        CD2[Public website 2]
        Yarp[Local traffic entry point]
    end

    AppHost --> CM
    AppHost --> CD1
    AppHost --> CD2
    AppHost --> Yarp
    Yarp --> CM
    Yarp --> CD1
    Yarp --> CD2
```

## Shared services

```mermaid
flowchart LR
    CM[Content management]
    Delivery[Public website servers]
    SQL[(SQL database)]
    Cache[(Redis cache)]
    Storage[Blob storage]
    Mailpit[SMTP]
    DbGate[Database viewer]
    RedisInsight[Cache viewer]

    CM --> SQL
    Delivery --> SQL
    CM --> Cache
    Delivery --> Cache
    CM --> Storage
    Delivery --> Storage
    CM --> Mailpit
    Delivery --> Mailpit
    DbGate --> SQL
    RedisInsight --> Cache
```
