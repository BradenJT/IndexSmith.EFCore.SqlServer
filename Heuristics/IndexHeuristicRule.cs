using IndexSmith.EFCore.SqlServer.Options;
using Microsoft.EntityFrameworkCore.Metadata;

namespace IndexSmith.EFCore.SqlServer.Heuristics;

/// <summary>
/// Base class for heuristic rules that evaluate properties for indexing.
/// </summary>
public abstract class IndexHeuristicRule
{
    /// <summary>
    /// The name of this rule for diagnostic purposes.
    /// </summary>
    public abstract string RuleName { get; }

    /// <summary>
    /// Evaluates whether this rule applies to the given property
    /// and returns a score contribution if it does.
    /// </summary>
    /// <param name="property">The property to evaluate.</param>
    /// <param name="entityType">The entity type containing the property.</param>
    /// <param name="options">The current configuration options.</param>
    /// <returns>A score contribution, or null if the rule doesn't apply.</returns>
    public abstract ScoreContribution? Evaluate(
        IMutableProperty property,
        IMutableEntityType entityType,
        AutoIndexOptions options);

    /// <summary>
    /// Identifies composite index candidates based on entity-level analysis.
    /// Override this method to suggest composite indexes.
    /// </summary>
    /// <param name="entityType">The entity type to analyze.</param>
    /// <param name="options">The current configuration options.</param>
    /// <returns>A collection of composite index candidates.</returns>
    public virtual IEnumerable<IndexCandidate> IdentifyCompositeIndexes(
        IMutableEntityType entityType,
        AutoIndexOptions options)
    {
        return [];
    }
}

/// <summary>
/// Rule that scores foreign key properties for indexing.
/// </summary>
public sealed class ForeignKeyHeuristicRule : IndexHeuristicRule
{
    public override string RuleName => "ForeignKey";

    public override ScoreContribution? Evaluate(
        IMutableProperty property,
        IMutableEntityType entityType,
        AutoIndexOptions options)
    {
        if (!options.EnableForeignKeyIndexes)
            return null;

        var isForeignKey = entityType.GetForeignKeys()
            .Any(fk => fk.Properties.Contains(property));

        if (isForeignKey)
        {
            return new ScoreContribution(RuleName, 40, "Property is a foreign key");
        }

        return null;
    }
}

/// <summary>
/// Rule that scores soft delete properties for indexing.
/// </summary>
public sealed class SoftDeleteHeuristicRule : IndexHeuristicRule
{
    public override string RuleName => "SoftDelete";

    public override ScoreContribution? Evaluate(
        IMutableProperty property,
        IMutableEntityType entityType,
        AutoIndexOptions options)
    {
        if (!options.EnableSoftDeleteIndexes)
            return null;

        var isSoftDelete = options.SoftDeletePropertyPatterns
            .Any(pattern => property.Name.Equals(pattern, StringComparison.OrdinalIgnoreCase));

        if (isSoftDelete)
        {
            return new ScoreContribution(RuleName, 30, "Property is a soft delete indicator");
        }

        return null;
    }
}

/// <summary>
/// Rule that scores enum/state properties for indexing.
/// </summary>
public sealed class EnumStateHeuristicRule : IndexHeuristicRule
{
    public override string RuleName => "EnumState";

    public override ScoreContribution? Evaluate(
        IMutableProperty property,
        IMutableEntityType entityType,
        AutoIndexOptions options)
    {
        if (!options.EnableEnumIndexes)
            return null;

        var clrType = property.ClrType;
        var isEnum = clrType.IsEnum ||
                     (Nullable.GetUnderlyingType(clrType)?.IsEnum ?? false);

        // Also check for common state property naming patterns
        var isStateProperty = property.Name.EndsWith("Status", StringComparison.OrdinalIgnoreCase) ||
                              property.Name.EndsWith("State", StringComparison.OrdinalIgnoreCase) ||
                              property.Name.EndsWith("Type", StringComparison.OrdinalIgnoreCase);

        if (isEnum || isStateProperty)
        {
            return new ScoreContribution(RuleName, 25, isEnum ? "Property is an enum" : "Property appears to be a state indicator");
        }

        return null;
    }
}

/// <summary>
/// Rule that scores tenant ID properties for indexing.
/// </summary>
public sealed class TenantIdHeuristicRule : IndexHeuristicRule
{
    public override string RuleName => "TenantId";

    public override ScoreContribution? Evaluate(
        IMutableProperty property,
        IMutableEntityType entityType,
        AutoIndexOptions options)
    {
        if (!options.EnableTenantIndexes)
            return null;

        var isTenantId = options.TenantIdPropertyPatterns
            .Any(pattern => property.Name.Equals(pattern, StringComparison.OrdinalIgnoreCase));

        if (isTenantId)
        {
            return new ScoreContribution(RuleName, 40, "Property is a tenant identifier");
        }

        return null;
    }

    public override IEnumerable<IndexCandidate> IdentifyCompositeIndexes(
        IMutableEntityType entityType,
        AutoIndexOptions options)
    {
        if (!options.EnableCompositeIndexes || !options.EnableTenantIndexes)
            yield break;

        var tenantProperty = entityType.GetProperties()
            .FirstOrDefault(p => options.TenantIdPropertyPatterns
                .Any(pattern => p.Name.Equals(pattern, StringComparison.OrdinalIgnoreCase)));

        if (tenantProperty == null)
            yield break;

        var softDeleteProperty = entityType.GetProperties()
            .FirstOrDefault(p => options.SoftDeletePropertyPatterns
                .Any(pattern => p.Name.Equals(pattern, StringComparison.OrdinalIgnoreCase)));

        if (softDeleteProperty != null)
        {
            var score = new IndexScore();
            score.AddContribution("TenantId", 40);
            score.AddContribution("SoftDelete", 30);
            score.AddContribution("CompositeBonus", 10);

            yield return new IndexCandidate
            {
                EntityType = entityType,
                Properties = [tenantProperty, softDeleteProperty],
                Score = score,
                Source = IndexCandidateSource.Heuristic
            };
        }
    }
}

/// <summary>
/// Rule that penalizes properties that should not be indexed.
/// </summary>
public sealed class ExclusionHeuristicRule : IndexHeuristicRule
{
    public override string RuleName => "Exclusion";

    public override ScoreContribution? Evaluate(
        IMutableProperty property,
        IMutableEntityType entityType,
        AutoIndexOptions options)
    {
        // Exclude audit columns
        var isExcluded = options.ExcludedPropertyPatterns
            .Any(pattern => property.Name.Equals(pattern, StringComparison.OrdinalIgnoreCase));

        if (isExcluded)
        {
            return new ScoreContribution(RuleName, -100, "Property matches exclusion pattern");
        }

        // Exclude nvarchar(max) or large strings
        var maxLength = property.GetMaxLength();
        if (property.ClrType == typeof(string) && (maxLength == null || maxLength > options.MaxIndexableStringLength))
        {
            return new ScoreContribution(RuleName, -100, "String property exceeds maximum indexable length");
        }

        return null;
    }
}
