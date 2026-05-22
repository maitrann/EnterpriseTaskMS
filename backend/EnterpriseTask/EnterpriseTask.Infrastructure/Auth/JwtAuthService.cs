using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using EnterpriseTask.Application.Auth;
using EnterpriseTask.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace EnterpriseTask.Infrastructure.Auth;

public sealed class JwtAuthService(ApplicationDbContext dbContext, IConfiguration configuration) : PostgresQueryBase(dbContext), IAuthService
{
    private static readonly HttpClient SupabaseHttpClient = new();

    public async Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        UserLoginRow? user = null;
        var supabaseUserId = await AuthenticateWithSupabaseAuthAsync(normalizedEmail, request.Password, cancellationToken);
        if (supabaseUserId is not null)
        {
            user = await LoadUserByIdAsync(supabaseUserId.Value, cancellationToken);
        }
        else if (AllowProfilePasswordBypass())
        {
            user = await LoadUserByLoginAsync(normalizedEmail, cancellationToken);
        }

        if (user is null || !user.IsActive)
        {
            return null;
        }

        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(GetExpireMinutes());
        var token = CreateToken(user, expiresAt);
        return new LoginResponse(token, expiresAt, ToDto(user));
    }

    public async Task<AuthUserDto?> GetUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await LoadUserByIdAsync(userId, cancellationToken);
        return user is null ? null : ToDto(user);
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

    private async Task<Guid?> AuthenticateWithSupabaseAuthAsync(string email, string password, CancellationToken cancellationToken)
    {
        var supabaseUrl = configuration["Supabase:Url"];
        var anonKey = configuration["Supabase:AnonKey"];
        if (string.IsNullOrWhiteSpace(supabaseUrl) || string.IsNullOrWhiteSpace(anonKey))
        {
            return null;
        }

        var endpoint = $"{supabaseUrl.TrimEnd('/')}/auth/v1/token?grant_type=password";
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(new { email, password }),
                Encoding.UTF8,
                "application/json")
        };

        request.Headers.TryAddWithoutValidation("apikey", anonKey);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", anonKey);

        using var response = await SupabaseHttpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        if (!document.RootElement.TryGetProperty("user", out var userElement)
            || !userElement.TryGetProperty("id", out var idElement)
            || !Guid.TryParse(idElement.GetString(), out var userId))
        {
            return null;
        }

        return userId;
    }

    private async Task<UserLoginRow?> LoadUserByLoginAsync(string normalizedEmailOrCode, CancellationToken cancellationToken)
    {
        var matches = await QueryAsync(GetUserSql("WHERE LOWER(p.email) = LOWER(@email) OR LOWER(p.employee_code) = LOWER(@email)"),
            MapUserLoginRow,
            [("@email", normalizedEmailOrCode)],
            cancellationToken);

        return matches.SingleOrDefault();
    }

    private async Task<UserLoginRow?> LoadUserByIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        var matches = await QueryAsync(GetUserSql("WHERE p.id = @id"),
            MapUserLoginRow,
            [("@id", userId)],
            cancellationToken);

        return matches.SingleOrDefault();
    }

    private static string GetUserSql(string whereClause)
    {
        return $$"""
            SELECT p.id, COALESCE(p.employee_code, p.email, p.id::text) AS username, p.email,
                   p.full_name,
                   MIN(COALESCE(r.name, r.code)) AS role_label,
                   COALESCE(string_agg(DISTINCT r.code, ',' ORDER BY r.code), '') AS role_codes,
                   p.avatar_url,
                   p.department_id,
                   p.is_active,
                   p.created_at
            FROM profiles p
            LEFT JOIN user_roles ur ON ur.user_id = p.id
            LEFT JOIN roles r ON r.id = ur.role_id
            {{whereClause}}
            GROUP BY p.id, p.employee_code, p.email, p.full_name, p.avatar_url, p.department_id, p.is_active, p.created_at
            LIMIT 1;
            """;
    }

    private static UserLoginRow MapUserLoginRow(System.Data.Common.DbDataReader reader)
    {
        return new UserLoginRow(
            reader.GetGuidValue("id"),
            reader.GetStringValue("username"),
            reader.GetNullableString("email"),
            reader.GetNullableString("full_name"),
            reader.GetNullableString("role_label"),
            reader.GetNullableString("avatar_url"),
            reader.GetNullableInt64("department_id"),
            reader.GetBooleanValue("is_active"),
            reader.GetDateTimeOffsetValue("created_at"),
            reader.GetStringValue("role_codes"));
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
