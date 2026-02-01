using IndexSmith.EFCore.SqlServer.Diagnostics;
using IndexSmith.EFCore.SqlServer.Heuristics;
using IndexSmith.EFCore.SqlServer.Options;
using IndexSmith.EFCore.SqlServer.Provider;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.Extensions.Logging;

namespace IndexSmith.EFCore.SqlServer.Conventions;

/// <summary>
/// EF Core convention that applies automatic indexes during model finalization.
/// This runs after all other model configuration is complete.
/// </summary>
public sealed class AutoIndexModelFinalizingConvention : IModelFinalizingConvention
{
    private readonly AutoIndexOptions _options;
    private readonly ILogger? _logger;

    public AutoIndexModelFinalizingConvention(AutoIndexOptions options, ILogger? logger = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger;
    }

    public void ProcessModelFinalizing(
        IConventionModelBuilder modelBuilder,
        IConventionContext<IConventionModelBuilder> context)
    {
        var engine = new IndexHeuristicEngine(_options);
        var diagnosticLogger = new AutoIndexLogger(_options, _logger);

        foreach (var entityType in modelBuilder.Metadata.GetEntityTypes())
        {
            // Skip owned types and query types
            if (entityType.IsOwned() || entityType.FindPrimaryKey() == null)
            {
                continue;
            }

            ProcessEntityType(entityType, engine, diagnosticLogger);
        }

        if (_options.EnableDiagnostics && _logger != null)
        {
            _logger.LogInformation("{Summary}", diagnosticLogger.GetSummary());
        }
    }

    private void ProcessEntityType(
        IConventionEntityType entityType,
        IndexHeuristicEngine engine,
        AutoIndexLogger logger)
    {
        // Get the mutable version for analysis
        var mutableEntityType = entityType as IMutableEntityType;
        if (mutableEntityType == null)
        {
            return;
        }

        try
        {
            var candidates = engine.AnalyzeEntity(mutableEntityType);

            foreach (var candidate in candidates)
            {
                var created = TryCreateIndex(entityType, candidate, logger);
                logger.LogDecision(candidate, created);
            }
        }
        catch (Exception ex)
        {
            logger.LogError($"Error processing entity {entityType.Name}", ex);
        }
    }

    private bool TryCreateIndex(
        IConventionEntityType entityType,
        IndexCandidate candidate,
        AutoIndexLogger logger)
    {
        // Validate the index can be created in SQL Server
        var columns = candidate.Properties
            .Select(p => (p.ClrType, p.GetMaxLength()))
            .ToList();

        var (isValid, error) = SqlServerIndexCapabilities.ValidateIndexKey(columns);
        if (!isValid)
        {
            if (_options.EnableDiagnostics && _logger != null)
            {
                _logger.LogWarning(
                    "[IndexSmith] Skipping index {IndexName}: {Error}",
                    candidate.GetIndexName(),
                    error);
            }
            return false;
        }

        // Check if index already exists on these properties
        var existingIndex = entityType.GetIndexes()
            .FirstOrDefault(idx =>
                idx.Properties.Select(p => p.Name).SequenceEqual(
                    candidate.Properties.Select(p => p.Name)));

        if (existingIndex != null)
        {
            return false;
        }

        // Get the convention properties
        var conventionProperties = candidate.Properties
            .Select(p => entityType.FindProperty(p.Name))
            .Where(p => p != null)
            .Cast<IConventionProperty>()
            .ToList();

        if (conventionProperties.Count != candidate.Properties.Count)
        {
            return false;
        }

        // Create the index
        var builder = entityType.Builder;
        var indexBuilder = builder.HasIndex(conventionProperties, fromDataAnnotation: false);

        if (indexBuilder == null)
        {
            return false;
        }

        // Set index name via annotation
        var index = indexBuilder.Metadata;
        index.SetDatabaseName(candidate.GetIndexName());

        // Set uniqueness if specified
        if (candidate.IsUnique)
        {
            indexBuilder.IsUnique(true, fromDataAnnotation: false);
        }

        return true;
    }
}
