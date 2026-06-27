using EnterpriseTask.Application.Common;
using EnterpriseTask.Application.Departments;
using EnterpriseTask.Infrastructure.Persistence;

namespace EnterpriseTask.Infrastructure.Departments;

public sealed class PostgresDepartmentQueries(ApplicationDbContext dbContext) : PostgresQueryBase(dbContext), IDepartmentQueries
{
    public Task<IReadOnlyList<DepartmentCardDto>> GetCardsAsync(UserScope scope, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
              d.name,
              d.description,
              COUNT(DISTINCT u.id)::int AS members,
              COUNT(DISTINCT t.id) FILTER (WHERE COALESCE(ts.is_closed, FALSE) = FALSE)::int AS active_tasks,
              COUNT(DISTINCT t.id) FILTER (WHERE COALESCE(ts.is_closed, FALSE) = TRUE)::int AS completed_tasks,
              COALESCE(MAX(u.full_name), MAX(u.email), MAX(u.employee_code), 'Chưa phân công') AS lead,
              '95%' AS sla,
              CASE
                WHEN COUNT(DISTINCT t.id) FILTER (WHERE COALESCE(ts.is_closed, FALSE) = FALSE) > 5 THEN 'amber'
                WHEN COUNT(DISTINCT t.id) FILTER (WHERE COALESCE(ts.is_closed, FALSE) = TRUE) > 0 THEN 'emerald'
                ELSE 'blue'
              END AS tone
            FROM departments d
            LEFT JOIN profiles u ON u.department_id = d.id
            LEFT JOIN tasks t ON t.department_id = d.id
            LEFT JOIN task_statuses ts ON ts.id = t.status_id
            WHERE @isElevated OR d.id = @departmentId
            GROUP BY d.id, d.name, d.description
            ORDER BY d.name;
            """;

        return QueryAsync(sql, reader => new DepartmentCardDto(
            reader.GetStringValue("name"),
            reader.GetNullableString("description"),
            reader.GetInt32Value("members"),
            reader.GetInt32Value("active_tasks"),
            reader.GetInt32Value("completed_tasks"),
            reader.GetStringValue("lead"),
            reader.GetStringValue("sla"),
            reader.GetStringValue("tone")),
            [("@departmentId", scope.DepartmentId), ("@isElevated", scope.CanSeeAllData)],
            cancellationToken);
    }

    public Task<IReadOnlyList<DepartmentOptionDto>> GetOptionsAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT id, name
            FROM departments
            WHERE is_active = TRUE
            ORDER BY name;
            """;

        return QueryAsync(
            sql,
            reader => new DepartmentOptionDto(
                reader.GetInt64Value("id"),
                reader.GetStringValue("name")),
            cancellationToken);
    }

    public Task<IReadOnlyList<DepartmentListItemDto>> GetListAsync(bool includeInactive, CancellationToken cancellationToken)
    {
        return QueryAsync(
            GetDepartmentListSql(),
            MapDepartmentListItem,
            [("@includeInactive", includeInactive)],
            cancellationToken);
    }

    public async Task<IReadOnlyList<DepartmentTreeNodeDto>> GetTreeAsync(bool includeInactive, CancellationToken cancellationToken)
    {
        var items = await GetListAsync(includeInactive, cancellationToken);
        return BuildTree(items);
    }

    private static string GetDepartmentListSql()
    {
        return """
            SELECT
              d.id,
              d.company_id,
              d.code,
              d.name,
              d.description,
              d.parent_department_id,
              parent.name AS parent_department_name,
              d.manager_id,
              COALESCE(manager.full_name, manager.email, manager.employee_code) AS manager_name,
              COUNT(DISTINCT member.id)::int AS member_count,
              COUNT(DISTINCT task.id) FILTER (WHERE COALESCE(status.is_closed, FALSE) = FALSE)::int AS active_task_count,
              d.is_active,
              d.created_at,
              d.updated_at
            FROM departments d
            LEFT JOIN departments parent ON parent.id = d.parent_department_id
            LEFT JOIN profiles manager ON manager.id = d.manager_id
            LEFT JOIN profiles member ON member.department_id = d.id AND member.is_active = TRUE
            LEFT JOIN tasks task ON task.department_id = d.id
            LEFT JOIN task_statuses status ON status.id = task.status_id
            WHERE @includeInactive OR d.is_active = TRUE
            GROUP BY d.id, parent.name, manager.full_name, manager.email, manager.employee_code
            ORDER BY COALESCE(parent.name, d.name), d.parent_department_id NULLS FIRST, d.name;
            """;
    }

