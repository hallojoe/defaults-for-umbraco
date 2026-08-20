# Codex Plan: Add a Local SMTP Server to the .NET Aspire Setup

Implement a development-only SMTP server in the existing .NET Aspire solution using **Mailpit**.

## Goal

Add Mailpit to the Aspire `AppHost` so local development gets:

- an SMTP server for the application to send emails through
- a browser-based Mailpit UI for inspecting captured emails
- Aspire-managed endpoints and configuration
- no hardcoded host ports in the application
- no impact on non-development/production environments

## Tasks

1. Inspect the existing Aspire `AppHost` setup and identify:
   - the `DistributedApplication` entry point
   - the ASP.NET project(s) that need to send email
   - any existing email configuration/options
   - the current email implementation, such as MailKit, `SmtpClient`, FluentEmail, or a custom service

2. Add a Mailpit container resource to the Aspire `AppHost` using:

   - image: `axllent/mailpit`
   - SMTP container port: `1025`
   - Mailpit web UI container port: `8025`

3. Let Aspire dynamically allocate host ports. Do not require fixed host ports unless the existing project structure makes that necessary.

4. Name the endpoints clearly, for example:
   - `smtp`
   - `ui`

5. Add a reference from each application that sends email to the Mailpit resource.

6. Pass the SMTP connection details into the application through Aspire configuration/service discovery rather than hardcoding:

   ```text
   localhost:1025
   ```

7. Update the application's email configuration so local development uses Mailpit with:
   - no TLS
   - no authentication
   - the SMTP host and port supplied by Aspire

8. Reuse the existing email abstraction and configuration model where possible. Do not introduce a second email-sending implementation unless necessary.

9. If the application currently has settings such as:

   ```json
   {
     "Email": {
       "Host": "",
       "Port": 0
     }
   }
   ```

   integrate Aspire-provided values into that existing options model rather than bypassing it.

10. Keep production behavior unchanged. Mailpit must only exist as part of the local Aspire development topology.

## Desired AppHost Shape

Aim for something conceptually similar to:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var mailpit = builder
    .AddContainer("mailpit", "axllent/mailpit")
    .WithEndpoint(
        targetPort: 1025,
        name: "smtp")
    .WithHttpEndpoint(
        targetPort: 8025,
        name: "ui");

builder
    .AddProject<Projects.MyApplication>("application")
    .WithReference(mailpit);

builder.Build().Run();
```

Adapt this to the APIs and conventions already used by the repository.

## Implementation Requirements

- Follow existing code style and project conventions.
- Prefer strongly typed options for SMTP configuration.
- Avoid magic strings where the repository already has configuration constants.
- Preserve existing comments.
- Do not refactor unrelated code.
- Ensure resources are disposed correctly if the SMTP implementation uses disposable clients.
- Respect cancellation tokens in async email operations.
- Keep the change small and development-focused.

## Validation

After implementation, verify that:

1. The Aspire AppHost starts successfully.
2. Mailpit appears as a resource in the Aspire dashboard.
3. The Mailpit UI endpoint is clickable from the Aspire dashboard.
4. The application can resolve the Mailpit SMTP endpoint.
5. Sending a test email succeeds.
6. The email appears in the Mailpit web UI.
7. Restarting Aspire with dynamically assigned ports still works without changing application configuration.
8. Existing production/staging SMTP configuration remains unaffected.

## Deliverables

After making the changes, provide:

- a summary of the files changed
- a short explanation of how SMTP configuration flows from Aspire into the application
- any assumptions made
- instructions for testing an email locally
- any package changes, if required