using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Testing;

/// <summary>
/// Options for running a test suite.
/// </summary>
public class TestSuiteOptions
{
    /// <summary>
    /// Whether to run tests in parallel.
    /// </summary>
    public bool RunInParallel { get; set; } = true;

    /// <summary>
    /// Maximum number of parallel tests.
    /// </summary>
    public int MaxParallelism { get; set; } = 4;

    /// <summary>
    /// Whether to skip dynamic verification tests.
    /// </summary>
    public bool SkipDynamicTests { get; set; } = true; // Default to true for automation

    /// <summary>
    /// Whether to skip stability tests.
    /// </summary>
    public bool SkipStabilityTests { get; set; } = false;

    /// <summary>
    /// Number of stability samples.
    /// </summary>
    public int StabilitySampleCount { get; set; } = 5;

    /// <summary>
    /// Delay between stability samples in milliseconds.
    /// </summary>
    public int StabilitySampleDelayMs { get; set; } = 200;

    /// <summary>
    /// Whether to verify pointer chains.
    /// </summary>
    public bool VerifyPointerChains { get; set; } = true;

    /// <summary>
    /// Target game version.
    /// </summary>
    public string? TargetGameVersion { get; set; }

    /// <summary>
    /// Minimum confidence threshold.
    /// </summary>
    public double MinimumConfidenceThreshold { get; set; } = 0.7;

    /// <summary>
    /// User interaction provider.
    /// </summary>
    public IUserInteractionProvider? UserInteraction { get; set; }

    /// <summary>
    /// Progress reporter.
    /// </summary>
    public IProgress<TestProgress>? ProgressReporter { get; set; }
}

/// <summary>
/// Result of a complete test suite.
/// </summary>
public class TestSuiteResult
{
    /// <summary>
    /// Individual signature test results.
    /// </summary>
    public List<SignatureTestResult> Results { get; set; } = new();

    /// <summary>
    /// Total number of signatures tested.
    /// </summary>
    public int TotalSignatures { get; set; }

    /// <summary>
    /// Number of signatures that passed.
    /// </summary>
    public int PassedCount => Results.Count(r => r.OverallPassed);

    /// <summary>
    /// Number of signatures that failed.
    /// </summary>
    public int FailedCount => Results.Count(r => !r.OverallPassed);

    /// <summary>
    /// Overall success rate.
    /// </summary>
    public double SuccessRate => TotalSignatures > 0 ? (double)PassedCount / TotalSignatures : 0;

    /// <summary>
    /// When the suite started.
    /// </summary>
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// When the suite completed.
    /// </summary>
    public DateTime CompletedAt { get; set; }

    /// <summary>
    /// Total duration.
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Process ID that was tested.
    /// </summary>
    public int ProcessId { get; set; }
}

/// <summary>
/// Result of testing a single signature.
/// </summary>
public class SignatureTestResult
{
    /// <summary>
    /// Unique identifier for the signature.
    /// </summary>
    public string SignatureId { get; set; } = "";

    /// <summary>
    /// Name of the signature.
    /// </summary>
    public string SignatureName { get; set; } = "";

    /// <summary>
    /// Game title.
    /// </summary>
    public string GameTitle { get; set; } = "";

    /// <summary>
    /// Whether all tests passed.
    /// </summary>
    public bool OverallPassed { get; set; }

    /// <summary>
    /// Individual test results.
    /// </summary>
    public List<IndividualTestResult> Tests { get; set; } = new();

    /// <summary>
    /// Full verification result.
    /// </summary>
    public VerificationResult? VerificationResult { get; set; }

    /// <summary>
    /// Confidence score.
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    /// Health score.
    /// </summary>
    public int HealthScore { get; set; }

    /// <summary>
    /// Health rating.
    /// </summary>
    public HealthRating HealthRating { get; set; }

    /// <summary>
    /// Failure reason if OverallPassed is false.
    /// </summary>
    public string? FailureReason { get; set; }

    /// <summary>
    /// When the test was run.
    /// </summary>
    public DateTime TestedAt { get; set; }

    /// <summary>
    /// Duration of the test.
    /// </summary>
    public TimeSpan Duration { get; set; }
}

/// <summary>
/// Individual test result.
/// </summary>
public class IndividualTestResult
{
    /// <summary>
    /// Type of test.
    /// </summary>
    public TestType TestType { get; set; }

    /// <summary>
    /// Whether the test passed.
    /// </summary>
    public bool Passed { get; set; }

    /// <summary>
    /// Test message.
    /// </summary>
    public string Message { get; set; } = "";

    /// <summary>
    /// Test duration.
    /// </summary>
    public TimeSpan Duration { get; set; }
}

/// <summary>
/// Types of tests.
/// </summary>
public enum TestType
{
    Static,
    Dynamic,
    Stability,
    PointerChain,
    PatternQuality,
    ValueRange,
    MemoryAccess
}

/// <summary>
/// Test progress information.
/// </summary>
public class TestProgress
{
    /// <summary>
    /// Number of completed tests.
    /// </summary>
    public int CompletedCount { get; set; }

    /// <summary>
    /// Total number of tests.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Name of current signature being tested.
    /// </summary>
    public string CurrentSignature { get; set; } = "";

    /// <summary>
    /// Status of current test.
    /// </summary>
    public TestStatus Status { get; set; }

