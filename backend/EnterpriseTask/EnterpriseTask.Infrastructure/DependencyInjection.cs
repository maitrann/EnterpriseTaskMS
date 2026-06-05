using EnterpriseTask.Application.Auth;
using EnterpriseTask.Application.Common;
using EnterpriseTask.Application.Departments;
using EnterpriseTask.Application.Development;
using EnterpriseTask.Application.InterDepartmentRequests;
using EnterpriseTask.Application.Projects;
using EnterpriseTask.Application.Tasks;
using EnterpriseTask.Infrastructure.Auth;
using EnterpriseTask.Infrastructure.Departments;
using EnterpriseTask.Infrastructure.Development;
using EnterpriseTask.Infrastructure.InterDepartmentRequests;
using EnterpriseTask.Infrastructure.Persistence;
using EnterpriseTask.Infrastructure.Projects;
using EnterpriseTask.Infrastructure.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseTask.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration, bool enableDetailedErrors)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? configuration.GetConnectionString("Default");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Missing database connection string. Configure ConnectionStrings:DefaultConnection.");
        }

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseNpgsql(connectionString);

            if (enableDetailedErrors)
            {
                options.EnableDetailedErrors();
            }
        });

        services.AddScoped<ITaskQueries, PostgresTaskQueries>();
        services.AddScoped<ITaskCommands, PostgresTaskCommands>();
        services.AddScoped<ITaskAccessReader, PostgresTaskAccessReader>();
        services.AddScoped<IDepartmentQueries, PostgresDepartmentQueries>();
        services.AddScoped<IProjectQueries, PostgresProjectQueries>();
        services.AddScoped<IInterDepartmentRequestQueries, PostgresInterDepartmentRequestQueries>();
        services.AddScoped<IInterDepartmentRequestCommands, PostgresInterDepartmentRequestCommands>();
        services.AddScoped<IAuthService, JwtAuthService>();
        services.AddScoped<IDatabaseHealthCheck, PostgresDatabaseHealthCheck>();
        services.AddScoped<IDatabaseSeeder, DatabaseSeeder>();

        return services;
    }
}
