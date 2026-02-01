namespace IndexSmith.EFCore.SqlServer.Attributes;

/// <summary>
/// Forces an index to be created on the decorated property,
/// regardless of heuristic scoring.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class AutoIndexAttribute : Attribute
{
    /// <summary>
    /// Optional custom name for the index. If not specified,
    /// a name will be generated automatically.
    /// </summary>
    public string? IndexName { get; set; }

    /// <summary>
    /// When true, creates a unique index. Default is false.
    /// </summary>
    public bool IsUnique { get; set; }
}
