# What are we trying to model?

A simple load-balanced Umbraco setup:

Backoffice / CM  
https://cm.dev.localhost

- Single instance
- Used by editors
- Scales up by adding resources to the machine
- Responsible for management and scheduled work

CM has one focused responsibility: managing content.
