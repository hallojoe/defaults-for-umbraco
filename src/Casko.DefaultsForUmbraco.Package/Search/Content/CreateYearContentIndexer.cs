using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Search.Core.Models.Indexing;
using Umbraco.Cms.Search.Core.Services.ContentIndexing;

namespace Casko.DefaultsForUmbraco.Package.Search.Content;

internal sealed class CreateYearContentIndexer : IContentIndexer
{
    private const string CreateYearFieldName = "createYear";
    private const string CreateMonthFieldName = "createMonth";
    private const string CreateDayFieldName = "createDay";
    private const string OverrideCreateDateAlias = "overrideCreateDate";

    public Task<IEnumerable<IndexField>> GetIndexFieldsAsync(
        IContentBase content,
        string?[] cultures,
        bool published,
        CancellationToken cancellationToken)
    {
        if (!published)
        {
            return Task.FromResult(Enumerable.Empty<IndexField>());
        }

        List<IndexField> fields = [];
        IEnumerable<string?> effectiveCultures = cultures.Length > 0 ? cultures : [null];

        foreach (var culture in effectiveCultures)
        {
            var resolvedCreateDate = ResolveCreateDate(content, culture);
            var value = new IndexValue
            {
                Integers = [resolvedCreateDate.Year]
            };

            fields.Add(new IndexField(CreateYearFieldName, value, culture!, null!));
        }

        return Task.FromResult<IEnumerable<IndexField>>(fields);
    }

    private static DateTime ResolveCreateDate(IContentBase content, string? culture)
    {
        if (TryGetOverrideCreateDate(content, culture, out var overrideCreateDate))
        {
            return overrideCreateDate;
        }
        
        return content.CreateDate;
    }

    private static bool TryGetOverrideCreateDate(IContentBase content, string? culture, out DateTime overrideCreateDate)
    {
        overrideCreateDate = default;

        if (!content.Properties.TryGetValue(OverrideCreateDateAlias, out var property))
        {
            return false;
        }

        var value = property.GetValue(culture, segment: null, published: true);
        if (value is null)
        {
            return false;
        }

        switch (value)
        {
            case DateTime dateTime:
                overrideCreateDate = dateTime;
                return true;
            case DateTimeOffset dateTimeOffset:
                overrideCreateDate = dateTimeOffset.DateTime;
                return true;
            case string stringValue when DateTime.TryParse(stringValue, out var parsedDateTime):
                overrideCreateDate = parsedDateTime;
                return true;
            default:
                return false;
        }
    }
}
