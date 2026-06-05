using EnterpriseTask.Application.Tasks;
using EnterpriseTask.Domain.Tasks;
using EnterpriseTask.Infrastructure.Persistence;

namespace EnterpriseTask.Infrastructure.Tasks;

public sealed class PostgresTaskCommands(ApplicationDbContext dbContext, ITaskAccessReader taskAccessReader)
    : PostgresCommandBase(dbContext), ITaskCommands
{
    public async Task<TaskCreateResult> CreateAsync(Guid actorUserId, CreateTaskRequest request, CancellationToken cancellationToken)
    {
        if (!await taskAccessReader.HasPermissionAsync(actorUserId, "task.create", cancellationToken))
        {
            return new TaskCreateResult(TaskCommandResult.Forbidden);
        }

        if (!await taskAccessReader.CanUseDepartmentAsync(actorUserId, request.DepartmentId, cancellationToken))
        {
            return new TaskCreateResult(TaskCommandResult.Forbidden);
        }

        if (request.AssigneeId is not null
            && request.AssigneeId.Value != actorUserId
            && !await taskAccessReader.HasPermissionAsync(actorUserId, "task.assign", cancellationToken))
        {
            return new TaskCreateResult(TaskCommandResult.Forbidden);
        }

        var code = $"CV-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}";
        const string sql = """
            INSERT INTO tasks (code, project_id, parent_task_id, title, description, task_type, department_id,
                               status_id, priority_id, reporter_id, created_by, start_date, due_date, progress,
                               source, estimated_hours, actual_hours)
            VALUES (@code, @projectId, @parentTaskId, @title, @description, @taskType, @departmentId,
                    COALESCE((SELECT id FROM task_statuses WHERE code = 'new'), 1),
                    @priorityId, @actorUserId, @actorUserId, @startDate, @dueDate, 0,
                    @source, @estimatedHours, 0)
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
                ("@estimatedHours", request.EstimatedHours)
            ],
            cancellationToken);

        if (request.AssigneeId is not null)
        {
            await ExecuteAsync(
                """
                INSERT INTO task_assignments (task_id, user_id, assignment_type, assigned_by)
                VALUES (@taskId, @userId, 'assignee', @actorUserId)
                ON CONFLICT DO NOTHING;
                """,
                [("@taskId", taskId), ("@userId", request.AssigneeId.Value), ("@actorUserId", actorUserId)],
                cancellationToken);
        }

        await ReplaceAssignmentsAsync(taskId, request.CollaboratorIds, "co_assignee", actorUserId, cancellationToken);
        await ReplaceAssignmentsAsync(taskId, request.WatcherIds, "watcher", actorUserId, cancellationToken);
        await ReplaceTagsAsync(taskId, request.Tags, cancellationToken);

        return new TaskCreateResult(TaskCommandResult.Success, taskId);
    }

    public async Task<TaskCommandResult> UpdateAsync(Guid actorUserId, Guid taskId, UpdateTaskRequest request, CancellationToken cancellationToken)
    {
        var access = await taskAccessReader.GetTaskAccessAsync(actorUserId, taskId, "task.update", cancellationToken);
        if (!access.Exists)
        {
            return TaskCommandResult.NotFound;
        }

        if (!access.CanAccess || !access.HasPermission)
        {
            return TaskCommandResult.Forbidden;
        }

        if (!await taskAccessReader.CanUseDepartmentAsync(actorUserId, request.DepartmentId, cancellationToken))
        {
            return TaskCommandResult.Forbidden;
        }

        if (request.AssigneeId is not null
            && request.AssigneeId.Value != actorUserId
            && !await taskAccessReader.HasPermissionAsync(actorUserId, "task.assign", cancellationToken))
        {
            return TaskCommandResult.Forbidden;
        }

        var currentStatusId = await GetTaskStatusIdAsync(taskId, cancellationToken);
        if (request.StatusId is not null
            && request.StatusId != currentStatusId
            && !TaskWorkflowPolicy.CanTransition(currentStatusId, request.StatusId.Value))
        {
            return TaskCommandResult.Forbidden;
        }

        var progress = TaskProgressPolicy.Normalize(request.Progress);
        var currentStatusId = await GetTaskStatusIdAsync(taskId, cancellationToken);
        if (!TaskWorkflowPolicy.CanTransition(currentStatusId, request.StatusId))
        {
            return TaskCommandResult.Forbidden;
        }

        var affected = await ExecuteAsync(
            """
            UPDATE tasks
            SET title = @title,
                description = @description,
                task_type = @taskType,
                project_id = @projectId,
                parent_task_id = @parentTaskId,
                department_id = @departmentId,
                status_id = @statusId,
                priority_id = @priorityId,
                start_date = @startDate,
                due_date = @dueDate,
                progress = @progress,
                estimated_hours = @estimatedHours,
                actual_hours = @actualHours,
                source = @source,
                urgency_level = @urgencyLevel,
                security_level = @securityLevel,
                updated_at = now()
            WHERE id = @taskId;
            """,
            [
                ("@title", request.Title.Trim()),
                ("@description", request.Description?.Trim()),
                ("@taskType", request.TaskType?.Trim()),
                ("@projectId", request.ProjectId),
                ("@parentTaskId", request.ParentTaskId),
                ("@departmentId", request.DepartmentId),
                ("@statusId", request.StatusId),
                ("@priorityId", request.PriorityId),
                ("@startDate", request.StartDate),
                ("@dueDate", request.DueDate),
                ("@progress", progress),
                ("@estimatedHours", request.EstimatedHours),
                ("@actualHours", request.ActualHours),
                ("@source", request.Source?.Trim()),
                ("@urgencyLevel", request.UrgencyLevel?.Trim()),
                ("@securityLevel", request.SecurityLevel?.Trim()),
                ("@taskId", taskId)
            ],
            cancellationToken);

        if (affected == 0)
        {
            return TaskCommandResult.NotFound;
        }

        await ReplaceSingleAssignmentAsync(taskId, request.AssigneeId, "assignee", actorUserId, cancellationToken);
        await ReplaceAssignmentsAsync(taskId, request.CollaboratorIds, "co_assignee", actorUserId, cancellationToken);
        await ReplaceAssignmentsAsync(taskId, request.WatcherIds, "watcher", actorUserId, cancellationToken);
        await ReplaceTagsAsync(taskId, request.Tags, cancellationToken);

        return TaskCommandResult.Success;
    }

    public async Task<TaskCommandResult> UpdateStatusAsync(Guid actorUserId, Guid taskId, UpdateTaskStatusRequest request, CancellationToken cancellationToken)
    {
        var access = await taskAccessReader.GetTaskAccessAsync(actorUserId, taskId, "task.update", cancellationToken);
        if (!access.Exists)
        {
            return TaskCommandResult.NotFound;
        }

        if (!access.CanAccess || !access.HasPermission)
        {
            return TaskCommandResult.Forbidden;
        }

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

    public async Task<TaskCommandResult> TransferAssigneeAsync(Guid actorUserId, Guid taskId, TransferTaskAssigneeRequest request, CancellationToken cancellationToken)
    {
        var access = await taskAccessReader.GetTaskAccessAsync(actorUserId, taskId, "task.update", cancellationToken);
        if (!access.Exists)
        {
            return TaskCommandResult.NotFound;
        }

        if (!access.CanAccess || !access.HasPermission || !await taskAccessReader.HasPermissionAsync(actorUserId, "task.assign", cancellationToken))
        {
            return TaskCommandResult.Forbidden;
        }

        await ReplaceSingleAssignmentAsync(taskId, request.AssigneeId, "assignee", actorUserId, cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.Reason))
        {
            await AddCommentAsync(actorUserId, taskId, new AddTaskCommentRequest($"Transfer assignee: {request.Reason.Trim()}"), cancellationToken);
        }

        await TouchTaskAsync(taskId, cancellationToken);
        return TaskCommandResult.Success;
    }

    public async Task<TaskCreateResult> DuplicateAsync(Guid actorUserId, Guid taskId, DuplicateTaskRequest request, CancellationToken cancellationToken)
    {
        if (!await taskAccessReader.HasPermissionAsync(actorUserId, "task.create", cancellationToken))
        {
            return new TaskCreateResult(TaskCommandResult.Forbidden);
        }

        if (!await taskAccessReader.CanAccessTaskAsync(actorUserId, taskId, cancellationToken))
        {
            return new TaskCreateResult(TaskCommandResult.NotFound);
        }

        var code = $"CV-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}";
        const string sql = """
            INSERT INTO tasks (code, project_id, parent_task_id, title, description, task_type, source,
                               department_id, status_id, priority_id, urgency_level, security_level,
                               reporter_id, created_by, start_date, due_date, progress,
                               subtask_progress_auto_sync, estimated_hours, actual_hours)
            SELECT @code, project_id, parent_task_id, COALESCE(NULLIF(@title, ''), title || ' (copy)'),
                   description, task_type, source, department_id,
                   COALESCE((SELECT id FROM task_statuses WHERE code = 'new'), status_id),
                   priority_id, urgency_level, security_level, @actorUserId, @actorUserId,
                   start_date, due_date, 0, subtask_progress_auto_sync, estimated_hours, 0
            FROM tasks
            WHERE id = @taskId
            RETURNING id;
            """;

        var duplicatedId = await ExecuteScalarAsync<Guid>(sql,
            [
                ("@code", code),
                ("@title", request.Title?.Trim()),
                ("@actorUserId", actorUserId),
                ("@taskId", taskId)
            ],
            cancellationToken);

        if (duplicatedId == Guid.Empty)
        {
            return new TaskCreateResult(TaskCommandResult.NotFound);
        }

        if (!request.ResetPeople)
        {
            await ExecuteAsync(
                """
                INSERT INTO task_assignments (task_id, user_id, assignment_type, assigned_by)
                SELECT @duplicatedId, user_id, assignment_type, @actorUserId
                FROM task_assignments
                WHERE task_id = @taskId
                ON CONFLICT DO NOTHING;
                """,
                [("@duplicatedId", duplicatedId), ("@actorUserId", actorUserId), ("@taskId", taskId)],
                cancellationToken);

            await ExecuteAsync(
                """
                INSERT INTO subtasks (task_id, title, assignee_id, status, due_date, progress, done, sort_order)
                SELECT @duplicatedId, title, assignee_id, status, due_date, 0, FALSE, sort_order
                FROM subtasks
                WHERE task_id = @taskId;
                """,
                [("@duplicatedId", duplicatedId), ("@taskId", taskId)],
                cancellationToken);
        }

        await ExecuteAsync(
            """
            INSERT INTO task_tags (task_id, tag_id)
            SELECT @duplicatedId, tag_id
            FROM task_tags
            WHERE task_id = @taskId
            ON CONFLICT DO NOTHING;
            """,
            [("@duplicatedId", duplicatedId), ("@taskId", taskId)],
            cancellationToken);

        if (!request.ResetAttachments)
        {
            await ExecuteAsync(
                """
                INSERT INTO attachments (task_id, file_name, file_url, content_type, file_size, uploaded_by)
                SELECT @duplicatedId, file_name, file_url, content_type, file_size, @actorUserId
                FROM attachments
                WHERE task_id = @taskId;
                """,
                [("@duplicatedId", duplicatedId), ("@actorUserId", actorUserId), ("@taskId", taskId)],
                cancellationToken);
        }

        return new TaskCreateResult(TaskCommandResult.Success, duplicatedId);
    }

    public async Task<TaskCreateResult> AddCommentAsync(Guid actorUserId, Guid taskId, AddTaskCommentRequest request, CancellationToken cancellationToken)
    {
        var access = await taskAccessReader.GetTaskAccessAsync(actorUserId, taskId, "comment.create", cancellationToken);
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

    public async Task<TaskCreateResult> RequestExtensionAsync(Guid actorUserId, Guid taskId, CreateTaskExtensionRequest request, CancellationToken cancellationToken)
    {
        if (!await taskAccessReader.CanAccessTaskAsync(actorUserId, taskId, cancellationToken))
        {
            return new TaskCreateResult(TaskCommandResult.NotFound);
        }

        const string sql = """
            INSERT INTO task_extension_requests (task_id, requested_due_date, reason, requested_by_user_id)
            VALUES (@taskId, @requestedDueDate, @reason, @actorUserId)
            RETURNING id;
            """;

        var id = await ExecuteScalarAsync<Guid>(sql,
            [
                ("@taskId", taskId),
                ("@requestedDueDate", request.RequestedDueDate),
                ("@reason", request.Reason.Trim()),
                ("@actorUserId", actorUserId)
            ],
            cancellationToken);

        return new TaskCreateResult(TaskCommandResult.Success, id);
    }

    public async Task<TaskCommandResult> ReviewExtensionAsync(Guid actorUserId, Guid taskId, Guid requestId, ReviewTaskExtensionRequest request, CancellationToken cancellationToken)
    {
        var access = await taskAccessReader.GetTaskAccessAsync(actorUserId, taskId, "task.update", cancellationToken);
        if (!access.Exists || !await ExtensionRequestExistsAsync(taskId, requestId, cancellationToken))
        {
            return TaskCommandResult.NotFound;
        }

        if (!access.CanAccess || !access.HasPermission)
        {
            return TaskCommandResult.Forbidden;
        }

        var affected = await ExecuteAsync(
            """
            UPDATE task_extension_requests
            SET status = CASE WHEN @approved THEN 'approved'::public.extension_request_status ELSE 'rejected'::public.extension_request_status END,
                reviewed_by_user_id = @actorUserId,
                reviewed_at = now(),
                review_note = @reviewNote
            WHERE id = @requestId AND task_id = @taskId AND status = 'pending';
            """,
            [
                ("@approved", request.Approved),
                ("@actorUserId", actorUserId),
                ("@reviewNote", request.ReviewNote?.Trim()),
                ("@requestId", requestId),
                ("@taskId", taskId)
            ],
            cancellationToken);

        if (affected == 0)
        {
            return TaskCommandResult.NotFound;
        }

        if (request.Approved)
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
                [("@requestId", requestId), ("@taskId", taskId)],
                cancellationToken);
        }
        else
        {
            await TouchTaskAsync(taskId, cancellationToken);
        }

        return TaskCommandResult.Success;
    }

    public async Task<TaskCreateResult> CreateSubTaskAsync(Guid actorUserId, Guid taskId, CreateSubTaskRequest request, CancellationToken cancellationToken)
    {
        var access = await taskAccessReader.GetTaskAccessAsync(actorUserId, taskId, "task.update", cancellationToken);
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
        var progress = TaskProgressPolicy.Normalize(request.Progress ?? 0);

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
        var access = await taskAccessReader.GetTaskAccessAsync(actorUserId, taskId, "task.update", cancellationToken);
        if (!access.Exists || !await SubTaskExistsAsync(taskId, subTaskId, cancellationToken))
        {
            return TaskCommandResult.NotFound;
        }

        if (!access.CanAccess || !access.HasPermission)
        {
            return TaskCommandResult.Forbidden;
        }

        int? progress = request.Progress is null ? null : TaskProgressPolicy.Normalize(request.Progress.Value);
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
        var access = await taskAccessReader.GetTaskAccessAsync(actorUserId, taskId, "task.update", cancellationToken);
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

    private async Task ReplaceSingleAssignmentAsync(
        Guid taskId,
        Guid? userId,
        string assignmentType,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        await ExecuteAsync(
            "DELETE FROM task_assignments WHERE task_id = @taskId AND assignment_type = CAST(@assignmentType AS public.task_assignment_type);",
            [("@taskId", taskId), ("@assignmentType", assignmentType)],
            cancellationToken);

        if (userId is null)
        {
            return;
        }

        await ExecuteAsync(
            """
            INSERT INTO task_assignments (task_id, user_id, assignment_type, assigned_by)
            VALUES (@taskId, @userId, CAST(@assignmentType AS public.task_assignment_type), @actorUserId)
            ON CONFLICT DO NOTHING;
            """,
            [("@taskId", taskId), ("@userId", userId.Value), ("@assignmentType", assignmentType), ("@actorUserId", actorUserId)],
            cancellationToken);
    }

    private async Task ReplaceAssignmentsAsync(
        Guid taskId,
        IReadOnlyCollection<Guid>? userIds,
        string assignmentType,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        await ExecuteAsync(
            "DELETE FROM task_assignments WHERE task_id = @taskId AND assignment_type = CAST(@assignmentType AS public.task_assignment_type);",
            [("@taskId", taskId), ("@assignmentType", assignmentType)],
            cancellationToken);

        foreach (var userId in (userIds ?? []).Distinct())
        {
            await ExecuteAsync(
                """
                INSERT INTO task_assignments (task_id, user_id, assignment_type, assigned_by)
                VALUES (@taskId, @userId, CAST(@assignmentType AS public.task_assignment_type), @actorUserId)
                ON CONFLICT DO NOTHING;
                """,
                [("@taskId", taskId), ("@userId", userId), ("@assignmentType", assignmentType), ("@actorUserId", actorUserId)],
                cancellationToken);
        }
    }

    private async Task ReplaceTagsAsync(Guid taskId, IReadOnlyCollection<string>? tags, CancellationToken cancellationToken)
    {
        await ExecuteAsync("DELETE FROM task_tags WHERE task_id = @taskId;", [("@taskId", taskId)], cancellationToken);

        foreach (var tag in (tags ?? []).Select(item => item.Trim()).Where(item => !string.IsNullOrWhiteSpace(item)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var tagId = await ExecuteScalarAsync<long>(
                """
                INSERT INTO tags (name)
                VALUES (@name)
                ON CONFLICT (name) DO UPDATE SET name = EXCLUDED.name
                RETURNING id;
                """,
                [("@name", tag)],
                cancellationToken);

            await ExecuteAsync(
                """
                INSERT INTO task_tags (task_id, tag_id)
                VALUES (@taskId, @tagId)
                ON CONFLICT DO NOTHING;
                """,
                [("@taskId", taskId), ("@tagId", tagId)],
                cancellationToken);
        }
    }

    private async Task TouchTaskAsync(Guid taskId, CancellationToken cancellationToken)
    {
        await ExecuteAsync("UPDATE tasks SET updated_at = now() WHERE id = @taskId;", [("@taskId", taskId)], cancellationToken);
    }

    private async Task<bool> ExtensionRequestExistsAsync(Guid taskId, Guid requestId, CancellationToken cancellationToken)
    {
        const string sql = "SELECT EXISTS (SELECT 1 FROM task_extension_requests WHERE id = @requestId AND task_id = @taskId);";
        return await ExecuteScalarAsync<bool>(sql,
            [("@requestId", requestId), ("@taskId", taskId)],
            cancellationToken);
    }

    private async Task<long?> GetTaskStatusIdAsync(Guid taskId, CancellationToken cancellationToken)
    {
        const string sql = "SELECT status_id FROM tasks WHERE id = @taskId;";
        return await ExecuteScalarAsync<long?>(sql, [("@taskId", taskId)], cancellationToken);
    }

    private async Task<bool> SubTaskExistsAsync(Guid taskId, Guid subTaskId, CancellationToken cancellationToken)
    {
        const string sql = "SELECT EXISTS (SELECT 1 FROM subtasks WHERE id = @subTaskId AND task_id = @taskId);";
        return await ExecuteScalarAsync<bool>(sql,
            [("@subTaskId", subTaskId), ("@taskId", taskId)],
            cancellationToken);
    }
}
