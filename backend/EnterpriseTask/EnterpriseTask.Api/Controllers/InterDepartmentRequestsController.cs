using EnterpriseTask.Application.Common;
using EnterpriseTask.Application.InterDepartmentRequests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EnterpriseTask.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicyNames.AuthenticatedUser)]
[Route("api/inter-department-requests")]
public sealed class InterDepartmentRequestsController(
    IInterDepartmentRequestQueries requestQueries,
    IInterDepartmentRequestCommands requestCommands,
    AssignInterRequestOwnerHandler assignOwnerHandler,
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
    [EnableRateLimiting("ApiMutation")]
    public async Task<ActionResult> Create(CreateInterDepartmentRequestCommand request, CancellationToken cancellationToken)
    {
        var result = await requestCommands.CreateAsync(currentUser.GetRequiredScope(), request, cancellationToken);
        return ToCreateActionResult(result, created: true);
    }

    [HttpPost("{id:guid}/acknowledge")]
    [EnableRateLimiting("ApiMutation")]
    public async Task<IActionResult> Acknowledge(Guid id, CancellationToken cancellationToken)
    {
        return ToActionResult(await requestCommands.AcknowledgeAsync(currentUser.GetRequiredScope(), id, cancellationToken));
    }

    [HttpPost("{id:guid}/assign-owner")]
    [EnableRateLimiting("ApiMutation")]
    public async Task<IActionResult> AssignOwner(Guid id, AssignOwnerRequest request, CancellationToken cancellationToken)
    {
        return ToActionResult(await assignOwnerHandler.HandleAsync(currentUser.GetRequiredScope(), id, request, cancellationToken));
    }

    [HttpPost("{id:guid}/status")]
    [EnableRateLimiting("ApiMutation")]
    public async Task<IActionResult> UpdateStatus(Guid id, UpdateRequestStatusRequest request, CancellationToken cancellationToken)
    {
        return ToActionResult(await requestCommands.UpdateStatusAsync(currentUser.GetRequiredScope(), id, request, cancellationToken));
    }

    [HttpPost("{id:guid}/messages")]
    [EnableRateLimiting("ApiMutation")]
    public async Task<ActionResult> AddMessage(Guid id, AddRequestMessageRequest request, CancellationToken cancellationToken)
    {
        var result = await requestCommands.AddMessageAsync(currentUser.GetRequiredScope(), id, request, cancellationToken);
        return ToCreateActionResult(result, created: false);
    }

    [HttpPost("{id:guid}/close")]
    [EnableRateLimiting("ApiMutation")]
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
            InterDepartmentRequestCommandResult.InvalidState => Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Request mutation conflict",
                detail: "The requested inter-department request change is not valid for the current request state."),
            _ => Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Request not found",
                detail: "The requested inter-department request or related resource was not found.")
        };
    }

    private ActionResult ToCreateActionResult(InterDepartmentRequestCreateResult result, bool created)
    {
        return result.Result switch
        {
            InterDepartmentRequestCommandResult.Success when created => CreatedAtAction(nameof(Get), new { id = result.Id }, new { id = result.Id }),
            InterDepartmentRequestCommandResult.Success => Ok(new { id = result.Id }),
            InterDepartmentRequestCommandResult.Forbidden => Forbid(),
            InterDepartmentRequestCommandResult.InvalidState => Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Request mutation conflict",
                detail: "The requested inter-department request change is not valid for the current request state."),
            _ => Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Request not found",
                detail: "The requested inter-department request or related resource was not found.")
        };
    }
}
