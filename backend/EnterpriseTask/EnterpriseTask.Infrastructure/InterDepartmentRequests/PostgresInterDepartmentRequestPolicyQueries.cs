using EnterpriseTask.Application.Common;
using EnterpriseTask.Application.InterDepartmentRequests;
using EnterpriseTask.Infrastructure.Persistence;

namespace EnterpriseTask.Infrastructure.InterDepartmentRequests;

public sealed class PostgresInterDepartmentRequestPolicyQueries(ApplicationDbContext dbContext)
    : PostgresCommandBase(dbContext), IInterDepartmentRequestPolicyQueries
{
    public async Task<InterDepartmentRequestAccessResult> GetAccessAsync(
        UserScope scope,
        Guid requestId,
        CancellationToken cancellationToken)
    {
        var exists = await ExecuteScalarAsync<bool>(
            "SELECT EXISTS (SELECT 1 FROM inter_department_requests WHERE id = @id);",
            [("@id", requestId)],
            cancellationToken);

        if (!exists)
        {
            return new InterDepartmentRequestAccessResult(false, false, false, null, null, null);
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

        return new InterDepartmentRequestAccessResult(true, canAccess, canCoordinate, requesterUserId, targetDepartmentId, status);
    }

    public async Task<bool> OwnerBelongsToTargetDepartmentAsync(
        Guid ownerId,
        long? targetDepartmentId,
        CancellationToken cancellationToken)
    {
        if (targetDepartmentId is null)
        {
            return false;
        }

        const string sql = """
            SELECT EXISTS (
                SELECT 1
                FROM profiles
                WHERE id = @ownerId
                  AND department_id = @targetDepartmentId
                  AND is_active = TRUE
            );
            """;

        return await ExecuteScalarAsync<bool>(
            sql,
            [("@ownerId", ownerId), ("@targetDepartmentId", targetDepartmentId.Value)],
            cancellationToken);
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
}
