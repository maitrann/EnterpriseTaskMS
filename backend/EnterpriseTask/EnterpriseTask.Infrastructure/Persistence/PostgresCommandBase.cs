using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace EnterpriseTask.Infrastructure.Persistence;

public abstract class PostgresCommandBase : PostgresQueryBase
{
    protected PostgresCommandBase(ApplicationDbContext dbContext) : base(dbContext)
    {
    }

    protected async Task<T> ExecuteInTransactionAsync<T>(
        Func<Task<T>> action,
        CancellationToken cancellationToken)
    {
        if (DbContext.Database.CurrentTransaction is not null)
        {
            return await action();
        }

        await using var transaction = await DbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var result = await action();
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    protected async Task<T?> ExecuteScalarAsync<T>(
        string sql,
        IReadOnlyList<(string Name, object? Value)> parameters,
        CancellationToken cancellationToken)
    {
        var connection = DbContext.Database.GetDbConnection();
        var shouldClose = connection.State == ConnectionState.Closed;

        if (shouldClose)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.Transaction = DbContext.Database.CurrentTransaction?.GetDbTransaction();
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

            var targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
            return (T)Convert.ChangeType(result, targetType);
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
        var connection = DbContext.Database.GetDbConnection();
        var shouldClose = connection.State == ConnectionState.Closed;

        if (shouldClose)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.Transaction = DbContext.Database.CurrentTransaction?.GetDbTransaction();
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
