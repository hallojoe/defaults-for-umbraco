using Casko.DefaultsForUmbraco.Package.Search.Configuration;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Umbraco.Cms.Search.Core.Models.Indexing;
using Umbraco.Cms.Search.Core.Services.ContentIndexing;
using Umbraco.Extensions;

namespace Casko.DefaultsForUmbraco.Package.Search.Content;

internal sealed class UrlPathContentIndexer(IDocumentUrlService documentUrlService) : IContentIndexer
{
    public Task<IEnumerable<IndexField>> GetIndexFieldsAsync(
        IContentBase content,
        string?[] cultures,
        bool published,
        CancellationToken cancellationToken)
    {
        if (!published || string.IsNullOrWhiteSpace(content.ContentType.Alias))
        {
            return Task.FromResult(Enumerable.Empty<IndexField>());
        }

        List<IndexField> fields = [];
        IEnumerable<string?> effectiveCultures = cultures.Length > 0 ? cultures : [null];

        foreach (var culture in effectiveCultures)
        {
            // This should get the URL without invoking any content cache things.
            var legacyRouteContentUrl = documentUrlService.GetLegacyRouteFormat(content.Key, culture, false);
            
            // The URL legacyRouteContentUrl will have a format like: 1001/a/b/c
            // We need to remove the first part of the URL, which is the content ID.
            var contentRelativeUrl = legacyRouteContentUrl[(legacyRouteContentUrl.IndexOf('/', 1) + 1)..];
            
            var value = new IndexValue
            {
                Keywords = [contentRelativeUrl]
            };

            fields.Add(new IndexField(Constants.UrlPath, value, culture!, null!));
        }

        return Task.FromResult<IEnumerable<IndexField>>(fields);
    }
}
