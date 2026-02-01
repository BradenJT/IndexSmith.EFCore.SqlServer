namespace IndexSmith.EFCore.SqlServer.Heuristics;

/// <summary>
/// Represents the calculated score for an index candidate,
/// including contributing rules and their individual scores.
/// </summary>
public sealed class IndexScore
{
    private readonly List<ScoreContribution> _contributions = [];

    /// <summary>
    /// The total score for this index candidate.
    /// </summary>
    public int TotalScore => _contributions.Sum(c => c.Points);

    /// <summary>
    /// Individual contributions to the score from each rule.
    /// </summary>
    public IReadOnlyList<ScoreContribution> Contributions => _contributions;

    /// <summary>
    /// Adds a score contribution from a rule.
    /// </summary>
    public void AddContribution(string ruleName, int points, string? reason = null)
    {
        _contributions.Add(new ScoreContribution(ruleName, points, reason));
    }

    /// <summary>
    /// Returns true if the total score meets or exceeds the threshold.
    /// </summary>
    public bool MeetsThreshold(int threshold) => TotalScore >= threshold;

    /// <summary>
    /// Returns a diagnostic string representation of the score.
    /// </summary>
    public override string ToString()
    {
        var contributions = string.Join(", ", _contributions.Select(c => $"{c.RuleName}({c.Points:+#;-#;0})"));
        return $"Score: {TotalScore} | Rules: {contributions}";
    }
}

/// <summary>
/// Represents a single contribution to an index score.
/// </summary>
public sealed record ScoreContribution(string RuleName, int Points, string? Reason = null);
