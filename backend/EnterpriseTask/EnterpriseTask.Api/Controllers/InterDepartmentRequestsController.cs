using EnterpriseTask.Application.Common;
using EnterpriseTask.Application.InterDepartmentRequests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseTask.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/inter-department-requests")]
public sealed class InterDepartmentRequestsController(
    IInterDepartmentRequestQueries requestQueries,
    IInterDepartmentRequestCommands requestCommands,
    ICurrentUserContext currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<InterDepartmentRequestDto>>> Get(CancellationToken cancellationToken)
    {
        return Ok(await requestQueries.GetRequestsAsync(currentUser.GetRequiredScope(), cancellationToken));
    }

    [HttpGet("department-options")]
    public async Task<ActionResult<IReadOnlyList<RequestDepartmentRefDto>>> GetDepartmentOptions(CancellationToken cancellationToken)
    {
        return Ok(await requestQueries.GetDepartmentOptionsAsync(cancellationToken));
    }

    [HttpGet("owner-options")]
    public async Task<ActionResult<IReadOnlyList<RequestOwnerRefDto>>> GetOwnerOptions(CancellationToken cancellationToken)
    {
        return Ok(await requestQueries.GetOwnerOptionsAsync(cancellationToken));
    }

    [HttpGet("sla-policies")]
    public async Task<ActionResult<IReadOnlyList<RequestSlaPolicyDto>>> GetSlaPolicies(CancellationToken cancellationToken)
    {
        return Ok(await requestQueries.GetSlaPoliciesAsync(cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult> Create(CreateInterDepartmentRequestCommand request, CancellationToken cancellationToken)
    {
        var result = await requestCommands.CreateAsync(currentUser.GetRequiredScope(), request, cancellationToken);
        return ToCreateActionResult(result, created: true);
    }

    [HttpPost("{id:guid}/acknowledge")]
    public async Task<IActionResult> Acknowledge(Guid id, CancellationToken cancellationToken)
    {
        return ToActionResult(await requestCommands.AcknowledgeAsync(currentUser.GetRequiredScope(), id, cancellationToken));
    }

    [HttpPost("{id:guid}/assign-owner")]
    public async Task<IActionResult> AssignOwner(Guid id, AssignOwnerRequest request, CancellationToken cancellationToken)
    {
        return ToActionResult(await requestCommands.AssignOwnerAsync(currentUser.GetRequiredScope(), id, request, cancellationToken));
    }

    [HttpPost("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, UpdateRequestStatusRequest request, CancellationToken cancellationToken)
    {
        return ToActionResult(await requestCommands.UpdateStatusAsync(currentUser.GetRequiredScope(), id, request, cancellationToken));
    }

    [HttpPost("{id:guid}/messages")]
    public async Task<ActionResult> AddMessage(Guid id, AddRequestMessageRequest request, CancellationToken cancellationToken)
    {
        var result = await requestCommands.AddMessageAsync(currentUser.GetRequiredScope(), id, request, cancellationToken);
        return ToCreateActionResult(result, created: false);
    }

    [HttpPost("{id:guid}/close")]
    public async Task<IActionResult> Close(Guid id, CancellationToken cancellationToken)
    {
        return ToActionResult(await requestCommands.CloseAsync(currentUser.GetRequiredScope(), id, cancellationToken));
    }

    private IActionResult ToActionResult(InterDepartmentRequestCommandResult result)
    {
        return result switch
        {
            InterDepartmentRequestCommandResult.Success => NoContent(),
            InterDepartmentRequestCommandResult.Forbidden => Forbid(),
            _ => NotFound()
        };
    }

    private ActionResult ToCreateActionResult(InterDepartmentRequestCreateResult result, bool created)
    {
        return result.Result switch
        {
            InterDepartmentRequestCommandResult.Success when created => CreatedAtAction(nameof(Get), new { id = result.Id }, new { id = result.Id }),
            InterDepartmentRequestCommandResult.Success => Ok(new { id = result.Id }),
            InterDepartmentRequestCommandResult.Forbidden => Forbid(),
            _ => NotFound()
        };
    }
}
