using EnterpriseTask.Application.Common;
using EnterpriseTask.Application.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EnterpriseTask.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicyNames.AuthenticatedUser)]
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

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TaskDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetActorId(out var actorUserId))
        {
            return Unauthorized();
        }

        var task = await taskQueries.GetTaskAsync(actorUserId, id, cancellationToken);
        return task is null ? NotFound() : Ok(task);
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
    [Authorize(Policy = AuthorizationPolicyNames.TaskCreate)]
    [EnableRateLimiting("ApiMutation")]
    public async Task<ActionResult> Create(CreateTaskRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetActorId(out var actorUserId))
        {
            return Unauthorized();
        }

        var result = await createTaskHandler.HandleAsync(actorUserId, request, cancellationToken);
        return result.Result == TaskCommandResult.Forbidden
            ? Forbid()
            : CreatedAtAction(nameof(GetById), new { id = result.Id }, new { id = result.Id });
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicyNames.TaskUpdate)]
    [EnableRateLimiting("ApiMutation")]
    public async Task<IActionResult> Update(Guid id, UpdateTaskRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetActorId(out var actorUserId))
        {
            return Unauthorized();
        }

        return ToActionResult(await taskCommands.UpdateAsync(actorUserId, id, request, cancellationToken));
    }

    [HttpPost("{id:guid}/status")]
    [Authorize(Policy = AuthorizationPolicyNames.TaskUpdate)]
    [EnableRateLimiting("ApiMutation")]
    public async Task<IActionResult> UpdateStatus(Guid id, UpdateTaskStatusRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetActorId(out var actorUserId))
        {
            return Unauthorized();
        }

        return ToActionResult(await updateTaskStatusHandler.HandleAsync(actorUserId, id, request, cancellationToken));
    }

    [HttpPost("{id:guid}/assignee")]
    [Authorize(Policy = AuthorizationPolicyNames.TaskAssign)]
    [EnableRateLimiting("ApiMutation")]
    public async Task<IActionResult> TransferAssignee(Guid id, TransferTaskAssigneeRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetActorId(out var actorUserId))
        {
            return Unauthorized();
        }

        return ToActionResult(await taskCommands.TransferAssigneeAsync(actorUserId, id, request, cancellationToken));
    }

    [HttpPost("{id:guid}/duplicate")]
    [Authorize(Policy = AuthorizationPolicyNames.TaskCreate)]
    [EnableRateLimiting("ApiMutation")]
    public async Task<ActionResult> Duplicate(Guid id, DuplicateTaskRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetActorId(out var actorUserId))
        {
            return Unauthorized();
        }

        var result = await taskCommands.DuplicateAsync(actorUserId, id, request, cancellationToken);
        return await ToDuplicateActionResultAsync(actorUserId, result, cancellationToken);
    }

    [HttpPost("{id:guid}/archive")]
    [Authorize(Policy = AuthorizationPolicyNames.TaskUpdate)]
    [EnableRateLimiting("ApiMutation")]
    public async Task<IActionResult> Archive(Guid id, ArchiveTaskRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetActorId(out var actorUserId))
        {
            return Unauthorized();
        }

        return ToActionResult(await taskCommands.ArchiveAsync(actorUserId, id, request, cancellationToken));
    }

    [HttpPost("{id:guid}/comments")]
    [Authorize(Policy = AuthorizationPolicyNames.CommentCreate)]
    [EnableRateLimiting("ApiMutation")]
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
    [EnableRateLimiting("ApiMutation")]
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
    [Authorize(Policy = AuthorizationPolicyNames.TaskUpdate)]
    [EnableRateLimiting("ApiMutation")]
    public async Task<IActionResult> ReviewExtension(Guid id, Guid requestId, ReviewTaskExtensionRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetActorId(out var actorUserId))
        {
            return Unauthorized();
        }

        return ToActionResult(await taskCommands.ReviewExtensionAsync(actorUserId, id, requestId, request, cancellationToken));
    }

    [HttpPost("{id:guid}/subtasks")]
    [Authorize(Policy = AuthorizationPolicyNames.TaskUpdate)]
    [EnableRateLimiting("ApiMutation")]
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
    [Authorize(Policy = AuthorizationPolicyNames.TaskUpdate)]
    [EnableRateLimiting("ApiMutation")]
    public async Task<IActionResult> UpdateSubTask(Guid id, Guid subTaskId, UpdateSubTaskRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetActorId(out var actorUserId))
        {
            return Unauthorized();
        }

        return ToActionResult(await taskCommands.UpdateSubTaskAsync(actorUserId, id, subTaskId, request, cancellationToken));
    }

    [HttpDelete("{id:guid}/subtasks/{subTaskId:guid}")]
    [Authorize(Policy = AuthorizationPolicyNames.TaskUpdate)]
    [EnableRateLimiting("ApiMutation")]
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
            TaskCommandResult.Conflict => Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Task mutation conflict",
                detail: "The requested task change is not valid for the current task state."),
            _ => Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Task not found",
                detail: "The requested task or related resource was not found.")
        };
    }

    private ActionResult ToCreateActionResult(TaskCreateResult result)
    {
        return result.Result switch
        {
            TaskCommandResult.Success => Ok(new { id = result.Id }),
            TaskCommandResult.Forbidden => Forbid(),
            TaskCommandResult.Conflict => Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Task mutation conflict",
                detail: "The requested task change is not valid for the current task state."),
            _ => Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Task not found",
                detail: "The requested task or related resource was not found.")
        };
    }

    private async Task<ActionResult> ToDuplicateActionResultAsync(
        Guid actorUserId,
        TaskDuplicateResult result,
        CancellationToken cancellationToken)
    {
        return result.Result switch
        {
            TaskCommandResult.Success => CreatedAtAction(
                nameof(GetById),
                new { id = result.Id },
                new
                {
                    id = result.Id,
                    task = result.Task ?? (result.Id is null
                        ? null
                        : await taskQueries.GetTaskAsync(actorUserId, result.Id.Value, cancellationToken))
                }),
            TaskCommandResult.Forbidden => Forbid(),
            TaskCommandResult.Conflict => Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Task mutation conflict",
                detail: "The requested task change is not valid for the current task state."),
            _ => Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Task not found",
                detail: "The requested task or related resource was not found.")
        };
    }
}
