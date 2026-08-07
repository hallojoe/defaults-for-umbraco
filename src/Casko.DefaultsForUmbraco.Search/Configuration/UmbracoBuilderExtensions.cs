using System.Text.Json.Serialization.Metadata;
using Casko.DefaultsForUmbraco.Search.Content;
using Casko.DefaultsForUmbraco.Search.Controllers;
using Casko.DefaultsForUmbraco.Search.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Search.Core.Models.Searching.Faceting;
using Umbraco.Cms.Search.Core.Services.ContentIndexing;
using Umbraco.Cms.Search.Provider.Examine.Configuration;
using Umbraco.Cms.Web.Common.ApplicationBuilder;

namespace Casko.DefaultsForUmbraco.Search.Configuration;
public sealed class DefaultSearchApiConfigureSwaggerGenOptions : IConfigureOptions<SwaggerGenOptions>
{
    public void Configure(SwaggerGenOptions options)
    {
        if (!options.SwaggerGeneratorOptions.SwaggerDocs.ContainsKey("search"))
        {
            options.SwaggerDoc("search", new OpenApiInfo
            {
                Title = "search",
                Version = "1.0"
            });
        }

        options.OperationFilter<DefaultSearchApiHeadersOperationFilter>();
    }
}


public sealed class DefaultSearchApiHeadersOperationFilter : IOperationFilter
{
    private const string ApiGroupName = "default-search";
    private const string ApiKeyHeaderName = "Api-Key";
    private const string CultureHeaderName = "culture";

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (!string.Equals(context.ApiDescription.GroupName, ApiGroupName, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        operation.Parameters ??= [];

        AddHeaderParameter(operation, ApiKeyHeaderName, "Delivery API key.");

        var hasCultureHeader = context.ApiDescription.ParameterDescriptions.Any(parameter =>
            string.Equals(parameter.Name, CultureHeaderName, StringComparison.OrdinalIgnoreCase) &&
            parameter.Source?.Id == "Header");

        if (hasCultureHeader)
        {
            AddHeaderParameter(operation, CultureHeaderName, "Optional culture header used when resolving the sitemap path.");
        }
    }

    private static void AddHeaderParameter(OpenApiOperation operation, string headerName, string description)
    {
        operation.Parameters ??= [];
        var parameters = operation.Parameters;

        var alreadyExists = parameters.Any(parameter =>
            string.Equals(parameter.Name, headerName, StringComparison.OrdinalIgnoreCase) &&
            parameter.In == ParameterLocation.Header);

        if (alreadyExists)
        {
            return;
        }

        parameters.Add(new OpenApiParameter
        {
            Name = headerName,
            In = ParameterLocation.Header,
            Description = description,
            Required = false,
            Schema = new OpenApiSchema
            {
                Type = JsonSchemaType.String
            }
        });
    }
}


public static class UmbracoBuilderExtensions
{
    public static IUmbracoBuilder AddSearchServices(this IUmbracoBuilder builder)
    {
        builder.Services.Configure<UrlResolverSettings>(builder.Config.GetSection(UrlResolverSettings.Key));

        builder.Services.AddSingleton<IUrlService, CmsUrlService>();

        builder.Services.AddSingleton<ISearchService, SearchService>();
        builder.Services.AddSingleton<IContentIndexer, CreateYearContentIndexer>();
        builder.Services.AddSingleton<IContentIndexer, DocumentTypeContentIndexer>();

        builder.Services
            .AddControllers()
            .AddApplicationPart(typeof(Search2Controller).Assembly);

        builder.Services.ConfigureOptions<DefaultSearchApiConfigureSwaggerGenOptions>();
        builder.Services.Configure<UmbracoPipelineOptions>(options =>
        {
            options.AddFilter(new UmbracoPipelineFilter(
                "search-controllers",
                endpoints: app => app.UseEndpoints(endpoints => endpoints.MapControllers())));
        });
        
        return builder;
    }

    public static IUmbracoBuilder ConfigureExamineSearchProvider(this IUmbracoBuilder builder)
    {
        // By default, Examine (Lucene) filters out facet values that are not active (picked) within a facet group,
        // if any facet value is active within that facet group.
        // Expanding facets changes that behavior to include non-active (valid) facet values in the result.
        // NOTE: This incurs a performance penalty when querying. 
        builder.Services.Configure<SearcherOptions>(options => options.ExpandFacetValues = true);

        // the Examine search provider requires explicit definitions of the fields used for faceting and/or sorting. 
        builder.Services.Configure<FieldOptions>(options =>
        {
            List<FieldOptions.Field> fields = options.Fields?.ToList() ?? [];

            UpsertField(fields, Constants.TagsFieldName, FieldValues.Keywords, sortable: false, facetable: true);
            UpsertField(fields, Constants.CreateYearFieldName, FieldValues.Integers, sortable: true, facetable: true);
            UpsertField(fields, Constants.DocumentTypeFieldName, FieldValues.Keywords, sortable: false, facetable: true);
            UpsertField(fields, Constants.UrlName, FieldValues.Keywords, sortable: false, facetable: false);
            UpsertField(fields, Constants.UrlPath, FieldValues.Keywords, sortable: false, facetable: false);

            options.Fields = fields.ToArray();
        });

        return builder;
    }

    public static IUmbracoBuilder ConfigureExamineSearchProviderJsonOptions(this IUmbracoBuilder builder)
    {
        builder.Services.AddControllers().AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.TypeInfoResolver =
                options.JsonSerializerOptions.TypeInfoResolver!.WithAddedModifier(typeInfo =>
                {
                    if (typeInfo.Type != typeof(FacetValue))
                    {
                        return;
                    }

                    // Allow all the search core facet value types to be serialized as implementations of FacetValue
                    typeInfo.PolymorphismOptions = new()
                    {
                        DerivedTypes =
                        {
                            new JsonDerivedType(typeof(IntegerRangeFacetValue)),
                            new JsonDerivedType(typeof(DecimalRangeFacetValue)),
                            new JsonDerivedType(typeof(DateTimeOffsetRangeFacetValue)),
                            new JsonDerivedType(typeof(IntegerExactFacetValue)),
                            new JsonDerivedType(typeof(DecimalExactFacetValue)),
                            new JsonDerivedType(typeof(DateTimeOffsetExactFacetValue)),
                            new JsonDerivedType(typeof(KeywordFacetValue)),
                        }
                    };
                });
        });

        return builder;
    }

    private static void UpsertField(
        List<FieldOptions.Field> fields,
        string propertyName,
        FieldValues fieldValues,
        bool sortable,
        bool facetable)
    {
        var field = fields.FirstOrDefault(item => item.PropertyName == propertyName);
        fields.RemoveAll(item => item.PropertyName == propertyName);
        fields.Add(new()
        {
            PropertyName = propertyName,
            FieldValues = fieldValues,
            Sortable = field?.Sortable == true || sortable,
            Facetable = field?.Facetable == true || facetable,
            Segments = field?.Segments ?? []
        });
    }
}
