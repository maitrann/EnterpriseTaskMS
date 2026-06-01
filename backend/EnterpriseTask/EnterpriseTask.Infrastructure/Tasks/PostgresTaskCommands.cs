using EnterpriseTask.Application.Tasks;
using EnterpriseTask.Infrastructure.Persistence;

namespace EnterpriseTask.Infrastructure.Tasks;

public sealed class PostgresTaskCommands(ApplicationDbContext dbContext) : PostgresCommandBase(dbContext), ITaskCommands
{
    public async Task<TaskCreateResult> CreateAsync(Guid actorUserId, CreateTaskRequest request, CancellationToken cancellationToken)
    {
        var collaborators = NormalizeUserIds(request.CollaboratorIds);
        var watchers = NormalizeUserIds(request.WatcherIds);
        var code = $"CV-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}";
        const string sql = """
            INSERT INTO tasks (code, project_id, parent_task_id, title, description, task_type, department_id,
                               status_id, priority_id, reporter_id, created_by, start_date, due_date, progress,
                               source, urgency_level, security_level, is_confidential, estimated_hours, actual_hours)
            VALUES (@code, @projectId, @parentTaskId, @title, @description, @taskType, @departmentId,
                    COALESCE((SELECT id FROM task_statuses WHERE code = 'new'), 1),
                    @priorityId, @actorUserId, @actorUserId, @startDate, @dueDate, 0,
                    @source, @urgencyLevel, @securityLevel, @isConfidential, @estimatedHours, 0)
            RETURNING id;
            """;

        var taskId = await ExecuteScalarAsync<Guid>(sql,
            [
                ("@code", code),
                ("@projectId", request.ProjectId),
                ("@parentTaskId", request.ParentTaskId),
                ("@title", request.Title.Trim()),
                ("@description", request.Description?.Trim()),
                ("@taskType", request.TaskType?.Trim()),
                ("@departmentId", request.DepartmentId),
                ("@priorityId", request.PriorityId),
                ("@actorUserId", actorUserId),
                ("@startDate", request.StartDate),
                ("@dueDate", request.DueDate),
                ("@source", request.Source?.Trim()),
                ("@urgencyLevel", request.UrgencyLevel?.Trim()),
                ("@securityLevel", request.SecurityLevel?.Trim()),
                ("@isConfidential", IsConfidential(request.SecurityLevel)),
                ("@estimatedHours", request.EstimatedHours)
            ],
            cancellationToken);

        if (request.AssigneeId is not null)
        {
            await AddTaskAssignmentAsync(taskId, request.AssigneeId.Value, "assignee", actorUserId, cancellationToken);
        }

        foreach (var userId in collaborators)
        {
            await AddTaskAssignmentAsync(taskId, userId, "co_assignee", actorUserId, cancellationToken);
        }

        foreach (var userId in watchers)
        {
            await AddTaskAssignmentAsync(taskId, userId, "watcher", actorUserId, cancellationToken);
        }

        foreach (var tag in NormalizeTags(request.Tags))
        {
            await AddTaskTagAsync(taskId, tag, cancellationToken);
        }

        return new TaskCreateResult(TaskCommandResult.Success, taskId);
    }

    public async Task<TaskCommandResult> UpdateStatusAsync(Guid actorUserId, Guid taskId, UpdateTaskStatusRequest request, CancellationToken cancellationToken)
    {
        var affected = await ExecuteAsync(
            "UPDATE tasks SET status_id = @statusId, updated_at = now() WHERE id = @taskId;",
            [("@statusId", request.StatusId), ("@taskId", taskId)],
            cancellationToken);

        if (affected > 0 && !string.IsNullOrWhiteSpace(request.Note))
        {
            await AddCommentAsync(actorUserId, taskId, new AddTaskCommentRequest(request.Note), cancellationToken);
        }

        return affected > 0 ? TaskCommandResult.Success : TaskCommandResult.NotFound;
    }

