# Loadbalanced local Umbraco instances 

Casko Defaults for Umbraco is a ready-to-run local environment for running a single `SchedulingPublisher` and multiple `Subsriber`. It uses .NET Aspire to coordinate a dedicated backoffice site, two delivery instances, and the local resources they share. Concrete local resource choices for this setup is: Azure SQL Edge, Azurite, Redis cache, and Mailpit.


## Get started

- [Install .NET and Docker](INSTALL.md)
- [Setup hostnames](HOSTS.md)
- [Run the environment](RUN.md)

```mermaid
flowchart LR
  B[Browser] --> P[Reverse proxy]
  P -->|cm.dev.localhost| CM[Scheduling Publisher]
  P -->|cd.dev.localhost| CD1[Subscriber 1]
  P -->|cd.dev.localhost| CD2[Subscriber 2]
  CM & CD1 & CD2 --> SQL[SQL server]
  CM & CD1 & CD2 --> CACHE[Distributed cache]
  CM & CD1 & CD2 --> BLOB[Blob Storage]
```

## More

See [the architecture diagrams](DIAGRAMS.md) for the logical flow, server roles, routing, and shared resorces.

See also [Working agreement for contributors and coding agents](AGENTS.md)