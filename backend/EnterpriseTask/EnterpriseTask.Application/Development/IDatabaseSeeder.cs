namespace EnterpriseTask.Application.Development;

public interface IDatabaseSeeder
{
    Task SeedAsync(CancellationToken cancellationToken);
}
