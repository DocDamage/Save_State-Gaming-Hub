using SaveState.Core.RomManagement.Entities;

namespace SaveState.Core.RomManagement.Services;

public interface IRomVerificationService
{
    Task<RomVerificationResult> VerifyRomAsync(RomFile rom, CancellationToken ct = default);
    Task<string> CalculateChecksumAsync(string filePath, CancellationToken ct = default);
}
