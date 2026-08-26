# Casko Defaults for Umbraco

## One content platform. Two purposeful experiences.

Casko Defaults for Umbraco is a ready-to-run local environment for teams building dependable Umbraco websites. It uses .NET Aspire to coordinate a dedicated content-management site, two public-site instances, and the local services they share.

Editors work in a focused backoffice while visitors use the public site through a single local entry point. SQL, Redis, blob storage, and SMTP are provisioned alongside the sites, so the development topology is visible, repeatable, and close to a scalable delivery setup.

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
s
Start here:

- [Install the local environment](INSTALL.md)
- [Run the environment](RUN.md)
- [Working agreement for contributors and coding agents](AGENTS.md)

See [the architecture diagrams](DIAGRAMS.md) for the logical flow, server roles, routing, and shared services.
