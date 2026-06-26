using EnterpriseTask.Api.Auth;
using EnterpriseTask.Application.Common;
using EnterpriseTask.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.OpenApi;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserContext, HttpCurrentUserContext>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment.IsDevelopment());

var jwtSecret = GetRequiredConfiguration(builder.Configuration, "Jwt:Secret");
var jwtIssuer = GetRequiredConfiguration(builder.Configuration, "Jwt:Issuer");
var jwtAudience = GetRequiredConfiguration(builder.Configuration, "Jwt:Audience");
if (Encoding.UTF8.GetByteCount(jwtSecret) < 32)
{
    throw new InvalidOperationException("Jwt:Secret must be at least 32 UTF-8 bytes for HMAC-SHA256 signing.");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ClockSkew = TimeSpan.FromMinutes(2)
        };
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthorizationPolicyNames.AuthenticatedUser, policy =>
        policy.RequireAuthenticatedUser());
    options.AddPolicy(AuthorizationPolicyNames.AdminOnly, policy =>
        policy.RequireRole(RoleCodes.Admin));
    options.AddPolicy(AuthorizationPolicyNames.ElevatedDataReader, policy =>
        policy.RequireRole(RoleCodes.Admin, RoleCodes.Director));
    options.AddPolicy(AuthorizationPolicyNames.DepartmentDataReader, policy =>
        policy.RequireRole(RoleCodes.Admin, RoleCodes.Director, RoleCodes.Manager));
    AddPermissionPolicy(options, AuthorizationPolicyNames.TaskCreate, PermissionCodes.TaskCreate);
    AddPermissionPolicy(options, AuthorizationPolicyNames.TaskUpdate, PermissionCodes.TaskUpdate);
    AddPermissionPolicy(options, AuthorizationPolicyNames.TaskAssign, PermissionCodes.TaskAssign);
    AddPermissionPolicy(options, AuthorizationPolicyNames.CommentCreate, PermissionCodes.CommentCreate);
});
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("AuthLogin", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
    options.AddPolicy("ApiMutation", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.User.Identity?.Name ?? context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 120,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:4200", "https://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Enterprise Task Management API",
        Version = "v1",
        Description = "Internal API for task, project, department, and inter-department request workflows."
    });

    options.TagActionsBy(api =>
    {
        var controllerName = api.ActionDescriptor.RouteValues["controller"];
        return [controllerName ?? "API"];
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Paste a JWT access token returned by POST /api/auth/login."
    });

});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Enterprise Task Management API v1");
        options.RoutePrefix = "swagger";
        options.DocumentTitle = "Enterprise Task Management API";
        options.DisplayRequestDuration();
        options.EnableTryItOutByDefault();
        options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
    });

    app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
}

app.UseHttpsRedirection();

app.UseCors("Frontend");

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

static string GetRequiredConfiguration(IConfiguration configuration, string key)
{
    var value = configuration[key];
    if (string.IsNullOrWhiteSpace(value))
    {
        throw new InvalidOperationException($"Missing required configuration '{key}'.");
    }

    return value;
}

static void AddPermissionPolicy(AuthorizationOptions options, string policyName, string permissionCode)
{
    options.AddPolicy(policyName, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.AddRequirements(new PermissionRequirement(permissionCode));
    });
}
