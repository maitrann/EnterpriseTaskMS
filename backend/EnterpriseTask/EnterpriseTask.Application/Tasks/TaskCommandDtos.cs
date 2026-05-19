namespace EnterpriseTask.Application.Tasks;

public sealed record CreateTaskRequest(
    string Title,
    string? Description,
    string? TaskType,
    long? ProjectId,
    long? ParentTaskId,
    long? DepartmentId,
    long? AssigneeId,
    long? PriorityId,
    DateOnly? StartDate,
    DateOnly? DueDate,
    decimal? EstimatedHours,
    string? Source);

public sealed record UpdateTaskStatusRequest(long StatusId, string? Note);

public sealed record AddTaskCommentRequest(long? UserId, string Content);

public sealed record CreateSubTaskRequest(string Title, long? AssigneeId, DateOnly? DueDate, int? Progress);

public sealed record UpdateSubTaskRequest(string? Title, long? AssigneeId, DateOnly? DueDate, int? Progress, bool? Done);
