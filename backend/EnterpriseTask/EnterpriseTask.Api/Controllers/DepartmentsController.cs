using EnterpriseTask.Application.Departments;
using EnterpriseTask.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EnterpriseTask.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicyNames.AuthenticatedUser)]
[Route("api/departments")]
public sealed class DepartmentsController(
    IDepartmentQueries departmentQueries,
    IDepartmentAdministrationCommands departmentCommands,
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

    [Authorize(Policy = AuthorizationPolicyNames.AdminOnly)]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DepartmentListItemDto>>> GetList(
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        return Ok(await departmentQueries.GetListAsync(includeInactive, cancellationToken));
    }

    [Authorize(Policy = AuthorizationPolicyNames.AdminOnly)]
    [HttpGet("tree")]
    public async Task<ActionResult<IReadOnlyList<DepartmentTreeNodeDto>>> GetTree(
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        return Ok(await departmentQueries.GetTreeAsync(includeInactive, cancellationToken));
    }

    [Authorize(Policy = AuthorizationPolicyNames.AdminOnly)]
    [HttpPost]
    [EnableRateLimiting("ApiMutation")]
    public async Task<IActionResult> Create(
        DepartmentCreateRequest request,
        CancellationToken cancellationToken)
    {
        var validationProblem = ValidateDepartmentName(request.Name);
        if (validationProblem is not null)
        {
            return validationProblem;
        }

        var result = await departmentCommands.CreateAsync(request, cancellationToken);
        return result.Result == DepartmentAdministrationResult.Success
            ? CreatedAtAction(nameof(GetList), new { includeInactive = true }, new { id = result.DepartmentId })
            : ToActionResult(result.Result);
    }

    [Authorize(Policy = AuthorizationPolicyNames.AdminOnly)]
    [HttpPut("{id:long}")]
    [EnableRateLimiting("ApiMutation")]
    public async Task<IActionResult> Update(
        long id,
        DepartmentUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var validationProblem = ValidateDepartmentName(request.Name);
        if (validationProblem is not null)
        {
            return validationProblem;
        }

        return ToActionResult((await departmentCommands.UpdateAsync(id, request, cancellationToken)).Result);
    }

    [Authorize(Policy = AuthorizationPolicyNames.AdminOnly)]
    [HttpPut("{id:long}/manager")]
    [EnableRateLimiting("ApiMutation")]
    public async Task<IActionResult> AssignManager(
        long id,
        DepartmentManagerAssignmentRequest request,
        CancellationToken cancellationToken)
    {
        return ToActionResult((await departmentCommands.AssignManagerAsync(id, request, cancellationToken)).Result);
    }

    [Authorize(Policy = AuthorizationPolicyNames.AdminOnly)]
    [HttpPost("{id:long}/deactivate")]
    [EnableRateLimiting("ApiMutation")]
    public async Task<IActionResult> Deactivate(long id, CancellationToken cancellationToken)
    {
        return ToActionResult((await departmentCommands.DeactivateAsync(id, cancellationToken)).Result);
    }

    private IActionResult? ValidateDepartmentName(string name)
    {
        if (!string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
        {
            ["name"] = ["Department name is required."]
        }));
    }

    private IActionResult ToActionResult(DepartmentAdministrationResult result)
    {
        return result switch
        {
            DepartmentAdministrationResult.Success => NoContent(),
            DepartmentAdministrationResult.NotFound => NotFound(),
            DepartmentAdministrationResult.CompanyNotFound => Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Company not found",
                detail: "The requested company was not found."),
            DepartmentAdministrationResult.ParentNotFound => Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Parent department not found",
                detail: "The requested active parent department was not found in the same company."),
            DepartmentAdministrationResult.ManagerNotFound => Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Manager not found",
                detail: "The requested active manager profile was not found."),
            DepartmentAdministrationResult.SelfParentDenied => Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Department hierarchy conflict",
                detail: "A department cannot be its own parent."),
            DepartmentAdministrationResult.CycleDenied => Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Department hierarchy conflict",
                detail: "The requested parent would create a department hierarchy cycle."),
            DepartmentAdministrationResult.ActiveTasksDenied => Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Department deactivation conflict",
                detail: "A department with active tasks cannot be deactivated."),
            DepartmentAdministrationResult.ActiveChildrenDenied => Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Department deactivation conflict",
                detail: "A department with active child departments cannot be deactivated."),
            DepartmentAdministrationResult.DuplicateCodeOrName => Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Department duplicate",
                detail: "A department with the same code or name already exists in the company."),
            _ => Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Unexpected department administration result.")
        };
    }
}
