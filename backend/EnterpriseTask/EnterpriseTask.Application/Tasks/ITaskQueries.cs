using EnterpriseTask.Application.Common;

namespace EnterpriseTask.Application.Tasks;

public interface ITaskQueries
{
    Task<IReadOnlyList<TaskDto>> GetTasksAsync(UserScope scope, CancellationToken cancellationToken);

    Task<IReadOnlyList<TaskActivityDto>> GetActivitiesAsync(UserScope scope, CancellationToken cancellationToken);

    Task<TaskFormOptionsDto> GetFormOptionsAsync(UserScope scope, CancellationToken cancellationToken);
}

public interface ITaskCommands
{
    Task<long> CreateAsync(CreateTaskRequest request, CancellationToken cancellationToken);

    Task<bool> UpdateAsync(long taskId, UpdateTaskRequest request, CancellationToken cancellationToken);

    Task<long?> DuplicateAsync(long taskId, DuplicateTaskRequest request, CancellationToken cancellationToken);

    Task<bool> UpdateStatusAsync(long taskId, UpdateTaskStatusRequest request, CancellationToken cancellationToken);

    Task<bool> TransferAssigneeAsync(long taskId, TransferTaskAssigneeRequest request, CancellationToken cancellationToken);

    Task<long?> AddCommentAsync(long taskId, AddTaskCommentRequest request, CancellationToken cancellationToken);

    Task<long?> RequestExtensionAsync(long taskId, CreateTaskExtensionRequest request, CancellationToken cancellationToken);

    Task<bool> ReviewExtensionAsync(long taskId, long requestId, ReviewTaskExtensionRequest request, CancellationToken cancellationToken);

    Task<long?> CreateSubTaskAsync(long taskId, CreateSubTaskRequest request, CancellationToken cancellationToken);

    Task<bool> UpdateSubTaskAsync(long taskId, long subTaskId, UpdateSubTaskRequest request, CancellationToken cancellationToken);

    Task<bool> DeleteSubTaskAsync(long taskId, long subTaskId, CancellationToken cancellationToken);
}
