using EnterpriseTask.Application.Common;
using EnterpriseTask.Infrastructure.Persistence;

namespace EnterpriseTask.Infrastructure.Auth;

public sealed class PostgresPermissionChecker(ApplicationDbContext dbContext)
    : PostgresCommandBase(dbContext), IPermissionChecker
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
}
