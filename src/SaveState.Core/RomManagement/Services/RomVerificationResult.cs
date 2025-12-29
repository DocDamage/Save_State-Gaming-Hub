namespace SaveState.Core.RomManagement.Services;

public class RomVerificationResult
{
    public bool IsValid { get; }
    public string? ExpectedChecksum { get; }
    public string? ActualChecksum { get; }
    public string? ErrorMessage { get; }
    public DateTime VerifiedAt { get; }

    private RomVerificationResult(bool isValid, string? expectedChecksum, string? actualChecksum, string? errorMessage)
    {
        IsValid = isValid;
        ExpectedChecksum = expectedChecksum;
        ActualChecksum = actualChecksum;
        ErrorMessage = errorMessage;
        VerifiedAt = DateTime.UtcNow;
    }

    public static RomVerificationResult Valid(string expectedChecksum, string actualChecksum)
    {
        return new RomVerificationResult(true, expectedChecksum, actualChecksum, null);
    }

    public static RomVerificationResult Invalid(string expectedChecksum, string actualChecksum, string errorMessage)
    {
        return new RomVerificationResult(false, expectedChecksum, actualChecksum, errorMessage);
    }

    public static RomVerificationResult Error(string errorMessage)
    {
        return new RomVerificationResult(false, null, null, errorMessage);
    }
}
