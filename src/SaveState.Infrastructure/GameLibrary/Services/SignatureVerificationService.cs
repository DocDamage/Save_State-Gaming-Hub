using System.Diagnostics;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Services;

/// <summary>
/// Service implementation for verifying memory signatures.
/// </summary>
public partial class SignatureVerificationService : ISignatureVerificationService
{
    private readonly ILogger<SignatureVerificationService> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly IGameMemoryReader _memoryReader;
    private readonly IMemoryPatternDatabase _patternDatabase;

    public SignatureVerificationService(
        ILogger<SignatureVerificationService> logger,
        ITimeProvider timeProvider,
        IGameMemoryReader memoryReader,
        IMemoryPatternDatabase patternDatabase)
    {
        _logger = logger;
        _timeProvider = timeProvider;
        _memoryReader = memoryReader;
        _patternDatabase = patternDatabase;
    }

    /// <inheritdoc />
    public async Task<Result<VerificationResult>> VerifySignatureAsync(
        GameMemorySignature signature,
        int processId,
        VerificationOptions options,
        CancellationToken ct = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = new VerificationResult
        {
            ProcessId = processId,
            TestedGameVersion = options.TargetGameVersion
        };

        try
        {
            _logger.LogInformation("Starting verification for signature '{SignatureName}' on process {ProcessId}",
                signature.Name, processId);

            // Ensure we're attached to the process
            if (!_memoryReader.IsAttached)
            {
                var attachResult = await _memoryReader.AttachToProcessAsync(processId, ct);
                if (attachResult.IsFailure)
                {
                    result.FailureReason = $"Failed to attach to process: {attachResult.Error}";
                    return Result.Success(result);
                }
            }

            // Run static verification tests
            var staticResult = await RunStaticVerificationAsync(signature, processId, ct);
            result.TestResults.Add(staticResult);

            if (!staticResult.Passed)
            {
                result.IsValid = false;
                result.FailureReason = staticResult.Message;
                result.HealthScore = CalculateHealthScore(result.TestResults, signature);
                return Result.Success(result);
            }

            // Set found address and current value from static test
            if (staticResult.Metadata.TryGetValue("FoundAddress", out var addr))
            {
                result.FoundAddress = (IntPtr)addr;
            }
            if (staticResult.Metadata.TryGetValue("CurrentValue", out var val))
            {
                result.CurrentValue = val;
            }

            // Run pointer chain verification if applicable
            if (options.VerifyPointerChains && signature.Pattern.Contains("->"))
            {
                var pointerResult = await RunPointerChainVerificationAsync(signature, processId, ct);
                result.TestResults.Add(pointerResult);
            }

            // Run stability verification
            if (!options.SkipStabilityTests)
            {
                var stabilityResult = await RunStabilityVerificationAsync(
                    signature, result.FoundAddress!.Value, options, ct);
                result.TestResults.Add(stabilityResult);
            }

            // Run dynamic verification
            if (!options.SkipDynamicTests && options.UserInteraction != null)
            {
                var dynamicResult = await RunDynamicVerificationAsync(
                    signature, result.FoundAddress!.Value, options, ct);
                result.TestResults.Add(dynamicResult);
            }

            // Calculate final results
            result.IsValid = result.TestResults.All(r => r.Passed);
            result.Confidence = CalculateConfidence(result.TestResults);
            result.HealthScore = CalculateHealthScore(result.TestResults, signature);
            result.VerificationDuration = stopwatch.Elapsed;

            if (!result.IsValid)
            {
                var failedTest = result.TestResults.FirstOrDefault(r => !r.Passed);
                result.FailureReason = failedTest?.Message ?? "Unknown verification failure";
            }

            _logger.LogInformation(
                "Verification completed for '{SignatureName}': Valid={IsValid}, Confidence={Confidence:P}",
                signature.Name, result.IsValid, result.Confidence);

            return Result.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying signature '{SignatureName}'", signature.Name);
            result.FailureReason = $"Verification error: {ex.Message}";
            result.VerificationDuration = stopwatch.Elapsed;
            return Result.Success(result);
        }
    }

    /// <inheritdoc />
    public async Task<Result<BatchVerificationResult>> VerifySignaturesAsync(
        List<GameMemorySignature> signatures,
        int processId,
        VerificationOptions options,
        CancellationToken ct = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var batchResult = new BatchVerificationResult();

        _logger.LogInformation("Starting batch verification of {Count} signatures on process {ProcessId}",
            signatures.Count, processId);

        foreach (var signature in signatures)
        {
            ct.ThrowIfCancellationRequested();

            var result = await VerifySignatureAsync(signature, processId, options, ct);
            if (result.IsSuccess)
            {
                batchResult.Results.Add(result.Value);
            }
            else
            {
                batchResult.Results.Add(new VerificationResult
                {
                    IsValid = false,
                    FailureReason = result.Error,
                    TestedGameVersion = options.TargetGameVersion,
                    ProcessId = processId
                });
            }

            // Report progress if user interaction is available
            if (options.UserInteraction != null)
            {
                var progress = (int)((double)batchResult.Results.Count / signatures.Count * 100);
                await options.UserInteraction.NotifyProgressAsync(
                    $"Verified {signature.Name}", progress, ct);
            }
        }

        batchResult.TotalDuration = stopwatch.Elapsed;

        _logger.LogInformation(
            "Batch verification completed: {Passed}/{Total} passed ({Rate:P})",
            batchResult.PassedCount, batchResult.TotalCount, batchResult.SuccessRate);

        return Result.Success(batchResult);
    }

