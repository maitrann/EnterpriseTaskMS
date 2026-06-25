namespace EnterpriseTask.Application.Common;

public interface IDatabaseHealthCheck
{
    Task<DatabaseHealthStatus> CheckAsync(CancellationToken cancellationToken);
}

public sealed record DatabaseHealthStatus(
    bool IsConfigured,
    bool CanConnect,
    string Status,
    string? LastAppliedMigration,
    string? Message);

public interface IDatabaseMigrator
{
    Task<DatabaseMigrationStatus> GetStatusAsync(CancellationToken cancellationToken);

    Task<DatabaseMigrationResult> ApplyAsync(CancellationToken cancellationToken);
}

public sealed record DatabaseMigrationStatus(
    bool IsConfigured,
    bool CanConnect,
    string? LastAppliedMigration,
    IReadOnlyList<string> PendingMigrations,
    string? Message);

public sealed record DatabaseMigrationResult(
    bool IsConfigured,
    bool CanConnect,
    IReadOnlyList<string> AppliedMigrations,
    IReadOnlyList<string> SkippedMigrations,
    string? LastAppliedMigration,
    string? Message);
