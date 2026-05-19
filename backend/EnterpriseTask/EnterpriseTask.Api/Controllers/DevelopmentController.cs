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

        await databaseSeeder.SeedAsync(cancellationToken);
        return Ok(new
        {
            message = "Development seed data has been applied.",
            accounts = new[] { "admin@etms.local", "chau.hr@etms.local", "tran.dev@etms.local" }
        });
    }
}
