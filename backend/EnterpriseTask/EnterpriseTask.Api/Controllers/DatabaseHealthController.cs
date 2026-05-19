using EnterpriseTask.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseTask.Api.Controllers;

[ApiController]
[Route("api/health/database")]
public sealed class DatabaseHealthController(ApplicationDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);

        return canConnect
            ? Ok(new { status = "Healthy" })
            : StatusCode(StatusCodes.Status503ServiceUnavailable, new { status = "Unhealthy" });
    }
}
