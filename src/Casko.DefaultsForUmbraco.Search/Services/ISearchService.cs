using Casko.DefaultsForUmbraco.Search.Models;
using Umbraco.Cms.Search.Core.Models.Searching;

namespace Casko.DefaultsForUmbraco.Search.Services;

/// <summary>
/// Searches published Umbraco content and returns result documents with configured facet groups.
/// </summary>
public interface ISearchService
{
    /// <summary>
    /// Searches published content using the configured query, paging, filters and facets.
    /// </summary>
    /// <param name="parameters">The search query, paging and facet filter parameters.</param>
    /// <returns>The matching documents, facet groups and suggestions.</returns>
    Task<SearchResponse> SearchAsync(SearchParameters parameters);
    
    /// <summary>
    /// Builds a map of all urls for a given key.
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    Task<List<string>> BuildUrlMap(Guid key);

}

public interface IUrlService
{
    /// <summary>
    /// Builds a map of all urls for a given key.
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    Task<List<Document>> BuildUrlMap(Guid key);
}
