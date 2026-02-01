namespace IndexSmith.EFCore.SqlServer.Attributes;

/// <summary>
/// Forces creation of a composite index on the specified properties.
/// Apply to the entity class.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public sealed class CompositeAutoIndexAttribute : Attribute
{
    /// <summary>
    /// The property names to include in the composite index, in order.
    /// </summary>
    public string[] PropertyNames { get; }

    /// <summary>
    /// Optional custom name for the index. If not specified,
    /// a name will be generated automatically.
    /// </summary>
    public string? IndexName { get; set; }

    /// <summary>
    /// When true, creates a unique composite index. Default is false.
    /// </summary>
    public bool IsUnique { get; set; }

    /// <summary>
    /// Creates a composite index on the specified properties.
    /// </summary>
    /// <param name="propertyNames">The property names to include in the index, in order.</param>
    public CompositeAutoIndexAttribute(params string[] propertyNames)
    {
        if (propertyNames == null || propertyNames.Length < 2)
        {
            throw new ArgumentException("Composite index requires at least two properties.", nameof(propertyNames));
        }

        PropertyNames = propertyNames;
    }
}