    public async Task<TaskCreateResult> AddCommentAsync(Guid actorUserId, Guid taskId, AddTaskCommentRequest request, CancellationToken cancellationToken)
    {
        var access = await GetTaskAccessAsync(actorUserId, taskId, "comment.create", cancellationToken);
        if (!access.Exists)
        {
            return new TaskCreateResult(TaskCommandResult.NotFound);
        }

        if (!access.CanAccess || !access.HasPermission)
        {
            return new TaskCreateResult(TaskCommandResult.Forbidden);
        }

        const string sql = """
            INSERT INTO task_comments (task_id, user_id, content)
            VALUES (@taskId, @userId, @content)
            RETURNING id;
            """;

        var id = await ExecuteScalarAsync<Guid>(sql,
            [("@taskId", taskId), ("@userId", actorUserId), ("@content", request.Content.Trim())],
            cancellationToken);

        return new TaskCreateResult(TaskCommandResult.Success, id);
    }

    public async Task<TaskCreateResult> CreateSubTaskAsync(Guid actorUserId, Guid taskId, CreateSubTaskRequest request, CancellationToken cancellationToken)
    {
        var access = await GetTaskAccessAsync(actorUserId, taskId, "task.update", cancellationToken);
        if (!access.Exists)
        {
            return new TaskCreateResult(TaskCommandResult.NotFound);
        }

        if (!access.CanAccess || !access.HasPermission)
        {
            return new TaskCreateResult(TaskCommandResult.Forbidden);
        }

        const string sql = """
            INSERT INTO subtasks (task_id, title, assignee_id, due_date, progress, done, sort_order)
            VALUES (@taskId, @title, @assigneeId, @dueDate, @progress, @done,
                    COALESCE((SELECT MAX(sort_order) + 1 FROM subtasks WHERE task_id = @taskId), 1))
            RETURNING id;
            """;
        var progress = Math.Clamp(request.Progress ?? 0, 0, 100);

        var id = await ExecuteScalarAsync<Guid>(sql,
            [
                ("@taskId", taskId),
                ("@title", request.Title.Trim()),
                ("@assigneeId", request.AssigneeId),
                ("@dueDate", request.DueDate),
                ("@progress", progress),
                ("@done", progress == 100)
            ],
            cancellationToken);

        return new TaskCreateResult(TaskCommandResult.Success, id);
    }

    public async Task<TaskCommandResult> UpdateSubTaskAsync(Guid actorUserId, Guid taskId, Guid subTaskId, UpdateSubTaskRequest request, CancellationToken cancellationToken)
    {
        var access = await GetTaskAccessAsync(actorUserId, taskId, "task.update", cancellationToken);
        if (!access.Exists || !await SubTaskExistsAsync(taskId, subTaskId, cancellationToken))
        {
            return TaskCommandResult.NotFound;
        }

        if (!access.CanAccess || !access.HasPermission)
        {
            return TaskCommandResult.Forbidden;
        }

        int? progress = request.Progress is null ? null : Math.Clamp(request.Progress.Value, 0, 100);
        var affected = await ExecuteAsync(
            """
            UPDATE subtasks
            SET title = COALESCE(@title, title),
                assignee_id = COALESCE(@assigneeId, assignee_id),
                due_date = COALESCE(@dueDate, due_date),
                progress = COALESCE(@progress, progress),
                done = COALESCE(@done, done),
                completed_at = CASE WHEN COALESCE(@done, done) = TRUE THEN COALESCE(completed_at, now()) ELSE NULL END,
                updated_at = now()
            WHERE id = @subTaskId AND task_id = @taskId;
            """,
            [
                ("@title", request.Title?.Trim()),
                ("@assigneeId", request.AssigneeId),
                ("@dueDate", request.DueDate),
                ("@progress", progress),
                ("@done", request.Done),
                ("@subTaskId", subTaskId),
                ("@taskId", taskId)
            ],
            cancellationToken);

        return affected > 0 ? TaskCommandResult.Success : TaskCommandResult.NotFound;
    }

