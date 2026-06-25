using EnterpriseTask.Application.Development;
using EnterpriseTask.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseTask.Api.Controllers;

[ApiController]
[Route("api/dev")]
public sealed class DevelopmentController(
    IDatabaseSeeder databaseSeeder,
    IDatabaseMigrator databaseMigrator,
    IWebHostEnvironment environment) : ControllerBase
{
    [HttpPost("migrate")]
    public async Task<IActionResult> Migrate(CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment())
        {
            return NotFound();
        }

        var result = await databaseMigrator.ApplyAsync(cancellationToken);
        if (!result.IsConfigured || !result.CanConnect)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, result);
        }

        return Ok(result);
    }

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
            message = "Development seed is intentionally a no-op. Apply migrations, create users in Supabase Auth, then assign roles in public.user_roles.",
            accounts = Array.Empty<string>()
        });
    }
}
