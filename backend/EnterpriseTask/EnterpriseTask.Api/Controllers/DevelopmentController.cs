using EnterpriseTask.Application.Development;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseTask.Api.Controllers;

[ApiController]
[Route("api/dev")]
public sealed class DevelopmentController(IDatabaseSeeder databaseSeeder, IWebHostEnvironment environment) : ControllerBase
{
    [HttpPost("seed")]
    public async Task<IActionResult> Seed(CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment())
        {
            return NotFound();
        }

        try
        {
            await databaseSeeder.SeedAsync(cancellationToken);
        }
        catch (NotSupportedException exception)
        {
            return BadRequest(new { message = exception.Message });
        }

        return Ok(new
        {
            message = "Development seed data has been applied.",
            accounts = Array.Empty<string>()
        });
    }
}
