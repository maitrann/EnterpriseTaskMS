using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EnterpriseTask.Application.Auth;
using EnterpriseTask.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace EnterpriseTask.Infrastructure.Auth;

public sealed class JwtAuthService(ApplicationDbContext dbContext, IConfiguration configuration) : PostgresQueryBase(dbContext), IAuthService
{
    public async Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var matches = await QueryAsync(
            """
            SELECT id, username, email, password_hash, full_name, role_label, avatar_url, department_id, is_active, created_at
            FROM users
            WHERE LOWER(email) = LOWER(@email) OR LOWER(username) = LOWER(@email)
            LIMIT 1;
            """,
            reader => new UserLoginRow(
                reader.GetInt64Value("id"),
                reader.GetStringValue("username"),
                reader.GetNullableString("email"),
                reader.GetNullableString("password_hash"),
                reader.GetNullableString("full_name"),
                reader.GetNullableString("role_label"),
                reader.GetNullableString("avatar_url"),
                reader.GetNullableInt64("department_id"),
                reader.GetBooleanValue("is_active"),
                reader.GetDateTimeOffsetValue("created_at")),
            [("@email", normalizedEmail)],
            cancellationToken);

        var user = matches.SingleOrDefault();
        if (user is null || !user.IsActive || !PasswordHasher.Verify(request.Password, user.PasswordHash))
        {
            return null;
        }

        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(GetExpireMinutes());
        var token = CreateToken(user, expiresAt);
        return new LoginResponse(token, expiresAt, ToDto(user));
    }

    public async Task<AuthUserDto?> GetUserAsync(long userId, CancellationToken cancellationToken)
    {
        var users = await QueryAsync(
            """
            SELECT id, username, email, full_name, role_label, avatar_url, department_id, is_active, created_at
            FROM users
            WHERE id = @id
            LIMIT 1;
            """,
            reader => new AuthUserDto(
                reader.GetInt64Value("id"),
                reader.GetStringValue("username"),
                reader.GetNullableString("email"),
                reader.GetNullableString("full_name"),
                reader.GetNullableString("role_label"),
                reader.GetNullableString("avatar_url"),
                reader.GetNullableInt64("department_id"),
                reader.GetBooleanValue("is_active"),
                reader.GetDateTimeOffsetValue("created_at")),
            [("@id", userId)],
            cancellationToken);

        return users.SingleOrDefault();
    }

    private string CreateToken(UserLoginRow user, DateTimeOffset expiresAt)
    {
        var secret = configuration["Jwt:Secret"];
        if (string.IsNullOrWhiteSpace(secret))
        {
            throw new InvalidOperationException("Missing Jwt:Secret configuration.");
        }

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
            SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username)
        };

        if (!string.IsNullOrWhiteSpace(user.Role))
        {
            claims.Add(new Claim(ClaimTypes.Role, user.Role));
        }

        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            claims: claims,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private int GetExpireMinutes()
    {
        return int.TryParse(configuration["Jwt:ExpireMinutes"], out var minutes) ? minutes : 60;
    }

    private static AuthUserDto ToDto(UserLoginRow user)
    {
        return new AuthUserDto(
            user.Id,
            user.Username,
            user.Email,
            user.FullName,
            user.Role,
            user.AvatarUrl,
            user.DepartmentId,
            user.IsActive,
            user.CreatedAt);
    }

    private sealed record UserLoginRow(
        long Id,
        string Username,
        string? Email,
        string? PasswordHash,
        string? FullName,
        string? Role,
        string? AvatarUrl,
        long? DepartmentId,
        bool IsActive,
        DateTimeOffset CreatedAt);
}
