# Infrastructure is only half the setup

Running the containers is the easy part.

Umbraco still needs to understand the topology it is running in.

We need to configure:

- CM and CD URLs
- BackOfficeHost
- UmbracoApplicationUrl
- Forwarded headers / reverse proxy behaviour

These settings make generated URLs and the backoffice behave as if the proxy were the public entry point.
