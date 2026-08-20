using System.ComponentModel.DataAnnotations;

namespace Casko.DefaultsForUmbraco.EmailQueue.Api.Models;

public sealed class EmailQueueOptions
{
    public const string SectionName = "EmailQueue";

    [Required]
    public string BlobContainerName { get; init; } = "emails";

    [Required]
    public string QueueName { get; init; } = "outbound-email";
}
