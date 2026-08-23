# Casko Defaults for Umbraco

## One content platform. Two purposeful experiences.

Casko Defaults for Umbraco is a ready-to-run local environment for teams building dependable Umbraco websites. It uses .NET Aspire to coordinate a dedicated content-management site, two public-site instances, and the local services they share.

Editors work in a focused backoffice while visitors use the public site through a single local entry point. SQL, Redis, blob storage, and SMTP are provisioned alongside the sites, so the development topology is visible, repeatable, and close to a scalable delivery setup.

![Casko Defaults for Umbraco architecture](docs/images/casko-defaults-for-umbraco-architecture.png)

See [the architecture diagrams](DIAGRAMS.md) for the logical flow, server roles, routing, and shared services.

### Built for a confident publishing flow

- **A focused editor experience** — manage content in Umbraco without exposing the backoffice to website visitors.
- **A resilient public site** — run two public-site instances locally to reflect a scalable delivery setup.
- **Everything visible in one place** — the Aspire dashboard starts the environment and shows its health, activity, SMTP, and supporting services.

Start here:

- [Install the local environment](INSTALL.md)
- [Run the environment](RUN.md)
- [Working agreement for contributors and coding agents](AGENTS.md)
