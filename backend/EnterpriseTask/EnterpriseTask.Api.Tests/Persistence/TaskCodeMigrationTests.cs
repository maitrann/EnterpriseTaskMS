using EnterpriseTask.Infrastructure.Persistence;
using Xunit;

namespace EnterpriseTask.Api.Tests.Persistence;

public sealed class TaskCodeMigrationTests
{
    [Fact]
    public void CollisionSafeTaskCodeMigration_DefinesSequenceFunctionAndTaskDefault()
    {
        var sql = ReadMigrationSql("0003_collision_safe_task_code.sql");

        Assert.Contains("CREATE SEQUENCE IF NOT EXISTS public.task_code_seq", sql, StringComparison.Ordinal);
        Assert.Contains("CREATE OR REPLACE FUNCTION public.next_task_code()", sql, StringComparison.Ordinal);
        Assert.Contains("nextval('public.task_code_seq'::regclass)", sql, StringComparison.Ordinal);
        Assert.Contains("ALTER COLUMN code SET DEFAULT public.next_task_code()", sql, StringComparison.Ordinal);
        Assert.Contains("IF NEW.code IS NULL OR btrim(NEW.code) = '' THEN", sql, StringComparison.Ordinal);
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
