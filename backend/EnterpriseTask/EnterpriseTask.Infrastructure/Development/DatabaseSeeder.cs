using EnterpriseTask.Application.Development;

namespace EnterpriseTask.Infrastructure.Development;

public sealed class DatabaseSeeder : IDatabaseSeeder
{
    public Task SeedAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
