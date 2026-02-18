using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace SaveState.Benchmarks;

/// <summary>
/// Benchmarks for save state operations.
/// </summary>
[SimpleJob(RuntimeMoniker.Net90, launchCount: 1, warmupCount: 3, iterationCount: 5)]
[MemoryDiagnoser]
[RankColumn]
public class SaveStateBenchmarks
{
    private byte[] _saveData = null!;
    private string _savePath = null!;

    [Params(1024 * 1024, 10 * 1024 * 1024, 50 * 1024 * 1024)] // 1MB, 10MB, 50MB
    public int FileSize { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _saveData = new byte[FileSize];
        Random.Shared.NextBytes(_saveData);
        
        _savePath = Path.Combine(Path.GetTempPath(), $"benchmark_save_{FileSize}.dat");
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        if (File.Exists(_savePath))
        {
            File.Delete(_savePath);
        }
    }

    [Benchmark]
    public async Task SaveToDiskAsync()
    {
        await File.WriteAllBytesAsync(_savePath, _saveData);
    }

    [Benchmark]
    public async Task LoadFromDiskAsync()
    {
        await File.ReadAllBytesAsync(_savePath);
    }

    [Benchmark]
    public byte[] CompressData()
    {
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionLevel.Optimal))
        {
            gzip.Write(_saveData, 0, _saveData.Length);
        }
        return output.ToArray();
    }

    [Benchmark]
    public byte[] DecompressData()
    {
        var compressed = CompressData();
        
        using var input = new MemoryStream(compressed);
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(input, CompressionMode.Decompress))
        {
            gzip.CopyTo(output);
        }
        return output.ToArray();
    }

    [Benchmark]
    public string CalculateHash_MD5()
    {
        using var md5 = MD5.Create();
        var hash = md5.ComputeHash(_saveData);
        return Convert.ToHexString(hash);
    }

    [Benchmark]
    public string CalculateHash_SHA256()
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(_saveData);
        return Convert.ToHexString(hash);
    }

    [Benchmark]
    public string CalculateHash_SHA512()
    {
        using var sha512 = SHA512.Create();
        var hash = sha512.ComputeHash(_saveData);
        return Convert.ToHexString(hash);
    }
}
