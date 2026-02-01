namespace IndexSmith.EFCore.SqlServer.Provider;

/// <summary>
/// SQL Server-specific index capabilities and constraints.
/// </summary>
public static class SqlServerIndexCapabilities
{
    /// <summary>
    /// Maximum key length for a clustered index in bytes.
    /// </summary>
    public const int MaxClusteredIndexKeyLength = 900;

    /// <summary>
    /// Maximum key length for a nonclustered index in bytes.
    /// </summary>
    public const int MaxNonClusteredIndexKeyLength = 1700;

    /// <summary>
    /// Maximum number of columns in an index key.
    /// </summary>
    public const int MaxKeyColumns = 16;

    /// <summary>
    /// Maximum number of columns in an index (including INCLUDE columns).
    /// </summary>
    public const int MaxTotalColumns = 32;

    /// <summary>
    /// Determines whether a CLR type is suitable for indexing in SQL Server.
    /// </summary>
    public static bool IsIndexableType(Type clrType)
    {
        var underlyingType = Nullable.GetUnderlyingType(clrType) ?? clrType;

        // Exclude types that cannot be indexed
        if (underlyingType == typeof(byte[]))
            return false;

        // Most primitive types are indexable
        if (underlyingType.IsPrimitive || underlyingType.IsEnum)
            return true;

        // Common value types
        if (underlyingType == typeof(decimal) ||
            underlyingType == typeof(DateTime) ||
            underlyingType == typeof(DateTimeOffset) ||
            underlyingType == typeof(DateOnly) ||
            underlyingType == typeof(TimeOnly) ||
            underlyingType == typeof(TimeSpan) ||
            underlyingType == typeof(Guid))
            return true;

        // Strings are conditionally indexable (based on length)
        if (underlyingType == typeof(string))
            return true;

        return false;
    }

    /// <summary>
    /// Estimates the byte size of a column for index key length calculation.
    /// </summary>
    public static int EstimateColumnByteSize(Type clrType, int? maxLength = null)
    {
        var underlyingType = Nullable.GetUnderlyingType(clrType) ?? clrType;

        return underlyingType switch
        {
            _ when underlyingType == typeof(bool) => 1,
            _ when underlyingType == typeof(byte) => 1,
            _ when underlyingType == typeof(short) => 2,
            _ when underlyingType == typeof(int) => 4,
            _ when underlyingType == typeof(long) => 8,
            _ when underlyingType == typeof(float) => 4,
            _ when underlyingType == typeof(double) => 8,
            _ when underlyingType == typeof(decimal) => 17,
            _ when underlyingType == typeof(DateTime) => 8,
            _ when underlyingType == typeof(DateTimeOffset) => 10,
            _ when underlyingType == typeof(DateOnly) => 3,
            _ when underlyingType == typeof(TimeOnly) => 5,
            _ when underlyingType == typeof(TimeSpan) => 8,
            _ when underlyingType == typeof(Guid) => 16,
            _ when underlyingType == typeof(string) => (maxLength ?? 450) * 2, // nvarchar uses 2 bytes per char
            _ when underlyingType.IsEnum => 4, // Enums typically stored as int
            _ => 0
        };
    }

    /// <summary>
    /// Validates whether a set of columns can form a valid index key.
    /// </summary>
    public static (bool IsValid, string? Error) ValidateIndexKey(
        IEnumerable<(Type ClrType, int? MaxLength)> columns,
        bool isClustered = false)
    {
        var columnList = columns.ToList();

        if (columnList.Count > MaxKeyColumns)
        {
            return (false, $"Index exceeds maximum of {MaxKeyColumns} key columns");
        }

        var totalByteSize = columnList.Sum(c => EstimateColumnByteSize(c.ClrType, c.MaxLength));
        var maxKeyLength = isClustered ? MaxClusteredIndexKeyLength : MaxNonClusteredIndexKeyLength;

        if (totalByteSize > maxKeyLength)
        {
            return (false, $"Index key size ({totalByteSize} bytes) exceeds maximum ({maxKeyLength} bytes)");
        }

        return (true, null);
    }
}
