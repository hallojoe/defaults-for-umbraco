using Casko.DefaultsForUmbraco.Package.Search.Configuration;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Search.Core.Models.Indexing;
using Umbraco.Cms.Search.Core.Models.Searching;
using Umbraco.Cms.Search.Core.Models.Searching.Filtering;
using Umbraco.Cms.Search.Core.Services;
using Umbraco.Cms.Search.Provider.Examine.Helpers;
using Umbraco.Cms.Search.Provider.Examine.Services;
using SearchCoreConstants = Umbraco.Cms.Search.Core.Constants;

namespace Casko.DefaultsForUmbraco.Package.Search.Services;

public class UrlResolverSettings
{
    public const string Key = "Casko:Search:Url";
    public ushort PageSize { get; set; } = 1000;
}

public sealed class CmsUrlService(
    IOptions<UrlResolverSettings> urlResolverSettings,
    IActiveIndexManager activeIndexManager, 
    ISearcher searcher) : IUrlService
{
    /// <inheritdoc />
    public async Task<List<Document>> BuildUrlMap(Guid key)
    {
        var indexName = activeIndexManager.ResolveActiveIndexName(SearchCoreConstants.IndexAliases.PublishedContent);
        
        List<Document> documents = [];

        var rootDocument = await FindRootResult(key);
        
        if (rootDocument is null)
        {
            return documents;
        }

        //urls.AddRange(GetUrlPathValues(rootResult));

        var skip = 0;
        long total;

        do
        {
            var results = await searcher.SearchAsync(
                indexAlias: "Umb_PublishedContent",
                culture: "da",
                filters:
                [
                    new KeywordFilter(
                        "Umb_PathIds",
                        [key.ToString("D")],
                        false)
                ]);
 
            total = results.Total;

            var urls2 = results.Documents.ToList();

            documents.AddRange(results.Documents);
            
            skip += urlResolverSettings.Value.PageSize;
        }
        while (skip < total);

        return documents;
    }

    private async Task<Document?> FindRootResult( Guid key)
    {
        var keyString = key.ToString("D");
        var result = await searcher.SearchAsync(
            indexAlias: "Umb_PublishedContent",
            // query: keyString,
            culture: "da",
            filters:
            [
                new KeywordFilter(
                    FieldName: "Umb_Id",
                    Values: [keyString],
                    Negate: false
                )
            ], 
            skip:0, 
            take: 1);

        var document = result.Documents.FirstOrDefault();

        return document;
    }


    private static string[] GetIdFieldNames()
        =>
        [
            SearchCoreConstants.FieldNames.Id,
            FieldNameHelper.QueryableKeywordFieldName(SearchCoreConstants.FieldNames.Id),
            FieldNameHelper.FieldName(SearchCoreConstants.FieldNames.Id, nameof(IndexValue.Keywords))
        ];

    private static bool IsUrlPathField(string fieldName)
        => fieldName == Constants.UrlPath
           || fieldName == FieldNameHelper.QueryableKeywordFieldName(Constants.UrlPath)
           || fieldName.StartsWith(FieldNameHelper.FieldName(Constants.UrlPath, nameof(IndexValue.Keywords)), StringComparison.Ordinal);

    private static string[] GetPathIdsFieldNames()
        =>
        [
            SearchCoreConstants.FieldNames.PathIds,
            FieldNameHelper.QueryableKeywordFieldName(SearchCoreConstants.FieldNames.PathIds),
            FieldNameHelper.FieldName(SearchCoreConstants.FieldNames.PathIds, nameof(IndexValue.Keywords)),
            FieldNameHelper.FieldName(SearchCoreConstants.FieldNames.PathIds, nameof(IndexValue.Integers))
        ];

    private static bool IsPathIdsField(string fieldName)
        => GetPathIdsFieldNames().Any(pathIdsFieldName =>
            fieldName == pathIdsFieldName || fieldName.StartsWith($"{pathIdsFieldName}_", StringComparison.Ordinal));
}
