using EnterpriseTask.Application.Projects;
using EnterpriseTask.Infrastructure.Persistence;

namespace EnterpriseTask.Infrastructure.Projects;

public sealed class PostgresProjectQueries(ApplicationDbContext dbContext) : PostgresQueryBase(dbContext), IProjectQueries
{
    public Task<IReadOnlyList<ProjectDto>> GetProjectsAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT id, code, name, description, department_id, owner_id, start_date, end_date,
                   status::text AS status, created_by, created_at, updated_at
            FROM projects
            ORDER BY created_at DESC, id DESC;
            """;

        return QueryAsync(sql, reader => new ProjectDto(
            reader.GetInt64Value("id"),
            reader.GetNullableString("code"),
            reader.GetStringValue("name"),
            reader.GetNullableString("description"),
            reader.GetNullableInt64("department_id"),
            reader.GetNullableInt64("owner_id"),
            reader.GetNullableDateOnly("start_date"),
            reader.GetNullableDateOnly("end_date"),
            reader.GetNullableString("status"),
            reader.GetNullableInt64("created_by"),
            reader.GetDateTimeOffsetValue("created_at"),
            reader.GetNullableDateTimeOffset("updated_at")), cancellationToken);
    }
}
