using System.Reflection;
using IndexSmith.EFCore.SqlServer.Attributes;
using IndexSmith.EFCore.SqlServer.Options;
using Microsoft.EntityFrameworkCore.Metadata;

namespace IndexSmith.EFCore.SqlServer.Heuristics;

/// <summary>
/// Engine that evaluates entity properties and generates index candidates
/// based on heuristic rules and configuration options.
/// </summary>
public sealed class IndexHeuristicEngine
{
    private readonly AutoIndexOptions _options;
    private readonly IReadOnlyList<IndexHeuristicRule> _rules;

    public IndexHeuristicEngine(AutoIndexOptions options)
        : this(options, GetDefaultRules())
    {
    }

    public IndexHeuristicEngine(AutoIndexOptions options, IReadOnlyList<IndexHeuristicRule> rules)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _rules = rules ?? throw new ArgumentNullException(nameof(rules));
    }

    /// <summary>
    /// Gets the default set of heuristic rules.
    /// </summary>
    public static IReadOnlyList<IndexHeuristicRule> GetDefaultRules() =>
    [
        new ExclusionHeuristicRule(),
        new ForeignKeyHeuristicRule(),
        new TenantIdHeuristicRule(),
        new SoftDeleteHeuristicRule(),
        new EnumStateHeuristicRule()
    ];

    /// <summary>
    /// Analyzes an entity type and returns all qualifying index candidates.
    /// </summary>
    public IEnumerable<IndexCandidate> AnalyzeEntity(IMutableEntityType entityType)
    {
        var candidates = new List<IndexCandidate>();

        // Process explicit [CompositeAutoIndex] attributes first
        candidates.AddRange(GetCompositeAttributeCandidates(entityType));

        // Process explicit [AutoIndex] attributes
        candidates.AddRange(GetExplicitAttributeCandidates(entityType));

        // Process heuristic-based single property candidates
        candidates.AddRange(GetHeuristicCandidates(entityType));

        // Process heuristic-based composite candidates
        candidates.AddRange(GetCompositeHeuristicCandidates(entityType));

        // Filter candidates that meet the threshold (explicit attributes always pass)
        return candidates.Where(c =>
            c.Source == IndexCandidateSource.ExplicitAttribute ||
            c.Source == IndexCandidateSource.CompositeAttribute ||
            c.Score.MeetsThreshold(_options.ScoreThreshold));
    }

    private IEnumerable<IndexCandidate> GetCompositeAttributeCandidates(IMutableEntityType entityType)
    {
        var clrType = entityType.ClrType;
        if (clrType == null)
            yield break;

        var compositeAttributes = clrType.GetCustomAttributes<CompositeAutoIndexAttribute>();
        foreach (var attr in compositeAttributes)
        {
            var properties = new List<IMutableProperty>();
            foreach (var propName in attr.PropertyNames)
            {
                var property = entityType.FindProperty(propName);
                if (property != null)
                {
                    properties.Add(property);
                }
            }

            if (properties.Count == attr.PropertyNames.Length)
            {
                var score = new IndexScore();
                score.AddContribution("ExplicitAttribute", 100, "Composite index explicitly requested");

                yield return new IndexCandidate
                {
                    EntityType = entityType,
                    Properties = properties,
                    Score = score,
                    IsUnique = attr.IsUnique,
                    CustomName = attr.IndexName,
                    Source = IndexCandidateSource.CompositeAttribute
                };
            }
        }
    }

    private IEnumerable<IndexCandidate> GetExplicitAttributeCandidates(IMutableEntityType entityType)
    {
        foreach (var property in entityType.GetProperties())
        {
            // Check for [NoAutoIndex] attribute
            var propertyInfo = property.PropertyInfo;
            if (propertyInfo?.GetCustomAttribute<NoAutoIndexAttribute>() != null)
            {
                continue;
            }

            // Check for [AutoIndex] attribute
            var autoIndexAttr = propertyInfo?.GetCustomAttribute<AutoIndexAttribute>();
            if (autoIndexAttr != null)
            {
                var score = new IndexScore();
                score.AddContribution("ExplicitAttribute", 100, "Index explicitly requested");

                yield return new IndexCandidate
                {
                    EntityType = entityType,
                    Properties = [property],
                    Score = score,
                    IsUnique = autoIndexAttr.IsUnique,
                    CustomName = autoIndexAttr.IndexName,
                    Source = IndexCandidateSource.ExplicitAttribute
                };
            }
        }
    }

    private IEnumerable<IndexCandidate> GetHeuristicCandidates(IMutableEntityType entityType)
    {
        foreach (var property in entityType.GetProperties())
        {
            // Skip if [NoAutoIndex] attribute is present
            var propertyInfo = property.PropertyInfo;
            if (propertyInfo?.GetCustomAttribute<NoAutoIndexAttribute>() != null)
            {
                continue;
            }

            // Skip if [AutoIndex] attribute is present (already handled)
            if (propertyInfo?.GetCustomAttribute<AutoIndexAttribute>() != null)
            {
                continue;
            }

            // Skip primary key properties (already indexed)
            if (property.IsPrimaryKey())
            {
                continue;
            }

            var score = new IndexScore();

            foreach (var rule in _rules)
            {
                var contribution = rule.Evaluate(property, entityType, _options);
                if (contribution != null)
                {
                    score.AddContribution(contribution.RuleName, contribution.Points, contribution.Reason);
                }
            }

            if (score.TotalScore > 0)
            {
                yield return new IndexCandidate
                {
                    EntityType = entityType,
                    Properties = [property],
                    Score = score,
                    Source = IndexCandidateSource.Heuristic
                };
            }
        }
    }

    private IEnumerable<IndexCandidate> GetCompositeHeuristicCandidates(IMutableEntityType entityType)
    {
        foreach (var rule in _rules)
        {
            foreach (var candidate in rule.IdentifyCompositeIndexes(entityType, _options))
            {
                yield return candidate;
            }
        }
    }
}
