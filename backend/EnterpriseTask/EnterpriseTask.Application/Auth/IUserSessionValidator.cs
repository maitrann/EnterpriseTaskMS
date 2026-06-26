namespace EnterpriseTask.Application.Auth;

public interface IUserSessionValidator
{
    Task<bool> IsValidAsync(
        Guid userId,
        IReadOnlyCollection<string> roleCodes,
        CancellationToken cancellationToken);
}
