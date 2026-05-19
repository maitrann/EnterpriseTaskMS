using EnterpriseTask.Application.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseTask.Api.Controllers;

[ApiController]
[Route("api/tasks")]
public sealed class TasksController(ITaskQueries taskQueries, ITaskCommands taskCommands) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TaskDto>>> Get(CancellationToken cancellationToken)
    {
        return Ok(await taskQueries.GetTasksAsync(cancellationToken));
    }

    [HttpGet("activities")]
    public async Task<ActionResult<IReadOnlyList<TaskActivityDto>>> GetActivities(CancellationToken cancellationToken)
    {
        return Ok(await taskQueries.GetActivitiesAsync(cancellationToken));
    }

    [HttpGet("form-options")]
    public async Task<ActionResult<TaskFormOptionsDto>> GetFormOptions(CancellationToken cancellationToken)
    {
        return Ok(await taskQueries.GetFormOptionsAsync(cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult> Create(CreateTaskRequest request, CancellationToken cancellationToken)
    {
        var id = await taskCommands.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id }, new { id });
    }

    [HttpPost("{id:long}/status")]
    public async Task<IActionResult> UpdateStatus(long id, UpdateTaskStatusRequest request, CancellationToken cancellationToken)
    {
        return await taskCommands.UpdateStatusAsync(id, request, cancellationToken) ? NoContent() : NotFound();
    }

    [HttpPost("{id:long}/comments")]
    public async Task<ActionResult> AddComment(long id, AddTaskCommentRequest request, CancellationToken cancellationToken)
    {
        var commentId = await taskCommands.AddCommentAsync(id, request, cancellationToken);
        return commentId is null ? NotFound() : Ok(new { id = commentId });
    }

    [HttpPost("{id:long}/subtasks")]
    public async Task<ActionResult> CreateSubTask(long id, CreateSubTaskRequest request, CancellationToken cancellationToken)
    {
        var subTaskId = await taskCommands.CreateSubTaskAsync(id, request, cancellationToken);
        return subTaskId is null ? NotFound() : Ok(new { id = subTaskId });
    }

    [HttpPut("{id:long}/subtasks/{subTaskId:long}")]
    public async Task<IActionResult> UpdateSubTask(long id, long subTaskId, UpdateSubTaskRequest request, CancellationToken cancellationToken)
    {
        return await taskCommands.UpdateSubTaskAsync(id, subTaskId, request, cancellationToken) ? NoContent() : NotFound();
    }

    [HttpDelete("{id:long}/subtasks/{subTaskId:long}")]
    public async Task<IActionResult> DeleteSubTask(long id, long subTaskId, CancellationToken cancellationToken)
    {
        return await taskCommands.DeleteSubTaskAsync(id, subTaskId, cancellationToken) ? NoContent() : NotFound();
    }
}
