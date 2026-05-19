using EnterpriseTask.Application.Common;

namespace EnterpriseTask.Application.Tasks;

public sealed record TaskDto(
    long Id,
    string Code,
    long? ProjectId,
    long? ParentTaskId,
    string Title,
    string? Description,
    string? TaskType,
    long? DepartmentId,
    long? StatusId,
    long? PriorityId,
    string? UrgencyLevel,
    string? SecurityLevel,
    long? ReporterId,
    long? AssigneeId,
    long[] CollaboratorIds,
    long[] WatcherIds,
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
    long Id,
    long TaskId,
    string Title,
    long? AssigneeId,
    DateOnly? DueDate,
    int Progress,
    bool Done,
    long CreatedAt,
    long? UpdatedAt,
    long? CompletedAt,
    int Order);

public sealed record TaskExtensionRequestDto(
    long Id,
    DateOnly RequestedDueDate,
    string Reason,
    string Status,
    long? RequestedByUserId,
    DateTimeOffset RequestedAt,
    long? ReviewedByUserId,
    DateTimeOffset? ReviewedAt,
    string? ReviewNote);

public sealed record TaskActivityDto(
    long Id,
    long TaskId,
    long? UserId,
    string? ActionType,
    string? OldValue,
    string? NewValue,
    DateTimeOffset CreatedAt);

public sealed record TaskMemberOptionDto(long Id, string Label, string Role, long? DepartmentId);

public sealed record TaskDepartmentOptionDto(long Id, string Label);

public sealed record TaskFormOptionsDto(
    IReadOnlyList<OptionDto<string>> TaskTypes,
    IReadOnlyList<TaskDepartmentOptionDto> Departments,
    IReadOnlyList<TaskMemberOptionDto> Users,
    IReadOnlyList<OptionDto<long>> Priorities,
    IReadOnlyList<OptionDto<string>> UrgencyLevels,
    IReadOnlyList<OptionDto<string>> SecurityLevels,
    IReadOnlyList<OptionDto<string>> Sources);
