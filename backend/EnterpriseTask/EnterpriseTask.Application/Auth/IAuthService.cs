namespace EnterpriseTask.Application.Auth;

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken);

    Task<AuthUserDto?> GetUserAsync(Guid userId, CancellationToken cancellationToken);
}
