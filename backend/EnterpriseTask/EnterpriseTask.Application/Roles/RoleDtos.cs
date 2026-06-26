namespace EnterpriseTask.Application.Roles;

public sealed record RoleDto(
    long Id,
    string Code,
    string Name,
    string? Description,
    IReadOnlyList<PermissionDto> Permissions,
    DateTimeOffset CreatedAt);

public sealed record PermissionDto(
    long Id,
    string Code,
    string Name,
    string Module,
    string? Description,
    DateTimeOffset CreatedAt);
