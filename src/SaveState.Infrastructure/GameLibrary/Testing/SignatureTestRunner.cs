using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Testing;

/// <summary>
/// Runner for automated signature testing suites.
/// </summary>
public class SignatureTestRunner
{
    private readonly ISignatureVerificationService _verificationService;
    private readonly ILogger<SignatureTestRunner> _logger;
    private readonly ITimeProvider _timeProvider;

    public SignatureTestRunner(
        ISignatureVerificationService verificationService,
        ILogger<SignatureTestRunner> logger,
        ITimeProvider timeProvider)
    {
        _verificationService = verificationService;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Runs a complete test suite for multiple signatures.
    /// </summary>
    public async Task<TestSuiteResult> RunTestSuiteAsync(
        List<GameMemorySignature> signatures,
        int processId,
        TestSuiteOptions? options = null,
        CancellationToken ct = default)
    {
        options ??= new TestSuiteOptions();
        var stopwatch = Stopwatch.StartNew();
        var result = new TestSuiteResult
        {
            StartedAt = _timeProvider.UtcNow,
            ProcessId = processId,
            TotalSignatures = signatures.Count
        };

        _logger.LogInformation(
            "Starting test suite for {Count} signatures on process {ProcessId}",
            signatures.Count, processId);

        // Create test results collection
        var testResults = new ConcurrentBag<SignatureTestResult>();
        var completedCount = 0;

        if (options.RunInParallel)
        {
            // Run tests in parallel with throttling
            var semaphore = new SemaphoreSlim(options.MaxParallelism);
            var tasks = signatures.Select(async signature =>
            {
                await semaphore.WaitAsync(ct);
                try
                {
                    var testResult = await TestSignatureAsync(signature, processId, options, ct);
                    testResults.Add(testResult);

                    var currentCompleted = Interlocked.Increment(ref completedCount);
                    options.ProgressReporter?.Report(new TestProgress
                    {
                        CompletedCount = currentCompleted,
                        TotalCount = signatures.Count,
                        CurrentSignature = signature.Name,
                        Status = testResult.OverallPassed ? TestStatus.Passed : TestStatus.Failed
                    });
                }
                finally
                {
                    semaphore.Release();
                }
            }).ToList();

            await Task.WhenAll(tasks);
        }
        else
        {
            // Run tests sequentially
            foreach (var signature in signatures)
            {
                ct.ThrowIfCancellationRequested();

                var testResult = await TestSignatureAsync(signature, processId, options, ct);
                testResults.Add(testResult);

                completedCount++;
                options.ProgressReporter?.Report(new TestProgress
                {
                    CompletedCount = completedCount,
                    TotalCount = signatures.Count,
                    CurrentSignature = signature.Name,
                    Status = testResult.OverallPassed ? TestStatus.Passed : TestStatus.Failed
                });
            }
        }

        result.Results = testResults.ToList();
        result.CompletedAt = _timeProvider.UtcNow;
        result.Duration = stopwatch.Elapsed;

        _logger.LogInformation(
            "Test suite completed: {Passed}/{Total} passed in {Duration}",
            result.PassedCount, result.TotalSignatures, result.Duration);

        return result;
    }

    /// <summary>
    /// Tests a single signature with all verification types.
    /// </summary>
    private async Task<SignatureTestResult> TestSignatureAsync(
        GameMemorySignature signature,
        int processId,
        TestSuiteOptions options,
        CancellationToken ct)
    {
        var result = new SignatureTestResult
        {
            SignatureId = $"{signature.GameTitle}/{signature.Name}",
            SignatureName = signature.Name,
            GameTitle = signature.GameTitle,
            TestedAt = _timeProvider.UtcNow
        };

        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Run verification with all tests enabled
            var verificationOptions = new VerificationOptions
            {
                SkipDynamicTests = options.SkipDynamicTests,
                SkipStabilityTests = options.SkipStabilityTests,
                StabilitySampleCount = options.StabilitySampleCount,
                StabilitySampleDelayMs = options.StabilitySampleDelayMs,
                VerifyPointerChains = options.VerifyPointerChains,
                TargetGameVersion = options.TargetGameVersion,
                MinimumConfidenceThreshold = options.MinimumConfidenceThreshold,
                UserInteraction = options.UserInteraction
            };

            var verificationResult = await _verificationService.VerifySignatureAsync(
                signature, processId, verificationOptions, ct);

            if (verificationResult.IsSuccess)
            {
                var vr = verificationResult.Value;
                result.VerificationResult = vr;
                result.Confidence = vr.Confidence;
                result.HealthScore = vr.HealthScore.OverallScore;
                result.HealthRating = vr.HealthScore.GetRating();

                // Map verification tests to test results
                foreach (var test in vr.TestResults)
                {
                    result.Tests.Add(new IndividualTestResult
                    {
                        TestType = MapTestType(test.TestType),
                        Passed = test.Passed,
                        Message = test.Message,
                        Duration = test.Duration
                    });
                }

                result.OverallPassed = vr.IsValid && vr.Confidence >= options.MinimumConfidenceThreshold;
                result.FailureReason = vr.FailureReason;
            }
            else
            {
                result.OverallPassed = false;
                result.FailureReason = verificationResult.Error;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing signature '{SignatureName}'", signature.Name);
            result.OverallPassed = false;
            result.FailureReason = $"Test error: {ex.Message}";
        }

        result.Duration = stopwatch.Elapsed;
        return result;
    }

    /// <summary>
    /// Runs a regression test comparing current results with baseline.
    /// </summary>
    public async Task<RegressionTestResult> RunRegressionTestAsync(
        List<GameMemorySignature> signatures,
        int processId,
        TestSuiteResult baselineResults,
        TestSuiteOptions? options = null,
        CancellationToken ct = default)
    {
        options ??= new TestSuiteOptions();
        var currentResults = await RunTestSuiteAsync(signatures, processId, options, ct);

        var result = new RegressionTestResult
        {
            BaselineResults = baselineResults,
            CurrentResults = currentResults,
            StartedAt = currentResults.StartedAt,
            CompletedAt = currentResults.CompletedAt,
            Duration = currentResults.Duration
        };

        // Compare results
        var baselineById = baselineResults.Results.ToDictionary(r => r.SignatureId);
        var currentById = currentResults.Results.ToDictionary(r => r.SignatureId);

        foreach (var current in currentResults.Results)
        {
            if (baselineById.TryGetValue(current.SignatureId, out var baseline))
            {
                // Compare with baseline
                if (current.OverallPassed && !baseline.OverallPassed)
                {
                    result.FixedSignatures.Add(current);
                }
                else if (!current.OverallPassed && baseline.OverallPassed)
                {
                    result.RegressedSignatures.Add(new RegressionDetails
                    {
                        SignatureTest = current,
                        BaselinePassed = true,
                        CurrentPassed = false,
                        BaselineConfidence = baseline.Confidence,
                        CurrentConfidence = current.Confidence
                    });
                }
                else if (current.OverallPassed && baseline.OverallPassed)
                {
                    // Both passed - check for confidence change
                    if (current.Confidence < baseline.Confidence - 0.2)
                    {
                        result.DegradedSignatures.Add(new RegressionDetails
                        {
                            SignatureTest = current,
                            BaselinePassed = true,
                            CurrentPassed = true,
                            BaselineConfidence = baseline.Confidence,
                            CurrentConfidence = current.Confidence
                        });
                    }
                    else
                    {
                        result.StableSignatures.Add(current);
                    }
                }
            }
            else
            {
                // New signature
                result.NewSignatures.Add(current);
            }
        }

        // Find removed signatures
        foreach (var baseline in baselineResults.Results)
        {
            if (!currentById.ContainsKey(baseline.SignatureId))
            {
                result.RemovedSignatures.Add(baseline);
            }
        }

        return result;
    }

    /// <summary>
    /// Exports test results to JSON file.
    /// </summary>
    public async Task<Result<string>> ExportResultsAsync(
        TestSuiteResult results,
        string outputPath,
        ExportFormat format = ExportFormat.Json,
        CancellationToken ct = default)
    {
        try
        {
            string content = format switch
            {
                ExportFormat.Json => JsonSerializer.Serialize(results, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                }),
                ExportFormat.Csv => ConvertToCsv(results),
                ExportFormat.Markdown => ConvertToMarkdown(results),
                _ => throw new ArgumentException($"Unsupported format: {format}")
            };

            await File.WriteAllTextAsync(outputPath, content, ct);
            _logger.LogInformation("Test results exported to {Path}", outputPath);

            return Result.Success(outputPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export test results");
            return Result.Failure<string>($"Export failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Filters signatures by health rating.
    /// </summary>
    public List<SignatureTestResult> FilterByHealthRating(
        TestSuiteResult results,
        params HealthRating[] ratings)
    {
        return results.Results
            .Where(r => ratings.Contains(r.HealthRating))
            .ToList();
    }

    /// <summary>
    /// Gets a summary report of test results.
    /// </summary>
    public TestSummaryReport GenerateSummaryReport(TestSuiteResult results)
    {
        var report = new TestSummaryReport
        {
            GeneratedAt = _timeProvider.UtcNow,
            TotalSignatures = results.TotalSignatures,
            PassedCount = results.PassedCount,
            FailedCount = results.FailedCount,
            SuccessRate = results.SuccessRate,
            Duration = results.Duration
        };

        // Group by health rating
        report.ByHealthRating = results.Results
            .GroupBy(r => r.HealthRating)
            .ToDictionary(
                g => g.Key,
                g => new HealthRatingSummary
                {
                    Count = g.Count(),
                    Signatures = g.Select(r => r.SignatureName).ToList(),
                    AverageConfidence = g.Average(r => r.Confidence)
                });

        // Group by game
        report.ByGame = results.Results
            .GroupBy(r => r.GameTitle)
            .ToDictionary(
                g => g.Key,
                g => new GameSummary
                {
                    Count = g.Count(),
                    Passed = g.Count(r => r.OverallPassed),
                    Failed = g.Count(r => !r.OverallPassed),
                    AverageHealthScore = g.Average(r => r.HealthScore)
                });

        // Critical issues
        report.CriticalIssues = results.Results
            .Where(r => !r.OverallPassed && r.HealthScore < 30)
            .Select(r => new CriticalIssue
            {
                SignatureName = r.SignatureName,
                GameTitle = r.GameTitle,
                Issue = r.FailureReason ?? "Unknown failure",
                HealthScore = r.HealthScore
            })
            .ToList();

        return report;
    }

    #region Private Helpers

    private static TestType MapTestType(VerificationTestType type)
    {
        return type switch
        {
            VerificationTestType.Static => TestType.Static,
            VerificationTestType.Dynamic => TestType.Dynamic,
            VerificationTestType.Stability => TestType.Stability,
            VerificationTestType.PointerChain => TestType.PointerChain,
            VerificationTestType.PatternQuality => TestType.PatternQuality,
            VerificationTestType.ValueRange => TestType.ValueRange,
            VerificationTestType.MemoryAccess => TestType.MemoryAccess,
            _ => TestType.Static
        };
    }

    private static string ConvertToCsv(TestSuiteResult results)
    {
        var lines = new List<string>
        {
            "Signature,Game,Passed,Confidence,Health Score,Health Rating,Duration,Failure Reason"
        };

        foreach (var r in results.Results)
        {
            var failureReason = (r.FailureReason ?? "").Replace("\"", "\"\"");
            lines.Add($"\"{r.SignatureName}\",\"{r.GameTitle}\",{r.OverallPassed},{r.Confidence:F2},{r.HealthScore},{r.HealthRating},{r.Duration.TotalMilliseconds:F0},\"{failureReason}\"");
        }

        return string.Join("\n", lines);
    }

    private static string ConvertToMarkdown(TestSuiteResult results)
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("# Signature Test Results");
        sb.AppendLine();
        sb.AppendLine($"**Generated:** {results.CompletedAt:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine($"**Duration:** {results.Duration.TotalSeconds:F1}s");
        sb.AppendLine($"**Total Signatures:** {results.TotalSignatures}");
        sb.AppendLine($"**Passed:** {results.PassedCount} ({results.SuccessRate:P0})");
        sb.AppendLine($"**Failed:** {results.FailedCount}");
        sb.AppendLine();

        sb.AppendLine("## Results by Signature");
        sb.AppendLine();
        sb.AppendLine("| Signature | Game | Status | Confidence | Health |");
        sb.AppendLine("|-----------|------|--------|------------|--------|");

        foreach (var r in results.Results.OrderByDescending(r => r.Confidence))
        {
            var status = r.OverallPassed ? "✅ Pass" : "❌ Fail";
            var health = $"{r.HealthScore} ({r.HealthRating})";
            sb.AppendLine($"| {r.SignatureName} | {r.GameTitle} | {status} | {r.Confidence:P0} | {health} |");
        }

        sb.AppendLine();

        // Failed signatures details
        var failures = results.Results.Where(r => !r.OverallPassed).ToList();
        if (failures.Any())
        {
            sb.AppendLine("## Failed Signatures Details");
            sb.AppendLine();

            foreach (var r in failures)
            {
                sb.AppendLine($"### {r.SignatureName} ({r.GameTitle})");
                sb.AppendLine($"- **Failure Reason:** {r.FailureReason}");
                sb.AppendLine($"- **Confidence:** {r.Confidence:P0}");
                sb.AppendLine($"- **Health Score:** {r.HealthScore} ({r.HealthRating})");
                sb.AppendLine($"- **Duration:** {r.Duration.TotalMilliseconds:F0}ms");
                sb.AppendLine();

                if (r.Tests.Any())
                {
                    sb.AppendLine("**Test Results:**");
                    foreach (var test in r.Tests)
                    {
                        var icon = test.Passed ? "✅" : "❌";
                        sb.AppendLine($"- {icon} {test.TestType}: {test.Message}");
                    }
                    sb.AppendLine();
                }
            }
        }

        return sb.ToString();
    }

    #endregion
}

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
