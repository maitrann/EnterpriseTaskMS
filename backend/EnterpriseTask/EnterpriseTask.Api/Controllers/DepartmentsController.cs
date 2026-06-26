using EnterpriseTask.Application.Departments;
using EnterpriseTask.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseTask.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicyNames.AuthenticatedUser)]
[Route("api/departments")]
public sealed class DepartmentsController(
    IDepartmentQueries departmentQueries,
    ICurrentUserContext currentUser) : ControllerBase
{
    [HttpGet("cards")]
    public async Task<ActionResult<IReadOnlyList<DepartmentCardDto>>> GetCards(CancellationToken cancellationToken)
    {
        return Ok(await departmentQueries.GetCardsAsync(currentUser.GetRequiredScope(), cancellationToken));
    }

    [HttpGet("options")]
    public async Task<ActionResult<IReadOnlyList<DepartmentOptionDto>>> GetOptions(CancellationToken cancellationToken)
    {
        return Ok(await departmentQueries.GetOptionsAsync(cancellationToken));
    }
}
