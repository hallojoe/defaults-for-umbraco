using Casko.DefaultsForUmbraco.Search.Configuration;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Search.Core.Models.Indexing;
using Umbraco.Cms.Search.Core.Services.ContentIndexing;

namespace Casko.DefaultsForUmbraco.Search.Content;

internal sealed class DocumentTypeContentIndexer : IContentIndexer
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

        var fields = new List<IndexField>();
        var effectiveCultures = cultures.Length > 0 ? cultures : [null];

        foreach (var culture in effectiveCultures)
        {
            var value = new IndexValue
            {
                Keywords = [content.ContentType.Alias]
            };

            fields.Add(new IndexField(Constants.DocumentTypeFieldName, value, culture!, null!));
        }

        return Task.FromResult<IEnumerable<IndexField>>(fields);
    }
}
