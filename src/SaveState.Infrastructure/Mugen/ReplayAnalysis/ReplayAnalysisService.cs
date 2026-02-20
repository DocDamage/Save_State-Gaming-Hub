using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Mugen.ReplayAnalysis;
using SaveState.Core.Mugen.ReplayAnalysis.Services;
using SaveState.Infrastructure.Mugen.Coaching.ReplayAnalysis;
using SaveState.Infrastructure.Mugen.ReplayAnalysis.Managers;
using SaveState.Infrastructure.Persistence;

// Alias to disambiguate the entity type from the namespace
using ReplayAnalysisEntity = SaveState.Core.Mugen.ReplayAnalysis.ReplayAnalysis;

namespace SaveState.Infrastructure.Mugen.ReplayAnalysisServices;

/// <summary>
/// Service for analyzing fighting game replays and generating highlights.
/// Acts as a thin coordinator delegating to specialized managers.
/// </summary>
public class ReplayAnalysisService : IReplayAnalysisService
{
    private readonly SaveStateDbContext _dbContext;
    private readonly ILogger<ReplayAnalysisService> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly ReplayParsingManager _parsingManager;
    private readonly HighlightReelManager _reelManager;
    private readonly QueryManager _queryManager;
    private readonly ComparisonManager _comparisonManager;

    public ReplayAnalysisService(
        SaveStateDbContext dbContext,
        ILogger<ReplayAnalysisService> logger,
        ITimeProvider timeProvider,
        ReplayParsingManager parsingManager,
        HighlightReelManager reelManager,
        QueryManager queryManager,
        ComparisonManager comparisonManager)
    {
        _dbContext = dbContext;
        _logger = logger;
        _timeProvider = timeProvider;
        _parsingManager = parsingManager;
        _reelManager = reelManager;
        _queryManager = queryManager;
        _comparisonManager = comparisonManager;
    }

    public async Task<Result<ReplayAnalysisEntity>> AnalyzeReplayAsync(
        ReplayAnalysisRequest request,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Starting replay analysis for {FilePath}", request.ReplayFilePath);

            // Validate file exists
            if (!File.Exists(request.ReplayFilePath))
            {
                return Result<ReplayAnalysisEntity>.Failure(
                    $"Replay file not found: {request.ReplayFilePath}",
                    ErrorType.NotFound);
            }

            // Calculate file hash for deduplication
            var fileHash = await _parsingManager.CalculateFileHashAsync(request.ReplayFilePath, ct);

            // Check if already analyzed
            var existing = await _dbContext.ReplayAnalyses
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.FileHash == fileHash, ct);

            if (existing != null)
            {
                _logger.LogInformation("Replay already analyzed with ID {AnalysisId}", existing.Id);
                return Result<ReplayAnalysisEntity>.Success(existing);
            }

            // Parse replay file
            var (metadata, events) = await _parsingManager.ParseReplayFileAsync(request.ReplayFilePath, ct);

            // Create analysis
            var analysis = new ReplayAnalysisEntity
            {
                ReplayFilePath = request.ReplayFilePath,
                Name = request.Name ?? Path.GetFileNameWithoutExtension(request.ReplayFilePath),
                Description = request.Description,
                Platform = metadata.Game ?? "MUGEN",
                ReplayDate = metadata.RecordedAt?.DateTime ?? File.GetCreationTime(request.ReplayFilePath),
                AnalyzedAt = _timeProvider.UtcNow,
                Duration = metadata.Duration ?? TimeSpan.FromSeconds(events.Count / 60.0),
                Player1Character = metadata.Player1 ?? "Unknown",
                Player2Character = metadata.Player2 ?? "Unknown",
                Player1Name = metadata.Player1,
                Player2Name = metadata.Player2,
                Winner = ReplayParsingManager.ParseWinner(metadata.Winner),
                TotalFrames = events.Count > 0 ? events.Max(e => e.Frame ?? 0) : 0,
                FileHash = fileHash,
                AnalysisVersion = "1.0"
            };

            var options = request.Options ?? new ReplayAnalysisOptions();

            // Detect combos
            if (options.DetectCombos)
            {
                analysis.Combos = ComboDetectionManager.DetectCombos(events, options.MinComboHits, options.MinComboDamage);
                _logger.LogInformation("Detected {ComboCount} combos", analysis.Combos.Count);
            }

            // Calculate stats
            (analysis.Player1Stats, analysis.Player2Stats) = StatisticsManager.CalculateCombatStats(events, analysis.Combos);

            // Detect comebacks
            if (options.DetectComebacks)
            {
                analysis.Comebacks = StatisticsManager.DetectComebacks(events, analysis.Duration);
                _logger.LogInformation("Detected {ComebackCount} comebacks", analysis.Comebacks.Count);
            }

            // Generate highlights
            if (options.GenerateHighlights)
            {
                analysis.Highlights = ComparisonManager.GenerateHighlights(analysis, events, options.MinHighlightIntensity);
                _logger.LogInformation("Generated {HighlightCount} highlights", analysis.Highlights.Count);
            }

            // Capture frame data if requested
            if (options.CaptureFrameData)
            {
                analysis.FrameData = ComparisonManager.CaptureFrameData(events);
            }

            // Save to database
            _dbContext.ReplayAnalyses.Add(analysis);
            await _dbContext.SaveChangesAsync(ct);

