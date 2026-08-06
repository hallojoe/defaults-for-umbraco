namespace Casko.DefaultsForUmbraco.Search.Models;

public sealed record SearchFacetValueResponse(
    string Value,
    long Count);
