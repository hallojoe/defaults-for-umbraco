using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Casko.DefaultsForUmbraco.Functions.Test;

public class EchoFunction
{
    [Function(nameof(Echo))]
    public IActionResult Echo(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "echo")] HttpRequest request)
    {
        var name = request.Query["name"].ToString();

        return new OkObjectResult(new
        {
            Status = "OK",
            Name = string.IsNullOrWhiteSpace(name) ? null : name,
            Timestamp = DateTimeOffset.UtcNow
        });
    }
}
