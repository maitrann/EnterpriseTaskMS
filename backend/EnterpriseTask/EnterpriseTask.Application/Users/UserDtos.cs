namespace EnterpriseTask.Application.Users;

public sealed record UserListQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    long? DepartmentId = null,
    bool? IsActive = null);

public sealed record UserListItemDto(
    Guid Id,
    string? EmployeeCode,
    string? Email,
    string? FullName,
    string? AvatarUrl,
    long? DepartmentId,
    string? DepartmentName,
    Guid? ManagerId,
    string? ManagerName,
    string? JobTitle,
    bool IsActive,
    IReadOnlyList<string> RoleCodes,
    IReadOnlyList<string> RoleNames,
    IReadOnlyList<long> ScopedDepartmentIds,
    IReadOnlyList<string> ScopedDepartmentNames,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record UserDetailDto(
    Guid Id,
    string? EmployeeCode,
    string? Email,
    string? FullName,
    string? AvatarUrl,
    long? DepartmentId,
    string? DepartmentName,
    Guid? ManagerId,
    string? ManagerName,
    string? JobTitle,
    bool IsActive,
    IReadOnlyList<string> RoleCodes,
    IReadOnlyList<string> RoleNames,
    IReadOnlyList<long> ScopedDepartmentIds,
    IReadOnlyList<string> ScopedDepartmentNames,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
