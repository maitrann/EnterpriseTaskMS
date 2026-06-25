namespace EnterpriseTask.Infrastructure.Persistence;

public sealed record DatabaseConnectionOptions(bool IsConfigured, string? ConnectionString)
{
    public static DatabaseConnectionOptions From(string? connectionString)
    {
        return string.IsNullOrWhiteSpace(connectionString)
            ? new DatabaseConnectionOptions(false, null)
            : new DatabaseConnectionOptions(true, connectionString);
    }
}
