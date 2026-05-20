using EnterpriseTask.Application.Common;
using EnterpriseTask.Application.Tasks;
using EnterpriseTask.Infrastructure.Persistence;

namespace EnterpriseTask.Infrastructure.Tasks;

public sealed class PostgresTaskQueries(ApplicationDbContext dbContext) : PostgresQueryBase(dbContext), ITaskQueries
{
    public async Task<IReadOnlyList<TaskDto>> GetTasksAsync(UserScope scope, CancellationToken cancellationToken)
    {
        const string tasksSql = """
            SELECT
              t.id, t.code, t.project_id, t.parent_task_id, t.title, t.description, t.task_type,
              t.department_id, t.status_id, t.priority_id, t.urgency_level, t.security_level,
              t.reporter_id, t.assignee_id, t.start_date, t.due_date, t.progress, t.source,
              t.subtask_progress_auto_sync, t.parent_completion_suggested, t.estimated_hours,
              t.actual_hours, t.created_at, t.updated_at,
              COALESCE((SELECT array_agg(user_id ORDER BY user_id) FROM task_collaborators WHERE task_id = t.id), ARRAY[]::bigint[]) AS collaborator_ids,
              COALESCE((SELECT array_agg(user_id ORDER BY user_id) FROM task_watchers WHERE task_id = t.id), ARRAY[]::bigint[]) AS watcher_ids,
              COALESCE((SELECT array_agg(file_name ORDER BY id) FROM attachments WHERE task_id = t.id), ARRAY[]::text[]) AS attachment_names,
              COALESCE((SELECT array_agg(tags.name ORDER BY tags.name) FROM task_tags JOIN tags ON tags.id = task_tags.tag_id WHERE task_tags.task_id = t.id), ARRAY[]::text[]) AS tags,
              COALESCE((SELECT array_agg(content ORDER BY created_at DESC) FROM task_comments WHERE task_id = t.id), ARRAY[]::text[]) AS processing_notes
            FROM tasks t
            WHERE @isAdmin
               OR (@isManager AND t.department_id = @departmentId)
               OR t.reporter_id = @userId
               OR t.assignee_id = @userId
               OR EXISTS (SELECT 1 FROM task_collaborators tc WHERE tc.task_id = t.id AND tc.user_id = @userId)
               OR EXISTS (SELECT 1 FROM task_watchers tw WHERE tw.task_id = t.id AND tw.user_id = @userId)
            ORDER BY t.created_at DESC, t.id DESC;
            """;

        var tasks = (await QueryAsync(tasksSql, reader => new TaskDto(
            reader.GetInt64Value("id"),
            reader.GetStringValue("code"),
            reader.GetNullableInt64("project_id"),
            reader.GetNullableInt64("parent_task_id"),
            reader.GetStringValue("title"),
            reader.GetNullableString("description"),
            reader.GetNullableString("task_type"),
            reader.GetNullableInt64("department_id"),
            reader.GetNullableInt64("status_id"),
            reader.GetNullableInt64("priority_id"),
            reader.GetNullableString("urgency_level"),
            reader.GetNullableString("security_level"),
            reader.GetNullableInt64("reporter_id"),
            reader.GetNullableInt64("assignee_id"),
            (long[])reader["collaborator_ids"],
            (long[])reader["watcher_ids"],
            reader.GetNullableDateOnly("start_date"),
            reader.GetNullableDateOnly("due_date"),
            reader.GetInt32Value("progress"),
            reader.GetNullableString("source"),
            (string[])reader["attachment_names"],
            (string[])reader["tags"],
            (string[])reader["processing_notes"],
            [],
            [],
            reader.GetBooleanValue("subtask_progress_auto_sync"),
            reader.GetBooleanValue("parent_completion_suggested"),
            reader.GetNullableDecimal("estimated_hours"),
            reader.GetNullableDecimal("actual_hours"),
            reader.GetDateTimeOffsetValue("created_at"),
            reader.GetNullableDateTimeOffset("updated_at")),
            ScopeParameters(scope),
            cancellationToken)).ToList();

        var subtasks = await GetSubtasksAsync(cancellationToken);
        var extensions = await GetExtensionRequestsAsync(cancellationToken);

        return tasks
            .Select(task => task with
            {
                Subtasks = subtasks.Where(item => item.TaskId == task.Id).ToArray(),
                ExtensionRequests = extensions.Where(item => item.TaskId == task.Id).Select(item => item.Request).ToArray()
            })
            .ToList();
    }

