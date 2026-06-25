using EnterpriseTask.Api.Auth;
using EnterpriseTask.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseTask.Api.Controllers;

internal static class ControllerScopeExtensions
{
    public static UserScope GetUserScope(this ControllerBase controller)
    {
        return ClaimsPrincipalScopeReader.GetRequiredScope(controller.User);
    }
}
