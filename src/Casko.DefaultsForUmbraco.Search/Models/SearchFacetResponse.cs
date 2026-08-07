namespace Casko.DefaultsForUmbraco.Search.Models;

public sealed record SearchFacetResponse(
    string Name,
    IReadOnlyCollection<SearchFacetValueResponse> Values);
