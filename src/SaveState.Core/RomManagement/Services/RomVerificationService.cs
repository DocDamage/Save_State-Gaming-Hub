using SaveState.Core.Common.Interfaces;
using SaveState.Core.RomManagement.Entities;

namespace SaveState.Core.RomManagement.Services;

public class RomVerificationService : IRomVerificationService
{
    private readonly IFileSystem _fileSystem;

    public RomVerificationService(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public async Task<RomVerificationResult> VerifyRomAsync(RomFile rom, CancellationToken ct = default)
    {
        try
        {
            // Check if file exists
            if (!await _fileSystem.FileExistsAsync(rom.FilePath.Value, ct).ConfigureAwait(false))
            {
                return RomVerificationResult.Error($"ROM file does not exist: {rom.FilePath.Value}");
            }

            // Calculate actual checksum
            var actualChecksum = await CalculateChecksumAsync(rom.FilePath.Value, ct).ConfigureAwait(false);

            // Compare with stored checksum
            if (string.IsNullOrEmpty(rom.Checksum))
            {
                return RomVerificationResult.Error("No checksum stored for ROM file");
            }

            if (string.Equals(rom.Checksum, actualChecksum, StringComparison.OrdinalIgnoreCase))
            {
                return RomVerificationResult.Valid(rom.Checksum, actualChecksum);
            }
            else
            {
                return RomVerificationResult.Invalid(rom.Checksum, actualChecksum, "Checksum mismatch");
            }
        }
        catch (Exception ex)
        {
            return RomVerificationResult.Error($"Verification failed: {ex.Message}");
        }
    }

    public async Task<string> CalculateChecksumAsync(string filePath, CancellationToken ct = default)
    {
        var fileBytes = await _fileSystem.ReadAllBytesAsync(filePath, ct).ConfigureAwait(false);

        // Use SHA256 for checksum calculation
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashBytes = sha256.ComputeHash(fileBytes);
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    }
}
