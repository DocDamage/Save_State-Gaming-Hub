using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;

namespace SaveState.Infrastructure.RomManagement;

/// <summary>
/// Calculates multiple hashes in parallel for improved performance.
/// </summary>
[SuppressMessage("Security", "CA5350:DoNotUseWeakCryptographicAlgorithms", Justification = "SHA1 required for No-Intro/Redump ROM database compatibility")]
[SuppressMessage("Security", "CA5351:DoNotUseBrokenCryptographicAlgorithms", Justification = "MD5 required for No-Intro/Redump ROM database compatibility")]
public class ParallelHashCalculator
{
    /// <summary>
    /// Calculates CRC32, MD5, and SHA1 hashes in parallel.
    /// </summary>
    public static ParallelHashResult CalculateHashes(byte[] data)
    {
        var result = new ParallelHashResult();

        // Use parallel processing for multiple hash algorithms
        Parallel.Invoke(
            () => result.Crc32 = CalculateCrc32(data),
            () => result.Md5 = CalculateMd5(data),
            () => result.Sha1 = CalculateSha1(data)
        );

        return result;
    }

    /// <summary>
    /// Calculates all hashes including SHA256.
    /// </summary>
    public static ParallelHashResult CalculateAllHashes(byte[] data)
    {
        var result = new ParallelHashResult();

        Parallel.Invoke(
            () => result.Crc32 = CalculateCrc32(data),
            () => result.Md5 = CalculateMd5(data),
            () => result.Sha1 = CalculateSha1(data),
            () => result.Sha256 = CalculateSha256(data)
        );

        return result;
    }

    /// <summary>
    /// Calculates hashes for a stream using buffered reading.
    /// </summary>
    public static async Task<ParallelHashResult> CalculateHashesAsync(Stream stream, CancellationToken ct = default)
    {
        var result = new ParallelHashResult();

        // Read stream into memory for parallel processing
        // For large files, we might want to process in chunks
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream, ct);
        var data = memoryStream.ToArray();

        return await Task.Run(() => CalculateHashes(data), ct);
    }

    private static string CalculateCrc32(byte[] data)
    {
        uint crc = 0xFFFFFFFF;
        foreach (var b in data)
        {
            crc ^= b;
            for (int i = 0; i < 8; i++)
            {
                crc = (crc >> 1) ^ (0xEDB88320 & ~(crc & 1));
            }
        }
        return (~crc).ToString("X8").ToLowerInvariant();
    }

    private static string CalculateMd5(byte[] data)
    {
        using var md5 = MD5.Create();
        var hash = md5.ComputeHash(data);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }

    private static string CalculateSha1(byte[] data)
    {
        using var sha1 = SHA1.Create();
        var hash = sha1.ComputeHash(data);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }

    private static string CalculateSha256(byte[] data)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(data);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }
}

/// <summary>
/// Result of parallel hash calculation.
/// </summary>
public class ParallelHashResult
{
    public string? Crc32 { get; set; }
    public string? Md5 { get; set; }
    public string? Sha1 { get; set; }
    public string? Sha256 { get; set; }

    public bool HasAllHashes => !string.IsNullOrEmpty(Crc32) &&
                                !string.IsNullOrEmpty(Md5) &&
                                !string.IsNullOrEmpty(Sha1);
}
