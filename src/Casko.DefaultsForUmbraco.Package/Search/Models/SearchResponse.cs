namespace Casko.DefaultsForUmbraco.Package.Search.Models;

public sealed record SearchResponse(
    long Total,
    IReadOnlyCollection<SearchItemResponse> Items,
    IReadOnlyCollection<SearchFacetResponse> Facets,
    IReadOnlyCollection<string> Suggestions);
