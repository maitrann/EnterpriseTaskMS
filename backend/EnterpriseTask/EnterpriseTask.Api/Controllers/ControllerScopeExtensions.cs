using System.Security.Claims;
using EnterpriseTask.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseTask.Api.Controllers;

internal static class ControllerScopeExtensions
{
    public static UserScope GetUserScope(this ControllerBase controller)
    {
        var userIdValue = controller.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdValue, out var userId))
        {
            throw new UnauthorizedAccessException("Missing authenticated user id.");
        }

        var departmentIdValue = controller.User.FindFirstValue("department_id");
        var departmentId = long.TryParse(departmentIdValue, out var parsedDepartmentId)
            ? parsedDepartmentId
            : (long?)null;

        var roles = controller.User.FindAll(ClaimTypes.Role)
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
