using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Mugen.ReplayAnalysis;
using SaveState.Core.Mugen.ReplayAnalysis.Services;
using SaveState.Infrastructure.Mugen.Coaching.ReplayAnalysis;
using SaveState.Infrastructure.Persistence;
using ReplayAnalysisEntity = SaveState.Core.Mugen.ReplayAnalysis.ReplayAnalysis;

namespace SaveState.Infrastructure.Mugen.ReplayAnalysis.Managers;

/// <summary>
/// Manager for comparing replays, finding similar matchups, and validation operations.
/// </summary>
public class ComparisonManager
{
    private readonly SaveStateDbContext _dbContext;
    private readonly ILogger<ComparisonManager> _logger;
    private readonly IReplayParsingEngine _parsingEngine;

    public ComparisonManager(
        SaveStateDbContext dbContext,
        ILogger<ComparisonManager> logger,
        IReplayParsingEngine parsingEngine)
    {
        _dbContext = dbContext;
        _logger = logger;
        _parsingEngine = parsingEngine;
    }

    /// <summary>
    /// Compares two replay analyses and generates a comparison report.
    /// </summary>
    public async Task<Result<ReplayComparison>> CompareReplaysAsync(
        Guid analysisId1,
        Guid analysisId2,
        CancellationToken ct = default)
    {
        try
        {
            var analysis1 = await _dbContext.ReplayAnalyses
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == analysisId1, ct);

            var analysis2 = await _dbContext.ReplayAnalyses
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == analysisId2, ct);

            if (analysis1 == null)
            {
                return Result<ReplayComparison>.Failure(
                    $"Analysis {analysisId1} not found",
                    ErrorType.NotFound);
            }

            if (analysis2 == null)
            {
                return Result<ReplayComparison>.Failure(
                    $"Analysis {analysisId2} not found",
                    ErrorType.NotFound);
            }

            var comparison = new ReplayComparison
            {
                Replay1 = analysis1,
                Replay2 = analysis2,
                Improvements = new List<string>(),
                Regressions = new List<string>(),
                Analysis = GenerateComparisonAnalysis(analysis1, analysis2)
            };

