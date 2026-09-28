using Examine;
using Examine.Search;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Examine;
using System.Text.Json.Serialization;


namespace Casko.DefaultsForUmbraco.Automate;


public record CmsUrl(
    string UrlPath, 
    DateTime LastUpdate, 
    string? Hostname, 
    string? Culture, 
    int? Id = null,
    Guid? Key = null);

public class UrlResolverSettings
{
    public const string Key = "XmlSitemaps:Providers:Examine";
    public ushort PageSize { get; set; } = 1000;
}


public interface ICmsUrlService
{
    /// <summary>
    /// Gets all root items in the content tree.
    /// </summary>
    /// <returns></returns>
    public Task<IEnumerable<CmsUrl>> GetUrlsByKeyAsync(Guid key, CancellationToken cancellationToken = default);
    
}

/// <summary>
/// Filters Examine search results before they are converted to sitemap URLs.
/// </summary>
public interface IExamineSitemapSearchResultFilter
{
    /// <summary>
    /// Gets whether the search result should be included in a sitemap.
    /// </summary>
    bool IsIncluded(ISearchResult searchResult);
}
internal sealed class ExamineSitemapSearchResultFilter(
    IOptions<XmlSitemapsOptions> xmlSitemapsOptions) : IExamineSitemapSearchResultFilter
{
    private const string NodeTypeAliasField = "__NodeTypeAlias";

    public bool IsIncluded(ISearchResult searchResult)
    {
        var options = xmlSitemapsOptions.Value;

        if (IsExcludedByProperty(searchResult, options) ||
            IsExcludedByContentTypeAlias(searchResult, options))
        {
            return false;
        }

        return IsIncludedByContentTypeAlias(searchResult, options);
    }

    private static bool IsExcludedByProperty(ISearchResult searchResult, XmlSitemapsOptions options)
    {
        return !string.IsNullOrWhiteSpace(options.ExcludingUrlPropertyAlias) &&
               !string.IsNullOrWhiteSpace(options.ExcludingUrlPropertyValue) &&
               searchResult.Values.TryGetValue(options.ExcludingUrlPropertyAlias, out var indexedValue) &&
               string.Equals(indexedValue, options.ExcludingUrlPropertyValue, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsExcludedByContentTypeAlias(ISearchResult searchResult, XmlSitemapsOptions options)
    {
        return searchResult.Values.TryGetValue(NodeTypeAliasField, out var contentTypeAlias) &&
               options.ExcludedContentTypeAliases.Contains(contentTypeAlias, StringComparer.OrdinalIgnoreCase);
    }

    private static bool IsIncludedByContentTypeAlias(ISearchResult searchResult, XmlSitemapsOptions options)
    {
        return options.IncludedContentTypeAliases.Count == 0 ||
               (searchResult.Values.TryGetValue(NodeTypeAliasField, out var contentTypeAlias) &&
                options.IncludedContentTypeAliases.Contains(contentTypeAlias, StringComparer.OrdinalIgnoreCase));
    }
}
public sealed class ExternalIndexUrlService(
    IOptions<WebRoutingSettings> webRoutingSettings,
    IOptions<XmlSitemapsOptions> xmlSitemapsOptions,
    IExamineSitemapSearchResultFilter searchResultFilter,
    ILanguageService languageService,
    IDomainService domainService,
    IDocumentUrlService documentUrlService,
    IExamineManager examineManager, 
    ILogger<ExternalIndexUrlService> logger) : ICmsUrlService
{
    /// <inheritdoc />
    public async Task<IEnumerable<CmsUrl>> GetUrlsByKeyAsync(Guid key, CancellationToken cancellationToken = default)
    {
        if(!examineManager.TryGetIndex(Umbraco.Cms.Core.Constants.UmbracoIndexes.ExternalIndexName, out var index))
        {
            logger.LogInformation($"Could not find examine index for {Umbraco.Cms.Core.Constants.UmbracoIndexes.ExternalIndexName}");
            return [];
        }

        List<ISearchResult> searchResultList = [];
        
        var skip = 0;
        long total;

        do
        {
         
            var searchResults = index.Searcher
                .CreateQuery(IndexTypes.Content)
                .NativeQuery("+pathKeys:" + key.ToString("D"))
                .Execute(new QueryOptions(skip, 1000));
            
            total = searchResults.TotalItemCount;
            
            logger.LogInformation("Search found {total} nodes", total);
            
            searchResultList.AddRange(searchResults);
            
            skip += 1000;
        }
        while (skip < total);

        var cmsUrls = new List<CmsUrl>();

        var languages = await GetCandidateLanguagesAsync();
        var assignedDomains = (await domainService.GetAssignedDomainsAsync(key, false)).ToArray();
        
        foreach (var searchResult in searchResultList)
        {
            if (!searchResultFilter.IsIncluded(searchResult))
            {
                continue;
            }
            
            if (!Guid.TryParse(searchResult.Values["__Key"], out var contentKey))
            {
                continue;
            }

            if (!int.TryParse(searchResult.Values["__NodeId"], out var contentId))
            {
                continue;
            }

            // if (!searchResult.Values.TryGetValue("__Path", out var path))
            // {
            //     continue;
            // }
            
            if (!long.TryParse(searchResult.Values["updateDate"], out var updateDateAsLong))
            {
                continue;
            }

            var updatedDate = new DateTime(updateDateAsLong);
            
            foreach (var language in languages)
            {
                var updatedDateForCulture = updatedDate;

                if (searchResult.Values.TryGetValue("updateDate_" + language, out var updateDateForCultureValue) &&
                    long.TryParse(updateDateForCultureValue, out var updatedDateAsLongForCulture))
                {
                    updatedDateForCulture = new DateTime(updatedDateAsLongForCulture);
                }

                if (!IsPublishedForCulture(searchResult, language))
                {
                    continue;
                }

                var url = documentUrlService.GetLegacyRouteFormat(contentKey, language, false);

                if (url.Equals("#"))
                {
                    continue;
                }

                var resolvedUrl = ResolveUrl(
                    url, 
                    language, 
                    assignedDomains, 
                    webRoutingSettings.Value.UmbracoApplicationUrl, 
                    true);
                
                cmsUrls.Add(new CmsUrl(
                    resolvedUrl.UrlPath,
                    updatedDateForCulture,
                    resolvedUrl.Hostname,
                    language,
                    contentId,
                    contentKey));
            }            
        }

        return cmsUrls;
    }
    
    internal static ResolvedCmsUrl ResolveUrl(
        string url,
        string? culture,
        IReadOnlyCollection<IDomain> assignedDomains,
        string? fallbackApplicationUrl, 
        bool addTrailingSlash = false)
    {
        if (TryCreateHttpUri(url, out var absoluteUrl))
        {
            return new ResolvedCmsUrl(
                RemoveIdFromLegacyRouteFormat(absoluteUrl.PathAndQuery, addTrailingSlash),
                absoluteUrl.GetLeftPart(UriPartial.Authority));
        }

        var sanitizedUrl = RemoveIdFromLegacyRouteFormat(url, addTrailingSlash);
        var hostname = ResolveHostname(culture, assignedDomains, fallbackApplicationUrl);

        return new ResolvedCmsUrl(sanitizedUrl, hostname);
    }

    internal async Task<string[]> GetCandidateLanguagesAsync()
    {
        var defaultLanguageCode = (await languageService.GetDefaultLanguageAsync())?.IsoCode;
        var languageCodes = (await languageService.GetAllAsync())
            .Select(language => language.IsoCode)
            .Where(languageCode => string.IsNullOrWhiteSpace(languageCode) is false)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (string.IsNullOrWhiteSpace(defaultLanguageCode) is false)
        {
            languageCodes.RemoveAll(languageCode =>
                string.Equals(languageCode, defaultLanguageCode, StringComparison.OrdinalIgnoreCase));
            languageCodes.Insert(0, defaultLanguageCode);
        }

        var includedCultures = xmlSitemapsOptions.Value.IncludedCultures;
        if (includedCultures.Count > 0)
        {
            languageCodes = languageCodes
                .Where(languageCode => includedCultures.Contains(languageCode, StringComparer.OrdinalIgnoreCase))
                .ToList();
        }

        var excludedCultures = xmlSitemapsOptions.Value.ExcludedCultures;
        if (excludedCultures.Count > 0)
        {
            languageCodes = languageCodes
                .Where(languageCode => !excludedCultures.Contains(languageCode, StringComparer.OrdinalIgnoreCase))
                .ToList();
        }

        return languageCodes.ToArray();
    }

    internal static bool IsPublishedForCulture(ISearchResult searchResult, string culture)
    {
        var cultureSpecificField = "__Published_" + culture;
        var publicationField = searchResult.Values.ContainsKey(cultureSpecificField)
            ? cultureSpecificField
            : "__Published";

        return searchResult.Values.TryGetValue(publicationField, out var publishedValue) &&
               string.Equals(publishedValue, "y", StringComparison.OrdinalIgnoreCase);
    }
    
    internal static string RemoveIdFromLegacyRouteFormat(string url, bool addTrailingSlash = false)
    {
        var trimmedUrl = url.TrimStart('/');
        var separatorIndex = trimmedUrl.IndexOf('/');
        var potentialContentIdPart = separatorIndex < 0
            ? trimmedUrl
            : trimmedUrl[..separatorIndex];

        if (!int.TryParse(potentialContentIdPart, out _))
        {
            return url;
        }

        var sanitizedUrl = separatorIndex < 0
            ? "/"
            : "/" + trimmedUrl[(separatorIndex + 1)..];

        if (string.IsNullOrWhiteSpace(sanitizedUrl))
        {
            return "/";
        }

        if (addTrailingSlash is false)
        {
            return sanitizedUrl == "/" ? sanitizedUrl : sanitizedUrl.TrimEnd('/');
        }

        return sanitizedUrl.EndsWith('/') ? sanitizedUrl : sanitizedUrl + '/';
    }

    internal static string? ResolveHostname(
        string? culture,
        IReadOnlyCollection<IDomain> assignedDomains,
        string? fallbackApplicationUrl)
    {
        var domain = assignedDomains
            .OrderBy(domain => domain.SortOrder)
            .FirstOrDefault(domain =>
                string.Equals(domain.LanguageIsoCode, culture, StringComparison.OrdinalIgnoreCase));

        var domainName = domain?.DomainName;

        if (!string.IsNullOrWhiteSpace(domainName))
        {
            return NormalizeHostname(domainName, fallbackApplicationUrl);
        }

        return string.IsNullOrWhiteSpace(fallbackApplicationUrl)
            ? null
            : NormalizeHostname(fallbackApplicationUrl, fallbackApplicationUrl);
    }

    internal static string NormalizeHostname(string hostname, string? fallbackApplicationUrl)
    {
        if (TryCreateHttpUri(hostname, out _))
        {
            return hostname.TrimEnd('/');
        }

        if (hostname.StartsWith('/'))
        {
            var fallbackOrigin = ResolveFallbackOrigin(fallbackApplicationUrl);
            return $"{fallbackOrigin}{hostname}".TrimEnd('/');
        }

        var scheme = ResolveFallbackScheme(fallbackApplicationUrl);
        return $"{scheme}://{hostname.Trim('/')}";
    }

    internal static string ResolveFallbackOrigin(string? fallbackApplicationUrl)
    {
        if (Uri.TryCreate(fallbackApplicationUrl, UriKind.Absolute, out var applicationUri))
        {
            return applicationUri.GetLeftPart(UriPartial.Authority).TrimEnd('/');
        }

        return string.Empty;
    }

    internal static string ResolveFallbackScheme(string? fallbackApplicationUrl)
    {
        if (Uri.TryCreate(fallbackApplicationUrl, UriKind.Absolute, out var applicationUri))
        {
            return applicationUri.Scheme;
        }

        return Uri.UriSchemeHttps;
    }

    private static bool TryCreateHttpUri(string? value, out Uri uri)
    {
        if (Uri.TryCreate(value, UriKind.Absolute, out var parsedUri) &&
            (parsedUri.Scheme == Uri.UriSchemeHttp || parsedUri.Scheme == Uri.UriSchemeHttps))
        {
            uri = parsedUri;
            return true;
        }

        uri = null!;
        return false;
    }
    
}

internal sealed record ResolvedCmsUrl(string UrlPath, string? Hostname);


public enum XmlSitemapsMode
{
    Single,
    Configuration
}

/// <summary>
/// XML sitemaps settings.
/// </summary>
public sealed class XmlSitemapsOptions
{
    private XmlSitemapsMode _mode = XmlSitemapsMode.Single;

    /// <summary>
    /// Key.
    /// </summary>
    public const string Key = "XmlSitemaps";

    /// <summary>
    /// Gets or sets a value indicating whether XML sitemaps are enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether configured sitemaps and sitemap indexes should be exposed as XML rewrite paths.
    /// </summary>
    public bool RewritesEnabled { get; set; }

    /// <summary>
    /// Gets or sets the path for the sitemap.
    /// </summary>
    public List<string> IncludedContentTypeAliases { get; set; } = [];

    /// <summary>
    /// Gets or sets the host name for the sitemap.
    /// </summary>
    public List<string> ExcludedContentTypeAliases { get; set; } = [];

    /// <summary>
    /// Gets or sets the culture for the sitemap.
    /// </summary>
    public List<string> IncludedCultures { get; set; } = [];

    /// <summary>
    /// Gets or sets the culture for the sitemap.
    /// </summary>
    public List<string> ExcludedCultures { get; set; } = [];

    /// <summary>
    /// Gets or sets the property alias whose value can exclude a content URL from generated sitemaps.
    /// </summary>
    public string? ExcludingUrlPropertyAlias { get; set; }

    /// <summary>
    /// Gets or sets the property value that excludes a content URL when found in the configured property.
    /// </summary>
    public string? ExcludingUrlPropertyValue { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to render alternate links for single culture sitemaps.
    /// </summary>
    public bool RenderAlternateLinksForSingleCultureSitemaps { get; set; }
    
    /// <summary>
    /// Gets or sets the mode for resolving root nodes. 0 = Single, 1 = Configuration.
    /// </summary>
    public XmlSitemapsMode Mode
    {
        get => _mode;
        set => _mode = value;
    }

    /// <summary>
    /// Gets or sets the mode for resolving root nodes. 0 = Single, 1 = Configuration.
    /// </summary>
    public XmlSitemapsMode RootNodeMode
    {
        get => Mode;
        set => Mode = value;
    }
    
    /// <summary>
    /// List of document type aliases that represent root nodes. Setting this makes it easier to locate root nodes in the content tree.
    /// </summary>
    public string[] RootContentTypeAliases { get; set; } = [];
    
    /// <summary>
    /// Gets or sets the level where routed root nodes are resolved.
    /// </summary>
    public int RootNodeSearchLevel { get; set; }

    /// <summary>
    /// Dictionary of sitemap configurations keyed by sitemap name.
    /// </summary>
    public Dictionary<string, SitemapOptions> Sitemaps { get; set; } = [];

    /// <summary>
    /// Dictionary of custom sitemap configurations keyed by sitemap name.
    /// </summary>
    public Dictionary<string, CustomSitemapOptions> CustomSitemaps { get; set; } = [];

    /// <summary>
    /// Dictionary of sitemap index configurations keyed by index name.
    /// </summary>
    public Dictionary<string, SitemapIndexOptions> Indexes { get; set; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether to use the Delivery API access policy.
    /// </summary>
    public bool UseDeliveryApiAccessPolicy { get; set; } = true;

    /// <summary>
    /// Gets or sets the name of the index to use as XML sitemaps provider.
    /// </summary>
    public string IndexName { get; set; } = Umbraco.Cms.Core.Constants.UmbracoIndexes.ExternalIndexName;
    
}


/// <summary>
/// Individual sitemap configuration.
/// </summary>
public sealed class SitemapOptions
{
    /// <summary>
    /// Gets or sets the public XML file name for this sitemap, without the .xml extension.
    /// </summary>
    public string? PublicName { get; set; }

    /// <summary>
    /// Gets or sets the path of content to render.
    /// </summary>
    public string? Path { get; set; }

    /// <summary>
    /// Gets or sets the host name from where path should be resolved..
    /// </summary>
    public string? HostName { get; set; }

    /// <summary>
    /// Primary culture of content to render.
    /// </summary>
    public string? Culture { get; set; }

    /// <summary>
    /// List of cultures to render.
    /// </summary>
    public List<string> IncludedCultures { get; set; } = [];

    /// <summary>
    /// List of cultures to exclude.
    /// </summary>
    public List<string> ExcludedCultures { get; set; } = [];

    /// <summary>
    /// List of document types to render.
    /// </summary>
    public List<string> IncludedDocumentTypeAliases { get; set; } = [];

    /// <summary>
    /// List of document types to exclude.
    /// </summary>
    public List<string> ExcludedDocumentTypeAliases { get; set; } = [];
}

/// <summary>
/// Custom sitemap configuration.
/// </summary>
public sealed class CustomSitemapOptions
{
    /// <summary>
    /// Gets or sets the public XML file name for this custom sitemap, without the .xml extension.
    /// </summary>
    public string? PublicName { get; set; }

    /// <summary>
    /// Gets or sets the alias of the custom sitemap provider.
    /// </summary>
    public string? ProviderAlias { get; set; }

    /// <summary>
    /// Gets or sets the host name for this custom XML sitemap.
    /// </summary>
    public string? HostName { get; set; }

    /// <summary>
    /// Gets or sets provider-specific settings.
    /// </summary>
    public Dictionary<string, string?> Settings { get; set; } = [];
}

/// <summary>
/// Sitemap index configuration.
/// </summary>
public sealed class SitemapIndexOptions
{
    /// <summary>
    /// Gets or sets the public XML file name for this sitemap index, without the .xml extension.
    /// </summary>
    public string? PublicName { get; set; }

    /// <summary>
    /// Gets or sets the host name for this XML sitemap index.
    /// </summary>
    public string? HostName { get; set; }

    /// <summary>
    /// Gets or sets the list of XML sitemap keys to include in the index.
    /// </summary>
    public List<string> Sitemaps { get; set; } = [];
}