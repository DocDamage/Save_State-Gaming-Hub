using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Services;

/// <summary>
/// Service for managing community verification data for memory signatures.
/// </summary>
public class CommunityVerificationData : ICommunityVerificationData
{
    private readonly ILogger<CommunityVerificationData> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly string _dataDirectory;
    private readonly ConcurrentDictionary<string, List<VerificationReport>> _reportsCache;
    private readonly SemaphoreSlim _saveLock = new(1, 1);

    public CommunityVerificationData(
        ILogger<CommunityVerificationData> logger,
        ITimeProvider timeProvider,
        string? dataDirectory = null)
    {
        _logger = logger;
        _timeProvider = timeProvider;
        _dataDirectory = dataDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SaveStateReborn",
            "CommunityVerification");
        _reportsCache = new ConcurrentDictionary<string, List<VerificationReport>>();

        EnsureDirectoryExists();
        LoadExistingData();
    }

    /// <inheritdoc />
    public Task<Result> ReportWorkingAsync(
        string signatureId,
        string gameVersion,
        string? userId = null,
        CancellationToken ct = default)
    {
        return AddReportAsync(new VerificationReport
        {
            SignatureId = signatureId,
            GameVersion = gameVersion,
            IsWorking = true,
            ReportedAt = _timeProvider.UtcNow,
            UserId = userId ?? Guid.NewGuid().ToString("N")[..8],
            Reason = null
        }, ct);
    }

    /// <inheritdoc />
    public Task<Result> ReportBrokenAsync(
        string signatureId,
        string gameVersion,
        string reason,
        string? userId = null,
        CancellationToken ct = default)
    {
        return AddReportAsync(new VerificationReport
        {
            SignatureId = signatureId,
            GameVersion = gameVersion,
            IsWorking = false,
            ReportedAt = _timeProvider.UtcNow,
            UserId = userId ?? Guid.NewGuid().ToString("N")[..8],
            Reason = reason
        }, ct);
    }

    /// <inheritdoc />
    public Task<Result<CommunityStats>> GetStatsAsync(
        string signatureId,
        CancellationToken ct = default)
    {
        var reports = GetReportsForSignature(signatureId);

        var stats = new CommunityStats
        {
            SignatureId = signatureId,
            TotalReports = reports.Count,
            WorkingReports = reports.Count(r => r.IsWorking),
            BrokenReports = reports.Count(r => !r.IsWorking),
            LastReportedAt = reports.Any() ? reports.Max(r => r.ReportedAt) : null,
            ReportsByVersion = reports
                .GroupBy(r => r.GameVersion)
                .ToDictionary(
                    g => g.Key,
                    g => new VersionStats
                    {
                        Working = g.Count(r => r.IsWorking),
                        Broken = g.Count(r => !r.IsWorking)
                    }),
            RecentReports = reports
                .OrderByDescending(r => r.ReportedAt)
                .Take(10)
                .Select(r => new RecentReport
                {
                    IsWorking = r.IsWorking,
                    GameVersion = r.GameVersion,
                    ReportedAt = r.ReportedAt,
                    Reason = r.Reason
                })
                .ToList()
        };

        return Task.FromResult(Result.Success(stats));
    }

    /// <inheritdoc />
    public Task<Result<Dictionary<string, double>>> GetSuccessRatesAsync(
        CancellationToken ct = default)
    {
        var rates = new Dictionary<string, double>();

        foreach (var signatureId in _reportsCache.Keys)
        {
            var reports = GetReportsForSignature(signatureId);
            if (reports.Any())
            {
                rates[signatureId] = (double)reports.Count(r => r.IsWorking) / reports.Count;
            }
        }

        return Task.FromResult(Result.Success(rates));
    }

    /// <inheritdoc />
    public Task<Result<List<string>>> GetTopWorkingSignaturesAsync(
        int count = 10,
        CancellationToken ct = default)
    {
        var workingRates = _reportsCache
            .Select(kvp => new
            {
                SignatureId = kvp.Key,
                SuccessRate = kvp.Value.Any()
                    ? (double)kvp.Value.Count(r => r.IsWorking) / kvp.Value.Count
                    : 0
            })
            .Where(x => x.SuccessRate >= 0.8)
            .OrderByDescending(x => x.SuccessRate)
            .Take(count)
            .Select(x => x.SignatureId)
            .ToList();

        return Task.FromResult(Result.Success(workingRates));
    }

    /// <inheritdoc />
    public Task<Result<List<string>>> GetBrokenSignaturesAsync(
        CancellationToken ct = default)
    {
        var broken = _reportsCache
            .Where(kvp =>
            {
                var recentReports = kvp.Value
                    .Where(r => r.ReportedAt > _timeProvider.UtcNow.AddDays(-30))
                    .ToList();

                if (recentReports.Count < 3) return false;

                var failureRate = (double)recentReports.Count(r => !r.IsWorking) / recentReports.Count;
                return failureRate >= 0.7;
            })
            .Select(kvp => kvp.Key)
            .ToList();

        return Task.FromResult(Result.Success(broken));
    }

    /// <inheritdoc />
    public async Task<Result> MigrateReportsAsync(
        string oldSignatureId,
        string newSignatureId,
        CancellationToken ct = default)
    {
        if (_reportsCache.TryRemove(oldSignatureId, out var reports))
        {
            foreach (var report in reports)
            {
                report.SignatureId = newSignatureId;
                report.MigratedFrom = oldSignatureId;
            }

            var newReports = _reportsCache.GetOrAdd(newSignatureId, _ => new List<VerificationReport>());
            newReports.AddRange(reports);

            await SaveReportsAsync(newSignatureId, ct);

            _logger.LogInformation(
                "Migrated {Count} reports from {OldId} to {NewId}",
                reports.Count, oldSignatureId, newSignatureId);
        }

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> ClearOldReportsAsync(
        TimeSpan olderThan,
        CancellationToken ct = default)
    {
        var cutoff = _timeProvider.UtcNow.Subtract(olderThan);
        var clearedCount = 0;

        foreach (var signatureId in _reportsCache.Keys)
        {
            var reports = GetReportsForSignature(signatureId);
            var oldCount = reports.Count;
            reports.RemoveAll(r => r.ReportedAt < cutoff);
            clearedCount += oldCount - reports.Count;

            if (reports.Count != oldCount)
            {
                await SaveReportsAsync(signatureId, ct);
            }
        }

        _logger.LogInformation("Cleared {Count} old verification reports", clearedCount);
        return Result.Success();
    }

    /// <inheritdoc />
    public Task<Result<int>> GetTotalReportCountAsync(CancellationToken ct = default)
    {
        var count = _reportsCache.Values.Sum(r => r.Count);
        return Task.FromResult(Result.Success(count));
    }

    #region Private Methods

    private async Task<Result> AddReportAsync(VerificationReport report, CancellationToken ct)
    {
        try
        {
            var reports = _reportsCache.GetOrAdd(report.SignatureId, _ => new List<VerificationReport>());

            lock (reports)
            {
                // Limit reports per signature to prevent unbounded growth
                if (reports.Count >= 1000)
                {
                    reports.RemoveAt(0);
                }

                reports.Add(report);
            }

            await SaveReportsAsync(report.SignatureId, ct);

            _logger.LogDebug(
                "Added verification report for {SignatureId}: Working={IsWorking}, Version={Version}",
                report.SignatureId, report.IsWorking, report.GameVersion);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding verification report");
            return Result.Failure($"Failed to add report: {ex.Message}");
        }
    }

    private List<VerificationReport> GetReportsForSignature(string signatureId)
    {
        return _reportsCache.GetValueOrDefault(signatureId, new List<VerificationReport>());
    }

    private async Task SaveReportsAsync(string signatureId, CancellationToken ct)
    {
        await _saveLock.WaitAsync(ct);
        try
        {
            var reports = GetReportsForSignature(signatureId);
            var filePath = GetFilePath(signatureId);

            var json = JsonSerializer.Serialize(reports, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            await File.WriteAllTextAsync(filePath, json, ct);
        }
        finally
        {
            _saveLock.Release();
        }
    }

    private void LoadExistingData()
    {
        try
        {
            if (!Directory.Exists(_dataDirectory))
            {
                return;
            }

            var files = Directory.GetFiles(_dataDirectory, "*.json");
            foreach (var file in files)
            {
                try
                {
                    var signatureId = Path.GetFileNameWithoutExtension(file);
                    var json = File.ReadAllText(file);
                    var reports = JsonSerializer.Deserialize<List<VerificationReport>>(json);

                    if (reports != null)
                    {
                        _reportsCache[signatureId] = reports;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error loading verification data from {File}", file);
                }
            }

            _logger.LogInformation(
                "Loaded verification data for {Count} signatures",
                _reportsCache.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading existing verification data");
        }
    }

    private void EnsureDirectoryExists()
    {
        if (!Directory.Exists(_dataDirectory))
        {
            Directory.CreateDirectory(_dataDirectory);
        }
    }

    private string GetFilePath(string signatureId)
    {
        // Sanitize signature ID for file system
        var safeId = string.Concat(
            signatureId.Split(Path.GetInvalidFileNameChars()))
            .Replace(" ", "_");

        return Path.Combine(_dataDirectory, $"{safeId}.json");
    }

    #endregion
}

/// <summary>
/// Interface for community verification data management.
/// </summary>
public interface ICommunityVerificationData
{
    /// <summary>
    /// Reports that a signature is working.
    /// </summary>
    Task<Result> ReportWorkingAsync(
        string signatureId,
        string gameVersion,
        string? userId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Reports that a signature is broken.
    /// </summary>
    Task<Result> ReportBrokenAsync(
        string signatureId,
        string gameVersion,
        string reason,
        string? userId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets statistics for a signature.
    /// </summary>
    Task<Result<CommunityStats>> GetStatsAsync(
        string signatureId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets success rates for all signatures.
    /// </summary>
    Task<Result<Dictionary<string, double>>> GetSuccessRatesAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Gets the top working signatures.
    /// </summary>
    Task<Result<List<string>>> GetTopWorkingSignaturesAsync(
        int count = 10,
        CancellationToken ct = default);

    /// <summary>
    /// Gets signatures that are reported as broken.
    /// </summary>
    Task<Result<List<string>>> GetBrokenSignaturesAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Migrates reports from one signature to another.
    /// </summary>
    Task<Result> MigrateReportsAsync(
        string oldSignatureId,
        string newSignatureId,
        CancellationToken ct = default);

    /// <summary>
    /// Clears old reports.
    /// </summary>
    Task<Result> ClearOldReportsAsync(
        TimeSpan olderThan,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the total number of reports.
    /// </summary>
    Task<Result<int>> GetTotalReportCountAsync(CancellationToken ct = default);
}

/// <summary>
/// Internal verification report record.
/// </summary>
public class VerificationReport
{
    public string SignatureId { get; set; } = "";
    public string GameVersion { get; set; } = "";
    public bool IsWorking { get; set; }
    public DateTime ReportedAt { get; set; }
    public string UserId { get; set; } = "";
    public string? Reason { get; set; }
    public string? MigratedFrom { get; set; }
}

/// <summary>
/// Community statistics for a signature.
/// </summary>
public class CommunityStats
{
    public string SignatureId { get; set; } = "";
    public int TotalReports { get; set; }
    public int WorkingReports { get; set; }
    public int BrokenReports { get; set; }
    public DateTime? LastReportedAt { get; set; }
    public Dictionary<string, VersionStats> ReportsByVersion { get; set; } = new();
    public List<RecentReport> RecentReports { get; set; } = new();

    public double SuccessRate => TotalReports > 0
        ? (double)WorkingReports / TotalReports
        : 0;
}

/// <summary>
/// Statistics for a specific version.
/// </summary>
public class VersionStats
{
    public int Working { get; set; }
    public int Broken { get; set; }
    public double SuccessRate => (Working + Broken) > 0
        ? (double)Working / (Working + Broken)
        : 0;
}

/// <summary>
/// Recent report information.
/// </summary>
public class RecentReport
{
    public bool IsWorking { get; set; }
    public string GameVersion { get; set; } = "";
    public DateTime ReportedAt { get; set; }
    public string? Reason { get; set; }
}
