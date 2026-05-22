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
    string? Source);

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
