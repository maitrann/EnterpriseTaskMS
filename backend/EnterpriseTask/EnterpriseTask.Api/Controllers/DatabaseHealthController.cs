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
        var canConnect = await databaseHealthCheck.CanConnectAsync(cancellationToken);

        return canConnect
            ? Ok(new { status = "Healthy" })
            : StatusCode(StatusCodes.Status503ServiceUnavailable, new { status = "Unhealthy" });
    }
}
