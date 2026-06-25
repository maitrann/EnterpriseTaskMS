using EnterpriseTask.Application.Common;

namespace EnterpriseTask.Infrastructure.Persistence;

public sealed class PostgresDatabaseHealthCheck(IDatabaseMigrator databaseMigrator) : IDatabaseHealthCheck
{
    public async Task<DatabaseHealthStatus> CheckAsync(CancellationToken cancellationToken)
    {
        var status = await databaseMigrator.GetStatusAsync(cancellationToken);
        var healthStatus = status is { IsConfigured: true, CanConnect: true }
            ? "Healthy"
            : status.IsConfigured ? "Unhealthy" : "Misconfigured";

        return new DatabaseHealthStatus(
            status.IsConfigured,
            status.CanConnect,
            healthStatus,
            status.LastAppliedMigration,
            status.Message);
    }
}
