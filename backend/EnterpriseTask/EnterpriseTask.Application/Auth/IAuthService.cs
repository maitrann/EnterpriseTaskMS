namespace EnterpriseTask.Application.Auth;

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken);

    Task<LoginResponse?> RefreshAsync(string refreshToken, CancellationToken cancellationToken);

    Task LogoutAsync(string refreshToken, CancellationToken cancellationToken);

    Task<AuthUserDto?> GetUserAsync(Guid userId, CancellationToken cancellationToken);
}
