using EnterpriseTask.Application.Common;

namespace EnterpriseTask.Application.Tasks;

public interface ITaskQueries
{
    Task<IReadOnlyList<TaskDto>> GetTasksAsync(Guid actorUserId, CancellationToken cancellationToken);

    Task<IReadOnlyList<TaskActivityDto>> GetActivitiesAsync(Guid actorUserId, CancellationToken cancellationToken);

    Task<TaskFormOptionsDto> GetFormOptionsAsync(Guid actorUserId, CancellationToken cancellationToken);
}

public interface ITaskCommands
{
    Task<TaskCreateResult> CreateAsync(Guid actorUserId, CreateTaskRequest request, CancellationToken cancellationToken);

    Task<TaskCommandResult> UpdateStatusAsync(Guid actorUserId, Guid taskId, UpdateTaskStatusRequest request, CancellationToken cancellationToken);

    Task<TaskCreateResult> AddCommentAsync(Guid actorUserId, Guid taskId, AddTaskCommentRequest request, CancellationToken cancellationToken);

    Task<TaskCreateResult> CreateSubTaskAsync(Guid actorUserId, Guid taskId, CreateSubTaskRequest request, CancellationToken cancellationToken);

    Task<TaskCommandResult> UpdateSubTaskAsync(Guid actorUserId, Guid taskId, Guid subTaskId, UpdateSubTaskRequest request, CancellationToken cancellationToken);

    Task<TaskCommandResult> DeleteSubTaskAsync(Guid actorUserId, Guid taskId, Guid subTaskId, CancellationToken cancellationToken);
}
