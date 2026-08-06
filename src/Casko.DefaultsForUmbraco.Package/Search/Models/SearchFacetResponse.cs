namespace Casko.DefaultsForUmbraco.Package.Search.Models;

public sealed record SearchFacetResponse(
    string Name,
    IReadOnlyCollection<SearchFacetValueResponse> Values);