            _logger.LogInformation("Replay analysis completed with ID {AnalysisId}", analysis.Id);
            return Result<ReplayAnalysisEntity>.Success(analysis);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze replay {FilePath}", request.ReplayFilePath);
            return Result<ReplayAnalysisEntity>.Failure(
                $"Analysis failed: {ex.Message}",
                ErrorType.Internal);
        }
    }

    public Task<Result<ReplayAnalysisEntity>> GetAnalysisAsync(Guid analysisId, CancellationToken ct = default)
        => _queryManager.GetAnalysisAsync(analysisId, ct);

    public Task<Result<List<ReplayAnalysisSummary>>> GetAnalysesAsync(
        ReplayAnalysisFilter? filter = null,
        CancellationToken ct = default)
        => _queryManager.GetAnalysesAsync(filter, ct);

    public Task<Result> DeleteAnalysisAsync(Guid analysisId, CancellationToken ct = default)
        => _queryManager.DeleteAnalysisAsync(analysisId, ct);

    public Task<Result<List<DetectedCombo>>> GetCombosAsync(
        Guid analysisId,
        int? player = null,
        int? minHits = null,
        CancellationToken ct = default)
        => _queryManager.GetCombosAsync(analysisId, player, minHits, ct);

    public Task<Result<List<HighlightMoment>>> GetHighlightsAsync(
        Guid analysisId,
        HighlightType? type = null,
        int? minIntensity = null,
        CancellationToken ct = default)
        => _queryManager.GetHighlightsAsync(analysisId, type, minIntensity, ct);

    public Task<Result<List<ComebackMoment>>> GetComebacksAsync(
        Guid analysisId,
        ComebackSeverity? minSeverity = null,
        CancellationToken ct = default)
        => _queryManager.GetComebacksAsync(analysisId, minSeverity, ct);

    public Task<Result<ReplayComparison>> CompareReplaysAsync(
        Guid analysisId1,
        Guid analysisId2,
        CancellationToken ct = default)
        => _comparisonManager.CompareReplaysAsync(analysisId1, analysisId2, ct);

    public Task<Result<HighlightReel>> GenerateHighlightReelAsync(
        Guid analysisId,
        List<Guid> highlightIds,
        HighlightReelOptions options,
        CancellationToken ct = default)
        => _reelManager.GenerateHighlightReelAsync(analysisId, highlightIds, options, ct);

    public async Task<Result<HighlightReel>> AutoGenerateHighlightReelAsync(
        Guid analysisId,
        int maxDurationSeconds = 60,
        CancellationToken ct = default)
    {
        var highlightsResult = await _queryManager.GetHighlightsAsync(analysisId, null, 70, ct);

        if (highlightsResult.IsFailure)
        {
            return Result<HighlightReel>.Failure(highlightsResult.Error!, highlightsResult.ErrorType);
        }

        return await _reelManager.AutoGenerateHighlightReelAsync(
            analysisId,
            highlightsResult.Value!,
            maxDurationSeconds,
            ct);
    }

    public Task<Result<string>> ExportHighlightReelAsync(
        Guid reelId,
        string outputPath,
        ExportFormat format,
        CancellationToken ct = default)
        => _reelManager.ExportHighlightReelAsync(reelId, outputPath, format, ct);

    public async Task<Result<ComboStatistics>> GetComboStatisticsAsync(
        string character,
        int? minReplays = null,
        CancellationToken ct = default)
    {
        try
        {
            var analyses = await _dbContext.ReplayAnalyses
                .AsNoTracking()
                .Where(r => r.Player1Character == character || r.Player2Character == character)
                .ToListAsync(ct);

            var allCombos = analyses
                .SelectMany(r => r.Combos)
                .Where(c => c.Character == character)
                .ToList();

            var stats = new ComboStatistics
            {
                Character = character,
                TotalReplaysAnalyzed = analyses.Count,
                TotalCombosFound = allCombos.Count,
                LongestComboHits = allCombos.Any() ? allCombos.Max(c => c.HitCount) : 0,
                AverageComboHits = allCombos.Any() ? (decimal)allCombos.Average(c => c.HitCount) : 0,
                HighestComboDamage = allCombos.Any() ? allCombos.Max(c => c.TotalDamage) : 0,
                AverageComboDamage = allCombos.Any() ? (decimal)allCombos.Average(c => c.TotalDamage) : 0,
                TopCombos = allCombos.OrderByDescending(c => c.QualityScore).Take(10).ToList(),
                DifficultyDistribution = allCombos
                    .GroupBy(c => c.Difficulty)
                    .ToDictionary(g => g.Key, g => g.Count())
            };

            return Result<ComboStatistics>.Success(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get combo statistics for {Character}", character);
            return Result<ComboStatistics>.Failure(
                $"Statistics query failed: {ex.Message}",
                ErrorType.Internal);
        }
    }

    public Task<Result> TagAnalysisAsync(
        Guid analysisId,
        List<string> tags,
        CancellationToken ct = default)
        => _queryManager.TagAnalysisAsync(analysisId, tags, ct);

    public Task<Result<List<ReplayAnalysisSummary>>> FindSimilarReplaysAsync(
        Guid analysisId,
        int maxResults = 10,
        CancellationToken ct = default)
        => _comparisonManager.FindSimilarReplaysAsync(analysisId, maxResults, ct);

    public Task<Result<ReplayFileInfo>> ValidateReplayFileAsync(
        string filePath,
        CancellationToken ct = default)
        => _comparisonManager.ValidateReplayFileAsync(filePath, ct);

    public Task<Result<List<FrameSnapshot>>> GetFrameRangeAsync(
        Guid analysisId,
        int startFrame,
        int endFrame,
        CancellationToken ct = default)
        => _queryManager.GetFrameRangeAsync(analysisId, startFrame, endFrame, ct);
}
