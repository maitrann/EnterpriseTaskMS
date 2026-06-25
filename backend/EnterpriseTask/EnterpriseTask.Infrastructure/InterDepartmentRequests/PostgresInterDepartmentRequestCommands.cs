using System.Text.Json;
using EnterpriseTask.Application.Common;
using EnterpriseTask.Application.InterDepartmentRequests;
using EnterpriseTask.Infrastructure.Persistence;

namespace EnterpriseTask.Infrastructure.InterDepartmentRequests;

public sealed class PostgresInterDepartmentRequestCommands(ApplicationDbContext dbContext)
    : PostgresCommandBase(dbContext), IInterDepartmentRequestCommands
{
    private static readonly HashSet<string> RequestStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "new",
        "received",
        "processing",
        "waiting-requester",
        "waiting-target",
        "done",
        "closed",
        "rejected"
    };

    public async Task<InterDepartmentRequestCreateResult> CreateAsync(UserScope scope, CreateInterDepartmentRequestCommand request, CancellationToken cancellationToken)
    {
        var code = $"IR-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}";
        var requesterDepartmentId = request.RequesterDepartmentId ?? scope.DepartmentId;
        if (!await CanUseRequesterDepartmentAsync(scope, requesterDepartmentId, cancellationToken))
        {
            return new InterDepartmentRequestCreateResult(InterDepartmentRequestCommandResult.Forbidden);
        }

        const string sql = """
            INSERT INTO inter_department_requests
              (code, type, title, description, requester_department_id, requester_user_id, target_department_id,
               priority, status, due_date, sla_policy_key, sla_started_at, sla_due_at, form_values, latest_message, note)
            VALUES
              (@code, @type::inter_request_type, @title, @description, @requesterDepartmentId, @requesterUserId, @targetDepartmentId,
               @priority::request_priority_code, 'new', @dueDate, @type::inter_request_type, now(),
               (@dueDate::date + time '17:00')::timestamp with time zone, @formValues::jsonb, @latestMessage, @note)
            RETURNING id;
            """;

        var id = await ExecuteScalarAsync<Guid>(sql,
            [
                ("@code", code),
                ("@type", request.Type.Trim().ToLowerInvariant()),
                ("@title", request.Title.Trim()),
                ("@description", request.Description.Trim()),
                ("@requesterDepartmentId", requesterDepartmentId),
                ("@requesterUserId", scope.UserId),
                ("@targetDepartmentId", request.TargetDepartmentId),
                ("@priority", request.Priority.Trim().ToLowerInvariant()),
                ("@dueDate", request.DueDate),
                ("@formValues", JsonSerializer.Serialize(request.FormValues ?? [])),
                ("@latestMessage", request.Note ?? request.Description),
                ("@note", request.Note)
            ],
            cancellationToken);

        return new InterDepartmentRequestCreateResult(InterDepartmentRequestCommandResult.Success, id);
    }

    public async Task<InterDepartmentRequestCommandResult> AcknowledgeAsync(UserScope scope, Guid requestId, CancellationToken cancellationToken)
    {
        var access = await GetRequestAccessAsync(scope, requestId, cancellationToken);
        if (!access.Exists)
        {
            return InterDepartmentRequestCommandResult.NotFound;
        }

        if (!access.CanCoordinate)
        {
            return InterDepartmentRequestCommandResult.Forbidden;
        }

        if (access.Status != "new")
        {
            return InterDepartmentRequestCommandResult.InvalidState;
        }

        var affected = await ExecuteAsync(
            """
            UPDATE inter_department_requests
            SET status = 'received', received_at = COALESCE(received_at, now()), updated_at = now(),
                latest_message = 'Bộ phận tiếp nhận đã nhận phiếu và đang chờ phân công xử lý.'
            WHERE id = @id AND status = 'new';
            """,
            [("@id", requestId)],
            cancellationToken);

        return affected > 0 ? InterDepartmentRequestCommandResult.Success : InterDepartmentRequestCommandResult.NotFound;
    }

    public async Task<InterDepartmentRequestCommandResult> AssignOwnerAsync(UserScope scope, Guid requestId, AssignOwnerRequest request, CancellationToken cancellationToken)
    {
        var affected = await ExecuteAsync(
            """
            UPDATE inter_department_requests
            SET owner_id = @ownerId, status = 'processing', updated_at = now(),
                latest_message = 'Phiếu đã được phân công người xử lý.'
            WHERE id = @id;
            """,
            [("@id", requestId), ("@ownerId", request.OwnerId)],
            cancellationToken);

        return affected > 0 ? InterDepartmentRequestCommandResult.Success : InterDepartmentRequestCommandResult.NotFound;
    }

    public async Task<InterDepartmentRequestCommandResult> UpdateStatusAsync(UserScope scope, Guid requestId, UpdateRequestStatusRequest request, CancellationToken cancellationToken)
    {
        var access = await GetRequestAccessAsync(scope, requestId, cancellationToken);
        if (!access.Exists)
        {
            return InterDepartmentRequestCommandResult.NotFound;
        }

        if (!access.CanCoordinate)
        {
            return InterDepartmentRequestCommandResult.Forbidden;
        }

        var nextStatus = request.Status.Trim().ToLowerInvariant();
        if (!RequestStatuses.Contains(nextStatus) || nextStatus == "closed")
        {
            return InterDepartmentRequestCommandResult.InvalidState;
        }

        if (!IsAllowedTransition(access.Status, nextStatus))
        {
            return InterDepartmentRequestCommandResult.InvalidState;
        }

        var affected = await ExecuteAsync(
            """
            UPDATE inter_department_requests
            SET status = @status::inter_request_status,
                closed_at = CASE WHEN @status = 'closed' THEN now() ELSE closed_at END,
                updated_at = now(),
                latest_message = 'Trạng thái phiếu đã được cập nhật.'
            WHERE id = @id;
            """,
            [("@id", requestId), ("@status", nextStatus)],
            cancellationToken);

        return affected > 0 ? InterDepartmentRequestCommandResult.Success : InterDepartmentRequestCommandResult.NotFound;
    }

    public async Task<InterDepartmentRequestCreateResult> AddMessageAsync(UserScope scope, Guid requestId, AddRequestMessageRequest request, CancellationToken cancellationToken)
    {
        var access = await GetRequestAccessAsync(scope, requestId, cancellationToken);
        if (!access.Exists)
        {
            return new InterDepartmentRequestCreateResult(InterDepartmentRequestCommandResult.NotFound);
        }

        if (!access.CanAccess)
        {
            return new InterDepartmentRequestCreateResult(InterDepartmentRequestCommandResult.Forbidden);
        }

        var authorName = await GetActorNameAsync(scope.UserId, cancellationToken);
        var authorDepartment = await GetActorDepartmentNameAsync(scope.UserId, cancellationToken);
        var authorRole = access.CanCoordinate && access.RequesterUserId != scope.UserId
            ? "processor"
            : "requester";

        const string sql = """
            INSERT INTO inter_request_messages
              (request_id, author_user_id, author_name, author_role, author_department, body)
            VALUES
              (@requestId, @authorUserId, @authorName, @authorRole::request_message_role, @authorDepartment, @body)
            RETURNING id;
            """;

        return await ExecuteInTransactionAsync(async () =>
        {
            var id = await ExecuteScalarAsync<Guid>(sql,
                [
                    ("@requestId", requestId),
                    ("@authorUserId", scope.UserId),
                    ("@authorName", authorName),
                    ("@authorRole", authorRole),
                    ("@authorDepartment", authorDepartment),
                    ("@body", request.Body.Trim())
                ],
                cancellationToken);

            await ExecuteAsync(
                "UPDATE inter_department_requests SET latest_message = @body, updated_at = now() WHERE id = @requestId;",
                [("@requestId", requestId), ("@body", request.Body.Trim())],
                cancellationToken);

            return new InterDepartmentRequestCreateResult(InterDepartmentRequestCommandResult.Success, id);
        }, cancellationToken);
    }

    public async Task<InterDepartmentRequestCommandResult> CloseAsync(UserScope scope, Guid requestId, CancellationToken cancellationToken)
    {
        var access = await GetRequestAccessAsync(scope, requestId, cancellationToken);
        if (!access.Exists)
        {
            return InterDepartmentRequestCommandResult.NotFound;
        }

        if (!access.CanCoordinate && access.RequesterUserId != scope.UserId)
        {
            return InterDepartmentRequestCommandResult.Forbidden;
        }

        if (access.Status != "done")
        {
            return InterDepartmentRequestCommandResult.InvalidState;
        }

        var affected = await ExecuteAsync(
            """
            UPDATE inter_department_requests
            SET status = 'closed', closed_at = COALESCE(closed_at, now()), updated_at = now(),
                latest_message = 'Bên yêu cầu đã xác nhận và đóng phiếu.'
            WHERE id = @id;
            """,
            [("@id", requestId)],
            cancellationToken);

        return affected > 0 ? InterDepartmentRequestCommandResult.Success : InterDepartmentRequestCommandResult.NotFound;
    }

    private async Task<RequestAccessRow> GetRequestAccessAsync(UserScope scope, Guid requestId, CancellationToken cancellationToken)
    {
        var exists = await ExecuteScalarAsync<bool>(
            "SELECT EXISTS (SELECT 1 FROM inter_department_requests WHERE id = @id);",
            [("@id", requestId)],
            cancellationToken);

        if (!exists)
        {
            return new RequestAccessRow(false, false, false, null, null, null);
        }

        var canAccess = await ExecuteScalarAsync<bool>(
            """
            SELECT EXISTS (
              SELECT 1
              FROM inter_department_requests r
              WHERE r.id = @id
                AND (
                  @isElevated
                  OR r.requester_user_id = @userId
                  OR r.owner_id = @userId
                  OR (r.requester_department_id IS NOT NULL AND r.requester_department_id = @departmentId)
                  OR (r.target_department_id IS NOT NULL AND r.target_department_id = @departmentId)
                  OR EXISTS (
                    SELECT 1
                    FROM user_department_scopes uds
                    WHERE uds.user_id = @userId
                      AND uds.department_id IN (r.requester_department_id, r.target_department_id)
                  )
                )
            );
            """,
            CreateScopeParameters(scope, requestId),
            cancellationToken);

        var canCoordinate = await ExecuteScalarAsync<bool>(
            """
            SELECT EXISTS (
              SELECT 1
              FROM inter_department_requests r
              WHERE r.id = @id
                AND (
                  @isElevated
                  OR r.owner_id = @userId
                  OR (
                    @isManager
                    AND (
                      (r.target_department_id IS NOT NULL AND r.target_department_id = @departmentId)
                      OR EXISTS (
                        SELECT 1
                        FROM user_department_scopes uds
                        WHERE uds.user_id = @userId
                          AND uds.department_id = r.target_department_id
                      )
                    )
                  )
                )
            );
            """,
            CreateScopeParameters(scope, requestId),
            cancellationToken);

        var requesterUserIdValue = await ExecuteScalarAsync<string>(
            "SELECT requester_user_id::text FROM inter_department_requests WHERE id = @id;",
            [("@id", requestId)],
            cancellationToken);
        var requesterUserId = Guid.TryParse(requesterUserIdValue, out var parsedRequesterUserId)
            ? parsedRequesterUserId
            : (Guid?)null;

        var targetDepartmentIdValue = await ExecuteScalarAsync<string>(
            "SELECT target_department_id::text FROM inter_department_requests WHERE id = @id;",
            [("@id", requestId)],
            cancellationToken);
        var targetDepartmentId = long.TryParse(targetDepartmentIdValue, out var parsedTargetDepartmentId)
            ? parsedTargetDepartmentId
            : (long?)null;

        var status = await ExecuteScalarAsync<string>(
            "SELECT status::text FROM inter_department_requests WHERE id = @id;",
            [("@id", requestId)],
            cancellationToken);

        return new RequestAccessRow(true, canAccess, canCoordinate, requesterUserId, targetDepartmentId, status);
    }

    private static IReadOnlyList<(string Name, object? Value)> CreateScopeParameters(UserScope scope, Guid requestId)
    {
        return
        [
            ("@id", requestId),
            ("@userId", scope.UserId),
            ("@departmentId", scope.DepartmentId),
            ("@isElevated", scope.CanSeeAllData),
            ("@isManager", scope.IsManager)
        ];
    }

    private async Task<bool> CanUseRequesterDepartmentAsync(UserScope scope, long? departmentId, CancellationToken cancellationToken)
    {
        if (departmentId is null || scope.CanSeeAllData)
        {
            return true;
        }

        if (scope.DepartmentId == departmentId)
        {
            return true;
        }

        const string sql = """
            SELECT EXISTS (
                SELECT 1
                FROM user_department_scopes
                WHERE user_id = @userId
                  AND department_id = @departmentId
            );
            """;

        return await ExecuteScalarAsync<bool>(sql,
            [("@userId", scope.UserId), ("@departmentId", departmentId.Value)],
            cancellationToken);
    }

    private static bool IsAllowedTransition(string? currentStatus, string nextStatus)
    {
        return currentStatus switch
        {
            "new" => nextStatus is "received" or "rejected",
            "received" => nextStatus is "processing" or "rejected",
            "processing" => nextStatus is "waiting-requester" or "waiting-target" or "done" or "rejected",
            "waiting-requester" => nextStatus is "processing" or "rejected",
            "waiting-target" => nextStatus is "processing" or "done" or "rejected",
            "done" or "closed" or "rejected" => false,
            _ => false
        };
    }

    private async Task<string> GetActorNameAsync(Guid actorUserId, CancellationToken cancellationToken)
    {
        const string sql = "SELECT COALESCE(full_name, email, employee_code, id::text) FROM profiles WHERE id = @id;";
        return await ExecuteScalarAsync<string>(sql, [("@id", actorUserId)], cancellationToken) ?? actorUserId.ToString();
    }

    private async Task<string?> GetActorDepartmentNameAsync(Guid actorUserId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT d.name
            FROM profiles p
            LEFT JOIN departments d ON d.id = p.department_id
            WHERE p.id = @id;
            """;

        return await ExecuteScalarAsync<string>(sql, [("@id", actorUserId)], cancellationToken);
    }

    private sealed record RequestAccessRow(
        bool Exists,
        bool CanAccess,
        bool CanCoordinate,
        Guid? RequesterUserId,
        long? TargetDepartmentId,
        string? Status);
}
