using Umbraco.Cms.Core.Models;

namespace Casko.DefaultsForUmbraco.Search.Models;

public sealed record SearchItemResponse(
    Guid Id,
    UmbracoObjectTypes ObjectType);
