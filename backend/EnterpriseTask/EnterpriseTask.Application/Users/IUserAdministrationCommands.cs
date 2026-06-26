namespace EnterpriseTask.Application.Users;

public interface IUserAdministrationCommands
{
    Task<UserAdministrationResult> SetActiveAsync(
        Guid actorUserId,
        Guid targetUserId,
        bool isActive,
        CancellationToken cancellationToken);

    Task<UserAdministrationResult> AssignRoleAsync(
        Guid targetUserId,
        long roleId,
        CancellationToken cancellationToken);

    Task<UserAdministrationResult> RemoveRoleAsync(
        Guid targetUserId,
        long roleId,
        CancellationToken cancellationToken);

    Task<UserAdministrationResult> AssignDepartmentScopeAsync(
        Guid targetUserId,
        long departmentId,
        CancellationToken cancellationToken);

    Task<UserAdministrationResult> RemoveDepartmentScopeAsync(
        Guid targetUserId,
        long departmentId,
        CancellationToken cancellationToken);
}

public enum UserAdministrationResult
{
    Success,
    NotFound,
    RoleNotFound,
    DepartmentNotFound,
    SelfLockDenied,
    LastAdminDenied
}

public sealed record UserRoleAssignmentRequest(long RoleId);

public sealed record UserDepartmentScopeAssignmentRequest(long DepartmentId);
