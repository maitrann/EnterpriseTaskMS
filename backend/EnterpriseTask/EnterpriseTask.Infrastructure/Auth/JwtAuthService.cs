using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EnterpriseTask.Application.Auth;
using EnterpriseTask.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace EnterpriseTask.Infrastructure.Auth;

public sealed class JwtAuthService(ApplicationDbContext dbContext, IConfiguration configuration) : PostgresCommandBase(dbContext), IAuthService
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

        return await CreateLoginResponseAsync(user, Guid.NewGuid(), cancellationToken);
    }

    public async Task<LoginResponse?> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return null;
        }

        var tokenHash = HashRefreshToken(refreshToken);
        var sessions = await QueryAsync(
            """
            SELECT s.id, s.user_id, s.family_id, s.expires_at, s.revoked_at, p.is_active
            FROM auth_refresh_sessions s
            JOIN profiles p ON p.id = s.user_id
            WHERE s.token_hash = @tokenHash
            LIMIT 1;
            """,
            MapRefreshSessionRow,
            [("@tokenHash", tokenHash)],
            cancellationToken);

        var session = sessions.SingleOrDefault();
        if (session is null
            || session.RevokedAt is not null
            || session.ExpiresAt <= DateTimeOffset.UtcNow
            || !session.IsActive)
        {
            return null;
        }

        var user = await LoadUserByIdAsync(session.UserId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            return null;
        }

        return await ExecuteInTransactionAsync<LoginResponse?>(async () =>
        {
            var revoked = await ExecuteAsync(
                """
                UPDATE auth_refresh_sessions
                SET revoked_at = now(), last_used_at = now()
                WHERE id = @id AND revoked_at IS NULL;
                """,
                [("@id", session.Id)],
                cancellationToken);

            if (revoked == 0)
            {
                return null;
            }

            return await CreateLoginResponseAsync(user, session.FamilyId, cancellationToken, session.Id);
        }, cancellationToken);
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return;
        }

        await ExecuteAsync(
            """
            WITH token_family AS (
                SELECT family_id
                FROM auth_refresh_sessions
                WHERE token_hash = @tokenHash
            )
            UPDATE auth_refresh_sessions
            SET revoked_at = COALESCE(revoked_at, now())
            WHERE family_id IN (SELECT family_id FROM token_family);
            """,
            [("@tokenHash", HashRefreshToken(refreshToken))],
            cancellationToken);
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

    private int GetRefreshTokenDays()
    {
        return int.TryParse(configuration["Auth:RefreshTokenDays"], out var days) ? days : 14;
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

    private async Task<LoginResponse> CreateLoginResponseAsync(
        UserLoginRow user,
        Guid familyId,
        CancellationToken cancellationToken,
        Guid? replacesSessionId = null)
    {
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(GetExpireMinutes());
        var refreshToken = CreateRefreshToken();
        var refreshSessionId = await ExecuteScalarAsync<Guid>(
            """
            INSERT INTO auth_refresh_sessions (user_id, token_hash, family_id, expires_at)
            VALUES (@userId, @tokenHash, @familyId, @expiresAt)
            RETURNING id;
            """,
            [
                ("@userId", user.Id),
                ("@tokenHash", HashRefreshToken(refreshToken)),
                ("@familyId", familyId),
                ("@expiresAt", DateTimeOffset.UtcNow.AddDays(GetRefreshTokenDays()))
            ],
            cancellationToken);

        if (replacesSessionId is not null)
        {
            await ExecuteAsync(
                """
                UPDATE auth_refresh_sessions
                SET replaced_by_session_id = @refreshSessionId
                WHERE id = @replacesSessionId;
                """,
                [("@refreshSessionId", refreshSessionId), ("@replacesSessionId", replacesSessionId.Value)],
                cancellationToken);
        }

        return new LoginResponse(CreateToken(user, expiresAt), expiresAt, ToDto(user), refreshToken);
    }

    private static string CreateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static string HashRefreshToken(string refreshToken)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken));
        return Convert.ToHexString(hash).ToLowerInvariant();
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

    private static RefreshSessionRow MapRefreshSessionRow(System.Data.Common.DbDataReader reader)
    {
        return new RefreshSessionRow(
            reader.GetGuidValue("id"),
            reader.GetGuidValue("user_id"),
            reader.GetGuidValue("family_id"),
            reader.GetDateTimeOffsetValue("expires_at"),
            reader.GetNullableDateTimeOffset("revoked_at"),
            reader.GetBooleanValue("is_active"));
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

    private sealed record RefreshSessionRow(
        Guid Id,
        Guid UserId,
        Guid FamilyId,
        DateTimeOffset ExpiresAt,
        DateTimeOffset? RevokedAt,
        bool IsActive);
}
