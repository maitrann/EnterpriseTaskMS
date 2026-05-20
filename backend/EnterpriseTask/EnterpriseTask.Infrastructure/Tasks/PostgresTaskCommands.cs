using EnterpriseTask.Application.Tasks;
using EnterpriseTask.Infrastructure.Persistence;

namespace EnterpriseTask.Infrastructure.Tasks;

public sealed class PostgresTaskCommands(ApplicationDbContext dbContext) : PostgresCommandBase(dbContext), ITaskCommands
{
    public async Task<long> CreateAsync(CreateTaskRequest request, CancellationToken cancellationToken)
    {
        var code = $"CV-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}";
        const string sql = """
            INSERT INTO tasks (code, project_id, parent_task_id, title, description, task_type, department_id,
                               status_id, priority_id, urgency_level, security_level, reporter_id, assignee_id,
                               start_date, due_date, progress, source, estimated_hours, actual_hours)
            VALUES (@code, @projectId, @parentTaskId, @title, @description, @taskType, @departmentId,
                    COALESCE((SELECT id FROM task_statuses WHERE code = 'new'), 1),
                    @priorityId, @urgencyLevel, @securityLevel, @reporterId, @assigneeId,
                    @startDate, @dueDate, 0, @source, @estimatedHours, 0)
            RETURNING id;
            """;

        var id = await ExecuteScalarAsync<long>(sql,
            [
                ("@code", code),
                ("@projectId", request.ProjectId),
                ("@parentTaskId", request.ParentTaskId),
                ("@title", request.Title.Trim()),
                ("@description", request.Description?.Trim()),
                ("@taskType", request.TaskType?.Trim()),
                ("@departmentId", request.DepartmentId),
                ("@priorityId", request.PriorityId),
                ("@urgencyLevel", request.UrgencyLevel?.Trim()),
                ("@securityLevel", request.SecurityLevel?.Trim()),
                ("@reporterId", 1),
                ("@assigneeId", request.AssigneeId),
                ("@startDate", request.StartDate),
                ("@dueDate", request.DueDate),
                ("@source", request.Source?.Trim()),
                ("@estimatedHours", request.EstimatedHours)
            ],
            cancellationToken);

        await ReplaceTaskLinksAsync(id, request.CollaboratorIds, request.WatcherIds, request.Tags, cancellationToken);
        return id;
    }

    public async Task<bool> UpdateAsync(long taskId, UpdateTaskRequest request, CancellationToken cancellationToken)
    {
        var affected = await ExecuteAsync(
            """
            UPDATE tasks
            SET project_id = @projectId,
                parent_task_id = @parentTaskId,
                title = @title,
                description = @description,
                task_type = @taskType,
                department_id = @departmentId,
                status_id = @statusId,
                priority_id = @priorityId,
                urgency_level = @urgencyLevel,
                security_level = @securityLevel,
                assignee_id = @assigneeId,
                start_date = @startDate,
                due_date = @dueDate,
                progress = @progress,
                source = @source,
                estimated_hours = @estimatedHours,
                actual_hours = @actualHours,
                updated_at = now()
            WHERE id = @taskId;
            """,
            [
                ("@taskId", taskId),
                ("@projectId", request.ProjectId),
                ("@parentTaskId", request.ParentTaskId),
                ("@title", request.Title.Trim()),
                ("@description", request.Description?.Trim()),
                ("@taskType", request.TaskType?.Trim()),
                ("@departmentId", request.DepartmentId),
                ("@statusId", request.StatusId),
                ("@priorityId", request.PriorityId),
                ("@urgencyLevel", request.UrgencyLevel?.Trim()),
                ("@securityLevel", request.SecurityLevel?.Trim()),
                ("@assigneeId", request.AssigneeId),
                ("@startDate", request.StartDate),
                ("@dueDate", request.DueDate),
                ("@progress", Math.Clamp(request.Progress, 0, 100)),
                ("@source", request.Source?.Trim()),
                ("@estimatedHours", request.EstimatedHours),
                ("@actualHours", request.ActualHours)
            ],
            cancellationToken);

        if (affected > 0)
        {
            await ReplaceTaskLinksAsync(taskId, request.CollaboratorIds, request.WatcherIds, request.Tags, cancellationToken);
        }

        return affected > 0;
    }

