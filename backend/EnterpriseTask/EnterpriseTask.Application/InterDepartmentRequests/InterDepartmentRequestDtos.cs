namespace EnterpriseTask.Application.InterDepartmentRequests;

public sealed record RequestDepartmentRefDto(string Id, string Name);

public sealed record RequestOwnerRefDto(string Id, string Name, string DepartmentId, string DepartmentName);

public sealed record RequestSlaPolicyDto(string Key, string Label, int TargetHours, int WarnHours);

public sealed record RequestSlaSnapshotDto(
    string PolicyKey,
    string PolicyLabel,
    int TargetHours,
    int WarnHours,
    string StartedAt,
    string DueAt,
    int RemainingHours,
    bool Breached);

public sealed record RequestMessageDto(
    string Id,
    string AuthorName,
    string AuthorRole,
    string? AuthorDepartment,
    string CreatedAt,
    string Body);

public sealed record InterDepartmentRequestDto(
    string Id,
    string Code,
    string Type,
    string Title,
    string Description,
    string RequesterDepartment,
    string RequesterDepartmentId,
    string RequesterName,
    Guid? RequesterUserId,
    string TargetDepartment,
    string TargetDepartmentId,
    string? Owner,
    string? OwnerId,
    string Priority,
    string Status,
    string CreatedAt,
    string? UpdatedAt,
    string? ReceivedAt,
    string? ClosedAt,
    string DueDate,
    RequestSlaSnapshotDto Sla,
    IReadOnlyDictionary<string, string> FormValues,
    string? LatestMessage,
    string? Note,
    IReadOnlyList<RequestMessageDto> Messages);
