### C# solution structure

```text

src/
  EmailQueue.Api/
    Controllers/
    Contracts/
    Infrastructure/
      BlobStorage/
      ServiceBus/
    Program.cs

  EmailQueue.Worker/
    Infrastructure/
      BlobStorage/
      ServiceBus/
      Smtp/
    Models/
    Services/
    Program.cs
```

EmailQueue.Api handles the HTTP endpoint, stores payloads in Azurite, and publishes messages to Service Bus.

EmailQueue.Worker is the standalone worker/function project. It consumes Service Bus messages, reads payloads from Azurite, sends via SMTP, and handles completion/retry/dead-lettering.

To avoid a shared third project, define the Service Bus message contract independently in both projects (or deserialize it as an internal transport DTO). Treat the serialized JSON message schema as the contract between the two applications. This also keeps the two deployables genuinely independent.

### Responsibilities

**EmailQueue.Api**
* ASP.NET Core Web API.
* Expose `POST /emails`.
* Validate request.
* Persist large payloads / attachments / template data to Azurite Blob Storage.
* Publish an `EmailRequested` message to Service Bus.
* Return `202 Accepted`.

**EmailQueue.Worker**

* .NET Worker Service.
* Consume messages from `outbound-email`.
* Receive in batches, e.g. max 50 messages or max 5 seconds wait.
* Load referenced payloads from Azurite.
* Send emails through SMTP.
* Limit SMTP concurrency.
* Complete successful messages.
* Abandon transient failures for retry.
* Explicitly dead-letter permanent failures.

**EmailQueue.Contracts**

* Shared DTOs/messages only.
* Example:

```csharp
public sealed record EmailRequested(
    Guid EmailId,
    string To,
    string Subject,
    string? BodyBlobName);
```

**EmailQueue.Infrastructure**

* Service Bus client/receiver/sender abstractions.
* Blob storage access.
* SMTP email sender.
* Dependency injection registrations.
* Shared configuration models.

### Service Bus setup

Create queue:

```text
outbound-email
```

Configure approximately:

```text
MaxDeliveryCount = 5
LockDuration = appropriate for email processing
DeadLetteringOnMessageExpiration = true
```

Worker behavior:

```text
Success
  -> Complete

Transient SMTP/network error
  -> Abandon
  -> Service Bus retries

Invalid request / missing template / invalid recipient
  -> DeadLetter

Repeated unexpected exception
  -> eventually DLQ after MaxDeliveryCount
```

### Batch behavior

Worker loop:

```text
Receive up to 50 messages
or wait up to 5 seconds
        ↓
Load required blobs
        ↓
Send emails with bounded concurrency
        ↓
Settle each Service Bus message individually
```

Do not require exactly 50 messages before processing.

### Dead-letter handling

Add a second worker later:

```text
EmailQueue.DeadLetterWorker/
```

Its responsibility:

* Read `outbound-email/$DeadLetterQueue`.
* Log dead-letter reason and description.
* Persist useful diagnostic information.
* Do not automatically replay messages.
* Provide a small command/admin endpoint later for explicit replay.

### Local dependencies

Use containers/dev services for:

```text
Azurite
Service Bus emulator
SMTP server
```

Configuration through `appsettings.Development.json` / environment variables:

```text
ServiceBus__ConnectionString
BlobStorage__ConnectionString
Smtp__Host
Smtp__Port
EmailWorker__BatchSize = 50
EmailWorker__BatchWaitSeconds = 5
EmailWorker__MaxConcurrency = 10
```

### Implementation order for Codex

1. Create solution and four projects.
2. Add project references and NuGet dependencies.
3. Define `EmailRequested` contract.
4. Implement Service Bus publisher.
5. Implement `POST /emails`.
6. Implement Azurite blob repository.
7. Implement SMTP sender.
8. Implement Worker batch receive loop.
9. Add message settlement and error classification.
10. Configure retries / DLQ.
11. Add structured logging with `EmailId` and Service Bus `MessageId`.
12. Add integration tests covering:

* successful email;
* batch of multiple emails;
* transient SMTP failure/retry;
* permanent failure/dead-letter;
* missing blob;
* duplicate message/idempotency.

13. Add Docker Compose/configuration for all local bricks.
14. Add README with architecture and test scenarios.

A good constraint for Codex is: **keep Azure SDK code inside `Infrastructure`; keep API and Worker focused on orchestration; make every message idempotent using `EmailId`.**
