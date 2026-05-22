using System.Data;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseTask.Infrastructure.Persistence;

public abstract class PostgresCommandBase(ApplicationDbContext dbContext)
{
    protected async Task<T?> ExecuteScalarAsync<T>(
        string sql,
        IReadOnlyList<(string Name, object? Value)> parameters,
        CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection();
        var shouldClose = connection.State == ConnectionState.Closed;

        if (shouldClose)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            AddParameters(command, parameters);

            var result = await command.ExecuteScalarAsync(cancellationToken);
            if (result is null or DBNull)
            {
                return default;
            }

            if (typeof(T) == typeof(Guid))
            {
                return (T)(object)(result is Guid guid ? guid : Guid.Parse(Convert.ToString(result) ?? string.Empty));
            }

            return (T)Convert.ChangeType(result, typeof(T));
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }
    }

    protected async Task<int> ExecuteAsync(
        string sql,
        IReadOnlyList<(string Name, object? Value)> parameters,
        CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection();
        var shouldClose = connection.State == ConnectionState.Closed;

        if (shouldClose)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            AddParameters(command, parameters);
            return await command.ExecuteNonQueryAsync(cancellationToken);
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }
    }

    private static void AddParameters(System.Data.Common.DbCommand command, IReadOnlyList<(string Name, object? Value)> parameters)
    {
        foreach (var (name, value) in parameters)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = value ?? DBNull.Value;
            command.Parameters.Add(parameter);
        }
    }
}
