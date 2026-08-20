using System.ComponentModel.DataAnnotations;

namespace Casko.DefaultsForUmbraco.EmailQueue.Worker.Models;

public sealed class EmailWorkerOptions
{
    public const string SectionName = "EmailWorker";

    [Range(1, 50)]
    public int BatchSize { get; init; } = 50;

    [Range(1, 60)]
    public int ReceiveWaitSeconds { get; init; } = 5;

    [Range(1, 50)]
    public int MaxConcurrency { get; init; } = 8;
}
