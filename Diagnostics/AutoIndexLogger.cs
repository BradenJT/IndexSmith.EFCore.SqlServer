using IndexSmith.EFCore.SqlServer.Heuristics;
using IndexSmith.EFCore.SqlServer.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IndexSmith.EFCore.SqlServer.Diagnostics;

/// <summary>
/// Logger for IndexSmith indexing decisions.
/// </summary>
public sealed class AutoIndexLogger
{
    private readonly ILogger? _logger;
    private readonly AutoIndexOptions _options;
    private readonly List<AutoIndexDecision> _decisions = [];

    public AutoIndexLogger(AutoIndexOptions options, ILogger? logger = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger;
    }

    /// <summary>
    /// All decisions made during the current analysis.
    /// </summary>
    public IReadOnlyList<AutoIndexDecision> Decisions => _decisions;

    /// <summary>
    /// Logs a decision about whether to create an index.
    /// </summary>
    public void LogDecision(IndexCandidate candidate, bool wasCreated)
    {
        var decision = new AutoIndexDecision
        {
            EntityName = candidate.EntityType.Name,
            TableName = candidate.EntityType.GetTableName() ?? candidate.EntityType.Name,
            PropertyNames = candidate.Properties.Select(p => p.Name).ToList(),
            ColumnNames = candidate.Properties.Select(p => p.GetColumnName() ?? p.Name).ToList(),
            IndexName = candidate.GetIndexName(),
            WasCreated = wasCreated,
            Score = candidate.Score,
            Threshold = _options.ScoreThreshold,
            Source = candidate.Source
        };

        _decisions.Add(decision);

        if (_options.EnableDiagnostics && _logger != null)
        {
            if (wasCreated)
            {
                _logger.LogInformation("{Decision}", decision.ToString());
            }
            else
            {
                _logger.LogDebug("{Decision}", decision.ToString());
            }
        }
    }

    /// <summary>
    /// Logs an error that occurred during indexing.
    /// </summary>
    public void LogError(string message, Exception? exception = null)
    {
        if (_options.EnableDiagnostics && _logger != null)
        {
            _logger.LogError(exception, "[IndexSmith] Error: {Message}", message);
        }
    }

    /// <summary>
    /// Gets a summary of all decisions.
    /// </summary>
    public string GetSummary()
    {
        var created = _decisions.Count(d => d.WasCreated);
        var skipped = _decisions.Count(d => !d.WasCreated);

        return $"[IndexSmith] Summary: {created} indexes created, {skipped} candidates skipped";
    }
}
