using EnterpriseTask.Application.Users;
using EnterpriseTask.Domain.Users;
using EnterpriseTask.Infrastructure.Persistence;

namespace EnterpriseTask.Infrastructure.Users;

public sealed class PostgresUserAdministrationCommands(ApplicationDbContext dbContext)
    : PostgresCommandBase(dbContext), IUserAdministrationCommands
{
    public async Task<UserAdministrationResult> SetActiveAsync(
        Guid actorUserId,
        Guid targetUserId,
        bool isActive,
        CancellationToken cancellationToken)
    {
        var target = await LoadUserLockRowAsync(targetUserId, cancellationToken);
        if (target is null)
        {
            return UserAdministrationResult.NotFound;
        }

        var decision = UserLockPolicy.CanChangeActiveState(new UserLockContext(
            actorUserId,
            targetUserId,
            target.IsAdmin,
            await CountActiveAdminsAsync(cancellationToken),
            isActive));

        if (decision == UserLockDecision.SelfLockDenied)
        {
            return UserAdministrationResult.SelfLockDenied;
        }

        if (decision == UserLockDecision.LastAdminDenied)
        {
            return UserAdministrationResult.LastAdminDenied;
        }

        return await ExecuteInTransactionAsync(async () =>
        {
            await ExecuteAsync(
                """
                UPDATE profiles
                SET is_active = @isActive,
                    updated_at = now()
                WHERE id = @targetUserId;
                """,
                [("@isActive", isActive), ("@targetUserId", targetUserId)],
                cancellationToken);

            if (!isActive)
            {
                await ExecuteAsync(
                    """
                    UPDATE auth_refresh_sessions
                    SET revoked_at = COALESCE(revoked_at, now())
                    WHERE user_id = @targetUserId
                      AND revoked_at IS NULL;
                    """,
                    [("@targetUserId", targetUserId)],
                    cancellationToken);
            }

            return UserAdministrationResult.Success;
        }, cancellationToken);
    }

    public async Task<UserAdministrationResult> AssignRoleAsync(
        Guid targetUserId,
        long roleId,
        CancellationToken cancellationToken)
    {
        if (!await UserExistsAsync(targetUserId, cancellationToken))
        {
            return UserAdministrationResult.NotFound;
        }

        if (!await RoleExistsAsync(roleId, cancellationToken))
        {
            return UserAdministrationResult.RoleNotFound;
        }

        return await ExecuteInTransactionAsync(async () =>
        {
            await ExecuteAsync(
                """
                INSERT INTO user_roles (user_id, role_id)
                VALUES (@targetUserId, @roleId)
                ON CONFLICT DO NOTHING;
                """,
                [("@targetUserId", targetUserId), ("@roleId", roleId)],
                cancellationToken);

            await RevokeRefreshSessionsAsync(targetUserId, cancellationToken);
            return UserAdministrationResult.Success;
        }, cancellationToken);
    }

    public async Task<UserAdministrationResult> RemoveRoleAsync(
        Guid targetUserId,
        long roleId,
        CancellationToken cancellationToken)
    {
        var target = await LoadUserRoleGrantRowAsync(targetUserId, roleId, cancellationToken);
        if (target is null)
        {
            return UserAdministrationResult.NotFound;
        }

        if (!target.RoleExists)
        {
            return UserAdministrationResult.RoleNotFound;
        }

        var decision = UserRoleGrantPolicy.CanRemoveRole(new UserRoleGrantContext(
            target.RoleCode == "admin",
            target.IsActive && target.HasAdminRole,
            await CountActiveAdminsAsync(cancellationToken)));

        if (decision == UserRoleGrantDecision.LastAdminDenied)
        {
            return UserAdministrationResult.LastAdminDenied;
        }

        return await ExecuteInTransactionAsync(async () =>
        {
            await ExecuteAsync(
                """
                DELETE FROM user_roles
                WHERE user_id = @targetUserId
                  AND role_id = @roleId;
                """,
                [("@targetUserId", targetUserId), ("@roleId", roleId)],
                cancellationToken);

            await RevokeRefreshSessionsAsync(targetUserId, cancellationToken);
            return UserAdministrationResult.Success;
        }, cancellationToken);
    }

    public async Task<UserAdministrationResult> AssignDepartmentScopeAsync(
        Guid targetUserId,
        long departmentId,
        CancellationToken cancellationToken)
    {
        if (!await UserExistsAsync(targetUserId, cancellationToken))
        {
            return UserAdministrationResult.NotFound;
        }

        if (!await ActiveDepartmentExistsAsync(departmentId, cancellationToken))
        {
            return UserAdministrationResult.DepartmentNotFound;
        }

        return await ExecuteInTransactionAsync(async () =>
        {
            await ExecuteAsync(
                """
                INSERT INTO user_department_scopes (user_id, department_id)
                VALUES (@targetUserId, @departmentId)
                ON CONFLICT DO NOTHING;
                """,
                [("@targetUserId", targetUserId), ("@departmentId", departmentId)],
                cancellationToken);

            await RevokeRefreshSessionsAsync(targetUserId, cancellationToken);
            return UserAdministrationResult.Success;
        }, cancellationToken);
    }

    public async Task<UserAdministrationResult> RemoveDepartmentScopeAsync(
        Guid targetUserId,
        long departmentId,
        CancellationToken cancellationToken)
    {
        if (!await UserExistsAsync(targetUserId, cancellationToken))
        {
            return UserAdministrationResult.NotFound;
        }

        if (!await DepartmentExistsAsync(departmentId, cancellationToken))
        {
            return UserAdministrationResult.DepartmentNotFound;
        }

        return await ExecuteInTransactionAsync(async () =>
        {
            await ExecuteAsync(
                """
                DELETE FROM user_department_scopes
                WHERE user_id = @targetUserId
                  AND department_id = @departmentId;
                """,
                [("@targetUserId", targetUserId), ("@departmentId", departmentId)],
                cancellationToken);

            await RevokeRefreshSessionsAsync(targetUserId, cancellationToken);
            return UserAdministrationResult.Success;
        }, cancellationToken);
    }

    private async Task<UserLockRow?> LoadUserLockRowAsync(Guid userId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT p.id,
                   EXISTS (
                       SELECT 1
                       FROM user_roles ur
                       JOIN roles r ON r.id = ur.role_id
                       WHERE ur.user_id = p.id
                         AND r.code = 'admin'
                   ) AS is_admin
            FROM profiles p
            WHERE p.id = @userId;
            """;

        var rows = await QueryAsync(
            sql,
            reader => new UserLockRow(
                reader.GetGuidValue("id"),
                reader.GetBooleanValue("is_admin")),
            [("@userId", userId)],
            cancellationToken);

        return rows.SingleOrDefault();
    }

    private async Task<int> CountActiveAdminsAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT COUNT(DISTINCT p.id)
            FROM profiles p
            JOIN user_roles ur ON ur.user_id = p.id
            JOIN roles r ON r.id = ur.role_id
            WHERE p.is_active = TRUE
              AND r.code = 'admin';
            """;

        return await ExecuteScalarAsync<int>(sql, [], cancellationToken);
    }

    private async Task<bool> UserExistsAsync(Guid userId, CancellationToken cancellationToken)
    {
        const string sql = "SELECT EXISTS (SELECT 1 FROM profiles WHERE id = @userId);";
        return await ExecuteScalarAsync<bool>(sql, [("@userId", userId)], cancellationToken);
    }

    private async Task<bool> RoleExistsAsync(long roleId, CancellationToken cancellationToken)
    {
        const string sql = "SELECT EXISTS (SELECT 1 FROM roles WHERE id = @roleId);";
        return await ExecuteScalarAsync<bool>(sql, [("@roleId", roleId)], cancellationToken);
    }

    private async Task<bool> DepartmentExistsAsync(long departmentId, CancellationToken cancellationToken)
    {
        const string sql = "SELECT EXISTS (SELECT 1 FROM departments WHERE id = @departmentId);";
        return await ExecuteScalarAsync<bool>(sql, [("@departmentId", departmentId)], cancellationToken);
    }

    private async Task<bool> ActiveDepartmentExistsAsync(long departmentId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1
                FROM departments
                WHERE id = @departmentId
                  AND is_active = TRUE
            );
            """;

        return await ExecuteScalarAsync<bool>(sql, [("@departmentId", departmentId)], cancellationToken);
    }

    private async Task<UserRoleGrantRow?> LoadUserRoleGrantRowAsync(
        Guid userId,
        long roleId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT p.id,
                   p.is_active,
                   EXISTS (SELECT 1 FROM roles WHERE id = @roleId) AS role_exists,
                   (SELECT code FROM roles WHERE id = @roleId) AS role_code,
                   EXISTS (
                       SELECT 1
                       FROM user_roles ur
                       JOIN roles r ON r.id = ur.role_id
                       WHERE ur.user_id = p.id
                         AND r.code = 'admin'
                   ) AS has_admin_role
            FROM profiles p
            WHERE p.id = @userId;
            """;

        var rows = await QueryAsync(
            sql,
            reader => new UserRoleGrantRow(
                reader.GetGuidValue("id"),
                reader.GetBooleanValue("is_active"),
                reader.GetBooleanValue("role_exists"),
                reader.GetNullableString("role_code"),
                reader.GetBooleanValue("has_admin_role")),
            [("@userId", userId), ("@roleId", roleId)],
            cancellationToken);

        return rows.SingleOrDefault();
    }

    private async Task RevokeRefreshSessionsAsync(Guid targetUserId, CancellationToken cancellationToken)
    {
        await ExecuteAsync(
            """
            UPDATE auth_refresh_sessions
            SET revoked_at = COALESCE(revoked_at, now())
            WHERE user_id = @targetUserId
              AND revoked_at IS NULL;
            """,
            [("@targetUserId", targetUserId)],
            cancellationToken);
    }

    private sealed record UserLockRow(Guid Id, bool IsAdmin);

    private sealed record UserRoleGrantRow(
        Guid Id,
        bool IsActive,
        bool RoleExists,
        string? RoleCode,
        bool HasAdminRole);
}
