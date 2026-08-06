namespace Casko.DefaultsForUmbraco.Package.Search.Models;

public sealed record SearchParameters(
    string? Query,
    string? Culture,
    int Skip,
    int Take,
    IReadOnlyCollection<string> Tags,
    IReadOnlyCollection<int> CreateYears,
    IReadOnlyCollection<string> DocumentTypes);