    public async Task<long?> DuplicateAsync(long taskId, DuplicateTaskRequest request, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO tasks (code, project_id, parent_task_id, title, description, task_type, department_id,
                               status_id, priority_id, urgency_level, security_level, reporter_id, assignee_id,
                               start_date, due_date, progress, source, estimated_hours, actual_hours)
            SELECT @code, project_id, parent_task_id, COALESCE(NULLIF(@title, ''), title || ' (copy)'),
                   description, task_type, department_id,
                   COALESCE((SELECT id FROM task_statuses WHERE code = 'new'), status_id),
                   priority_id, urgency_level, security_level, reporter_id,
                   CASE WHEN @resetPeople THEN NULL ELSE assignee_id END,
                   start_date, due_date, 0, source, estimated_hours, 0
            FROM tasks
            WHERE id = @taskId
            RETURNING id;
            """;

        var newId = await ExecuteScalarAsync<long>(sql,
            [
                ("@taskId", taskId),
                ("@code", $"CV-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}"),
                ("@title", request.Title?.Trim()),
                ("@resetPeople", request.ResetPeople)
            ],
            cancellationToken);

        if (newId <= 0)
        {
            return null;
        }

        if (!request.ResetPeople)
        {
            await ExecuteAsync(
                """
                INSERT INTO task_collaborators (task_id, user_id)
                SELECT @newId, user_id FROM task_collaborators WHERE task_id = @taskId
                ON CONFLICT DO NOTHING;
                INSERT INTO task_watchers (task_id, user_id)
                SELECT @newId, user_id FROM task_watchers WHERE task_id = @taskId
                ON CONFLICT DO NOTHING;
                """,
                [("@taskId", taskId), ("@newId", newId)],
                cancellationToken);
        }

        await ExecuteAsync(
            """
            INSERT INTO task_tags (task_id, tag_id)
            SELECT @newId, tag_id FROM task_tags WHERE task_id = @taskId
            ON CONFLICT DO NOTHING;
            """,
            [("@taskId", taskId), ("@newId", newId)],
            cancellationToken);

        return newId;
    }

    public async Task<bool> UpdateStatusAsync(long taskId, UpdateTaskStatusRequest request, CancellationToken cancellationToken)
    {
        var affected = await ExecuteAsync(
            "UPDATE tasks SET status_id = @statusId, updated_at = now() WHERE id = @taskId;",
            [("@statusId", request.StatusId), ("@taskId", taskId)],
            cancellationToken);

        if (affected > 0 && !string.IsNullOrWhiteSpace(request.Note))
        {
            await AddCommentAsync(taskId, new AddTaskCommentRequest(1, request.Note), cancellationToken);
        }

        return affected > 0;
    }

    public async Task<bool> TransferAssigneeAsync(long taskId, TransferTaskAssigneeRequest request, CancellationToken cancellationToken)
    {
        var affected = await ExecuteAsync(
            """
            UPDATE tasks
            SET assignee_id = @assigneeId, updated_at = now()
            WHERE id = @taskId;
            """,
            [("@taskId", taskId), ("@assigneeId", request.AssigneeId)],
            cancellationToken);

        if (affected > 0 && !string.IsNullOrWhiteSpace(request.Reason))
        {
            await AddCommentAsync(taskId, new AddTaskCommentRequest(1, $"Transfer assignee: {request.Reason}"), cancellationToken);
        }

        return affected > 0;
    }

    public async Task<long?> AddCommentAsync(long taskId, AddTaskCommentRequest request, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO task_comments (task_id, user_id, content)
            VALUES (@taskId, @userId, @content)
            RETURNING id;
            """;

        return await ExecuteScalarAsync<long>(sql,
            [("@taskId", taskId), ("@userId", request.UserId), ("@content", request.Content.Trim())],
            cancellationToken);
    }

