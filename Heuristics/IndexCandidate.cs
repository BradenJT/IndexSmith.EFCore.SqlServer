using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace IndexSmith.EFCore.SqlServer.Heuristics;

/// <summary>
/// Represents a candidate index to be evaluated for creation.
/// </summary>
public sealed class IndexCandidate
{
    /// <summary>
    /// The entity type this index belongs to.
    /// </summary>
    public required IMutableEntityType EntityType { get; init; }

    /// <summary>
    /// The properties included in this index.
    /// </summary>
    public required IReadOnlyList<IMutableProperty> Properties { get; init; }

    /// <summary>
    /// The calculated score for this index candidate.
    /// </summary>
    public IndexScore Score { get; set; } = new();

    /// <summary>
    /// Whether this is a composite index (multiple properties).
    /// </summary>
    public bool IsComposite => Properties.Count > 1;

    /// <summary>
    /// Whether this index should be unique.
    /// </summary>
    public bool IsUnique { get; init; }

    /// <summary>
    /// Custom index name, if specified.
    /// </summary>
    public string? CustomName { get; init; }

    /// <summary>
    /// The source that suggested this index candidate.
    /// </summary>
    public IndexCandidateSource Source { get; init; }

    /// <summary>
    /// Gets the generated index name based on table and column names.
    /// </summary>
    public string GetIndexName()
    {
        if (!string.IsNullOrEmpty(CustomName))
        {
            return CustomName;
        }

        var tableName = EntityType.GetTableName() ?? EntityType.Name;
        var columnNames = string.Join("_", Properties.Select(p => p.GetColumnName()));
        return $"IX_{tableName}_{columnNames}";
    }
}

/// <summary>
/// Indicates the source that suggested an index candidate.
/// </summary>
public enum IndexCandidateSource
{
    /// <summary>Index suggested by a heuristic rule.</summary>
    Heuristic,

    /// <summary>Index explicitly requested via [AutoIndex] attribute.</summary>
    ExplicitAttribute,

    /// <summary>Composite index requested via [CompositeAutoIndex] attribute.</summary>
    CompositeAttribute,

    /// <summary>Index for a foreign key relationship.</summary>
    ForeignKey,

    /// <summary>Index for a soft delete column.</summary>
    SoftDelete,

    /// <summary>Index for an enum/state column.</summary>
    EnumState,

    /// <summary>Index for a tenant ID column.</summary>
    TenantId
}
