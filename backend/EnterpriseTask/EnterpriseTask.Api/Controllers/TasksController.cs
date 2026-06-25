using EnterpriseTask.Application.Common;
using EnterpriseTask.Application.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseTask.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/tasks")]
public sealed class TasksController(
    ITaskQueries taskQueries,
    ITaskCommands taskCommands,
    CreateTaskHandler createTaskHandler,
    UpdateTaskStatusHandler updateTaskStatusHandler,
    ICurrentUserContext currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TaskDto>>> Get(CancellationToken cancellationToken)
    {
        return TryGetActorId(out var actorUserId)
            ? Ok(await taskQueries.GetTasksAsync(actorUserId, cancellationToken))
            : Unauthorized();
    }

    [HttpGet("activities")]
    public async Task<ActionResult<IReadOnlyList<TaskActivityDto>>> GetActivities(CancellationToken cancellationToken)
    {
        return TryGetActorId(out var actorUserId)
            ? Ok(await taskQueries.GetActivitiesAsync(actorUserId, cancellationToken))
            : Unauthorized();
    }

    [HttpGet("form-options")]
    public async Task<ActionResult<TaskFormOptionsDto>> GetFormOptions(CancellationToken cancellationToken)
    {
        return TryGetActorId(out var actorUserId)
            ? Ok(await taskQueries.GetFormOptionsAsync(actorUserId, cancellationToken))
            : Unauthorized();
    }

    [HttpPost]
    public async Task<ActionResult> Create(CreateTaskRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetActorId(out var actorUserId))
        {
            return Unauthorized();
        }

        var result = await createTaskHandler.HandleAsync(actorUserId, request, cancellationToken);
        return result.Result == TaskCommandResult.Forbidden
            ? Forbid()
            : CreatedAtAction(nameof(Get), new { id = result.Id }, new { id = result.Id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateTaskRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetActorId(out var actorUserId))
        {
            return Unauthorized();
        }

        return ToActionResult(await taskCommands.UpdateAsync(actorUserId, id, request, cancellationToken));
    }

    [HttpPost("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, UpdateTaskStatusRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetActorId(out var actorUserId))
        {
            return Unauthorized();
        }

        return ToActionResult(await updateTaskStatusHandler.HandleAsync(actorUserId, id, request, cancellationToken));
    }

    [HttpPost("{id:guid}/assignee")]
    public async Task<IActionResult> TransferAssignee(Guid id, TransferTaskAssigneeRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetActorId(out var actorUserId))
        {
            return Unauthorized();
        }

        return ToActionResult(await taskCommands.TransferAssigneeAsync(actorUserId, id, request, cancellationToken));
    }

    [HttpPost("{id:guid}/duplicate")]
    public async Task<ActionResult> Duplicate(Guid id, DuplicateTaskRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetActorId(out var actorUserId))
        {
            return Unauthorized();
        }

        var result = await taskCommands.DuplicateAsync(actorUserId, id, request, cancellationToken);
        return ToCreateActionResult(result);
    }

    [HttpPost("{id:guid}/comments")]
    public async Task<ActionResult> AddComment(Guid id, AddTaskCommentRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetActorId(out var actorUserId))
        {
            return Unauthorized();
        }

        var result = await taskCommands.AddCommentAsync(actorUserId, id, request, cancellationToken);
        return ToCreateActionResult(result);
    }

    [HttpPost("{id:guid}/extension-requests")]
    public async Task<ActionResult> RequestExtension(Guid id, CreateTaskExtensionRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetActorId(out var actorUserId))
        {
            return Unauthorized();
        }

        var result = await taskCommands.RequestExtensionAsync(actorUserId, id, request, cancellationToken);
        return ToCreateActionResult(result);
    }

    [HttpPost("{id:guid}/extension-requests/{requestId:guid}/review")]
    public async Task<IActionResult> ReviewExtension(Guid id, Guid requestId, ReviewTaskExtensionRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetActorId(out var actorUserId))
        {
            return Unauthorized();
        }

        return ToActionResult(await taskCommands.ReviewExtensionAsync(actorUserId, id, requestId, request, cancellationToken));
    }

    [HttpPost("{id:guid}/subtasks")]
    public async Task<ActionResult> CreateSubTask(Guid id, CreateSubTaskRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetActorId(out var actorUserId))
        {
            return Unauthorized();
        }

        var result = await taskCommands.CreateSubTaskAsync(actorUserId, id, request, cancellationToken);
        return ToCreateActionResult(result);
    }

    [HttpPut("{id:guid}/subtasks/{subTaskId:guid}")]
    public async Task<IActionResult> UpdateSubTask(Guid id, Guid subTaskId, UpdateSubTaskRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetActorId(out var actorUserId))
        {
            return Unauthorized();
        }

        return ToActionResult(await taskCommands.UpdateSubTaskAsync(actorUserId, id, subTaskId, request, cancellationToken));
    }

    [HttpDelete("{id:guid}/subtasks/{subTaskId:guid}")]
    public async Task<IActionResult> DeleteSubTask(Guid id, Guid subTaskId, CancellationToken cancellationToken)
    {
        if (!TryGetActorId(out var actorUserId))
        {
            return Unauthorized();
        }

        return ToActionResult(await taskCommands.DeleteSubTaskAsync(actorUserId, id, subTaskId, cancellationToken));
    }

    private bool TryGetActorId(out Guid actorUserId)
    {
        return currentUser.TryGetUserId(out actorUserId);
    }

    private IActionResult ToActionResult(TaskCommandResult result)
    {
        return result switch
        {
            TaskCommandResult.Success => NoContent(),
            TaskCommandResult.Forbidden => Forbid(),
            TaskCommandResult.Conflict => Conflict(),
            _ => NotFound()
        };
    }

    private ActionResult ToCreateActionResult(TaskCreateResult result)
    {
        return result.Result switch
        {
            TaskCommandResult.Success => Ok(new { id = result.Id }),
            TaskCommandResult.Forbidden => Forbid(),
            TaskCommandResult.Conflict => Conflict(),
            _ => NotFound()
        };
    }
}
