using EnterpriseTask.Application.Auth;
using EnterpriseTask.Infrastructure.Persistence;

namespace EnterpriseTask.Infrastructure.Auth;

public sealed class PostgresUserSessionValidator(ApplicationDbContext dbContext)
    : PostgresCommandBase(dbContext), IUserSessionValidator
{
    public async Task<bool> IsValidAsync(
        Guid userId,
        IReadOnlyCollection<string> roleCodes,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT p.is_active,
                   COALESCE(string_agg(DISTINCT r.code, ',' ORDER BY r.code), '') AS role_codes
            FROM profiles p
            LEFT JOIN user_roles ur ON ur.user_id = p.id
            LEFT JOIN roles r ON r.id = ur.role_id
            WHERE p.id = @userId
            GROUP BY p.id, p.is_active
            LIMIT 1;
            """;

        var rows = await QueryAsync(
            sql,
            reader => new UserSessionRow(
                reader.GetBooleanValue("is_active"),
                SplitRoleCodes(reader.GetStringValue("role_codes"))),
            [("@userId", userId)],
            cancellationToken);

        var row = rows.SingleOrDefault();
        return row is not null && row.IsActive && RoleSetsMatch(row.RoleCodes, roleCodes);
    }

    private static IReadOnlyCollection<string> SplitRoleCodes(string value)
    {
        return value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static bool RoleSetsMatch(IReadOnlyCollection<string> currentRoles, IReadOnlyCollection<string> tokenRoles)
    {
        return currentRoles.Order(StringComparer.OrdinalIgnoreCase)
            .SequenceEqual(tokenRoles.Order(StringComparer.OrdinalIgnoreCase), StringComparer.OrdinalIgnoreCase);
    }

    private sealed record UserSessionRow(bool IsActive, IReadOnlyCollection<string> RoleCodes);
}
