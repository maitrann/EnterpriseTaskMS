using EnterpriseTask.Application.Common;
using EnterpriseTask.Application.Users;
using EnterpriseTask.Infrastructure.Persistence;

namespace EnterpriseTask.Infrastructure.Users;

public sealed class PostgresUserQueries(ApplicationDbContext dbContext)
    : PostgresCommandBase(dbContext), IUserQueries
{
    public async Task<PagedResult<UserListItemDto>> GetUsersAsync(
        UserListQuery query,
        CancellationToken cancellationToken)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var offset = (page - 1) * pageSize;
        var search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim();
        var parameters = CreateFilterParameters(search, query.DepartmentId, query.IsActive);

        var total = await ExecuteScalarAsync<int>(
            $"""
            SELECT COUNT(*)
            FROM profiles p
            {UserFilterSql}
            """,
            parameters,
            cancellationToken);

        var users = await QueryAsync(
            $"""
            SELECT p.id,
                   p.employee_code,
                   p.email,
                   p.full_name,
                   p.avatar_url,
                   p.department_id,
                   d.name AS department_name,
                   p.manager_id,
                   manager.full_name AS manager_name,
                   p.job_title,
                   p.is_active,
                   p.created_at,
                   p.updated_at,
                   COALESCE(roles.role_codes, '') AS role_codes,
                   COALESCE(roles.role_names, '') AS role_names,
                   COALESCE(scopes.department_ids, '') AS scoped_department_ids,
                   COALESCE(scopes.department_names, '') AS scoped_department_names
            FROM profiles p
            LEFT JOIN departments d ON d.id = p.department_id
            LEFT JOIN profiles manager ON manager.id = p.manager_id
            LEFT JOIN LATERAL (
                SELECT string_agg(r.code, ',' ORDER BY r.code) AS role_codes,
                       string_agg(r.name, '|' ORDER BY r.code) AS role_names
                FROM user_roles ur
                JOIN roles r ON r.id = ur.role_id
                WHERE ur.user_id = p.id
            ) roles ON TRUE
            LEFT JOIN LATERAL (
                SELECT string_agg(uds.department_id::text, ',' ORDER BY dep.name) AS department_ids,
                       string_agg(dep.name, '|' ORDER BY dep.name) AS department_names
                FROM user_department_scopes uds
                JOIN departments dep ON dep.id = uds.department_id
                WHERE uds.user_id = p.id
            ) scopes ON TRUE
            {UserFilterSql}
            ORDER BY p.created_at DESC, p.full_name NULLS LAST, p.email NULLS LAST
            LIMIT @pageSize OFFSET @offset;
            """,
            MapUserListItem,
            [.. parameters, ("@pageSize", pageSize), ("@offset", offset)],
            cancellationToken);

        return new PagedResult<UserListItemDto>(users, page, pageSize, total);
    }

    public async Task<UserDetailDto?> GetUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT p.id,
                   p.employee_code,
                   p.email,
                   p.full_name,
                   p.avatar_url,
                   p.department_id,
                   d.name AS department_name,
                   p.manager_id,
                   manager.full_name AS manager_name,
                   p.job_title,
                   p.is_active,
                   p.created_at,
                   p.updated_at,
                   COALESCE(roles.role_codes, '') AS role_codes,
                   COALESCE(roles.role_names, '') AS role_names,
                   COALESCE(scopes.department_ids, '') AS scoped_department_ids,
                   COALESCE(scopes.department_names, '') AS scoped_department_names
            FROM profiles p
            LEFT JOIN departments d ON d.id = p.department_id
            LEFT JOIN profiles manager ON manager.id = p.manager_id
            LEFT JOIN LATERAL (
                SELECT string_agg(r.code, ',' ORDER BY r.code) AS role_codes,
                       string_agg(r.name, '|' ORDER BY r.code) AS role_names
                FROM user_roles ur
                JOIN roles r ON r.id = ur.role_id
                WHERE ur.user_id = p.id
            ) roles ON TRUE
            LEFT JOIN LATERAL (
                SELECT string_agg(uds.department_id::text, ',' ORDER BY dep.name) AS department_ids,
                       string_agg(dep.name, '|' ORDER BY dep.name) AS department_names
                FROM user_department_scopes uds
                JOIN departments dep ON dep.id = uds.department_id
                WHERE uds.user_id = p.id
            ) scopes ON TRUE
            WHERE p.id = @userId;
            """;

        var users = await QueryAsync(
            sql,
            MapUserDetail,
            [("@userId", userId)],
            cancellationToken);

        return users.SingleOrDefault();
    }

    private const string UserFilterSql = """
        WHERE (@search::text IS NULL
               OR p.email ILIKE '%' || @search::text || '%'
               OR p.full_name ILIKE '%' || @search::text || '%'
               OR p.employee_code ILIKE '%' || @search::text || '%')
          AND (@departmentId::bigint IS NULL OR p.department_id = @departmentId::bigint)
          AND (@isActive::boolean IS NULL OR p.is_active = @isActive::boolean)
        """;

    private static IReadOnlyList<(string Name, object? Value)> CreateFilterParameters(
        string? search,
        long? departmentId,
        bool? isActive)
    {
        return
        [
            ("@search", search),
            ("@departmentId", departmentId),
            ("@isActive", isActive)
        ];
    }

    private static UserListItemDto MapUserListItem(System.Data.Common.DbDataReader reader)
    {
        return new UserListItemDto(
            reader.GetGuidValue("id"),
            reader.GetNullableString("employee_code"),
            reader.GetNullableString("email"),
            reader.GetNullableString("full_name"),
            reader.GetNullableString("avatar_url"),
            reader.GetNullableInt64("department_id"),
            reader.GetNullableString("department_name"),
            reader.GetNullableGuid("manager_id"),
            reader.GetNullableString("manager_name"),
            reader.GetNullableString("job_title"),
            reader.GetBooleanValue("is_active"),
            SplitStrings(reader.GetStringValue("role_codes"), ','),
            SplitStrings(reader.GetStringValue("role_names"), '|'),
            SplitLongs(reader.GetStringValue("scoped_department_ids")),
            SplitStrings(reader.GetStringValue("scoped_department_names"), '|'),
            reader.GetDateTimeOffsetValue("created_at"),
            reader.GetNullableDateTimeOffset("updated_at"));
    }

    private static UserDetailDto MapUserDetail(System.Data.Common.DbDataReader reader)
    {
        var listItem = MapUserListItem(reader);
        return new UserDetailDto(
            listItem.Id,
            listItem.EmployeeCode,
            listItem.Email,
            listItem.FullName,
            listItem.AvatarUrl,
            listItem.DepartmentId,
            listItem.DepartmentName,
            listItem.ManagerId,
            listItem.ManagerName,
            listItem.JobTitle,
            listItem.IsActive,
            listItem.RoleCodes,
            listItem.RoleNames,
            listItem.ScopedDepartmentIds,
            listItem.ScopedDepartmentNames,
            listItem.CreatedAt,
            listItem.UpdatedAt);
    }

    private static IReadOnlyList<string> SplitStrings(string value, char separator)
    {
        return value.Split(separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static IReadOnlyList<long> SplitLongs(string value)
    {
        return SplitStrings(value, ',').Select(long.Parse).ToArray();
    }
}
