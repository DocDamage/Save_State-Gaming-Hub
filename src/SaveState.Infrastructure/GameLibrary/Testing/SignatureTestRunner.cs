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
