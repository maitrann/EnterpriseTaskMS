using System.Security.Claims;
using EnterpriseTask.Application.Common;

namespace EnterpriseTask.Api.Auth;

public sealed class HttpCurrentUserContext(IHttpContextAccessor httpContextAccessor) : ICurrentUserContext
{
    public bool TryGetUserId(out Guid userId)
    {
        var principal = httpContextAccessor.HttpContext?.User;
        return Guid.TryParse(principal?.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
    }

    public UserScope GetRequiredScope()
    {
        var principal = httpContextAccessor.HttpContext?.User
            ?? throw new UnauthorizedAccessException("Missing authenticated user.");

        if (!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            throw new UnauthorizedAccessException("Missing authenticated user id.");
        }

        var departmentId = long.TryParse(principal.FindFirstValue("department_id"), out var parsedDepartmentId)
            ? parsedDepartmentId
            : (long?)null;

        var roles = principal.FindAll(ClaimTypes.Role)
            .Select(claim => claim.Value.ToLowerInvariant())
            .ToArray();

        return new UserScope(
            userId,
            departmentId,
            roles.Any(role => role.Contains("admin")),
            roles.Any(role => role.Contains("director")),
            roles.Any(role => role.Contains("manager")));
    }
}
