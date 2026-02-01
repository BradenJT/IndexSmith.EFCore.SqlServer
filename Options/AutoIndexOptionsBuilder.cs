namespace IndexSmith.EFCore.SqlServer.Options;

/// <summary>
/// Fluent builder for configuring AutoIndexOptions.
/// </summary>
public sealed class AutoIndexOptionsBuilder
{
    private readonly AutoIndexOptions _options = new();

    /// <summary>
    /// Sets the minimum score threshold for automatic index creation.
    /// </summary>
    public AutoIndexOptionsBuilder WithScoreThreshold(int threshold)
    {
        _options.ScoreThreshold = threshold;
        return this;
    }

    /// <summary>
    /// Enables or disables automatic indexing of foreign key properties.
    /// </summary>
    public AutoIndexOptionsBuilder EnableForeignKeyIndexes(bool enable = true)
    {
        _options.EnableForeignKeyIndexes = enable;
        return this;
    }

    /// <summary>
    /// Enables or disables automatic indexing of soft delete columns.
    /// </summary>
    public AutoIndexOptionsBuilder EnableSoftDeleteIndexes(bool enable = true)
    {
        _options.EnableSoftDeleteIndexes = enable;
        return this;
    }

    /// <summary>
    /// Enables or disables automatic indexing of enum/state columns.
    /// </summary>
    public AutoIndexOptionsBuilder EnableEnumIndexes(bool enable = true)
    {
        _options.EnableEnumIndexes = enable;
        return this;
    }

    /// <summary>
    /// Enables or disables automatic indexing of tenant ID columns.
    /// </summary>
    public AutoIndexOptionsBuilder EnableTenantIndexes(bool enable = true)
    {
        _options.EnableTenantIndexes = enable;
        return this;
    }

    /// <summary>
    /// Enables or disables composite indexes for common patterns.
    /// </summary>
    public AutoIndexOptionsBuilder EnableCompositeIndexes(bool enable = true)
    {
        _options.EnableCompositeIndexes = enable;
        return this;
    }

    /// <summary>
    /// Enables or disables diagnostic logging of index decisions.
    /// </summary>
    public AutoIndexOptionsBuilder EnableDiagnostics(bool enable = true)
    {
        _options.EnableDiagnostics = enable;
        return this;
    }

    /// <summary>
    /// Sets the maximum string length for properties that can be indexed.
    /// </summary>
    public AutoIndexOptionsBuilder WithMaxIndexableStringLength(int maxLength)
    {
        _options.MaxIndexableStringLength = maxLength;
        return this;
    }

    /// <summary>
    /// Sets custom property name patterns for soft delete detection.
    /// </summary>
    public AutoIndexOptionsBuilder WithSoftDeletePatterns(params string[] patterns)
    {
        _options.SoftDeletePropertyPatterns = patterns;
        return this;
    }

    /// <summary>
    /// Sets custom property name patterns for tenant ID detection.
    /// </summary>
    public AutoIndexOptionsBuilder WithTenantIdPatterns(params string[] patterns)
    {
        _options.TenantIdPropertyPatterns = patterns;
        return this;
    }

    /// <summary>
    /// Sets custom property name patterns to exclude from automatic indexing.
    /// </summary>
    public AutoIndexOptionsBuilder WithExcludedPatterns(params string[] patterns)
    {
        _options.ExcludedPropertyPatterns = patterns;
        return this;
    }

    /// <summary>
    /// Builds the configured options.
    /// </summary>
    internal AutoIndexOptions Build() => _options;
}
