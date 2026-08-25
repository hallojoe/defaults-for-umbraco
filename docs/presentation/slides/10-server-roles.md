# Umbraco server roles

The instances may run the same application, but they do not have the same responsibility.

CM / Scheduling Publisher

- Backoffice
- Scheduled publishing
- Background work
- The instance allowed to perform publisher responsibilities

CD / Subscriber

- Public delivery
- Receives distributed cache instructions
- Avoids publisher-only responsibilities

The role should be explicit.
