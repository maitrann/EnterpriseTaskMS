using EnterpriseTask.Application.Common;

namespace EnterpriseTask.Api.Auth;

public sealed class HttpCurrentUserContext(IHttpContextAccessor httpContextAccessor) : ICurrentUserContext
{
    public bool TryGetUserId(out Guid userId)
    {
        return ClaimsPrincipalScopeReader.TryGetUserId(httpContextAccessor.HttpContext?.User, out userId);
    }

    public UserScope GetRequiredScope()
    {
        return ClaimsPrincipalScopeReader.GetRequiredScope(httpContextAccessor.HttpContext?.User);
    }
}
