using SaveState.Core.Common.Services;

namespace SaveState.Core.RomManagement.Services;

public class RomVerificationResult
{
    public bool IsValid { get; }
    public string? ExpectedChecksum { get; }
    public string? ActualChecksum { get; }
    public string? ErrorMessage { get; }
    public DateTime VerifiedAt { get; }

    private RomVerificationResult(bool isValid, string? expectedChecksum, string? actualChecksum, string? errorMessage, DateTime verifiedAt)
    {
        IsValid = isValid;
        ExpectedChecksum = expectedChecksum;
        ActualChecksum = actualChecksum;
        ErrorMessage = errorMessage;
        VerifiedAt = verifiedAt;
    }

    public static RomVerificationResult Valid(string expectedChecksum, string actualChecksum, DateTime verifiedAt)
    {
        return new RomVerificationResult(true, expectedChecksum, actualChecksum, null, verifiedAt);
    }

    public static RomVerificationResult Invalid(string expectedChecksum, string actualChecksum, string errorMessage, DateTime verifiedAt)
    {
        return new RomVerificationResult(false, expectedChecksum, actualChecksum, errorMessage, verifiedAt);
    }

    public static RomVerificationResult Error(string errorMessage, DateTime verifiedAt)
    {
        return new RomVerificationResult(false, null, null, errorMessage, verifiedAt);
    }

    [Obsolete("Use Valid(string, string, DateTime) with explicit timestamp")]
    public static RomVerificationResult Valid(string expectedChecksum, string actualChecksum)
    {
        return new RomVerificationResult(true, expectedChecksum, actualChecksum, null, DateTime.UtcNow);
    }

    [Obsolete("Use Invalid(string, string, string, DateTime) with explicit timestamp")]
    public static RomVerificationResult Invalid(string expectedChecksum, string actualChecksum, string errorMessage)
    {
        return new RomVerificationResult(false, expectedChecksum, actualChecksum, errorMessage, DateTime.UtcNow);
    }

    [Obsolete("Use Error(string, DateTime) with explicit timestamp")]
    public static RomVerificationResult Error(string errorMessage)
    {
        return new RomVerificationResult(false, null, null, errorMessage, DateTime.UtcNow);
    }
}
