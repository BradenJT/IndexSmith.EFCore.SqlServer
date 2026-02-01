namespace IndexSmith.EFCore.SqlServer.Options;

/// <summary>
/// Configuration options for IndexSmith automatic indexing behavior.
/// </summary>
public sealed class AutoIndexOptions
{
    /// <summary>
    /// Minimum score threshold for an index to be created automatically.
    /// Default is 50.
    /// </summary>
    public int ScoreThreshold { get; set; } = 50;

    /// <summary>
    /// Enable automatic indexing of foreign key properties.
    /// Default is true.
    /// </summary>
    public bool EnableForeignKeyIndexes { get; set; } = true;

    /// <summary>
    /// Enable automatic indexing of soft delete columns (e.g., IsDeleted, DeletedAt).
    /// Default is true.
    /// </summary>
    public bool EnableSoftDeleteIndexes { get; set; } = true;

    /// <summary>
    /// Enable automatic indexing of enum/state columns.
    /// Default is true.
    /// </summary>
    public bool EnableEnumIndexes { get; set; } = true;

    /// <summary>
    /// Enable automatic indexing of tenant ID columns.
    /// Default is true.
    /// </summary>
    public bool EnableTenantIndexes { get; set; } = true;

    /// <summary>
    /// Enable composite indexes for common patterns (e.g., TenantId + IsDeleted).
    /// Default is true.
    /// </summary>
    public bool EnableCompositeIndexes { get; set; } = true;

    /// <summary>
    /// Enable diagnostic logging of index decisions.
    /// Default is false.
    /// </summary>
    public bool EnableDiagnostics { get; set; } = false;

    /// <summary>
    /// Maximum string length for properties that can be indexed.
    /// Properties with max length greater than this will not be automatically indexed.
    /// Default is 256.
    /// </summary>
    public int MaxIndexableStringLength { get; set; } = 256;

    /// <summary>
    /// Property name patterns that indicate soft delete columns.
    /// </summary>
    public IReadOnlyList<string> SoftDeletePropertyPatterns { get; set; } =
    [
        "IsDeleted",
        "Deleted",
        "IsActive",
        "Active",
        "DeletedAt",
        "DeletedOn"
    ];

    /// <summary>
    /// Property name patterns that indicate tenant ID columns.
    /// </summary>
    public IReadOnlyList<string> TenantIdPropertyPatterns { get; set; } =
    [
        "TenantId",
        "OrganizationId",
        "CompanyId",
        "AccountId"
    ];

    /// <summary>
    /// Property name patterns that should be excluded from automatic indexing.
    /// </summary>
    public IReadOnlyList<string> ExcludedPropertyPatterns { get; set; } =
    [
        "CreatedAt",
        "CreatedOn",
        "UpdatedAt",
        "UpdatedOn",
        "ModifiedAt",
        "ModifiedOn",
        "CreatedBy",
        "UpdatedBy",
        "ModifiedBy"
    ];
}
