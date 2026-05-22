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
            SELECT p.id, COALESCE(p.employee_code, p.email, p.id::text) AS username, p.email,
                   p.full_name, COALESCE(r.name, r.code) AS role_label, p.avatar_url,
                   p.department_id, p.is_active, p.created_at
            FROM profiles p
            LEFT JOIN user_roles ur ON ur.user_id = p.id
            LEFT JOIN roles r ON r.id = ur.role_id
            WHERE LOWER(p.email) = LOWER(@email) OR LOWER(p.employee_code) = LOWER(@email)
            ORDER BY r.id
            LIMIT 1;
            """,
            reader => new UserLoginRow(
                reader.GetGuidValue("id"),
                reader.GetStringValue("username"),
                reader.GetNullableString("email"),
                reader.GetNullableString("full_name"),
                reader.GetNullableString("role_label"),
                reader.GetNullableString("avatar_url"),
                reader.GetNullableInt64("department_id"),
                reader.GetBooleanValue("is_active"),
                reader.GetDateTimeOffsetValue("created_at"),
                reader.GetStringValue("role_codes")),
            [("@email", normalizedEmail)],
            cancellationToken);

        var user = matches.SingleOrDefault();
        if (user is null || !user.IsActive || !AllowProfilePasswordBypass())
        {
            return null;
        }

        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(GetExpireMinutes());
        var token = CreateToken(user, expiresAt);
        return new LoginResponse(token, expiresAt, ToDto(user));
    }

    public async Task<AuthUserDto?> GetUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var users = await QueryAsync(
            """
            SELECT p.id, COALESCE(p.employee_code, p.email, p.id::text) AS username, p.email,
                   p.full_name, COALESCE(r.name, r.code) AS role_label, p.avatar_url,
                   p.department_id, p.is_active, p.created_at
            FROM profiles p
            LEFT JOIN user_roles ur ON ur.user_id = p.id
            LEFT JOIN roles r ON r.id = ur.role_id
            WHERE p.id = @id
            ORDER BY r.id
            LIMIT 1;
            """,
            reader => new AuthUserDto(
                reader.GetGuidValue("id"),
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
            new(ClaimTypes.Name, user.Username),
            new("department_id", user.DepartmentId?.ToString() ?? string.Empty)
        };

        foreach (var roleCode in user.RoleCodes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            claims.Add(new Claim(ClaimTypes.Role, roleCode));
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

    private bool AllowProfilePasswordBypass()
    {
        return bool.TryParse(configuration["Auth:AllowProfilePasswordBypass"], out var enabled) && enabled;
    }

    private static AuthUserDto ToDto(UserLoginRow user)
    {
        return new AuthUserDto(
            user.Id,
            user.Username,
            user.Email,
            user.FullName,
            string.IsNullOrWhiteSpace(user.RoleCodes) ? user.Role : user.RoleCodes,
            user.AvatarUrl,
            user.DepartmentId,
            user.IsActive,
            user.CreatedAt);
    }

    private sealed record UserLoginRow(
        Guid Id,
        string Username,
        string? Email,
        string? FullName,
        string? Role,
        string? AvatarUrl,
        long? DepartmentId,
        bool IsActive,
        DateTimeOffset CreatedAt,
        string RoleCodes);
}
