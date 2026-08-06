using Umbraco.Cms.Core.Models;

namespace Casko.DefaultsForUmbraco.Package.Search.Models;

public sealed record SearchItemResponse(
    Guid Id,
    UmbracoObjectTypes ObjectType);
