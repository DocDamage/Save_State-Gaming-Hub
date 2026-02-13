using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.SaveStates.Services;

namespace SaveState.Infrastructure.SaveStates;

/// <summary>
/// AES-based client-side encryption for cloud save state payloads.
/// </summary>
public sealed class CloudSaveEncryptionService : ICloudSaveEncryptionService
{
    private const int IvSizeBytes = 16;
    private readonly ILogger<CloudSaveEncryptionService> _logger;

    public CloudSaveEncryptionService(ILogger<CloudSaveEncryptionService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<string>> EncryptFileAsync(
        string sourceFilePath,
        string encryptionKey,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(sourceFilePath) || !File.Exists(sourceFilePath))
        {
            return Result.Failure<string>("Source file for encryption was not found.", ErrorType.NotFound);
        }

        if (string.IsNullOrWhiteSpace(encryptionKey))
        {
            return Result.Failure<string>("Encryption key is required.", ErrorType.Validation);
        }

        var outputPath = Path.Combine(Path.GetTempPath(), $"savestate-cloud-{Guid.NewGuid():N}.enc");

        try
        {
            var key = SHA256.HashData(Encoding.UTF8.GetBytes(encryptionKey));
            var iv = RandomNumberGenerator.GetBytes(IvSizeBytes);

            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            await using var input = File.OpenRead(sourceFilePath);
            await using var output = File.Create(outputPath);
            await output.WriteAsync(iv, ct).ConfigureAwait(false);

            await using var crypto = new CryptoStream(
                output,
                aes.CreateEncryptor(),
                CryptoStreamMode.Write,
                leaveOpen: true);

            await input.CopyToAsync(crypto, ct).ConfigureAwait(false);
            crypto.FlushFinalBlock();

            return Result.Success(outputPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to encrypt file {Path}", sourceFilePath);
            TryDelete(outputPath);
            return Result.Failure<string>($"Failed to encrypt file: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result<string>> DecryptFileAsync(
        string encryptedFilePath,
        string encryptionKey,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(encryptedFilePath) || !File.Exists(encryptedFilePath))
        {
            return Result.Failure<string>("Encrypted file was not found.", ErrorType.NotFound);
        }

        if (string.IsNullOrWhiteSpace(encryptionKey))
        {
            return Result.Failure<string>("Encryption key is required.", ErrorType.Validation);
        }

        var outputPath = Path.Combine(Path.GetTempPath(), $"savestate-cloud-{Guid.NewGuid():N}.bin");

        try
        {
            await using var input = File.OpenRead(encryptedFilePath);
            var iv = new byte[IvSizeBytes];
            var bytesRead = await input.ReadAsync(iv, ct).ConfigureAwait(false);
            if (bytesRead != IvSizeBytes)
            {
                return Result.Failure<string>("Encrypted payload is invalid.", ErrorType.Validation);
            }

            var key = SHA256.HashData(Encoding.UTF8.GetBytes(encryptionKey));
            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            await using var output = File.Create(outputPath);
            await using var crypto = new CryptoStream(input, aes.CreateDecryptor(), CryptoStreamMode.Read);
            await crypto.CopyToAsync(output, ct).ConfigureAwait(false);

            return Result.Success(outputPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decrypt file {Path}", encryptedFilePath);
            TryDelete(outputPath);
            return Result.Failure<string>($"Failed to decrypt file: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public string GetKeyFingerprint(string encryptionKey)
    {
        if (string.IsNullOrWhiteSpace(encryptionKey))
        {
            return string.Empty;
        }

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(encryptionKey));
        return Convert.ToHexString(hash.AsSpan(0, 8));
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Best effort cleanup.
        }
    }
}
