using EnterpriseTask.Application.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EnterpriseTask.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/tasks")]
public sealed class TasksController(ITaskQueries taskQueries, ITaskCommands taskCommands) : ControllerBase
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

        var result = await taskCommands.CreateAsync(actorUserId, request, cancellationToken);
        return result.Result == TaskCommandResult.Forbidden
            ? Forbid()
            : CreatedAtAction(nameof(Get), new { id = result.Id }, new { id = result.Id });
    }

    [HttpPost("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, UpdateTaskStatusRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetActorId(out var actorUserId))
        {
            return Unauthorized();
        }

        return ToActionResult(await taskCommands.UpdateStatusAsync(actorUserId, id, request, cancellationToken));
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
        return Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out actorUserId);
    }

    private IActionResult ToActionResult(TaskCommandResult result)
    {
        return result switch
        {
            TaskCommandResult.Success => NoContent(),
            TaskCommandResult.Forbidden => Forbid(),
            _ => NotFound()
        };
    }

    private ActionResult ToCreateActionResult(TaskCreateResult result)
    {
        return result.Result switch
        {
            TaskCommandResult.Success => Ok(new { id = result.Id }),
            TaskCommandResult.Forbidden => Forbid(),
            _ => NotFound()
        };
    }
}