    /// <summary>
    /// Progress percentage.
    /// </summary>
    public double PercentComplete => TotalCount > 0 ? (double)CompletedCount / TotalCount * 100 : 0;
}

/// <summary>
/// Test status.
/// </summary>
public enum TestStatus
{
    Running,
    Passed,
    Failed,
    Skipped
}

/// <summary>
/// Export formats.
/// </summary>
public enum ExportFormat
{
    Json,
    Csv,
    Markdown
}

/// <summary>
/// Result of a regression test.
/// </summary>
public class RegressionTestResult
{
    /// <summary>
    /// Baseline results.
    /// </summary>
    public TestSuiteResult? BaselineResults { get; set; }

    /// <summary>
    /// Current results.
    /// </summary>
    public TestSuiteResult? CurrentResults { get; set; }

    /// <summary>
    /// Signatures that were fixed (failed before, pass now).
    /// </summary>
    public List<SignatureTestResult> FixedSignatures { get; set; } = new();

    /// <summary>
    /// Signatures that regressed (passed before, fail now).
    /// </summary>
    public List<RegressionDetails> RegressedSignatures { get; set; } = new();

    /// <summary>
    /// Signatures with degraded confidence.
    /// </summary>
    public List<RegressionDetails> DegradedSignatures { get; set; } = new();

    /// <summary>
    /// Stable signatures (passed both times).
    /// </summary>
    public List<SignatureTestResult> StableSignatures { get; set; } = new();

    /// <summary>
    /// New signatures not in baseline.
    /// </summary>
    public List<SignatureTestResult> NewSignatures { get; set; } = new();

    /// <summary>
    /// Signatures removed since baseline.
    /// </summary>
    public List<SignatureTestResult> RemovedSignatures { get; set; } = new();

    /// <summary>
    /// When the test started.
    /// </summary>
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// When the test completed.
    /// </summary>
    public DateTime CompletedAt { get; set; }

    /// <summary>
    /// Test duration.
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Whether any regressions were found.
    /// </summary>
    public bool HasRegressions => RegressedSignatures.Any() || DegradedSignatures.Any();
}

/// <summary>
/// Details of a regression.
/// </summary>
public class RegressionDetails
{
    /// <summary>
    /// The signature test result.
    /// </summary>
    public SignatureTestResult? SignatureTest { get; set; }

    /// <summary>
    /// Whether the baseline test passed.
    /// </summary>
    public bool BaselinePassed { get; set; }

    /// <summary>
    /// Whether the current test passed.
    /// </summary>
    public bool CurrentPassed { get; set; }

    /// <summary>
    /// Baseline confidence.
    /// </summary>
    public double BaselineConfidence { get; set; }

    /// <summary>
    /// Current confidence.
    /// </summary>
    public double CurrentConfidence { get; set; }

    /// <summary>
    /// Confidence change.
    /// </summary>
    public double ConfidenceChange => CurrentConfidence - BaselineConfidence;
}

/// <summary>
/// Test summary report.
/// </summary>
public class TestSummaryReport
{
    /// <summary>
    /// When the report was generated.
    /// </summary>
    public DateTime GeneratedAt { get; set; }

    /// <summary>
    /// Total signatures.
    /// </summary>
    public int TotalSignatures { get; set; }

    /// <summary>
    /// Number passed.
    /// </summary>
    public int PassedCount { get; set; }

    /// <summary>
    /// Number failed.
    /// </summary>
    public int FailedCount { get; set; }

    /// <summary>
    /// Success rate.
    /// </summary>
    public double SuccessRate { get; set; }

    /// <summary>
    /// Duration.
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Breakdown by health rating.
    /// </summary>
    public Dictionary<HealthRating, HealthRatingSummary> ByHealthRating { get; set; } = new();

    /// <summary>
    /// Breakdown by game.
    /// </summary>
    public Dictionary<string, GameSummary> ByGame { get; set; } = new();

    /// <summary>
    /// Critical issues.
    /// </summary>
    public List<CriticalIssue> CriticalIssues { get; set; } = new();
}

/// <summary>
/// Summary for a health rating.
/// </summary>
public class HealthRatingSummary
{
    /// <summary>
    /// Number of signatures.
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// Signature names.
    /// </summary>
    public List<string> Signatures { get; set; } = new();

    /// <summary>
    /// Average confidence.
    /// </summary>
    public double AverageConfidence { get; set; }
}

/// <summary>
/// Summary for a game.
/// </summary>
public class GameSummary
{
    /// <summary>
    /// Total count.
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// Number passed.
    /// </summary>
    public int Passed { get; set; }

    /// <summary>
    /// Number failed.
    /// </summary>
    public int Failed { get; set; }

    /// <summary>
    /// Average health score.
    /// </summary>
    public double AverageHealthScore { get; set; }
}

/// <summary>
/// Critical issue details.
/// </summary>
public class CriticalIssue
{
    /// <summary>
    /// Signature name.
    /// </summary>
    public string SignatureName { get; set; } = "";

    /// <summary>
    /// Game title.
    /// </summary>
    public string GameTitle { get; set; } = "";

    /// <summary>
    /// Issue description.
    /// </summary>
    public string Issue { get; set; } = "";

    /// <summary>
    /// Health score.
    /// </summary>
    public int HealthScore { get; set; }
}
