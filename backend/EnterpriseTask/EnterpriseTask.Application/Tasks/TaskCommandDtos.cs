namespace EnterpriseTask.Application.Tasks;

public sealed record CreateTaskRequest(
    string Title,
    string? Description,
    string? TaskType,
    Guid? ProjectId,
    Guid? ParentTaskId,
    long? DepartmentId,
    Guid? AssigneeId,
    long? PriorityId,
    DateOnly? StartDate,
    DateOnly? DueDate,
    decimal? EstimatedHours,
    string? Source,
    string? UrgencyLevel,
    string? SecurityLevel,
    Guid[]? CollaboratorIds,
    Guid[]? WatcherIds,
    string[]? Tags);

public sealed record UpdateTaskRequest(
    string Title,
    string? Description,
    string? TaskType,
    long? ProjectId,
    long? ParentTaskId,
    long? DepartmentId,
    long? AssigneeId,
    long? StatusId,
    long? PriorityId,
    DateOnly? StartDate,
    DateOnly? DueDate,
    int Progress,
    decimal? EstimatedHours,
    decimal? ActualHours,
    string? Source,
    string? UrgencyLevel,
    string? SecurityLevel,
    Guid[]? CollaboratorIds,
    Guid[]? WatcherIds,
    string[]? Tags);

public sealed record UpdateTaskStatusRequest(long StatusId, string? Note);

public sealed record AddTaskCommentRequest(string Content);

public sealed record CreateSubTaskRequest(string Title, Guid? AssigneeId, DateOnly? DueDate, int? Progress);

public sealed record UpdateSubTaskRequest(string? Title, Guid? AssigneeId, DateOnly? DueDate, int? Progress, bool? Done);

public enum TaskCommandResult
{
    Success,
    NotFound,
    Forbidden
}

public sealed record TaskCreateResult(TaskCommandResult Result, Guid? Id = null);
