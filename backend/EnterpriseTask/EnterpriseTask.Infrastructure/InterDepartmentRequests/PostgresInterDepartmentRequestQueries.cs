using System.Text.Json;
using EnterpriseTask.Application.InterDepartmentRequests;
using EnterpriseTask.Infrastructure.Persistence;

namespace EnterpriseTask.Infrastructure.InterDepartmentRequests;

public sealed class PostgresInterDepartmentRequestQueries(ApplicationDbContext dbContext)
    : PostgresQueryBase(dbContext), IInterDepartmentRequestQueries
{
    public async Task<IReadOnlyList<InterDepartmentRequestDto>> GetRequestsAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
              r.id::text AS id, r.code, r.type::text AS type, r.title, r.description,
              COALESCE(rd.name, '') AS requester_department,
              COALESCE(r.requester_department_id::text, '') AS requester_department_id,
              COALESCE(ru.full_name, ru.email, ru.employee_code, '') AS requester_name,
              r.requester_user_id,
              COALESCE(td.name, '') AS target_department,
              COALESCE(r.target_department_id::text, '') AS target_department_id,
              COALESCE(ou.full_name, ou.email, ou.employee_code) AS owner,
              r.owner_id::text AS owner_id,
              r.priority::text AS priority,
              r.status::text AS status,
              r.created_at, r.updated_at, r.received_at, r.closed_at, r.due_date,
              COALESCE(p.key::text, r.sla_policy_key::text, r.type::text) AS policy_key,
              COALESCE(p.label, r.type::text) AS policy_label,
              COALESCE(p.target_hours, 24) AS target_hours,
              COALESCE(p.warn_hours, 4) AS warn_hours,
              COALESCE(r.sla_started_at, r.created_at) AS sla_started_at,
              COALESCE(r.sla_due_at, r.due_date::timestamp with time zone) AS sla_due_at,
              r.sla_breached,
              r.form_values::text AS form_values,
              r.latest_message,
              r.note
            FROM inter_department_requests r
            LEFT JOIN departments rd ON rd.id = r.requester_department_id
            LEFT JOIN departments td ON td.id = r.target_department_id
            LEFT JOIN profiles ru ON ru.id = r.requester_user_id
            LEFT JOIN profiles ou ON ou.id = r.owner_id
            LEFT JOIN inter_request_sla_policies p ON p.key = r.sla_policy_key
            ORDER BY r.created_at DESC;
            """;

        var requests = (await QueryAsync(sql, reader =>
        {
            var dueAt = reader.GetDateTimeOffsetValue("sla_due_at");
            var remainingHours = (int)Math.Round((dueAt - DateTimeOffset.UtcNow).TotalHours);

            return new InterDepartmentRequestDto(
                reader.GetStringValue("id"),
                reader.GetStringValue("code"),
                reader.GetStringValue("type"),
                reader.GetStringValue("title"),
                reader.GetStringValue("description"),
                reader.GetStringValue("requester_department"),
                reader.GetStringValue("requester_department_id"),
                reader.GetStringValue("requester_name"),
                reader.GetNullableGuid("requester_user_id"),
                reader.GetStringValue("target_department"),
                reader.GetStringValue("target_department_id"),
                reader.GetNullableString("owner"),
                reader.GetNullableString("owner_id"),
                reader.GetStringValue("priority"),
                reader.GetStringValue("status"),
                FormatDateTime(reader.GetDateTimeOffsetValue("created_at")),
                FormatNullableDateTime(reader.GetNullableDateTimeOffset("updated_at")),
                FormatNullableDateTime(reader.GetNullableDateTimeOffset("received_at")),
                FormatNullableDateTime(reader.GetNullableDateTimeOffset("closed_at")),
                FormatDate(reader.GetNullableDateOnly("due_date")),
                new RequestSlaSnapshotDto(
                    reader.GetStringValue("policy_key"),
                    reader.GetStringValue("policy_label"),
                    reader.GetInt32Value("target_hours"),
                    reader.GetInt32Value("warn_hours"),
                    reader.GetDateTimeOffsetValue("sla_started_at").ToString("O"),
                    dueAt.ToString("O"),
                    remainingHours,
                    reader.GetBooleanValue("sla_breached") || remainingHours < 0),
                ParseFormValues(reader.GetStringValue("form_values")),
                reader.GetNullableString("latest_message"),
                reader.GetNullableString("note"),
                []);
        }, cancellationToken)).ToList();

        var messages = await GetMessagesAsync(cancellationToken);

        return requests
            .Select(request => request with
            {
                Messages = messages.Where(item => item.RequestId == request.Id).Select(item => item.Message).ToList()
            })
            .ToList();
    }

    public Task<IReadOnlyList<RequestDepartmentRefDto>> GetDepartmentOptionsAsync(CancellationToken cancellationToken)
    {
        return QueryAsync(
            "SELECT id::text AS id, name FROM departments ORDER BY name;",
            reader => new RequestDepartmentRefDto(reader.GetStringValue("id"), reader.GetStringValue("name")),
            cancellationToken);
    }

    public Task<IReadOnlyList<RequestOwnerRefDto>> GetOwnerOptionsAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT u.id::text AS id, COALESCE(u.full_name, u.email, u.employee_code, u.id::text) AS name,
                   d.id::text AS department_id, d.name AS department_name
            FROM profiles u
            JOIN departments d ON d.id = u.department_id
            WHERE u.is_active = TRUE
            ORDER BY d.name, name;
            """;

        return QueryAsync(sql, reader => new RequestOwnerRefDto(
            reader.GetStringValue("id"),
            reader.GetStringValue("name"),
            reader.GetStringValue("department_id"),
            reader.GetStringValue("department_name")), cancellationToken);
    }

    public Task<IReadOnlyList<RequestSlaPolicyDto>> GetSlaPoliciesAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT key::text AS key, label, target_hours, warn_hours
            FROM inter_request_sla_policies
            ORDER BY label;
            """;

        return QueryAsync(sql, reader => new RequestSlaPolicyDto(
            reader.GetStringValue("key"),
            reader.GetStringValue("label"),
            reader.GetInt32Value("target_hours"),
            reader.GetInt32Value("warn_hours")), cancellationToken);
    }

    private async Task<IReadOnlyList<(string RequestId, RequestMessageDto Message)>> GetMessagesAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT m.request_id::text AS request_id, m.id::text AS id, m.author_name,
                   m.author_role::text AS author_role, m.author_department, m.created_at, m.body
            FROM inter_request_messages m
            ORDER BY m.created_at;
            """;

        return await QueryAsync(sql, reader => (
            reader.GetStringValue("request_id"),
            new RequestMessageDto(
                reader.GetStringValue("id"),
                reader.GetStringValue("author_name"),
                reader.GetStringValue("author_role"),
                reader.GetNullableString("author_department"),
                FormatDateTime(reader.GetDateTimeOffsetValue("created_at")),
                reader.GetStringValue("body"))), cancellationToken);
    }

    private static Dictionary<string, string> ParseFormValues(string value)
    {
        return JsonSerializer.Deserialize<Dictionary<string, string>>(value) ?? [];
    }

    private static string FormatDate(DateOnly? value)
    {
        return value is null ? string.Empty : $"{value.Value.Day:00}/{value.Value.Month:00}/{value.Value.Year}";
    }

    private static string FormatDateTime(DateTimeOffset value)
    {
        return value.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
    }

    private static string? FormatNullableDateTime(DateTimeOffset? value)
    {
        return value is null ? null : FormatDateTime(value.Value);
    }
}
