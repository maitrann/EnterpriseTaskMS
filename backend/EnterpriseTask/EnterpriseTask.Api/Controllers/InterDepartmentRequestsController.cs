using EnterpriseTask.Application.InterDepartmentRequests;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseTask.Api.Controllers;

[ApiController]
[Route("api/inter-department-requests")]
public sealed class InterDepartmentRequestsController(
    IInterDepartmentRequestQueries requestQueries,
    IInterDepartmentRequestCommands requestCommands) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<InterDepartmentRequestDto>>> Get(CancellationToken cancellationToken)
    {
        return Ok(await requestQueries.GetRequestsAsync(cancellationToken));
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
        var id = await requestCommands.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id }, new { id });
    }

    [HttpPost("{id:guid}/acknowledge")]
    public async Task<IActionResult> Acknowledge(Guid id, CancellationToken cancellationToken)
    {
        return await requestCommands.AcknowledgeAsync(id, cancellationToken) ? NoContent() : NotFound();
    }

    [HttpPost("{id:guid}/assign-owner")]
    public async Task<IActionResult> AssignOwner(Guid id, AssignOwnerRequest request, CancellationToken cancellationToken)
    {
        return await requestCommands.AssignOwnerAsync(id, request, cancellationToken) ? NoContent() : NotFound();
    }

    [HttpPost("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, UpdateRequestStatusRequest request, CancellationToken cancellationToken)
    {
        return await requestCommands.UpdateStatusAsync(id, request, cancellationToken) ? NoContent() : NotFound();
    }

    [HttpPost("{id:guid}/messages")]
    public async Task<ActionResult> AddMessage(Guid id, AddRequestMessageRequest request, CancellationToken cancellationToken)
    {
        var messageId = await requestCommands.AddMessageAsync(id, request, cancellationToken);
        return messageId is null ? NotFound() : Ok(new { id = messageId });
    }

    [HttpPost("{id:guid}/close")]
    public async Task<IActionResult> Close(Guid id, CancellationToken cancellationToken)
    {
        return await requestCommands.CloseAsync(id, cancellationToken) ? NoContent() : NotFound();
    }
}
