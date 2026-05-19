namespace EnterpriseTask.Application.Tasks;

public interface ITaskQueries
{
    Task<IReadOnlyList<TaskDto>> GetTasksAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<TaskActivityDto>> GetActivitiesAsync(CancellationToken cancellationToken);

    Task<TaskFormOptionsDto> GetFormOptionsAsync(CancellationToken cancellationToken);
}

public interface ITaskCommands
{
    Task<long> CreateAsync(CreateTaskRequest request, CancellationToken cancellationToken);

    Task<bool> UpdateStatusAsync(long taskId, UpdateTaskStatusRequest request, CancellationToken cancellationToken);

    Task<long?> AddCommentAsync(long taskId, AddTaskCommentRequest request, CancellationToken cancellationToken);

    Task<long?> CreateSubTaskAsync(long taskId, CreateSubTaskRequest request, CancellationToken cancellationToken);

    Task<bool> UpdateSubTaskAsync(long taskId, long subTaskId, UpdateSubTaskRequest request, CancellationToken cancellationToken);

    Task<bool> DeleteSubTaskAsync(long taskId, long subTaskId, CancellationToken cancellationToken);
}
