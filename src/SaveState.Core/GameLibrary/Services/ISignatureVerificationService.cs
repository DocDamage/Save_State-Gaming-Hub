using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.GameLibrary.Entities;

namespace SaveState.Core.GameLibrary.Services;

/// <summary>
/// Service for verifying memory signatures against real game processes.
/// </summary>
public interface ISignatureVerificationService
{
    /// <summary>
    /// Verifies a single signature against a running process.
    /// </summary>
    Task<Result<VerificationResult>> VerifySignatureAsync(
        GameMemorySignature signature,
        int processId,
        VerificationOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Verifies multiple signatures in batch against a running process.
    /// </summary>
    Task<Result<BatchVerificationResult>> VerifySignaturesAsync(
        List<GameMemorySignature> signatures,
        int processId,
        VerificationOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Validates the overall health of a signature without requiring a running process.
    /// </summary>
    Task<Result<ValidationReport>> ValidateSignatureHealthAsync(
        GameMemorySignature signature,
        CancellationToken ct = default);

    /// <summary>
    /// Gets suggestions for fixing a broken signature.
    /// </summary>
    Task<Result<List<PatternFixSuggestion>>> SuggestFixesAsync(
        GameMemorySignature signature,
        VerificationResult failureResult,
        CancellationToken ct = default);

    /// <summary>
    /// Gets community verification statistics for a signature.
    /// </summary>
    Task<Result<CommunityVerificationStats>> GetCommunityStatsAsync(
        string signatureId,
        CancellationToken ct = default);
}

/// <summary>
/// Options for controlling verification behavior.
/// </summary>
public class VerificationOptions
{
    /// <summary>
    /// Time to wait for dynamic verification in seconds.
    /// </summary>
    public int DynamicVerificationTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Whether to skip dynamic verification tests.
    /// </summary>
    public bool SkipDynamicTests { get; set; } = false;

    /// <summary>
    /// Whether to skip stability verification tests.
    /// </summary>
    public bool SkipStabilityTests { get; set; } = false;

    /// <summary>
    /// Number of stability samples to take.
    /// </summary>
    public int StabilitySampleCount { get; set; } = 10;

    /// <summary>
    /// Delay between stability samples in milliseconds.
    /// </summary>
    public int StabilitySampleDelayMs { get; set; } = 500;

    /// <summary>
    /// Whether to verify pointer chains.
    /// </summary>
    public bool VerifyPointerChains { get; set; } = true;

    /// <summary>
    /// Game version to verify against.
    /// </summary>
    public string? TargetGameVersion { get; set; }

    /// <summary>
    /// Minimum confidence threshold (0.0-1.0) for considering a signature valid.
    /// </summary>
    public double MinimumConfidenceThreshold { get; set; } = 0.7;

    /// <summary>
    /// User interaction provider for dynamic tests.
    /// </summary>
    public IUserInteractionProvider? UserInteraction { get; set; }
}

/// <summary>
/// Result of a single signature verification.
/// </summary>
public class VerificationResult
{
    /// <summary>
    /// Whether the signature passed all verification tests.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Confidence score (0.0-1.0) based on test results.
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    /// The memory address where the signature was found.
    /// </summary>
    public IntPtr? FoundAddress { get; set; }

    /// <summary>
    /// The current value read from the address.
    /// </summary>
    public object? CurrentValue { get; set; }

    /// <summary>
    /// Individual test results.
    /// </summary>
    public List<VerificationTestResult> TestResults { get; set; } = new();

    /// <summary>
    /// Reason for failure if IsValid is false.
    /// </summary>
    public string? FailureReason { get; set; }

    /// <summary>
    /// Health score for the signature.
    /// </summary>
    public SignatureHealthScore HealthScore { get; set; } = new();

    /// <summary>
    /// Time taken for verification.
    /// </summary>
    public TimeSpan VerificationDuration { get; set; }

    /// <summary>
    /// Timestamp of verification.
    /// </summary>
    public DateTime VerifiedAt { get; set; } = SystemTimeProvider.Instance.UtcNow;

    /// <summary>
    /// Game version that was tested.
    /// </summary>
    public string? TestedGameVersion { get; set; }

    /// <summary>
    /// Process ID that was tested.
    /// </summary>
    public int ProcessId { get; set; }
}

/// <summary>
/// Result of batch signature verification.
/// </summary>
public class BatchVerificationResult
{
    /// <summary>
    /// Individual signature results.
    /// </summary>
    public List<VerificationResult> Results { get; set; } = new();

    /// <summary>
    /// Number of signatures that passed verification.
    /// </summary>
    public int PassedCount => Results.Count(r => r.IsValid);

    /// <summary>
    /// Number of signatures that failed verification.
    /// </summary>
    public int FailedCount => Results.Count(r => !r.IsValid);

    /// <summary>
    /// Total number of signatures tested.
    /// </summary>
    public int TotalCount => Results.Count;

    /// <summary>
    /// Overall success rate (0.0-1.0).
    /// </summary>
    public double SuccessRate => TotalCount > 0 ? (double)PassedCount / TotalCount : 0;

    /// <summary>
    /// Time taken for the entire batch.
    /// </summary>
    public TimeSpan TotalDuration { get; set; }

    /// <summary>
    /// Signatures that passed verification.
    /// </summary>
    public List<VerificationResult> PassedSignatures => Results.Where(r => r.IsValid).ToList();

    /// <summary>
    /// Signatures that failed verification.
    /// </summary>
    public List<VerificationResult> FailedSignatures => Results.Where(r => !r.IsValid).ToList();
}

/// <summary>
/// Result of a single verification test.
/// </summary>
public class VerificationTestResult
{
    /// <summary>
    /// Name of the test.
    /// </summary>
    public string TestName { get; set; } = "";

    /// <summary>
    /// Type of test performed.
    /// </summary>
    public VerificationTestType TestType { get; set; }

    /// <summary>
    /// Whether the test passed.
    /// </summary>
    public bool Passed { get; set; }

    /// <summary>
    /// Detailed message from the test.
    /// </summary>
    public string Message { get; set; } = "";

    /// <summary>
    /// Time taken for the test.
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Additional data from the test.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Types of verification tests.
/// </summary>
public enum VerificationTestType
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
/// Health score for a memory signature.
/// </summary>
public class SignatureHealthScore
{
    /// <summary>
    /// Overall health score (0-100).
    /// </summary>
    public int OverallScore { get; set; }

    /// <summary>
    /// Quality of the pattern (specificity, uniqueness).
    /// </summary>
    public int PatternQuality { get; set; }

    /// <summary>
    /// Stability of the memory address across sessions.
    /// </summary>
    public int AddressStability { get; set; }

    /// <summary>
    /// Reliability of the value (behaves as expected).
    /// </summary>
    public int ValueReliability { get; set; }

    /// <summary>
    /// Compatibility across game versions.
    /// </summary>
    public int CompatibilityScore { get; set; }

    /// <summary>
    /// Community verification score.
    /// </summary>
    public int CommunityScore { get; set; }

    /// <summary>
    /// Gets the health rating based on overall score.
    /// </summary>
    public HealthRating GetRating()
    {
        return OverallScore switch
        {
            >= 90 => HealthRating.Excellent,
            >= 70 => HealthRating.Good,
            >= 50 => HealthRating.Fair,
            >= 30 => HealthRating.Poor,
            _ => HealthRating.Broken
        };
    }

    /// <summary>
    /// Gets a description of the health rating.
    /// </summary>
    public string GetRatingDescription()
    {
        return GetRating() switch
        {
            HealthRating.Excellent => "Signature is working perfectly across all tests",
            HealthRating.Good => "Signature is working well with minor issues",
            HealthRating.Fair => "Signature works but may be unreliable",
            HealthRating.Poor => "Signature has significant issues",
            HealthRating.Broken => "Signature is not working",
            _ => "Unknown health status"
        };
    }
}

/// <summary>
/// Health rating categories.
/// </summary>
public enum HealthRating
{
    Broken,
    Poor,
    Fair,
    Good,
    Excellent
}

/// <summary>
/// Validation report for a signature.
/// </summary>
public class ValidationReport
{
    /// <summary>
    /// The signature being validated.
    /// </summary>
    public required GameMemorySignature Signature { get; init; }

    /// <summary>
    /// Overall health score.
    /// </summary>
    public SignatureHealthScore HealthScore { get; set; } = new();

    /// <summary>
    /// Issues found during validation.
    /// </summary>
    public List<ValidationIssue> Issues { get; set; } = new();

    /// <summary>
    /// Recommendations for improving the signature.
    /// </summary>
    public List<string> Recommendations { get; set; } = new();

    /// <summary>
    /// Whether the signature is valid for use.
    /// </summary>
    public bool IsValidForUse => HealthScore.OverallScore >= 50;

    /// <summary>
    /// Timestamp of the report.
    /// </summary>
    public DateTime GeneratedAt { get; set; } = SystemTimeProvider.Instance.UtcNow;
}

/// <summary>
/// Validation issue details.
/// </summary>
public class ValidationIssue
{
    /// <summary>
    /// Severity of the issue.
    /// </summary>
    public IssueSeverity Severity { get; set; }

    /// <summary>
    /// Category of the issue.
    /// </summary>
    public IssueCategory Category { get; set; }

    /// <summary>
    /// Description of the issue.
    /// </summary>
    public string Description { get; set; } = "";

    /// <summary>
    /// Suggested fix for the issue.
    /// </summary>
    public string? SuggestedFix { get; set; }
}

/// <summary>
/// Issue severity levels.
/// </summary>
public enum IssueSeverity
{
    Info,
    Warning,
    Critical
}

/// <summary>
/// Issue categories.
/// </summary>
public enum IssueCategory
{
    Pattern,
    Offset,
    ValueType,
    Range,
    PointerChain,
    Metadata
}

/// <summary>
/// Suggestion for fixing a broken pattern.
/// </summary>
public class PatternFixSuggestion
{
    /// <summary>
    /// Type of fix suggested.
    /// </summary>
    public FixType Type { get; set; }

    /// <summary>
    /// Description of the suggested fix.
    /// </summary>
    public string Description { get; set; } = "";

    /// <summary>
    /// The suggested new pattern value.
    /// </summary>
    public string? SuggestedPattern { get; set; }

    /// <summary>
    /// The suggested new offset.
    /// </summary>
    public int? SuggestedOffset { get; set; }

    /// <summary>
    /// The suggested value type.
    /// </summary>
    public string? SuggestedValueType { get; set; }

    /// <summary>
    /// Confidence that this fix will work (0.0-1.0).
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    /// Reasoning behind the suggestion.
    /// </summary>
    public string Reasoning { get; set; } = "";
}

/// <summary>
/// Types of fixes that can be suggested.
/// </summary>
public enum FixType
{
    AdjustOffset,
    ChangeValueType,
    RelaxPattern,
    TightenPattern,
    UpdatePattern,
    AddModuleConstraint,
    RemoveModuleConstraint,
    AdjustRange
}

/// <summary>
/// Community verification statistics.
/// </summary>
public class CommunityVerificationStats
{
    /// <summary>
    /// Total number of reports.
    /// </summary>
    public int TotalReports { get; set; }

    /// <summary>
    /// Number of working reports.
    /// </summary>
    public int WorkingReports { get; set; }

    /// <summary>
    /// Number of broken reports.
    /// </summary>
    public int BrokenReports { get; set; }

    /// <summary>
    /// Success rate (0.0-1.0).
    /// </summary>
    public double SuccessRate => TotalReports > 0 ? (double)WorkingReports / TotalReports : 0;

    /// <summary>
    /// Reports grouped by game version.
    /// </summary>
    public Dictionary<string, VersionStats> ByVersion { get; set; } = new();

    /// <summary>
    /// Last report timestamp.
    /// </summary>
    public DateTime? LastReportedAt { get; set; }

    /// <summary>
    /// Average confidence score from community.
    /// </summary>
    public double AverageConfidence { get; set; }
}

/// <summary>
/// Statistics for a specific game version.
/// </summary>
public class VersionStats
{
    /// <summary>
    /// Game version.
    /// </summary>
    public string Version { get; set; } = "";

    /// <summary>
    /// Number of working reports.
    /// </summary>
    public int Working { get; set; }

    /// <summary>
    /// Number of broken reports.
    /// </summary>
    public int Broken { get; set; }

    /// <summary>
    /// Success rate for this version.
    /// </summary>
    public double SuccessRate => (Working + Broken) > 0 ? (double)Working / (Working + Broken) : 0;
}

/// <summary>
/// Interface for user interaction during dynamic verification.
/// </summary>
public interface IUserInteractionProvider
{
    /// <summary>
    /// Requests the user to perform an action.
    /// </summary>
    Task<bool> RequestActionAsync(string actionDescription, CancellationToken ct = default);

    /// <summary>
    /// Notifies the user of test progress.
    /// </summary>
    Task NotifyProgressAsync(string message, int progressPercent, CancellationToken ct = default);

    /// <summary>
    /// Asks the user a yes/no question.
    /// </summary>
    Task<bool> AskYesNoAsync(string question, CancellationToken ct = default);
}
