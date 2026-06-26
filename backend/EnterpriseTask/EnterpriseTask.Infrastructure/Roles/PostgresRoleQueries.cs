using EnterpriseTask.Application.Roles;
using EnterpriseTask.Infrastructure.Persistence;

namespace EnterpriseTask.Infrastructure.Roles;

public sealed class PostgresRoleQueries(ApplicationDbContext dbContext)
    : PostgresQueryBase(dbContext), IRoleQueries
{
    public async Task<IReadOnlyList<RoleDto>> GetRolesAsync(CancellationToken cancellationToken)
    {
        const string rolesSql = """
            SELECT id, code, name, description, created_at
            FROM roles
            ORDER BY code;
            """;

        const string permissionsSql = """
            SELECT rp.role_id,
                   p.id,
                   p.code,
                   p.name,
                   p.module,
                   p.description,
                   p.created_at
            FROM role_permissions rp
            JOIN permissions p ON p.id = rp.permission_id
            ORDER BY rp.role_id, p.module, p.code;
            """;

        var roles = await QueryAsync(rolesSql, MapRoleRow, cancellationToken);
        var permissions = await QueryAsync(permissionsSql, MapRolePermissionRow, cancellationToken);
        var permissionsByRole = permissions
            .GroupBy(row => row.RoleId)
            .ToDictionary(group => group.Key, group => group.Select(row => row.Permission).ToArray());

        return roles
            .Select(role => new RoleDto(
                role.Id,
                role.Code,
                role.Name,
                role.Description,
                permissionsByRole.GetValueOrDefault(role.Id, []),
                role.CreatedAt))
            .ToArray();
    }

    public Task<IReadOnlyList<PermissionDto>> GetPermissionsAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT id, code, name, module, description, created_at
            FROM permissions
            ORDER BY module, code;
            """;

        return QueryAsync(sql, MapPermission, cancellationToken);
    }

    private static RoleRow MapRoleRow(System.Data.Common.DbDataReader reader)
    {
        return new RoleRow(
            reader.GetInt64Value("id"),
            reader.GetStringValue("code"),
            reader.GetStringValue("name"),
            reader.GetNullableString("description"),
            reader.GetDateTimeOffsetValue("created_at"));
    }

    private static RolePermissionRow MapRolePermissionRow(System.Data.Common.DbDataReader reader)
    {
        return new RolePermissionRow(
            reader.GetInt64Value("role_id"),
            MapPermission(reader));
    }

    private static PermissionDto MapPermission(System.Data.Common.DbDataReader reader)
    {
        return new PermissionDto(
            reader.GetInt64Value("id"),
            reader.GetStringValue("code"),
            reader.GetStringValue("name"),
            reader.GetStringValue("module"),
            reader.GetNullableString("description"),
            reader.GetDateTimeOffsetValue("created_at"));
    }

    private sealed record RoleRow(
        long Id,
        string Code,
        string Name,
        string? Description,
        DateTimeOffset CreatedAt);

    private sealed record RolePermissionRow(long RoleId, PermissionDto Permission);
}