    private static DepartmentListItemDto MapDepartmentListItem(System.Data.Common.DbDataReader reader)
    {
        return new DepartmentListItemDto(
            reader.GetInt64Value("id"),
            reader.GetInt64Value("company_id"),
            reader.GetNullableString("code"),
            reader.GetStringValue("name"),
            reader.GetNullableString("description"),
            reader.GetNullableInt64("parent_department_id"),
            reader.GetNullableString("parent_department_name"),
            reader.GetNullableGuid("manager_id"),
            reader.GetNullableString("manager_name"),
            reader.GetInt32Value("member_count"),
            reader.GetInt32Value("active_task_count"),
            reader.GetBooleanValue("is_active"),
            reader.GetDateTimeOffsetValue("created_at"),
            reader.GetNullableDateTimeOffset("updated_at"));
    }

    private static IReadOnlyList<DepartmentTreeNodeDto> BuildTree(IReadOnlyList<DepartmentListItemDto> items)
    {
        var itemById = items.ToDictionary(item => item.Id);
        var childrenByParent = items
            .Where(item => item.ParentDepartmentId is not null)
            .GroupBy(item => item.ParentDepartmentId!.Value)
            .ToDictionary(group => group.Key, group => group.OrderBy(item => item.Name).ToList());

        var roots = items
            .Where(item => item.ParentDepartmentId is null || !itemById.ContainsKey(item.ParentDepartmentId.Value))
            .OrderBy(item => item.Name)
            .ToList();

        var rootIds = roots.Select(item => item.Id).ToHashSet();
        roots.AddRange(items
            .Where(item => !rootIds.Contains(item.Id) && IsDetachedFromRoots(item, itemById))
            .OrderBy(item => item.Name));

        return roots.Select(root => ToTreeNode(root, childrenByParent, new HashSet<long>())).ToList();
    }

    private static bool IsDetachedFromRoots(DepartmentListItemDto item, IReadOnlyDictionary<long, DepartmentListItemDto> itemById)
    {
        var visited = new HashSet<long>();
        var current = item;

        while (current.ParentDepartmentId is not null)
        {
            if (!visited.Add(current.Id))
            {
                return true;
            }

            if (!itemById.TryGetValue(current.ParentDepartmentId.Value, out var parent))
            {
                return true;
            }

            current = parent;
        }

        return false;
    }

    private static DepartmentTreeNodeDto ToTreeNode(
        DepartmentListItemDto item,
        IReadOnlyDictionary<long, List<DepartmentListItemDto>> childrenByParent,
        HashSet<long> path)
    {
        if (!path.Add(item.Id))
        {
            return ToTreeNode(item, Array.Empty<DepartmentTreeNodeDto>());
        }

        var children = childrenByParent.TryGetValue(item.Id, out var childItems)
            ? childItems
                .Where(child => !path.Contains(child.Id))
                .Select(child => ToTreeNode(child, childrenByParent, new HashSet<long>(path)))
                .ToList()
            : new List<DepartmentTreeNodeDto>();

        return ToTreeNode(item, children);
    }

    private static DepartmentTreeNodeDto ToTreeNode(
        DepartmentListItemDto item,
        IReadOnlyList<DepartmentTreeNodeDto> children)
    {
        return new DepartmentTreeNodeDto(
            item.Id,
            item.CompanyId,
            item.Code,
            item.Name,
            item.Description,
            item.ParentDepartmentId,
            item.ParentDepartmentName,
            item.ManagerId,
            item.ManagerName,
            item.MemberCount,
            item.ActiveTaskCount,
            item.IsActive,
            item.CreatedAt,
            item.UpdatedAt,
            children);
    }
}
