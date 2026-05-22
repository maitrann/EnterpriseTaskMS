using EnterpriseTask.Application.Departments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseTask.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/departments")]
public sealed class DepartmentsController(IDepartmentQueries departmentQueries) : ControllerBase
{
    [HttpGet("cards")]
    public async Task<ActionResult<IReadOnlyList<DepartmentCardDto>>> GetCards(CancellationToken cancellationToken)
    {
        return Ok(await departmentQueries.GetCardsAsync(this.GetUserScope(), cancellationToken));
    }
}
