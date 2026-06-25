using System.Data;
using System.Data.Common;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using EnterpriseTask.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseTask.Infrastructure.Persistence;

public sealed class PostgresDatabaseMigrator(
    ApplicationDbContext dbContext,
    DatabaseConnectionOptions connectionOptions) : IDatabaseMigrator
{
    private const string MigrationTableSql = """
        CREATE TABLE IF NOT EXISTS public.schema_migrations (
            version TEXT PRIMARY KEY,
            name TEXT NOT NULL,
            checksum TEXT NOT NULL,
            applied_at TIMESTAMPTZ NOT NULL DEFAULT now()
        );
        """;

    public async Task<DatabaseMigrationStatus> GetStatusAsync(CancellationToken cancellationToken)
    {
        if (!connectionOptions.IsConfigured)
        {
            return new DatabaseMigrationStatus(
                false,
                false,
                null,
                Array.Empty<string>(),
                "Missing database connection string. Configure ConnectionStrings:DefaultConnection.");
        }

        if (!await CanConnectAsync(cancellationToken))
        {
            return new DatabaseMigrationStatus(
                true,
                false,
                null,
                Array.Empty<string>(),
                "Database connection failed.");
        }

        var migrations = LoadEmbeddedMigrations();
        var applied = await ReadAppliedMigrationVersionsAsync(cancellationToken);
        var pending = migrations
            .Where(migration => !applied.Contains(migration.Version))
            .Select(migration => migration.FileName)
            .ToArray();

        return new DatabaseMigrationStatus(
            true,
            true,
            applied.LastOrDefault(),
            pending,
            pending.Length == 0 ? "Database migrations are up to date." : "Database has pending migrations.");
    }

    public async Task<DatabaseMigrationResult> ApplyAsync(CancellationToken cancellationToken)
    {
        if (!connectionOptions.IsConfigured)
        {
            return new DatabaseMigrationResult(
                false,
                false,
                Array.Empty<string>(),
                Array.Empty<string>(),
                null,
                "Missing database connection string. Configure ConnectionStrings:DefaultConnection.");
        }

        if (!await CanConnectAsync(cancellationToken))
        {
            return new DatabaseMigrationResult(
                true,
                false,
                Array.Empty<string>(),
                Array.Empty<string>(),
                null,
                "Database connection failed.");
        }

        var migrations = LoadEmbeddedMigrations();
        if (migrations.Count == 0)
        {
            return new DatabaseMigrationResult(
                true,
                true,
                Array.Empty<string>(),
                Array.Empty<string>(),
                null,
                "No embedded migrations were found.");
        }

        await ExecuteNonQueryAsync(MigrationTableSql, cancellationToken);

        var applied = await ReadAppliedMigrationVersionsAsync(cancellationToken);
        var appliedNow = new List<string>();
        var skipped = new List<string>();

        if (applied.Count == 0 && await HasExistingApplicationSchemaAsync(cancellationToken))
        {
            var baseline = migrations[0];
            await InsertMigrationRecordAsync(
                baseline.Version,
                baseline.Name,
                baseline.Checksum,
                cancellationToken);
            applied.Add(baseline.Version);
            skipped.Add($"{baseline.FileName} (baseline recorded for existing schema)");
        }

        foreach (var migration in migrations)
        {
            if (applied.Contains(migration.Version))
            {
                continue;
            }

            await ExecuteNonQueryAsync(migration.Sql, cancellationToken);
            await InsertMigrationRecordAsync(
                migration.Version,
                migration.Name,
                migration.Checksum,
                cancellationToken);

            applied.Add(migration.Version);
            appliedNow.Add(migration.FileName);
        }

        return new DatabaseMigrationResult(
            true,
            true,
            appliedNow,
            skipped,
            applied.LastOrDefault(),
            appliedNow.Count == 0 ? "Database migrations are up to date." : "Database migrations applied.");
    }

    private async Task<bool> CanConnectAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await dbContext.Database.CanConnectAsync(cancellationToken);
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private static IReadOnlyList<EmbeddedMigration> LoadEmbeddedMigrations()
    {
        var assembly = typeof(PostgresDatabaseMigrator).Assembly;
        var resources = assembly.GetManifestResourceNames()
            .Where(name => name.Contains(".Persistence.Migrations.", StringComparison.Ordinal)
                && name.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        var migrations = new List<EmbeddedMigration>(resources.Length);
        foreach (var resource in resources)
        {
            using var stream = assembly.GetManifestResourceStream(resource)
                ?? throw new InvalidOperationException($"Embedded migration '{resource}' was not found.");
            using var reader = new StreamReader(stream, Encoding.UTF8);
            var sql = reader.ReadToEnd();
            var fileName = ExtractFileName(resource);
            var version = ExtractVersion(fileName);
            var name = Path.GetFileNameWithoutExtension(fileName);
            var checksum = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(sql)));

            migrations.Add(new EmbeddedMigration(version, name, fileName, sql, checksum));
        }

        return migrations;
    }

    private async Task<HashSet<string>> ReadAppliedMigrationVersionsAsync(CancellationToken cancellationToken)
    {
        if (!await MigrationTableExistsAsync(cancellationToken))
        {
            return new HashSet<string>(StringComparer.Ordinal);
        }

        var versions = new HashSet<string>(StringComparer.Ordinal);
        await using var command = await CreateCommandAsync("""
            SELECT version
            FROM public.schema_migrations
            ORDER BY version;
            """, cancellationToken);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            versions.Add(reader.GetString(0));
        }

        return versions;
    }

    private async Task<bool> MigrationTableExistsAsync(CancellationToken cancellationToken)
    {
        var value = await ExecuteScalarAsync(
            "SELECT to_regclass('public.schema_migrations') IS NOT NULL;",
            cancellationToken);

        return value is bool exists && exists;
    }

    private async Task<bool> HasExistingApplicationSchemaAsync(CancellationToken cancellationToken)
    {
        var value = await ExecuteScalarAsync(
            "SELECT to_regclass('public.tasks') IS NOT NULL;",
            cancellationToken);

        return value is bool exists && exists;
    }

    private async Task InsertMigrationRecordAsync(
        string version,
        string name,
        string checksum,
        CancellationToken cancellationToken)
    {
        await using var command = await CreateCommandAsync("""
            INSERT INTO public.schema_migrations (version, name, checksum)
            VALUES (@version, @name, @checksum)
            ON CONFLICT (version) DO NOTHING;
            """, cancellationToken);
        AddParameter(command, "version", version);
        AddParameter(command, "name", name);
        AddParameter(command, "checksum", checksum);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<object?> ExecuteScalarAsync(string sql, CancellationToken cancellationToken)
    {
        await using var command = await CreateCommandAsync(sql, cancellationToken);
        return await command.ExecuteScalarAsync(cancellationToken);
    }

    private async Task ExecuteNonQueryAsync(string sql, CancellationToken cancellationToken)
    {
        await using var command = await CreateCommandAsync(sql, cancellationToken);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<DbCommand> CreateCommandAsync(string sql, CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        var command = connection.CreateCommand();
        command.CommandText = sql;
        return command;
    }

    private static void AddParameter(DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    private static string ExtractFileName(string resource)
    {
        const string marker = ".Persistence.Migrations.";
        var index = resource.IndexOf(marker, StringComparison.Ordinal);
        return index < 0 ? resource : resource[(index + marker.Length)..];
    }

    private static string ExtractVersion(string fileName)
    {
        var separatorIndex = fileName.IndexOf('_', StringComparison.Ordinal);
        if (separatorIndex <= 0)
        {
            separatorIndex = fileName.IndexOf('.', StringComparison.Ordinal);
        }

        if (separatorIndex <= 0)
        {
            throw new InvalidOperationException($"Migration file '{fileName}' must start with a version prefix.");
        }

        return fileName[..separatorIndex];
    }

    private sealed record EmbeddedMigration(
        string Version,
        string Name,
        string FileName,
        string Sql,
        string Checksum);
}
