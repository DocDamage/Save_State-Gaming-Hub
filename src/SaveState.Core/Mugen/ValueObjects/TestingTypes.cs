namespace SaveState.Core.Mugen.ValueObjects;

/// <summary>
/// Comprehensive move test analysis.
/// </summary>
public sealed record MoveTestAnalysis(
    string MoveName,
    IReadOnlyList<string> Strengths,
    IReadOnlyList<string> Weaknesses,
    IReadOnlyList<string> OptimalUsage,
    IReadOnlyList<string> CounterPlay,
    int Rating,
    DifficultyLevel Difficulty);

/// <summary>
/// Represents frame data for a move.
/// Contains timing information for startup, active, and recovery frames.
/// </summary>
public sealed record FrameData(
    int StartupFrames,
    int ActiveFrames,
    int RecoveryFrames,
    int FrameAdvantageOnHit,
    int FrameAdvantageOnBlock,
    int TotalFrames)
{
    /// <summary>
    /// Gets whether this move is plus on block.
    /// </summary>
    public bool IsPlusOnBlock => FrameAdvantageOnBlock > 0;

    /// <summary>
    /// Gets whether this move is plus on hit.
    /// </summary>
    public bool IsPlusOnHit => FrameAdvantageOnHit > 0;

    /// <summary>
    /// Gets whether this move is safe on block (0 or positive frames).
    /// </summary>
    public bool IsSafeOnBlock => FrameAdvantageOnBlock >= 0;
}

/// <summary>
/// Represents a comparison between two moves for balance analysis.
/// </summary>
public sealed record MoveComparison(
    string MoveAName,
    string MoveBName,
    int DamageDifference,
    int StartupDifference,
    int RecoveryDifference,
    double BalanceScore,
    IReadOnlyList<string> Differences)
{
    /// <summary>
    /// Gets whether the moves are similarly balanced.
    /// </summary>
    public bool AreSimilar => Math.Abs(BalanceScore) < 10.0;
}

/// <summary>
/// Represents the result of testing a character.
/// </summary>
public sealed record CharacterTestResult(
    Guid CharacterId,
    string CharacterName,
    int TotalMatches,
    int Wins,
    int Losses,
    double WinRate,
    IReadOnlyList<MoveTestResult> MoveResults,
    DateTimeOffset TestedAt)
{
    /// <summary>
    /// Gets whether the character passed the test (>50% win rate).
    /// </summary>
    public bool Passed => WinRate > 0.5;
}

/// <summary>
/// Represents the result of testing a move.
/// </summary>
public sealed record MoveTestResult(
    string MoveName,
    int TimesUsed,
    int TimesHit,
    int TimesMissed,
    int TimesBlocked,
    double HitRate,
    double SuccessRate,
    int AverageDamage,
    bool TestPassed,
    IReadOnlyList<TestRoundResult> RoundResults,
    IReadOnlyList<string> Issues,
    IReadOnlyList<string> Recommendations)
{
    /// <summary>
    /// Gets whether the move is effective (>60% hit rate).
    /// </summary>
    public bool IsEffective => HitRate > 0.6;
}

/// <summary>
/// Represents the result of balance testing.
/// </summary>
public sealed record BalanceTestResult(
    string MoveName,
    bool IsBalanced,
    double BalanceScore,
    IReadOnlyList<string> Issues,
    IReadOnlyList<string> Recommendations)
{
    /// <summary>
    /// Gets whether the move needs rebalancing.
    /// </summary>
    public bool NeedsRebalancing => !IsBalanced || BalanceScore < 40.0 || BalanceScore > 60.0;
}

/// <summary>
/// Represents the result of move performance simulation.
/// </summary>
public sealed record MoveSimulationResult(
    string MoveName,
    int ScenariosRun,
    int SuccessfulScenarios,
    double SuccessRate,
    IReadOnlyDictionary<string, double> ScenarioResults,
    IReadOnlyList<string> Observations)
{
    /// <summary>
    /// Gets whether the simulation was successful overall.
    /// </summary>
    public bool WasSuccessful => SuccessRate > 0.7;
}

/// <summary>
/// Represents a comparison between two test results.
/// </summary>
public sealed record TestComparison(
    string MoveId,
    int VersionA,
    int VersionB,
    double PerformanceDelta,
    IReadOnlyList<string> Improvements,
    IReadOnlyList<string> Regressions,
    string Recommendation)
{
    /// <summary>
    /// Gets whether version B is better than version A.
    /// </summary>
    public bool IsBetterVersion => PerformanceDelta > 0;
}

/// <summary>
/// Represents balance analysis for a move.
/// </summary>
public sealed record MoveBalanceAnalysis(
    string MoveName,
    double BalanceScore,
    IReadOnlyList<string> Strengths,
    IReadOnlyList<string> Weaknesses,
    IReadOnlyList<string> Recommendations)
{
    /// <summary>
    /// Gets whether the move is well-balanced.
    /// </summary>
    public bool IsBalanced => BalanceScore >= 40.0 && BalanceScore <= 60.0;
}

/// <summary>
/// Parameters for move testing.
/// </summary>
public sealed record TestParameters(
    string OpponentCharacter,
    int TestRounds,
    bool UseAi,
    TestDifficulty Difficulty,
    IReadOnlyList<string> TestScenarios);

/// <summary>
/// Difficulty for move testing.
/// </summary>
public enum TestDifficulty
{
    VeryEasy,
    Easy,
    Medium,
    Hard,
    VeryHard
}

/// <summary>
/// Result of a single test round.
/// </summary>
public sealed record TestRoundResult(
    int RoundNumber,
    bool Won,
    int DamageDealt,
    int DamageReceived,
    int HitsLanded,
    int HitsBlocked,
    TimeSpan Duration,
    IReadOnlyList<string> Events);
