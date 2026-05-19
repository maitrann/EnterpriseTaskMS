using System.Text.Json;
using EnterpriseTask.Application.InterDepartmentRequests;
using EnterpriseTask.Infrastructure.Persistence;

namespace EnterpriseTask.Infrastructure.InterDepartmentRequests;

public sealed class PostgresInterDepartmentRequestCommands(ApplicationDbContext dbContext)
    : PostgresCommandBase(dbContext), IInterDepartmentRequestCommands
{
    public async Task<Guid> CreateAsync(CreateInterDepartmentRequestCommand request, CancellationToken cancellationToken)
    {
        var code = $"IR-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}";
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
                ("@type", request.Type),
                ("@title", request.Title.Trim()),
                ("@description", request.Description.Trim()),
                ("@requesterDepartmentId", request.RequesterDepartmentId),
                ("@requesterUserId", request.RequesterUserId),
                ("@targetDepartmentId", request.TargetDepartmentId),
                ("@priority", request.Priority),
                ("@dueDate", request.DueDate),
                ("@formValues", JsonSerializer.Serialize(request.FormValues ?? [])),
                ("@latestMessage", request.Note ?? request.Description),
                ("@note", request.Note)
            ],
            cancellationToken);

        return id;
    }

    public async Task<bool> AcknowledgeAsync(Guid requestId, CancellationToken cancellationToken)
    {
        var affected = await ExecuteAsync(
            """
            UPDATE inter_department_requests
            SET status = 'received', received_at = COALESCE(received_at, now()), updated_at = now(),
                latest_message = 'Bộ phận tiếp nhận đã nhận phiếu và đang chờ phân công xử lý.'
            WHERE id = @id AND status = 'new';
            """,
            [("@id", requestId)],
            cancellationToken);

        return affected > 0;
    }

    public async Task<bool> AssignOwnerAsync(Guid requestId, AssignOwnerRequest request, CancellationToken cancellationToken)
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

        return affected > 0;
    }

    public async Task<bool> UpdateStatusAsync(Guid requestId, UpdateRequestStatusRequest request, CancellationToken cancellationToken)
    {
        var affected = await ExecuteAsync(
            """
            UPDATE inter_department_requests
            SET status = @status::inter_request_status,
                closed_at = CASE WHEN @status = 'closed' THEN now() ELSE closed_at END,
                updated_at = now(),
                latest_message = 'Trạng thái phiếu đã được cập nhật.'
            WHERE id = @id;
            """,
            [("@id", requestId), ("@status", request.Status)],
            cancellationToken);

        return affected > 0;
    }

    public async Task<Guid?> AddMessageAsync(Guid requestId, AddRequestMessageRequest request, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO inter_request_messages
              (request_id, author_user_id, author_name, author_role, author_department, body)
            VALUES
              (@requestId, @authorUserId, @authorName, @authorRole::request_message_role, @authorDepartment, @body)
            RETURNING id;
            """;

        var id = await ExecuteScalarAsync<Guid>(sql,
            [
                ("@requestId", requestId),
                ("@authorUserId", request.AuthorUserId),
                ("@authorName", request.AuthorName.Trim()),
                ("@authorRole", request.AuthorRole),
                ("@authorDepartment", request.AuthorDepartment),
                ("@body", request.Body.Trim())
            ],
            cancellationToken);

        await ExecuteAsync(
            "UPDATE inter_department_requests SET latest_message = @body, updated_at = now() WHERE id = @requestId;",
            [("@requestId", requestId), ("@body", request.Body.Trim())],
            cancellationToken);

        return id;
    }

    public async Task<bool> CloseAsync(Guid requestId, CancellationToken cancellationToken)
    {
        var affected = await ExecuteAsync(
            """
            UPDATE inter_department_requests
            SET status = 'closed', closed_at = COALESCE(closed_at, now()), updated_at = now(),
                latest_message = 'Bên yêu cầu đã xác nhận và đóng phiếu.'
            WHERE id = @id;
            """,
            [("@id", requestId)],
            cancellationToken);

        return affected > 0;
    }
}