    public async Task<long?> RequestExtensionAsync(long taskId, CreateTaskExtensionRequest request, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO task_extension_requests (task_id, requested_due_date, reason, status, requested_by_user_id)
            VALUES (@taskId, @requestedDueDate, @reason, 'pending', @requestedByUserId)
            RETURNING id;
            """;

        return await ExecuteScalarAsync<long>(sql,
            [
                ("@taskId", taskId),
                ("@requestedDueDate", request.RequestedDueDate),
                ("@reason", request.Reason.Trim()),
                ("@requestedByUserId", request.RequestedByUserId)
            ],
            cancellationToken);
    }

    public async Task<bool> ReviewExtensionAsync(long taskId, long requestId, ReviewTaskExtensionRequest request, CancellationToken cancellationToken)
    {
        var affected = await ExecuteAsync(
            """
            UPDATE task_extension_requests
            SET status = (CASE WHEN @approved THEN 'approved' ELSE 'rejected' END)::extension_request_status,
                reviewed_by_user_id = @reviewedByUserId,
                reviewed_at = now(),
                review_note = @reviewNote
            WHERE id = @requestId AND task_id = @taskId AND status = 'pending';
            """,
            [
                ("@taskId", taskId),
                ("@requestId", requestId),
                ("@approved", request.Approved),
                ("@reviewedByUserId", request.ReviewedByUserId),
                ("@reviewNote", request.ReviewNote?.Trim())
            ],
            cancellationToken);

        if (affected > 0 && request.Approved)
        {
            await ExecuteAsync(
                """
                UPDATE tasks
                SET due_date = (
                    SELECT requested_due_date
                    FROM task_extension_requests
                    WHERE id = @requestId AND task_id = @taskId
                ),
                updated_at = now()
                WHERE id = @taskId;
                """,
                [("@taskId", taskId), ("@requestId", requestId)],
                cancellationToken);
        }

        return affected > 0;
    }

    public async Task<long?> CreateSubTaskAsync(long taskId, CreateSubTaskRequest request, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO subtasks (task_id, title, assignee_id, due_date, progress, done, sort_order)
            VALUES (@taskId, @title, @assigneeId, @dueDate, @progress, @done,
                    COALESCE((SELECT MAX(sort_order) + 1 FROM subtasks WHERE task_id = @taskId), 1))
            RETURNING id;
            """;
        var progress = Math.Clamp(request.Progress ?? 0, 0, 100);

        return await ExecuteScalarAsync<long>(sql,
            [
                ("@taskId", taskId),
                ("@title", request.Title.Trim()),
                ("@assigneeId", request.AssigneeId),
                ("@dueDate", request.DueDate),
                ("@progress", progress),
                ("@done", progress == 100)
            ],
            cancellationToken);
    }

    public async Task<bool> UpdateSubTaskAsync(long taskId, long subTaskId, UpdateSubTaskRequest request, CancellationToken cancellationToken)
    {
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

        return affected > 0;
    }

    public async Task<bool> DeleteSubTaskAsync(long taskId, long subTaskId, CancellationToken cancellationToken)
    {
        var affected = await ExecuteAsync(
            "DELETE FROM subtasks WHERE id = @subTaskId AND task_id = @taskId;",
            [("@subTaskId", subTaskId), ("@taskId", taskId)],
            cancellationToken);

        return affected > 0;
    }

    private async Task ReplaceTaskLinksAsync(
        long taskId,
        long[]? collaboratorIds,
        long[]? watcherIds,
        string[]? tags,
        CancellationToken cancellationToken)
    {
        await ExecuteAsync(
            """
            DELETE FROM task_collaborators WHERE task_id = @taskId;
            INSERT INTO task_collaborators (task_id, user_id)
            SELECT @taskId, unnest(@collaboratorIds::bigint[])
            ON CONFLICT DO NOTHING;

            DELETE FROM task_watchers WHERE task_id = @taskId;
            INSERT INTO task_watchers (task_id, user_id)
            SELECT @taskId, unnest(@watcherIds::bigint[])
            ON CONFLICT DO NOTHING;
            """,
            [
                ("@taskId", taskId),
                ("@collaboratorIds", collaboratorIds ?? Array.Empty<long>()),
                ("@watcherIds", watcherIds ?? Array.Empty<long>())
            ],
            cancellationToken);

        var cleanTags = (tags ?? [])
            .Select(tag => tag.Trim())
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        await ExecuteAsync(
            """
            DELETE FROM task_tags WHERE task_id = @taskId;
            INSERT INTO tags (name)
            SELECT unnest(@tags::text[])
            ON CONFLICT (name) DO NOTHING;
            INSERT INTO task_tags (task_id, tag_id)
            SELECT @taskId, id FROM tags WHERE name = ANY(@tags::text[])
            ON CONFLICT DO NOTHING;
            """,
            [("@taskId", taskId), ("@tags", cleanTags)],
            cancellationToken);
    }
}
