using System.Globalization;
using Casko.DefaultsForUmbraco.Search.Configuration;
using Casko.DefaultsForUmbraco.Search.Models;
using Examine;
using Examine.Search;
using Umbraco.Cms.Search.Core.Extensions;
using Umbraco.Cms.Search.Core.Models.Indexing;
using Umbraco.Cms.Search.Core.Models.Searching;
using Umbraco.Cms.Search.Core.Models.Searching.Faceting;
using Umbraco.Cms.Search.Core.Models.Searching.Filtering;
using Umbraco.Cms.Search.Core.Services;
using Umbraco.Cms.Search.Provider.Examine.Helpers;
using Umbraco.Cms.Search.Provider.Examine.Services;
using SearchCoreConstants = Umbraco.Cms.Search.Core.Constants;

namespace Casko.DefaultsForUmbraco.Search.Services;

internal sealed class SearchService(
    ISearcherResolver searcherResolver,
    IExamineManager examineManager,
    IActiveIndexManager activeIndexManager) : ISearchService
{
    private const int UrlMapPageSize = 1000;

    private static readonly IReadOnlyDictionary<string, string> FacetFieldNames = new Dictionary<string, string>
    {
        [Constants.TagsFieldName] = Constants.Tags,
        [Constants.CreateYearFieldName] = Constants.CreateYearFieldName,
        [Constants.DocumentTypeFieldName] = Constants.DocumentTypeFieldName
    };



    /// <inheritdoc />
    public async Task<SearchResponse> SearchAsync(SearchParameters parameters)
    {
        var searcher = searcherResolver.GetRequiredSearcher(SearchCoreConstants.IndexAliases.PublishedContent);

        var result = await searcher.SearchAsync(
            indexAlias: SearchCoreConstants.IndexAliases.PublishedContent,
            query: parameters.Query ?? string.Empty,
            filters: CreateFilters(parameters),
            facets:
            [
                new KeywordFacet(Constants.TagsFieldName),
                new IntegerExactFacet(Constants.CreateYearFieldName),
                new KeywordFacet(Constants.DocumentTypeFieldName)
            ],
            sorters: [],
            culture: parameters.Culture ?? string.Empty,
            segment: string.Empty,
            accessContext: new AccessContext(Guid.Empty, []),
            skip: parameters.Skip,
            take: parameters.Take,
            maxSuggestions: 0);

        return new SearchResponse(
            result.Total,
            result.Documents.Select(document => new SearchItemResponse(document.Id, document.ObjectType)).ToArray(),
            result.Facets.Select(MapFacet).ToArray(),
            result.Suggestions?.ToArray() ?? []);
    }

    /// <inheritdoc />
    public Task<List<string>> BuildUrlMap(Guid key)
    {
        var indexName = activeIndexManager.ResolveActiveIndexName(SearchCoreConstants.IndexAliases.PublishedContent);
        if (examineManager.TryGetIndex(indexName, out var index) is false)
        {
            return Task.FromResult(new List<string>());
        }

        List<string> urls = [];
        var skip = 0;
        long total;

        do
        {
            var results = index.Searcher
                .CreateQuery()
                .Field(FieldNameHelper.QueryableKeywordFieldName(SearchCoreConstants.FieldNames.Id), key.ToString("D"))
                .Execute(QueryOptions.SkipTake(skip, UrlMapPageSize));

            total = results.TotalItemCount;
            urls.AddRange(results.SelectMany(GetUrlPathValues));
            skip += UrlMapPageSize;
        }
        while (skip < total);

        return Task.FromResult(urls.Distinct(StringComparer.OrdinalIgnoreCase).ToList());
    }

    private static IEnumerable<Filter> CreateFilters(SearchParameters parameters)
    {
        if (parameters.Tags.Count > 0)
        {
            yield return new KeywordFilter(Constants.TagsFieldName, parameters.Tags.ToArray(), false);
        }

        if (parameters.CreateYears.Count > 0)
        {
            yield return new IntegerExactFilter(Constants.CreateYearFieldName, parameters.CreateYears.ToArray(), false);
        }

        if (parameters.DocumentTypes.Count > 0)
        {
            yield return new KeywordFilter(Constants.DocumentTypeFieldName, parameters.DocumentTypes.ToArray(), false);
        }
    }

    private static SearchFacetResponse MapFacet(FacetResult facet)
        => new(
            FacetFieldNames.GetValueOrDefault(facet.FieldName, facet.FieldName),
            facet.Values.Select(MapFacetValue).ToArray());

    private static SearchFacetValueResponse MapFacetValue(FacetValue facetValue)
        => facetValue switch
        {
            KeywordFacetValue keywordFacetValue => new(keywordFacetValue.Key, keywordFacetValue.Count),
            IntegerExactFacetValue integerFacetValue => new(integerFacetValue.Key.ToString(CultureInfo.InvariantCulture), integerFacetValue.Count),
            DecimalExactFacetValue decimalFacetValue => new(decimalFacetValue.Key.ToString(CultureInfo.InvariantCulture), decimalFacetValue.Count),
            DateTimeOffsetExactFacetValue dateTimeOffsetFacetValue => new(dateTimeOffsetFacetValue.Key.ToString("O", CultureInfo.InvariantCulture), dateTimeOffsetFacetValue.Count),
            RangeFacetValue<int> rangeFacetValue => new(rangeFacetValue.Key, rangeFacetValue.Count),
            RangeFacetValue<decimal> rangeFacetValue => new(rangeFacetValue.Key, rangeFacetValue.Count),
            RangeFacetValue<DateTimeOffset> rangeFacetValue => new(rangeFacetValue.Key, rangeFacetValue.Count),
            _ => new(string.Empty, facetValue.Count)
        };

    private static IEnumerable<string> GetUrlPathValues(ISearchResult result)
        => result.AllValues
            .Where(field => IsUrlPathField(field.Key))
            .SelectMany(field => field.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value));

    private static bool IsUrlPathField(string fieldName)
        => fieldName == Constants.UrlPath
           || fieldName == FieldNameHelper.QueryableKeywordFieldName(Constants.UrlPath)
           || fieldName.StartsWith(FieldNameHelper.FieldName(Constants.UrlPath, nameof(IndexValue.Keywords)), StringComparison.Ordinal);
}