    public async Task<TaskCommandResult> DeleteSubTaskAsync(Guid actorUserId, Guid taskId, Guid subTaskId, CancellationToken cancellationToken)
    {
        var access = await GetTaskAccessAsync(actorUserId, taskId, "task.update", cancellationToken);
        if (!access.Exists || !await SubTaskExistsAsync(taskId, subTaskId, cancellationToken))
        {
            return TaskCommandResult.NotFound;
        }

        if (!access.CanAccess || !access.HasPermission)
        {
            return TaskCommandResult.Forbidden;
        }

        var affected = await ExecuteAsync(
            "DELETE FROM subtasks WHERE id = @subTaskId AND task_id = @taskId;",
            [("@subTaskId", subTaskId), ("@taskId", taskId)],
            cancellationToken);

        return affected > 0 ? TaskCommandResult.Success : TaskCommandResult.NotFound;
    }

    private async Task<bool> HasPermissionAsync(Guid actorUserId, string permissionCode, CancellationToken cancellationToken)
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

        return await ExecuteScalarAsync<bool>(sql,
            [("@actorUserId", actorUserId), ("@permissionCode", permissionCode)],
            cancellationToken);
    }

    private async Task AddTaskAssignmentAsync(
        Guid taskId,
        Guid userId,
        string assignmentType,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        await ExecuteAsync(
            """
            INSERT INTO task_assignments (task_id, user_id, assignment_type, assigned_by)
            VALUES (@taskId, @userId, @assignmentType::task_assignment_type, @actorUserId)
            ON CONFLICT DO NOTHING;
            """,
            [
                ("@taskId", taskId),
                ("@userId", userId),
                ("@assignmentType", assignmentType),
                ("@actorUserId", actorUserId)
            ],
            cancellationToken);
    }

    private async Task AddTaskTagAsync(Guid taskId, string tagName, CancellationToken cancellationToken)
    {
        const string sql = """
            WITH inserted_tag AS (
                INSERT INTO tags (name)
                VALUES (@tagName)
                ON CONFLICT (name) DO UPDATE SET name = EXCLUDED.name
                RETURNING id
            )
            INSERT INTO task_tags (task_id, tag_id)
            SELECT @taskId, id FROM inserted_tag
            ON CONFLICT DO NOTHING;
            """;

        await ExecuteAsync(sql, [("@taskId", taskId), ("@tagName", tagName)], cancellationToken);
    }

    private static Guid[] NormalizeUserIds(Guid[]? userIds)
    {
        return (userIds ?? [])
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToArray();
    }

    private static string[] NormalizeTags(string[]? tags)
    {
        return (tags ?? [])
            .Select(tag => tag.Trim())
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static bool IsConfidential(string? securityLevel)
    {
        return securityLevel?.Trim().Equals("internal", StringComparison.OrdinalIgnoreCase) == false;
    }

    private async Task<TaskAccessRow> GetTaskAccessAsync(Guid actorUserId, Guid taskId, string permissionCode, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
              EXISTS (SELECT 1 FROM tasks WHERE id = @taskId) AS exists,
              EXISTS (
                SELECT 1
                FROM user_roles ur
                JOIN role_permissions rp ON rp.role_id = ur.role_id
                JOIN permissions p ON p.id = rp.permission_id
                WHERE ur.user_id = @actorUserId
                  AND p.code = @permissionCode
              ) AS has_permission,
              EXISTS (
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
                  )
              ) AS can_access;
            """;

        var exists = await ExecuteScalarAsync<bool>(sql,
            [("@actorUserId", actorUserId), ("@taskId", taskId), ("@permissionCode", permissionCode)],
            cancellationToken);

        // ExecuteScalar returns the first column only; use focused queries for the other booleans.
        return new TaskAccessRow(
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

        return await ExecuteScalarAsync<bool>(sql,
            [("@actorUserId", actorUserId), ("@taskId", taskId)],
            cancellationToken);
    }

    private async Task<bool> SubTaskExistsAsync(Guid taskId, Guid subTaskId, CancellationToken cancellationToken)
    {
        const string sql = "SELECT EXISTS (SELECT 1 FROM subtasks WHERE id = @subTaskId AND task_id = @taskId);";
        return await ExecuteScalarAsync<bool>(sql,
            [("@subTaskId", subTaskId), ("@taskId", taskId)],
            cancellationToken);
    }

    private sealed record TaskAccessRow(bool Exists, bool HasPermission, bool CanAccess);
}
