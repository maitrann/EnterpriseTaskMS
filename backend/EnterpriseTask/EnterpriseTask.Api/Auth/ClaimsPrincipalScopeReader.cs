using System.Security.Claims;
using EnterpriseTask.Application.Common;

namespace EnterpriseTask.Api.Auth;

internal static class ClaimsPrincipalScopeReader
{
    public static bool TryGetUserId(ClaimsPrincipal? principal, out Guid userId)
    {
        return Guid.TryParse(principal?.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
    }

    public static UserScope GetRequiredScope(ClaimsPrincipal? principal)
    {
        if (principal is null)
        {
            throw new UnauthorizedAccessException("Missing authenticated user.");
        }

        if (!TryGetUserId(principal, out var userId))
        {
            throw new UnauthorizedAccessException("Missing authenticated user id.");
        }

        var departmentId = long.TryParse(principal.FindFirstValue("department_id"), out var parsedDepartmentId)
            ? parsedDepartmentId
            : (long?)null;

        var roleCodes = principal.FindAll(ClaimTypes.Role)
            .Select(claim => claim.Value.Trim())
            .Where(value => value.Length > 0);

        return UserScope.FromRoleCodes(userId, departmentId, roleCodes);
    }
}
