using System.Data.Common;

namespace EnterpriseTask.Infrastructure.Persistence;

internal static class DbDataReaderExtensions
{
    public static string GetStringValue(this DbDataReader reader, string name)
    {
        var value = reader[name];
        return value is DBNull ? string.Empty : Convert.ToString(value) ?? string.Empty;
    }

    public static string? GetNullableString(this DbDataReader reader, string name)
    {
        var value = reader[name];
        return value is DBNull ? null : Convert.ToString(value);
    }

    public static long GetInt64Value(this DbDataReader reader, string name)
    {
        return Convert.ToInt64(reader[name]);
    }

    public static long? GetNullableInt64(this DbDataReader reader, string name)
    {
        var value = reader[name];
        return value is DBNull ? null : Convert.ToInt64(value);
    }

    public static int GetInt32Value(this DbDataReader reader, string name)
    {
        return Convert.ToInt32(reader[name]);
    }

    public static decimal? GetNullableDecimal(this DbDataReader reader, string name)
    {
        var value = reader[name];
        return value is DBNull ? null : Convert.ToDecimal(value);
    }

    public static bool GetBooleanValue(this DbDataReader reader, string name)
    {
        return Convert.ToBoolean(reader[name]);
    }

    public static DateOnly? GetNullableDateOnly(this DbDataReader reader, string name)
    {
        var value = reader[name];
        return value switch
        {
            DBNull => null,
            DateOnly dateOnly => dateOnly,
            DateTime dateTime => DateOnly.FromDateTime(dateTime),
            _ => DateOnly.Parse(Convert.ToString(value) ?? string.Empty)
        };
    }

    public static DateTimeOffset GetDateTimeOffsetValue(this DbDataReader reader, string name)
    {
        var value = reader[name];
        return value is DateTimeOffset dateTimeOffset ? dateTimeOffset : new DateTimeOffset(Convert.ToDateTime(value));
    }

    public static DateTimeOffset? GetNullableDateTimeOffset(this DbDataReader reader, string name)
    {
        var value = reader[name];
        if (value is DBNull)
        {
            return null;
        }

        return value is DateTimeOffset dateTimeOffset ? dateTimeOffset : new DateTimeOffset(Convert.ToDateTime(value));
    }
}
