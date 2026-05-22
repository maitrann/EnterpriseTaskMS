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
}
