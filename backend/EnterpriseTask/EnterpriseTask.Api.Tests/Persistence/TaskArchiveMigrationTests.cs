using EnterpriseTask.Infrastructure.Persistence;
using Xunit;

namespace EnterpriseTask.Api.Tests.Persistence;

public sealed class TaskArchiveMigrationTests
{
    [Fact]
    public void TaskArchiveMigration_DefinesSoftArchiveColumnsAndIndexes()
    {
        var sql = ReadMigrationSql("0004_task_archive_policy.sql");

        Assert.Contains("ADD COLUMN IF NOT EXISTS archived_at TIMESTAMPTZ", sql, StringComparison.Ordinal);
        Assert.Contains("ADD COLUMN IF NOT EXISTS archived_by UUID", sql, StringComparison.Ordinal);
        Assert.Contains("ADD COLUMN IF NOT EXISTS archive_reason TEXT", sql, StringComparison.Ordinal);
        Assert.Contains("idx_tasks_unarchived_created_at", sql, StringComparison.Ordinal);
        Assert.Contains("WHERE archived_at IS NULL", sql, StringComparison.Ordinal);
    }

    private static string ReadMigrationSql(string fileName)
    {
        var assembly = typeof(PostgresDatabaseMigrator).Assembly;
        var resourceName = assembly.GetManifestResourceNames()
            .Single(name => name.EndsWith(".Persistence.Migrations." + fileName, StringComparison.Ordinal));

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Migration resource '{resourceName}' was not found.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
