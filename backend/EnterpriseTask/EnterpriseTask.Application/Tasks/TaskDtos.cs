using EnterpriseTask.Application.Common;

namespace EnterpriseTask.Application.Tasks;

public sealed record TaskDto(
    Guid Id,
    string Code,
    Guid? ProjectId,
    Guid? ParentTaskId,
    string Title,
    string? Description,
    string? TaskType,
    long? DepartmentId,
    long? StatusId,
    long? PriorityId,
    string? UrgencyLevel,
    string? SecurityLevel,
    Guid? ReporterId,
    Guid? AssigneeId,
    Guid[] CollaboratorIds,
    Guid[] WatcherIds,
    DateOnly? StartDate,
    DateOnly? DueDate,
    int Progress,
    string? Source,
    string[] AttachmentNames,
    string[] Tags,
    string[] ProcessingNotes,
    TaskExtensionRequestDto[] ExtensionRequests,
    SubTaskDto[] Subtasks,
    bool SubtaskProgressAutoSync,
    bool ParentCompletionSuggested,
    decimal? EstimatedHours,
    decimal? ActualHours,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record SubTaskDto(
    Guid Id,
    Guid TaskId,
    string Title,
    Guid? AssigneeId,
    string Status,
    DateOnly? DueDate,
    int Progress,
    bool Done,
    long CreatedAt,
    long? UpdatedAt,
    long? CompletedAt,
    int Order);

public sealed record TaskExtensionRequestDto(
    Guid Id,
    DateOnly RequestedDueDate,
    string Reason,
    string Status,
    Guid? RequestedByUserId,
    DateTimeOffset RequestedAt,
    Guid? ReviewedByUserId,
    DateTimeOffset? ReviewedAt,
    string? ReviewNote);

public sealed record TaskActivityDto(
    Guid Id,
    Guid TaskId,
    Guid? UserId,
    string? ActionType,
    string? OldValue,
    string? NewValue,
    DateTimeOffset CreatedAt);

public sealed record TaskMemberOptionDto(Guid Id, string Label, string Role, long? DepartmentId);

public sealed record TaskDepartmentOptionDto(long Id, string Label);

public sealed record TaskFormOptionsDto(
    IReadOnlyList<OptionDto<string>> TaskTypes,
    IReadOnlyList<TaskDepartmentOptionDto> Departments,
    IReadOnlyList<TaskMemberOptionDto> Users,
    IReadOnlyList<OptionDto<long>> Priorities,
    IReadOnlyList<OptionDto<string>> UrgencyLevels,
    IReadOnlyList<OptionDto<string>> SecurityLevels,
    IReadOnlyList<OptionDto<string>> Sources);