    /// <inheritdoc />
    public Task<Result<ValidationReport>> ValidateSignatureHealthAsync(
        GameMemorySignature signature,
        CancellationToken ct = default)
    {
        var report = new ValidationReport
        {
            Signature = signature,
            GeneratedAt = _timeProvider.UtcNow
        };

        // Pattern quality checks
        ValidatePatternQuality(signature, report);

        // Offset validation
        ValidateOffset(signature, report);

        // Value type validation
        ValidateValueType(signature, report);

        // Range validation
        ValidateRanges(signature, report);

        // Calculate health score without process access
        report.HealthScore = CalculateOfflineHealthScore(signature, report.Issues);

        // Generate recommendations
        report.Recommendations = GenerateRecommendations(signature, report.Issues);

        return Task.FromResult(Result.Success(report));
    }

    /// <inheritdoc />
    public Task<Result<List<PatternFixSuggestion>>> SuggestFixesAsync(
        GameMemorySignature signature,
        VerificationResult failureResult,
        CancellationToken ct = default)
    {
        var suggestions = new List<PatternFixSuggestion>();

        // Analyze failure reason and suggest fixes
        if (failureResult.FailureReason?.Contains("not found") == true)
        {
            // Pattern not found - suggest relaxed pattern
            suggestions.Add(new PatternFixSuggestion
            {
                Type = FixType.RelaxPattern,
                Description = "Pattern not found in memory. Try using wildcards for variable bytes.",
                SuggestedPattern = SuggestRelaxedPattern(signature.Pattern),
                Confidence = 0.6,
                Reasoning = "Some bytes in the pattern may vary between game versions or sessions."
            });

            // Suggest module constraint removal if present
            if (!string.IsNullOrEmpty(signature.ModuleName))
            {
                suggestions.Add(new PatternFixSuggestion
                {
                    Type = FixType.RemoveModuleConstraint,
                    Description = "Try scanning all modules instead of just one.",
                    SuggestedPattern = signature.Pattern,
                    Confidence = 0.4,
                    Reasoning = "The pattern might be in a different module (e.g., a DLL instead of the main executable)."
                });
            }
        }

        if (failureResult.FailureReason?.Contains("offset") == true ||
            failureResult.FailureReason?.Contains("read") == true)
        {
            // Offset issues - suggest nearby offsets
            for (int offset = -8; offset <= 8; offset += 4)
            {
                if (offset == 0) continue;
                suggestions.Add(new PatternFixSuggestion
                {
                    Type = FixType.AdjustOffset,
                    Description = $"Try offset {signature.Offset + offset}",
                    SuggestedOffset = signature.Offset + offset,
                    Confidence = 0.5 - Math.Abs(offset) * 0.05,
                    Reasoning = "The value might be located at a slightly different offset from the pattern."
                });
            }
        }

        if (failureResult.FailureReason?.Contains("value") == true ||
            failureResult.FailureReason?.Contains("range") == true)
        {
            // Value type issues
            var alternativeTypes = GetAlternativeValueTypes(signature.ValueType);
            foreach (var type in alternativeTypes)
            {
                suggestions.Add(new PatternFixSuggestion
                {
                    Type = FixType.ChangeValueType,
                    Description = $"Try reading as {type} instead of {signature.ValueType}",
                    SuggestedValueType = type,
                    Confidence = 0.45,
                    Reasoning = "The value might be stored in a different format than expected."
                });
            }
        }

        // If all else fails, suggest a complete rescan
        suggestions.Add(new PatternFixSuggestion
        {
            Type = FixType.UpdatePattern,
            Description = "Perform a new memory scan to find the updated pattern",
            Confidence = 0.8,
            Reasoning = "The game may have been updated significantly, requiring a fresh pattern scan."
        });

        return Task.FromResult(Result.Success(suggestions));
    }

    /// <inheritdoc />
    public Task<Result<CommunityVerificationStats>> GetCommunityStatsAsync(
        string signatureId,
        CancellationToken ct = default)
    {
        // This would typically query a database or API
        // For now, return mock data based on the pattern database
        var stats = new CommunityVerificationStats
        {
            TotalReports = 0,
            WorkingReports = 0,
            BrokenReports = 0
        };

        return Task.FromResult(Result.Success(stats));
    }

}

