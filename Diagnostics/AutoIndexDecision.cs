using IndexSmith.EFCore.SqlServer.Heuristics;

namespace IndexSmith.EFCore.SqlServer.Diagnostics;

/// <summary>
/// Represents a decision made about whether to create an index.
/// </summary>
public sealed class AutoIndexDecision
{
    /// <summary>
    /// The entity type name.
    /// </summary>
    public required string EntityName { get; init; }

    /// <summary>
    /// The table name in the database.
    /// </summary>
    public required string TableName { get; init; }

    /// <summary>
    /// The properties included in the index.
    /// </summary>
    public required IReadOnlyList<string> PropertyNames { get; init; }

    /// <summary>
    /// The column names in the database.
    /// </summary>
    public required IReadOnlyList<string> ColumnNames { get; init; }

    /// <summary>
    /// The generated index name.
    /// </summary>
    public required string IndexName { get; init; }

    /// <summary>
    /// Whether the index was created.
    /// </summary>
    public required bool WasCreated { get; init; }

    /// <summary>
    /// The score for this index candidate.
    /// </summary>
    public required IndexScore Score { get; init; }

    /// <summary>
    /// The score threshold that was applied.
    /// </summary>
    public required int Threshold { get; init; }

    /// <summary>
    /// The source that suggested this index.
    /// </summary>
    public required IndexCandidateSource Source { get; init; }

    /// <summary>
    /// The reason the index was created or skipped.
    /// </summary>
    public string Reason => WasCreated
        ? $"Score {Score.TotalScore} >= threshold {Threshold}"
        : $"Score {Score.TotalScore} < threshold {Threshold}";

    /// <summary>
    /// Returns a diagnostic string representation.
    /// </summary>
    public override string ToString()
    {
        var columns = string.Join(", ", ColumnNames);
        var status = WasCreated ? "CREATED" : "SKIPPED";
        return $"[IndexSmith] {TableName}({columns}) - {status}\n  {Score}\n  Source: {Source}";
    }
}
