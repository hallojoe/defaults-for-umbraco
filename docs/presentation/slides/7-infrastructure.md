# What does the environment need?

- SQL database
- Distributed cache: Redis or SQL
- Shared media storage: Blob Storage
- Reverse proxy / local virtual network
- SMTP server
- Persistent infrastructure volumes

Everything should be startable as one development environment.

Concrete local choices: Azure SQL Edge, Azurite, Redis or SQL cache, and Mailpit.