            return Result<ReplayComparison>.Success(comparison);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to compare replays");
            return Result<ReplayComparison>.Failure(
                $"Comparison failed: {ex.Message}",
                ErrorType.Internal);
        }
    }

    /// <summary>
    /// Finds similar replays based on character matchup.
    /// </summary>
    public async Task<Result<List<ReplayAnalysisSummary>>> FindSimilarReplaysAsync(
        Guid analysisId,
        int maxResults = 10,
        CancellationToken ct = default)
    {
        try
        {
            var source = await _dbContext.ReplayAnalyses
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == analysisId, ct);

            if (source == null)
            {
                return Result<List<ReplayAnalysisSummary>>.Failure(
                    $"Analysis {analysisId} not found",
                    ErrorType.NotFound);
            }

            // Find replays with same character matchup
            var similar = await _dbContext.ReplayAnalyses
                .AsNoTracking()
                .Where(r => r.Id != analysisId &&
                           ((r.Player1Character == source.Player1Character &&
                             r.Player2Character == source.Player2Character) ||
                            (r.Player1Character == source.Player2Character &&
                             r.Player2Character == source.Player1Character)))
                .OrderByDescending(r => r.AnalyzedAt)
                .Take(maxResults)
                .Select(r => new ReplayAnalysisSummary
                {
                    AnalysisId = r.Id,
                    Name = r.Name,
                    Player1Character = r.Player1Character,
                    Player2Character = r.Player2Character,
                    Duration = r.Duration,
                    TotalCombos = r.Combos.Count,
                    LongestComboHits = r.Combos.Any() ? r.Combos.Max(c => c.HitCount) : 0,
                    HighestComboDamage = r.Combos.Any() ? r.Combos.Max(c => c.TotalDamage) : 0,
                    HighlightCount = r.Highlights.Count,
                    ComebackCount = r.Comebacks.Count,
                    AnalyzedAt = r.AnalyzedAt,
                    Tags = r.Tags
                })
                .ToListAsync(ct);

            return Result<List<ReplayAnalysisSummary>>.Success(similar);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to find similar replays");
            return Result<List<ReplayAnalysisSummary>>.Failure(
                $"Search failed: {ex.Message}",
                ErrorType.Internal);
        }
    }

    /// <summary>
    /// Validates a replay file and returns file information.
    /// </summary>
    public async Task<Result<ReplayFileInfo>> ValidateReplayFileAsync(
        string filePath,
        CancellationToken ct = default)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                return Result<ReplayFileInfo>.Failure(
                    $"File not found: {filePath}",
                    ErrorType.NotFound);
            }

            var fileInfo = new FileInfo(filePath);
            var hash = await CalculateFileHashAsync(filePath, ct);

            // Try to parse to get metadata
            var (metadata, _) = await ParseReplayFileAsync(filePath, ct);

            var info = new ReplayFileInfo
            {
                FilePath = filePath,
                FileName = fileInfo.Name,
                FileSize = fileInfo.Length,
                FileHash = hash,
                FileDate = fileInfo.CreationTime,
                IsValid = true,
                Platform = metadata.Game ?? "Unknown",
                // Version not available in ReplayMetadata
                Duration = metadata.Duration,
                Player1Character = metadata.Player1,
                Player2Character = metadata.Player2,
                SupportedFormats = new List<string> { ".json", ".txt", ".rep", ".mrg" }
            };

            return Result<ReplayFileInfo>.Success(info);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate replay file {FilePath}", filePath);
            return Result<ReplayFileInfo>.Success(new ReplayFileInfo
            {
                FilePath = filePath,
                FileName = Path.GetFileName(filePath),
                IsValid = false,
                ValidationError = ex.Message,
                SupportedFormats = new List<string> { ".json", ".txt", ".rep", ".mrg" }
            });
        }
    }

    /// <summary>
    /// Generates highlight moments from combo and comeback data.
    /// </summary>
    public static List<HighlightMoment> GenerateHighlights(
        ReplayAnalysisEntity analysis,
        List<ReplayEvent> events,
        int minIntensity)
    {
        var highlights = new List<HighlightMoment>();

        // Add combo highlights
        foreach (var combo in analysis.Combos.Where(c => c.QualityScore >= minIntensity))
        {
            highlights.Add(new HighlightMoment
            {
                Type = combo.HitCount >= 15 ? HighlightType.Combo : HighlightType.Stylish,
                Description = $"{combo.HitCount}-hit combo for {combo.TotalDamage} damage",
                StartFrame = combo.StartFrame,
                EndFrame = combo.EndFrame,
                PrimaryPlayer = combo.Player,
                Character = combo.Character,
                IntensityScore = combo.QualityScore,
                Metadata = new Dictionary<string, object>
                {
                    ["hitCount"] = combo.HitCount,
                    ["damage"] = combo.TotalDamage,
                    ["difficulty"] = combo.Difficulty.ToString()
                }
            });
        }

        // Add comeback highlights
        foreach (var comeback in analysis.Comebacks)
        {
            highlights.Add(new HighlightMoment
            {
                Type = HighlightType.Comeback,
                Description = $"Amazing comeback from {comeback.LowestLifePercentage:F0}% health",
                StartFrame = comeback.ComebackStartFrame,
                EndFrame = comeback.ComebackEndFrame,
                PrimaryPlayer = comeback.Player,
                Character = comeback.Character,
                IntensityScore = comeback.ComebackScore,
                Metadata = new Dictionary<string, object>
                {
                    ["severity"] = comeback.Severity.ToString(),
                    ["lifeRecovered"] = comeback.LifeRecovered
                }
            });
        }

        // Add perfect round highlights
        if (analysis.HasPerfectRound)
        {
            highlights.Add(new HighlightMoment
            {
                Type = HighlightType.PerfectRound,
                Description = "Perfect round - no damage taken!",
                StartFrame = 0,
                EndFrame = analysis.TotalFrames,
                IntensityScore = 95
            });
        }

        return highlights.OrderByDescending(h => h.IntensityScore).ToList();
    }

    /// <summary>
    /// Captures frame-by-frame data from replay events.
    /// </summary>
    public static List<FrameSnapshot> CaptureFrameData(List<ReplayEvent> events)
    {
        // Convert events to frame snapshots
        var snapshots = new List<FrameSnapshot>();
        var frameGroups = events.GroupBy(e => e.Frame ?? 0).OrderBy(g => g.Key);

        foreach (var group in frameGroups)
        {
            var snapshot = new FrameSnapshot
            {
                FrameNumber = group.Key,
                P1State = "Neutral",
                P2State = "Neutral"
            };

            foreach (var evt in group)
            {
                if (evt.PlayerIndex == 1)
                {
                    snapshot.P1CurrentMove = evt.Move;
                    snapshot.P1IsAttacking = evt.Type == ReplayEventType.Hit || evt.Type == ReplayEventType.Move;
                    snapshot.P1IsHit = evt.Type == ReplayEventType.Hit;
                }
                else if (evt.PlayerIndex == 2)
                {
                    snapshot.P2CurrentMove = evt.Move;
                    snapshot.P2IsAttacking = evt.Type == ReplayEventType.Hit || evt.Type == ReplayEventType.Move;
                    snapshot.P2IsHit = evt.Type == ReplayEventType.Hit;
                }
            }

            snapshots.Add(snapshot);
        }

        return snapshots;
    }

    /// <summary>
    /// Generates a text analysis comparing two replays.
    /// </summary>
    public static string GenerateComparisonAnalysis(ReplayAnalysisEntity r1, ReplayAnalysisEntity r2)
    {
        var analysis = new System.Text.StringBuilder();

        analysis.AppendLine($"Comparing '{r1.Name}' vs '{r2.Name}'");
        analysis.AppendLine();

        if (r1.Combos.Count > r2.Combos.Count)
            analysis.AppendLine($"✓ Replay 1 has {r1.Combos.Count - r2.Combos.Count} more combos");
        else if (r2.Combos.Count > r1.Combos.Count)
            analysis.AppendLine($"✗ Replay 2 has {r2.Combos.Count - r1.Combos.Count} more combos");

        var longest1 = r1.LongestCombo?.HitCount ?? 0;
        var longest2 = r2.LongestCombo?.HitCount ?? 0;
        if (longest1 > longest2)
            analysis.AppendLine($"✓ Replay 1 has longer max combo ({longest1} vs {longest2} hits)");
        else if (longest2 > longest1)
            analysis.AppendLine($"✗ Replay 2 has longer max combo ({longest2} vs {longest1} hits)");

        return analysis.ToString();
    }

    // Helper methods

    private async Task<string> CalculateFileHashAsync(string filePath, CancellationToken ct)
    {
        await using var stream = File.OpenRead(filePath);
        var hash = await System.Security.Cryptography.SHA256.HashDataAsync(stream, ct);
        return Convert.ToHexString(hash);
    }

    private async Task<(ReplayMetadata Metadata, List<ReplayEvent> Events)> ParseReplayFileAsync(
        string filePath,
        CancellationToken ct)
    {
        var metadata = new ReplayMetadata();
        var events = new List<ReplayEvent>();

        var content = await File.ReadAllTextAsync(filePath, ct);
        var extension = Path.GetExtension(filePath).ToLowerInvariant();

        if (extension == ".json" || content.TrimStart().StartsWith("{"))
        {
            _parsingEngine.ParseJsonReplay(content, metadata, events);
        }
        else
        {
            _parsingEngine.ParseTextReplay(content, metadata, events);
        }

        return (metadata, events);
    }
}
