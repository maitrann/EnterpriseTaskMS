using EnterpriseTask.Application.Tasks;
using EnterpriseTask.Infrastructure.Persistence;

namespace EnterpriseTask.Infrastructure.Tasks;

public sealed class PostgresTaskPolicyQueries(ApplicationDbContext dbContext) : PostgresCommandBase(dbContext), ITaskPolicyQueries
{
    public async Task<bool> HasPermissionAsync(Guid actorUserId, string permissionCode, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1
                FROM user_roles ur
                JOIN role_permissions rp ON rp.role_id = ur.role_id
                JOIN permissions p ON p.id = rp.permission_id
                WHERE ur.user_id = @actorUserId
                  AND p.code = @permissionCode
            );
            """;

        return await ExecuteScalarAsync<bool>(
            sql,
            [("@actorUserId", actorUserId), ("@permissionCode", permissionCode)],
            cancellationToken);
    }

    public async Task<bool> CanUseDepartmentAsync(Guid actorUserId, long? departmentId, CancellationToken cancellationToken)
    {
        if (departmentId is null)
        {
            return true;
        }

        const string sql = """
            SELECT EXISTS (
                SELECT 1
                FROM user_roles ur
                JOIN roles r ON r.id = ur.role_id
                WHERE ur.user_id = @actorUserId
                  AND r.code IN ('admin', 'director')
            )
            OR EXISTS (
                SELECT 1
                FROM profiles p
                WHERE p.id = @actorUserId
                  AND p.department_id = @departmentId
            )
            OR EXISTS (
                SELECT 1
                FROM user_department_scopes uds
                WHERE uds.user_id = @actorUserId
                  AND uds.department_id = @departmentId
            );
            """;

        return await ExecuteScalarAsync<bool>(
            sql,
            [("@actorUserId", actorUserId), ("@departmentId", departmentId.Value)],
            cancellationToken);
    }

    public async Task<TaskAccessResult> GetAccessAsync(
        Guid actorUserId,
        Guid taskId,
        string permissionCode,
        CancellationToken cancellationToken)
    {
        var exists = await ExecuteScalarAsync<bool>(
            "SELECT EXISTS (SELECT 1 FROM tasks WHERE id = @taskId);",
            [("@taskId", taskId)],
            cancellationToken);

        return new TaskAccessResult(
            exists,
            await HasPermissionAsync(actorUserId, permissionCode, cancellationToken),
            await CanAccessTaskAsync(actorUserId, taskId, cancellationToken));
    }

    private async Task<bool> CanAccessTaskAsync(Guid actorUserId, Guid taskId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1
                FROM tasks t
                WHERE t.id = @taskId
                  AND (
                    EXISTS (
                      SELECT 1
                      FROM user_roles ur
                      JOIN roles r ON r.id = ur.role_id
                      WHERE ur.user_id = @actorUserId
                        AND r.code IN ('admin', 'director')
                    )
                    OR (
                      (
                        t.created_by = @actorUserId
                        OR t.reporter_id = @actorUserId
                        OR EXISTS (
                          SELECT 1 FROM task_assignments ta
                          WHERE ta.task_id = t.id AND ta.user_id = @actorUserId
                        )
                        OR (
                          t.department_id IS NOT NULL
                          AND EXISTS (
                            SELECT 1 FROM user_roles ur JOIN roles r ON r.id = ur.role_id
                            WHERE ur.user_id = @actorUserId AND r.code = 'manager'
                          )
                          AND (
                            EXISTS (SELECT 1 FROM profiles me WHERE me.id = @actorUserId AND me.department_id = t.department_id)
                            OR EXISTS (SELECT 1 FROM user_department_scopes uds WHERE uds.user_id = @actorUserId AND uds.department_id = t.department_id)
                          )
                        )
                      )
                      AND (
                        t.is_confidential = FALSE
                        OR t.created_by = @actorUserId
                        OR t.reporter_id = @actorUserId
                        OR EXISTS (
                          SELECT 1 FROM task_assignments ta
                          WHERE ta.task_id = t.id AND ta.user_id = @actorUserId
                        )
                      )
                    )
                  )
            );
            """;

        return await ExecuteScalarAsync<bool>(
            sql,
            [("@actorUserId", actorUserId), ("@taskId", taskId)],
            cancellationToken);
    }
}
