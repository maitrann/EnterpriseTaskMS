using EnterpriseTask.Application.Departments;
using EnterpriseTask.Domain.Departments;
using EnterpriseTask.Infrastructure.Persistence;
using Npgsql;

namespace EnterpriseTask.Infrastructure.Departments;

public sealed class PostgresDepartmentAdministrationCommands(ApplicationDbContext dbContext)
    : PostgresCommandBase(dbContext), IDepartmentAdministrationCommands
{
    private const string UniqueViolation = "23505";

    public async Task<DepartmentAdministrationCommandResult> CreateAsync(
        DepartmentCreateRequest request,
        CancellationToken cancellationToken)
    {
        if (!await CompanyExistsAsync(request.CompanyId, cancellationToken))
        {
            return new DepartmentAdministrationCommandResult(DepartmentAdministrationResult.CompanyNotFound);
        }

        if (request.ParentDepartmentId is not null
            && !await ActiveDepartmentExistsAsync(request.ParentDepartmentId.Value, request.CompanyId, cancellationToken))
        {
            return new DepartmentAdministrationCommandResult(DepartmentAdministrationResult.ParentNotFound);
        }

        var managerDecision = DepartmentManagerAssignmentPolicy.CanAssignManager(new DepartmentManagerAssignmentContext(
            request.ManagerId,
            request.ManagerId is not null && await ActiveProfileExistsAsync(request.ManagerId.Value, cancellationToken)));

        if (managerDecision == DepartmentManagerAssignmentDecision.ManagerUnavailable)
        {
            return new DepartmentAdministrationCommandResult(DepartmentAdministrationResult.ManagerNotFound);
        }

        try
        {
            var departmentId = await ExecuteScalarAsync<long>(
                """
                INSERT INTO departments (company_id, code, name, description, parent_department_id, manager_id)
                VALUES (@companyId, @code, @name, @description, @parentDepartmentId, @managerId)
                RETURNING id;
                """,
                [
                    ("@companyId", request.CompanyId),
                    ("@code", NormalizeOptional(request.Code)),
                    ("@name", request.Name.Trim()),
                    ("@description", NormalizeOptional(request.Description)),
                    ("@parentDepartmentId", request.ParentDepartmentId),
                    ("@managerId", request.ManagerId)
                ],
                cancellationToken);

            return new DepartmentAdministrationCommandResult(
                DepartmentAdministrationResult.Success,
                departmentId);
        }
        catch (PostgresException exception) when (exception.SqlState == UniqueViolation)
        {
            return new DepartmentAdministrationCommandResult(DepartmentAdministrationResult.DuplicateCodeOrName);
        }
    }

    public async Task<DepartmentAdministrationCommandResult> UpdateAsync(
        long departmentId,
        DepartmentUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var department = await LoadDepartmentAsync(departmentId, cancellationToken);
        if (department is null)
        {
            return new DepartmentAdministrationCommandResult(DepartmentAdministrationResult.NotFound);
        }

        if (request.ParentDepartmentId is not null
            && !await ActiveDepartmentExistsAsync(request.ParentDepartmentId.Value, department.CompanyId, cancellationToken))
        {
            return new DepartmentAdministrationCommandResult(DepartmentAdministrationResult.ParentNotFound);
        }

        var decision = DepartmentHierarchyPolicy.CanAssignParent(new DepartmentHierarchyContext(
            departmentId,
            request.ParentDepartmentId,
            await LoadDescendantDepartmentIdsAsync(departmentId, cancellationToken)));

        if (decision == DepartmentHierarchyDecision.SelfParentDenied)
        {
            return new DepartmentAdministrationCommandResult(DepartmentAdministrationResult.SelfParentDenied);
        }

        if (decision == DepartmentHierarchyDecision.CycleDenied)
        {
            return new DepartmentAdministrationCommandResult(DepartmentAdministrationResult.CycleDenied);
        }

        try
        {
            await ExecuteAsync(
                """
                UPDATE departments
                SET code = @code,
                    name = @name,
                    description = @description,
                    parent_department_id = @parentDepartmentId,
                    updated_at = now()
                WHERE id = @departmentId;
                """,
                [
                    ("@departmentId", departmentId),
                    ("@code", NormalizeOptional(request.Code)),
                    ("@name", request.Name.Trim()),
                    ("@description", NormalizeOptional(request.Description)),
                    ("@parentDepartmentId", request.ParentDepartmentId)
                ],
                cancellationToken);

            return new DepartmentAdministrationCommandResult(
                DepartmentAdministrationResult.Success,
                departmentId);
        }
        catch (PostgresException exception) when (exception.SqlState == UniqueViolation)
        {
            return new DepartmentAdministrationCommandResult(DepartmentAdministrationResult.DuplicateCodeOrName);
        }
    }

    public async Task<DepartmentAdministrationCommandResult> AssignManagerAsync(
        long departmentId,
        DepartmentManagerAssignmentRequest request,
        CancellationToken cancellationToken)
    {
        if (!await DepartmentExistsAsync(departmentId, cancellationToken))
        {
            return new DepartmentAdministrationCommandResult(DepartmentAdministrationResult.NotFound);
        }

        var managerDecision = DepartmentManagerAssignmentPolicy.CanAssignManager(new DepartmentManagerAssignmentContext(
            request.ManagerId,
            request.ManagerId is not null && await ActiveProfileExistsAsync(request.ManagerId.Value, cancellationToken)));

        if (managerDecision == DepartmentManagerAssignmentDecision.ManagerUnavailable)
        {
            return new DepartmentAdministrationCommandResult(DepartmentAdministrationResult.ManagerNotFound);
        }

        await ExecuteAsync(
            """
            UPDATE departments
            SET manager_id = @managerId,
                updated_at = now()
            WHERE id = @departmentId;
            """,
            [("@departmentId", departmentId), ("@managerId", request.ManagerId)],
            cancellationToken);

        return new DepartmentAdministrationCommandResult(
            DepartmentAdministrationResult.Success,
            departmentId);
    }

    public async Task<DepartmentAdministrationCommandResult> DeactivateAsync(
        long departmentId,
        CancellationToken cancellationToken)
    {
        if (!await DepartmentExistsAsync(departmentId, cancellationToken))
        {
            return new DepartmentAdministrationCommandResult(DepartmentAdministrationResult.NotFound);
        }

        var decision = DepartmentHierarchyPolicy.CanDeactivate(new DepartmentDeactivationContext(
            await CountActiveTasksAsync(departmentId, cancellationToken),
            await CountActiveChildDepartmentsAsync(departmentId, cancellationToken)));

        if (decision == DepartmentDeactivationDecision.ActiveTasksDenied)
        {
            return new DepartmentAdministrationCommandResult(DepartmentAdministrationResult.ActiveTasksDenied);
        }

        if (decision == DepartmentDeactivationDecision.ActiveChildrenDenied)
        {
            return new DepartmentAdministrationCommandResult(DepartmentAdministrationResult.ActiveChildrenDenied);
        }

        await ExecuteAsync(
            """
            UPDATE departments
            SET is_active = FALSE,
                updated_at = now()
            WHERE id = @departmentId;
            """,
            [("@departmentId", departmentId)],
            cancellationToken);

        return new DepartmentAdministrationCommandResult(
            DepartmentAdministrationResult.Success,
            departmentId);
    }

    private async Task<bool> CompanyExistsAsync(long companyId, CancellationToken cancellationToken)
    {
        const string sql = "SELECT EXISTS (SELECT 1 FROM companies WHERE id = @companyId);";
        return await ExecuteScalarAsync<bool>(sql, [("@companyId", companyId)], cancellationToken);
    }

    private async Task<bool> DepartmentExistsAsync(long departmentId, CancellationToken cancellationToken)
    {
        const string sql = "SELECT EXISTS (SELECT 1 FROM departments WHERE id = @departmentId);";
        return await ExecuteScalarAsync<bool>(sql, [("@departmentId", departmentId)], cancellationToken);
    }

    private async Task<bool> ActiveDepartmentExistsAsync(
        long departmentId,
        long companyId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1
                FROM departments
                WHERE id = @departmentId
                  AND company_id = @companyId
                  AND is_active = TRUE
            );
            """;

        return await ExecuteScalarAsync<bool>(
            sql,
            [("@departmentId", departmentId), ("@companyId", companyId)],
            cancellationToken);
    }

    private async Task<bool> ActiveProfileExistsAsync(Guid profileId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1
                FROM profiles
                WHERE id = @profileId
                  AND is_active = TRUE
            );
            """;

        return await ExecuteScalarAsync<bool>(sql, [("@profileId", profileId)], cancellationToken);
    }

    private async Task<DepartmentRow?> LoadDepartmentAsync(long departmentId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT id, company_id
            FROM departments
            WHERE id = @departmentId;
            """;

        var rows = await QueryAsync(
            sql,
            reader => new DepartmentRow(
                reader.GetInt64Value("id"),
                reader.GetInt64Value("company_id")),
            [("@departmentId", departmentId)],
            cancellationToken);

        return rows.SingleOrDefault();
    }

    private async Task<IReadOnlySet<long>> LoadDescendantDepartmentIdsAsync(
        long departmentId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            WITH RECURSIVE descendants AS (
                SELECT id, ARRAY[id] AS path
                FROM departments
                WHERE parent_department_id = @departmentId
                UNION ALL
                SELECT d.id, child.path || d.id
                FROM departments d
                JOIN descendants child ON child.id = d.parent_department_id
                WHERE NOT d.id = ANY(child.path)
            )
            SELECT id
            FROM descendants;
            """;

        var rows = await QueryAsync(
            sql,
            reader => reader.GetInt64Value("id"),
            [("@departmentId", departmentId)],
            cancellationToken);

        return rows.ToHashSet();
    }

    private async Task<int> CountActiveTasksAsync(long departmentId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT COUNT(*)::int
            FROM tasks t
            LEFT JOIN task_statuses ts ON ts.id = t.status_id
            WHERE t.department_id = @departmentId
              AND COALESCE(ts.is_closed, FALSE) = FALSE;
            """;

        return await ExecuteScalarAsync<int>(sql, [("@departmentId", departmentId)], cancellationToken);
    }

    private async Task<int> CountActiveChildDepartmentsAsync(long departmentId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT COUNT(*)::int
            FROM departments
            WHERE parent_department_id = @departmentId
              AND is_active = TRUE;
            """;

        return await ExecuteScalarAsync<int>(sql, [("@departmentId", departmentId)], cancellationToken);
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private sealed record DepartmentRow(long Id, long CompanyId);
}