    public Task<IReadOnlyList<TaskActivityDto>> GetActivitiesAsync(UserScope scope, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT a.id, a.task_id, a.user_id, a.action_type, a.old_value, a.new_value, a.created_at
            FROM task_activities a
            JOIN tasks t ON t.id = a.task_id
            WHERE @isAdmin
               OR (@isManager AND t.department_id = @departmentId)
               OR t.reporter_id = @userId
               OR t.assignee_id = @userId
               OR EXISTS (SELECT 1 FROM task_collaborators tc WHERE tc.task_id = t.id AND tc.user_id = @userId)
               OR EXISTS (SELECT 1 FROM task_watchers tw WHERE tw.task_id = t.id AND tw.user_id = @userId)
            ORDER BY a.created_at DESC, a.id DESC;
            """;

        return QueryAsync(sql, reader => new TaskActivityDto(
            reader.GetInt64Value("id"),
            reader.GetInt64Value("task_id"),
            reader.GetNullableInt64("user_id"),
            reader.GetNullableString("action_type"),
            reader.GetNullableString("old_value"),
            reader.GetNullableString("new_value"),
            reader.GetDateTimeOffsetValue("created_at")), ScopeParameters(scope), cancellationToken);
    }

    public async Task<TaskFormOptionsDto> GetFormOptionsAsync(UserScope scope, CancellationToken cancellationToken)
    {
        var departments = await QueryAsync(
            """
            SELECT id, name
            FROM departments
            WHERE @isAdmin OR id = @departmentId
            ORDER BY name;
            """,
            reader => new TaskDepartmentOptionDto(reader.GetInt64Value("id"), reader.GetStringValue("name")),
            ScopeParameters(scope),
            cancellationToken);

        var users = await QueryAsync(
            """
            SELECT id, COALESCE(full_name, username) AS label, COALESCE(role_label, '') AS role, department_id
            FROM users
            WHERE is_active = TRUE
              AND (@isAdmin OR department_id = @departmentId)
            ORDER BY label;
            """,
            reader => new TaskMemberOptionDto(
                reader.GetInt64Value("id"),
                reader.GetStringValue("label"),
                reader.GetStringValue("role"),
                reader.GetNullableInt64("department_id")),
            ScopeParameters(scope),
            cancellationToken);

        var priorities = await QueryAsync(
            "SELECT id, name, code::text AS code FROM task_priorities ORDER BY level;",
            reader => new OptionDto<long>(reader.GetInt64Value("id"), reader.GetStringValue("name"), reader.GetStringValue("code")),
            cancellationToken);

        return new TaskFormOptionsDto(
            [
                new("operations", "Vận hành"),
                new("approval", "Phê duyệt"),
                new("support", "Hỗ trợ"),
                new("reporting", "Báo cáo")
            ],
            departments,
            users,
            priorities,
            [
                new("low", "Thấp"),
                new("medium", "Trung bình"),
                new("high", "Cao"),
                new("critical", "Khẩn cấp")
            ],
            [
                new("internal", "Nội bộ"),
                new("confidential", "Bảo mật"),
                new("restricted", "Hạn chế")
            ],
            [
                new("manual", "Tạo thủ công"),
                new("project", "Từ dự án"),
                new("inter-request", "Từ yêu cầu liên phòng ban")
            ]);
    }

    private static IReadOnlyList<(string Name, object? Value)> ScopeParameters(UserScope scope) =>
    [
        ("@userId", scope.UserId),
        ("@departmentId", scope.DepartmentId),
        ("@isAdmin", scope.IsAdmin),
        ("@isManager", scope.IsManager)
    ];

    private Task<IReadOnlyList<SubTaskDto>> GetSubtasksAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT id, task_id, title, assignee_id, due_date, progress, done, sort_order, created_at, updated_at, completed_at
            FROM subtasks
            ORDER BY task_id, sort_order, id;
            """;

        return QueryAsync(sql, reader => new SubTaskDto(
            reader.GetInt64Value("id"),
            reader.GetInt64Value("task_id"),
            reader.GetStringValue("title"),
            reader.GetNullableInt64("assignee_id"),
            reader.GetNullableDateOnly("due_date"),
            reader.GetInt32Value("progress"),
            reader.GetBooleanValue("done"),
            reader.GetDateTimeOffsetValue("created_at").ToUnixTimeMilliseconds(),
            reader.GetNullableDateTimeOffset("updated_at")?.ToUnixTimeMilliseconds(),
            reader.GetNullableDateTimeOffset("completed_at")?.ToUnixTimeMilliseconds(),
            reader.GetInt32Value("sort_order")), cancellationToken);
    }

    private async Task<IReadOnlyList<(long TaskId, TaskExtensionRequestDto Request)>> GetExtensionRequestsAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT id, task_id, requested_due_date, reason, status::text AS status, requested_by_user_id,
                   requested_at, reviewed_by_user_id, reviewed_at, review_note
            FROM task_extension_requests
            ORDER BY requested_at DESC, id DESC;
            """;

        return await QueryAsync(sql, reader => (
            reader.GetInt64Value("task_id"),
            new TaskExtensionRequestDto(
                reader.GetInt64Value("id"),
                reader.GetNullableDateOnly("requested_due_date") ?? DateOnly.FromDateTime(DateTime.UtcNow),
                reader.GetStringValue("reason"),
                reader.GetStringValue("status"),
                reader.GetNullableInt64("requested_by_user_id"),
                reader.GetDateTimeOffsetValue("requested_at"),
                reader.GetNullableInt64("reviewed_by_user_id"),
                reader.GetNullableDateTimeOffset("reviewed_at"),
                reader.GetNullableString("review_note"))), cancellationToken);
    }
}
