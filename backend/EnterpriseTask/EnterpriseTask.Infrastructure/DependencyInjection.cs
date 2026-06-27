using EnterpriseTask.Application.Auth;
using EnterpriseTask.Application.Common;
using EnterpriseTask.Application.Departments;
using EnterpriseTask.Application.Development;
using EnterpriseTask.Application.InterDepartmentRequests;
using EnterpriseTask.Application.Projects;
using EnterpriseTask.Application.Roles;
using EnterpriseTask.Application.Tasks;
using EnterpriseTask.Application.Users;
using EnterpriseTask.Infrastructure.Auth;
using EnterpriseTask.Infrastructure.Departments;
using EnterpriseTask.Infrastructure.Development;
using EnterpriseTask.Infrastructure.InterDepartmentRequests;
using EnterpriseTask.Infrastructure.Persistence;
using EnterpriseTask.Infrastructure.Projects;
using EnterpriseTask.Infrastructure.Roles;
using EnterpriseTask.Infrastructure.Tasks;
using EnterpriseTask.Infrastructure.Users;
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

        var databaseConnection = DatabaseConnectionOptions.From(connectionString);
        services.AddSingleton(databaseConnection);

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            if (databaseConnection.IsConfigured)
            {
                options.UseNpgsql(databaseConnection.ConnectionString);
            }

            if (enableDetailedErrors)
            {
                options.EnableDetailedErrors();
            }
        });

        services.AddScoped<ITaskQueries, PostgresTaskQueries>();
        services.AddScoped<ITaskCommands, PostgresTaskCommands>();
        services.AddScoped<ITaskAccessReader, PostgresTaskAccessReader>();
        services.AddScoped<ITaskPolicyQueries, PostgresTaskPolicyQueries>();
        services.AddScoped<IPermissionChecker, PostgresPermissionChecker>();
        services.AddScoped<CreateTaskHandler>();
        services.AddScoped<UpdateTaskStatusHandler>();
        services.AddScoped<IDepartmentQueries, PostgresDepartmentQueries>();
        services.AddScoped<IDepartmentAdministrationCommands, PostgresDepartmentAdministrationCommands>();
        services.AddScoped<IProjectQueries, PostgresProjectQueries>();
        services.AddScoped<IUserQueries, PostgresUserQueries>();
        services.AddScoped<IUserAdministrationCommands, PostgresUserAdministrationCommands>();
        services.AddScoped<IRoleQueries, PostgresRoleQueries>();
        services.AddScoped<IInterDepartmentRequestQueries, PostgresInterDepartmentRequestQueries>();
        services.AddScoped<IInterDepartmentRequestCommands, PostgresInterDepartmentRequestCommands>();
        services.AddScoped<IInterDepartmentRequestPolicyQueries, PostgresInterDepartmentRequestPolicyQueries>();
        services.AddScoped<AssignInterRequestOwnerHandler>();
        services.AddScoped<IAuthService, JwtAuthService>();
        services.AddScoped<IUserSessionValidator, PostgresUserSessionValidator>();
        services.AddScoped<IDatabaseHealthCheck, PostgresDatabaseHealthCheck>();
        services.AddScoped<IDatabaseMigrator, PostgresDatabaseMigrator>();
        services.AddScoped<IDatabaseSeeder, DatabaseSeeder>();

        return services;
    }
}
