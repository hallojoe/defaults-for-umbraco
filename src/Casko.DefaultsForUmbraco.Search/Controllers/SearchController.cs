using Asp.Versioning;
using Casko.DefaultsForUmbraco.Search.Models;
using Casko.DefaultsForUmbraco.Search.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Umbraco.Cms.Api.Common.Attributes;

namespace Casko.DefaultsForUmbraco.Search.Controllers;

[ApiController]
[ApiVersion("1.0")]
[MapToApi($"default-search")]
[Route("api/default-search")]
public sealed class Search2Controller(ISearchService searchService, IUrlService urlService) : ControllerBase
{
    private const int DefaultTake = 20;
    private const int MaxTake = 100;

    [Produces("application/json")]
    [HttpGet("bykey")]
    public async Task<IActionResult> GetByKey([FromQuery(Name = "key")] Guid key)
    {

        var urls = await urlService.BuildUrlMap(key);

        return Ok(urls);
    }

    [Produces("application/json")]
    [HttpGet("")]
    public async Task<IActionResult> Get(
        [FromQuery(Name = "q")] string? query,
        [FromQuery] string? culture,
        [FromQuery] int skip = 0,
        [FromQuery] int take = DefaultTake)
    {
        if (skip < 0)
        {
            ModelState.AddModelError(nameof(skip), "Skip must be 0 or greater.");
        }

        if (take <= 0)
        {
            ModelState.AddModelError(nameof(take), "Take must be greater than 0.");
        }

        if (take > MaxTake)
        {
            take = MaxTake;
        }

        var tags = GetStringValues("tags");
        var documentTypes = GetStringValues("documentType");
        var createYears = GetIntegerValues("createYear");

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var response = await searchService.SearchAsync(new SearchParameters(
            query,
            culture,
            skip,
            take,
            tags,
            createYears,
            documentTypes));

        return Ok(response);
    }

    private IReadOnlyCollection<string> GetStringValues(string key)
        => SplitQueryValues(Request.Query[key])
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private IReadOnlyCollection<int> GetIntegerValues(string key)
    {
        List<int> values = [];

        foreach (var value in SplitQueryValues(Request.Query[key]))
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            if (int.TryParse(value.Trim(), out var parsedValue))
            {
                values.Add(parsedValue);
                continue;
            }

            ModelState.AddModelError(key, $"'{value}' is not a valid integer.");
        }

        return values.Distinct().ToArray();
    }

    private static IEnumerable<string> SplitQueryValues(StringValues values)
        => values.SelectMany(value => value?.Split(',', StringSplitOptions.TrimEntries) ?? []);
}
