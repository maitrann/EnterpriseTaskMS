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
                               status_id, priority_id, reporter_id, assignee_id, start_date, due_date, progress,
                               source, estimated_hours, actual_hours)
            VALUES (@code, @projectId, @parentTaskId, @title, @description, @taskType, @departmentId,
                    COALESCE((SELECT id FROM task_statuses WHERE code = 'new'), 1),
                    @priorityId, @reporterId, @assigneeId, @startDate, @dueDate, 0,
                    @source, @estimatedHours, 0)
            RETURNING id;
            """;

        return await ExecuteScalarAsync<long>(sql,
            [
                ("@code", code),
                ("@projectId", request.ProjectId),
                ("@parentTaskId", request.ParentTaskId),
                ("@title", request.Title.Trim()),
                ("@description", request.Description?.Trim()),
                ("@taskType", request.TaskType?.Trim()),
                ("@departmentId", request.DepartmentId),
                ("@priorityId", request.PriorityId),
                ("@reporterId", 1),
                ("@assigneeId", request.AssigneeId),
                ("@startDate", request.StartDate),
                ("@dueDate", request.DueDate),
                ("@source", request.Source?.Trim()),
                ("@estimatedHours", request.EstimatedHours)
            ],
            cancellationToken);
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
}
