using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Interfaces;
using SaveState.Core.RomManagement.Entities;
using SaveState.Core.RomManagement.RomValidation;
using SaveState.Core.RomManagement.RomValidation.Services;

namespace SaveState.Infrastructure.RomManagement.Validation;

internal sealed class RomHashWorkflowManager
{
    private readonly IFileSystem _fileSystem;
    private readonly IRomHashInfoRepository _hashRepository;
    private readonly ILogger<RomValidationService> _logger;

    public RomHashWorkflowManager(
        IFileSystem fileSystem,
        IRomHashInfoRepository hashRepository,
        ILogger<RomValidationService> logger)
    {
        _fileSystem = fileSystem;
        _hashRepository = hashRepository;
        _logger = logger;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA5351:DoNotUseBrokenCryptographicAlgorithms", Justification = "MD5 required for No-Intro/Redump ROM database compatibility")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA5350:DoNotUseWeakCryptographicAlgorithms", Justification = "SHA1 required for No-Intro/Redump ROM database compatibility")]
    public async Task<Result<RomHashInfo>> CalculateHashesAsync(
        RomFile romFile,
        RomValidationOptions options,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Calculating hashes for ROM: {RomTitle}", romFile.Title);

            if (!await _fileSystem.FileExistsAsync(romFile.FilePath.Value, ct).ConfigureAwait(false))
            {
                return Result<RomHashInfo>.Failure($"ROM file not found: {romFile.FilePath.Value}");
            }

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var fileBytes = await _fileSystem.ReadAllBytesAsync(romFile.FilePath.Value, ct).ConfigureAwait(false);

            var hashInfo = new RomHashInfo { RomFileId = (Guid)romFile.Id };
            var errors = new List<string>();

            if (options.CalculateCrc32)
            {
                try
                {
                    hashInfo.Crc32 = RomHashCalculationManager.CalculateCrc32(fileBytes);
                }
                catch (Exception ex)
                {
                    errors.Add($"CRC32 calculation failed: {ex.Message}");
                    _logger.LogWarning(ex, "CRC32 calculation failed for {RomTitle}", romFile.Title);
                }
            }

            if (options.CalculateMd5)
            {
                try
                {
                    using var md5 = MD5.Create();
                    var hash = md5.ComputeHash(fileBytes);
                    hashInfo.Md5 = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
                catch (Exception ex)
                {
                    errors.Add($"MD5 calculation failed: {ex.Message}");
                    _logger.LogWarning(ex, "MD5 calculation failed for {RomTitle}", romFile.Title);
                }
            }

            if (options.CalculateSha1)
            {
                try
                {
                    using var sha1 = SHA1.Create();
                    var hash = sha1.ComputeHash(fileBytes);
                    hashInfo.Sha1 = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
                catch (Exception ex)
                {
                    errors.Add($"SHA1 calculation failed: {ex.Message}");
                    _logger.LogWarning(ex, "SHA1 calculation failed for {RomTitle}", romFile.Title);
                }
            }

            if (options.CalculateSha256)
            {
                try
                {
                    using var sha256 = SHA256.Create();
                    var hash = sha256.ComputeHash(fileBytes);
                    hashInfo.Sha256 = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
                catch (Exception ex)
                {
                    errors.Add($"SHA256 calculation failed: {ex.Message}");
                    _logger.LogWarning(ex, "SHA256 calculation failed for {RomTitle}", romFile.Title);
                }
            }

            stopwatch.Stop();
            hashInfo.CalculationTime = stopwatch.Elapsed;
            hashInfo.IsComplete = errors.Count == 0;
            hashInfo.Errors = errors;

            await _hashRepository.AddAsync(hashInfo, ct).ConfigureAwait(false);

            _logger.LogInformation(
                "Hash calculation completed for {RomTitle} in {ElapsedMs}ms",
                romFile.Title,
                stopwatch.ElapsedMilliseconds);

            return Result<RomHashInfo>.Success(hashInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to calculate hashes for ROM: {RomTitle}", romFile.Title);
            return Result<RomHashInfo>.Failure($"Hash calculation failed: {ex.Message}");
        }
    }

    public async Task<Result<FileIntegrityResult>> VerifyFileIntegrityAsync(
        string filePath,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogDebug("Verifying file integrity: {FilePath}", filePath);
            var result = new FileIntegrityResult();

            if (!await _fileSystem.FileExistsAsync(filePath, ct).ConfigureAwait(false))
            {
                result.IsIntact = false;
                result.ReadErrors.Add("File does not exist");
                return Result<FileIntegrityResult>.Success(result);
            }

            result.FileSize = await _fileSystem.GetFileSizeAsync(filePath, ct).ConfigureAwait(false);

            try
            {
                var bytes = await _fileSystem.ReadAllBytesAsync(filePath, ct).ConfigureAwait(false);
                result.IsReadable = bytes.Length == result.FileSize;
                result.HeaderInfo = RomFileIntegrityAnalyzer.AnalyzeRomHeader(bytes, Path.GetExtension(filePath));
                result.IsValidFormat = result.HeaderInfo?.IsValidHeader ?? true;
            }
            catch (Exception ex)
            {
                result.IsReadable = false;
                result.ReadErrors.Add($"Read error: {ex.Message}");
            }

            result.IsIntact = result.IsReadable && result.IsValidFormat;
            return Result<FileIntegrityResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to verify file integrity: {FilePath}", filePath);
            return Result<FileIntegrityResult>.Failure($"Integrity check failed: {ex.Message}");
        }
    }
}
