using EnterpriseTask.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseTask.Api.Controllers;

[ApiController]
[Route("api/health/database")]
public sealed class DatabaseHealthController(IDatabaseHealthCheck databaseHealthCheck) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var status = await databaseHealthCheck.CheckAsync(cancellationToken);

        var response = new
        {
            status = status.Status,
            configured = status.IsConfigured,
            database = status.CanConnect ? "Connected" : "Unavailable",
            lastAppliedMigration = status.LastAppliedMigration,
            message = status.Message
        };

        return status is { IsConfigured: true, CanConnect: true }
            ? Ok(response)
            : StatusCode(StatusCodes.Status503ServiceUnavailable, response);
    }
}
