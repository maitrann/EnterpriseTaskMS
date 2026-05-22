namespace EnterpriseTask.Application.Auth;

public sealed record LoginRequest(string Email, string Password);

public sealed record AuthUserDto(
    Guid Id,
    string Username,
    string? Email,
    string? FullName,
    string? Role,
    string? AvatarUrl,
    long? DepartmentId,
    bool IsActive,
    DateTimeOffset CreatedAt);

public sealed record LoginResponse(string AccessToken, DateTimeOffset ExpiresAt, AuthUserDto User);
