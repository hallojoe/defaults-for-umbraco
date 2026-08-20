# NemLog-in 3 IT-system options

This note tracks the viable ways to run NemLog-in 3 for Umbraco members and backoffice users.

## Current safe bet: two IT-systems

Use one NemLog-in IT-system for delivery/member login and one for backoffice/user login.

| Purpose | Host | Entity ID / issuer | ACS |
| ------- | ---- | ------------------ | --- |
| Members | `https://cd.dev.localhost` | `https://cd.dev.localhost` | `/Auth/AssertionConsumerService` |
| Backoffice | `https://cm.dev.localhost` | `https://cm.dev.localhost` | `/Auth/AssertionConsumerService` |

Why this is the safe starting point:

- DevTest4 allows only one `AssertionConsumerService` element per IT-system.
- Each registration has one canonical host and one ACS.
- Member and backoffice login remain operationally separated.
- The callback returns to the same node family that started the login.

Implementation notes:

- `Subscriber` registers member NemLog-in only.
- `SchedulingPublisher` registers backoffice NemLog-in only.
- `Single` registers both for local all-in-one development.
- RelayState uses `IDistributedCache`, with an in-memory fallback only when no distributed cache is registered.
- The scheme is included in RelayState so a shared path can still be dispatched to the initiating Umbraco provider.
- If callbacks can land on a different node than the challenge, ASP.NET Data Protection keys must also be shared because the cached RelayState value is protected.

Metadata to upload:

- For members, upload metadata generated from `https://cd.dev.localhost/Metadata`.
- For backoffice, upload metadata generated from `https://cm.dev.localhost/Metadata`.
- Each uploaded metadata document must contain exactly one ACS: `/Auth/AssertionConsumerService`.

## Option 2: one IT-system with distributed RelayState

Use one NemLog-in IT-system and one canonical callback host for both member and backoffice login.

Example:

| Purpose | Host |
| ------- | ---- |
| Canonical NemLog-in callback | `https://cm.dev.localhost/Auth/AssertionConsumerService` |
| Member frontend | `https://cd.dev.localhost` |
| Backoffice | `https://cm.dev.localhost/umbraco/` |

Required work before choosing this:

- Always start or proxy NemLog-in challenges through the canonical callback host, or store enough state for the callback host to complete the initiating flow.
- Keep RelayState in shared distributed cache.
- Share ASP.NET Data Protection keys across CM/CD nodes.
- Scope correlation cookies to a shared parent domain, for example `.dev.localhost`.
- Validate return URLs across the allowed CM/CD hosts.
- Decide whether member sign-in cookies should be issued by CM, CD, or a dedicated auth endpoint.

Pros:

- Only one NemLog-in IT-system to administer.
- One certificate/issuer/metadata registration.

Cons:

- More moving parts in the application.
- Cross-host cookie and redirect behavior must be designed carefully.
- Harder to reason about failures because one host brokers login for another.

## Option 3: one IT-system while CM and CD share one hostname

This is the temporary development shape where both backoffice and delivery run on `https://samlcasko0001.dev.localhost`.

It works with one IT-system because there is one host and one ACS:

```text
https://samlcasko0001.dev.localhost/Auth/AssertionConsumerService
```

This does not prove that the same setup will work unchanged after splitting CM and CD onto different hostnames.

## Decision

Start with two IT-systems. Keep the distributed RelayState implementation because it reduces fragility now and keeps option 2 open later.
