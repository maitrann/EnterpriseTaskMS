namespace EnterpriseTask.Application.Common;

public interface ICurrentUserContext
{
    bool TryGetUserId(out Guid userId);

    UserScope GetRequiredScope();
}
