using EnterpriseTask.Application.Common;

namespace EnterpriseTask.Application.Departments;

public interface IDepartmentAdministrationCommands
{
    Task<DepartmentAdministrationCommandResult> CreateAsync(
        UserScope scope,
        DepartmentCreateRequest request,
        CancellationToken cancellationToken);

    Task<DepartmentAdministrationCommandResult> UpdateAsync(
        UserScope scope,
        long departmentId,
        DepartmentUpdateRequest request,
        CancellationToken cancellationToken);

    Task<DepartmentAdministrationCommandResult> AssignManagerAsync(
        UserScope scope,
        long departmentId,
        DepartmentManagerAssignmentRequest request,
        CancellationToken cancellationToken);

    Task<DepartmentAdministrationCommandResult> DeactivateAsync(
        UserScope scope,
        long departmentId,
        CancellationToken cancellationToken);
}

public sealed record DepartmentAdministrationCommandResult(
    DepartmentAdministrationResult Result,
    long? DepartmentId = null);

public enum DepartmentAdministrationResult
{
    Success,
    NotFound,
    CompanyNotFound,
    ParentNotFound,
    ManagerNotFound,
    SelfParentDenied,
    CycleDenied,
    ActiveTasksDenied,
    ActiveChildrenDenied,
    DuplicateCodeOrName
}

public sealed record DepartmentCreateRequest(
    long CompanyId,
    string? Code,
    string Name,
    string? Description,
    long? ParentDepartmentId,
    Guid? ManagerId);

public sealed record DepartmentUpdateRequest(
    string? Code,
    string Name,
    string? Description,
    long? ParentDepartmentId);

public sealed record DepartmentManagerAssignmentRequest(Guid? ManagerId);
