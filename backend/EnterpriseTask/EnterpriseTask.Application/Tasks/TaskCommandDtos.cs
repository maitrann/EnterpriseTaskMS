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
    string? Source,
    string? UrgencyLevel,
    string? SecurityLevel,
    long[]? CollaboratorIds,
    long[]? WatcherIds,
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
    long[]? CollaboratorIds,
    long[]? WatcherIds,
    string[]? Tags);

public sealed record UpdateTaskStatusRequest(long StatusId, string? Note);

public sealed record AddTaskCommentRequest(long? UserId, string Content);

public sealed record CreateSubTaskRequest(string Title, long? AssigneeId, DateOnly? DueDate, int? Progress);

public sealed record UpdateSubTaskRequest(string? Title, long? AssigneeId, DateOnly? DueDate, int? Progress, bool? Done);

public sealed record DuplicateTaskRequest(string? Title, bool ResetPeople, bool ResetAttachments);

public sealed record TransferTaskAssigneeRequest(long AssigneeId, string? Reason);

public sealed record CreateTaskExtensionRequest(DateOnly RequestedDueDate, string Reason, long? RequestedByUserId);

public sealed record ReviewTaskExtensionRequest(bool Approved, long? ReviewedByUserId, string? ReviewNote);
