using EnterpriseTask.Application.Common;
using EnterpriseTask.Application.Tasks;
using EnterpriseTask.Infrastructure.Persistence;

namespace EnterpriseTask.Infrastructure.Tasks;

public sealed class PostgresTaskQueries(ApplicationDbContext dbContext) : PostgresQueryBase(dbContext), ITaskQueries
{
    public async Task<IReadOnlyList<TaskDto>> GetTasksAsync(Guid actorUserId, CancellationToken cancellationToken)
    {
        const string tasksSql = """
            SELECT
              t.id, t.code, t.project_id, t.parent_task_id, t.title, t.description, t.task_type,
              t.department_id, t.status_id, t.priority_id, t.urgency_level, t.security_level,
              t.reporter_id, t.start_date, t.due_date, t.progress, t.source,
              t.subtask_progress_auto_sync, t.parent_completion_suggested, t.estimated_hours,
              t.actual_hours, t.created_at, t.updated_at,
              (SELECT user_id FROM task_assignments WHERE task_id = t.id AND assignment_type = 'assignee' ORDER BY assigned_at LIMIT 1) AS assignee_id,
              COALESCE((SELECT array_agg(user_id ORDER BY user_id) FROM task_assignments WHERE task_id = t.id AND assignment_type = 'co_assignee'), ARRAY[]::uuid[]) AS collaborator_ids,
              COALESCE((SELECT array_agg(user_id ORDER BY user_id) FROM task_assignments WHERE task_id = t.id AND assignment_type = 'watcher'), ARRAY[]::uuid[]) AS watcher_ids,
              COALESCE((SELECT array_agg(file_name ORDER BY id) FROM attachments WHERE task_id = t.id), ARRAY[]::text[]) AS attachment_names,
              COALESCE((SELECT array_agg(tags.name ORDER BY tags.name) FROM task_tags JOIN tags ON tags.id = task_tags.tag_id WHERE task_tags.task_id = t.id), ARRAY[]::text[]) AS tags,
              COALESCE((SELECT array_agg(content ORDER BY created_at DESC) FROM task_comments WHERE task_id = t.id), ARRAY[]::text[]) AS processing_notes
            FROM tasks t
            WHERE
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
                    SELECT 1
                    FROM task_assignments ta
                    WHERE ta.task_id = t.id
                      AND ta.user_id = @actorUserId
                  )
                  OR (
                    t.department_id IS NOT NULL
                    AND EXISTS (
                      SELECT 1
                      FROM user_roles ur
                      JOIN roles r ON r.id = ur.role_id
                      WHERE ur.user_id = @actorUserId
                        AND r.code = 'manager'
                    )
                    AND (
                      EXISTS (
                        SELECT 1
                        FROM profiles me
                        WHERE me.id = @actorUserId
                          AND me.department_id = t.department_id
                      )
                      OR EXISTS (
                        SELECT 1
                        FROM user_department_scopes uds
                        WHERE uds.user_id = @actorUserId
                          AND uds.department_id = t.department_id
                      )
                    )
                  )
                )
                AND (
                  t.is_confidential = FALSE
                  OR t.created_by = @actorUserId
                  OR t.reporter_id = @actorUserId
                  OR EXISTS (
                    SELECT 1
                    FROM task_assignments ta
                    WHERE ta.task_id = t.id
                      AND ta.user_id = @actorUserId
                  )
                )
              )
            ORDER BY t.created_at DESC, t.id DESC;
            """;

        var tasks = (await QueryAsync(tasksSql, reader => new TaskDto(
            reader.GetGuidValue("id"),
            reader.GetStringValue("code"),
            reader.GetNullableGuid("project_id"),
            reader.GetNullableGuid("parent_task_id"),
            reader.GetStringValue("title"),
            reader.GetNullableString("description"),
            reader.GetNullableString("task_type"),
            reader.GetNullableInt64("department_id"),
            reader.GetNullableInt64("status_id"),
            reader.GetNullableInt64("priority_id"),
            reader.GetNullableString("urgency_level"),
            reader.GetNullableString("security_level"),
            reader.GetNullableGuid("reporter_id"),
            reader.GetNullableGuid("assignee_id"),
            (Guid[])reader["collaborator_ids"],
            (Guid[])reader["watcher_ids"],
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
            [("@actorUserId", actorUserId)],
            cancellationToken)).ToList();

        var visibleTaskIds = tasks.Select(task => task.Id).ToArray();
        var subtasks = visibleTaskIds.Length == 0
            ? []
            : await GetSubtasksAsync(visibleTaskIds, cancellationToken);
        var extensions = visibleTaskIds.Length == 0
            ? []
            : await GetExtensionRequestsAsync(visibleTaskIds, cancellationToken);

        return tasks
            .Select(task => task with
            {
                Subtasks = subtasks.Where(item => item.TaskId == task.Id).ToArray(),
                ExtensionRequests = extensions.Where(item => item.TaskId == task.Id).Select(item => item.Request).ToArray()
            })
            .ToList();
    }

    public Task<IReadOnlyList<TaskActivityDto>> GetActivitiesAsync(Guid actorUserId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT a.id, a.task_id, a.user_id, a.action_type, a.old_value, a.new_value, a.created_at
            FROM task_activities a
            WHERE EXISTS (
              SELECT 1
              FROM tasks t
              WHERE t.id = a.task_id
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
                          SELECT 1
                          FROM user_roles ur
                          JOIN roles r ON r.id = ur.role_id
                          WHERE ur.user_id = @actorUserId
                            AND r.code = 'manager'
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
                      OR EXISTS (SELECT 1 FROM task_assignments ta WHERE ta.task_id = t.id AND ta.user_id = @actorUserId)
                    )
                  )
                )
            )
            ORDER BY a.created_at DESC, a.id DESC;
            """;

        return QueryAsync(sql, reader => new TaskActivityDto(
            reader.GetGuidValue("id"),
            reader.GetGuidValue("task_id"),
            reader.GetNullableGuid("user_id"),
            reader.GetNullableString("action_type"),
            reader.GetNullableString("old_value"),
            reader.GetNullableString("new_value"),
            reader.GetDateTimeOffsetValue("created_at")),
            [("@actorUserId", actorUserId)],
            cancellationToken);
    }

    public async Task<TaskFormOptionsDto> GetFormOptionsAsync(Guid actorUserId, CancellationToken cancellationToken)
    {
        var departments = await QueryAsync(
            """
            SELECT d.id, d.name
            FROM departments d
            WHERE
              EXISTS (
                SELECT 1
                FROM user_roles ur
                JOIN roles r ON r.id = ur.role_id
                WHERE ur.user_id = @actorUserId
                  AND r.code IN ('admin', 'director')
              )
              OR EXISTS (
                SELECT 1
                FROM profiles me
                WHERE me.id = @actorUserId
                  AND me.department_id = d.id
              )
              OR EXISTS (
                SELECT 1
                FROM user_department_scopes uds
                WHERE uds.user_id = @actorUserId
                  AND uds.department_id = d.id
              )
            ORDER BY d.name;
            """,
            reader => new TaskDepartmentOptionDto(reader.GetInt64Value("id"), reader.GetStringValue("name")),
            [("@actorUserId", actorUserId)],
            cancellationToken);

        var users = await QueryAsync(
            """
            SELECT p.id, COALESCE(p.full_name, p.email, p.employee_code, p.id::text) AS label,
                   COALESCE(p.job_title, '') AS role, p.department_id
            FROM profiles p
            WHERE is_active = TRUE
              AND (
                p.id = @actorUserId
                OR EXISTS (
                  SELECT 1
                  FROM user_roles ur
                  JOIN role_permissions rp ON rp.role_id = ur.role_id
                  JOIN permissions perm ON perm.id = rp.permission_id
                  WHERE ur.user_id = @actorUserId
                    AND perm.code = 'task.assign'
                )
              )
            ORDER BY label;
            """,
            reader => new TaskMemberOptionDto(
                reader.GetGuidValue("id"),
                reader.GetStringValue("label"),
                reader.GetStringValue("role"),
                reader.GetNullableInt64("department_id")),
            [("@actorUserId", actorUserId)],
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

    private Task<IReadOnlyList<SubTaskDto>> GetSubtasksAsync(Guid[] taskIds, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT s.id, s.task_id, s.title, s.assignee_id, s.status::text AS status, s.due_date, s.progress, s.done, s.sort_order, s.created_at, s.updated_at, s.completed_at
            FROM subtasks s
            WHERE s.task_id = ANY(@taskIds)
            ORDER BY s.task_id, s.sort_order, s.id;
            """;

        return QueryAsync(sql, reader => new SubTaskDto(
            reader.GetGuidValue("id"),
            reader.GetGuidValue("task_id"),
            reader.GetStringValue("title"),
            reader.GetNullableGuid("assignee_id"),
            reader.GetStringValue("status"),
            reader.GetNullableDateOnly("due_date"),
            reader.GetInt32Value("progress"),
            reader.GetBooleanValue("done"),
            reader.GetDateTimeOffsetValue("created_at").ToUnixTimeMilliseconds(),
            reader.GetNullableDateTimeOffset("updated_at")?.ToUnixTimeMilliseconds(),
            reader.GetNullableDateTimeOffset("completed_at")?.ToUnixTimeMilliseconds(),
            reader.GetInt32Value("sort_order")),
            [("@taskIds", taskIds)],
            cancellationToken);
    }

    private async Task<IReadOnlyList<(Guid TaskId, TaskExtensionRequestDto Request)>> GetExtensionRequestsAsync(Guid[] taskIds, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT e.id, e.task_id, e.requested_due_date, e.reason, e.status::text AS status, e.requested_by_user_id,
                   e.requested_at, e.reviewed_by_user_id, e.reviewed_at, e.review_note
            FROM task_extension_requests e
            WHERE e.task_id = ANY(@taskIds)
            ORDER BY e.requested_at DESC, e.id DESC;
            """;

        return await QueryAsync(sql, reader => (
            reader.GetGuidValue("task_id"),
            new TaskExtensionRequestDto(
                reader.GetGuidValue("id"),
                reader.GetNullableDateOnly("requested_due_date") ?? DateOnly.FromDateTime(DateTime.UtcNow),
                reader.GetStringValue("reason"),
                reader.GetStringValue("status"),
                reader.GetNullableGuid("requested_by_user_id"),
                reader.GetDateTimeOffsetValue("requested_at"),
                reader.GetNullableGuid("reviewed_by_user_id"),
                reader.GetNullableDateTimeOffset("reviewed_at"),
                reader.GetNullableString("review_note"))),
            [("@taskIds", taskIds)],
            cancellationToken);
    }
}
