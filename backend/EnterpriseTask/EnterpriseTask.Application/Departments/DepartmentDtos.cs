namespace EnterpriseTask.Application.Departments;

public sealed record DepartmentCardDto(
    string Name,
    string? Description,
    int Members,
    int ActiveTasks,
    int CompletedTasks,
    string Lead,
    string Sla,
    string Tone);

public sealed record DepartmentOptionDto(long Id, string Name);

public sealed record DepartmentListItemDto(
    long Id,
    long CompanyId,
    string? Code,
    string Name,
    string? Description,
    long? ParentDepartmentId,
    string? ParentDepartmentName,
    Guid? ManagerId,
    string? ManagerName,
    int MemberCount,
    int ActiveTaskCount,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record DepartmentTreeNodeDto(
    long Id,
    long CompanyId,
    string? Code,
    string Name,
    string? Description,
    long? ParentDepartmentId,
    string? ParentDepartmentName,
    Guid? ManagerId,
    string? ManagerName,
    int MemberCount,
    int ActiveTaskCount,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    IReadOnlyList<DepartmentTreeNodeDto> Children);
