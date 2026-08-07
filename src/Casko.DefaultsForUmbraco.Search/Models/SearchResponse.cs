namespace Casko.DefaultsForUmbraco.Search.Models;

public sealed record SearchResponse(
    long Total,
    IReadOnlyCollection<SearchItemResponse> Items,
    IReadOnlyCollection<SearchFacetResponse> Facets,
    IReadOnlyCollection<string> Suggestions);
