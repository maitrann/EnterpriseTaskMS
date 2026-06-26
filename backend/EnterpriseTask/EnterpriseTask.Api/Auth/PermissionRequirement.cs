using Microsoft.AspNetCore.Authorization;

namespace EnterpriseTask.Api.Auth;

internal sealed class PermissionRequirement(string permissionCode) : IAuthorizationRequirement
{
    public string PermissionCode { get; } = permissionCode;
}
