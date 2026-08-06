using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Search.BackOffice.DependencyInjection;
using Umbraco.Cms.Search.Core.DependencyInjection;
using Umbraco.Cms.Search.DeliveryApi.DependencyInjection;
using Umbraco.Cms.Search.Provider.Examine.DependencyInjection;

namespace Casko.DefaultsForUmbraco.Package.Search.Configuration;

public sealed class Composer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        // builder.DisableDefaultExamineIndexes();
        
        // add core services for search abstractions
        builder.AddSearchCore()
            .AddExamineSearchProvider()
            .AddBackOfficeSearch()
            .AddDeliveryApiSearch();

        builder.AddSearchServices()
            .ConfigureExamineSearchProvider()
            .ConfigureExamineSearchProviderJsonOptions();

    }
}
