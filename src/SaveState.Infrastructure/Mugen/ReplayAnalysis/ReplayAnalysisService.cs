using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Mugen.ReplayAnalysis;
using SaveState.Core.Mugen.ReplayAnalysis.Services;
using SaveState.Infrastructure.Mugen.Coaching.ReplayAnalysis;
using SaveState.Infrastructure.Persistence;

namespace SaveState.Infrastructure.Mugen.ReplayAnalysisServices;

/// <summary>
/// Service for analyzing fighting game replays and generating highlights.
/// </summary>
public class ReplayAnalysisService : IReplayAnalysisService
{
    private readonly SaveStateDbContext _dbContext;
    private readonly ILogger<ReplayAnalysisService> _logger;
    private readonly IReplayParsingEngine _parsingEngine;
    private readonly Dictionary<Guid, HighlightReel> _reels = new();

    public ReplayAnalysisService(
        SaveStateDbContext dbContext,
        ILogger<ReplayAnalysisService> logger,
        IReplayParsingEngine parsingEngine)
    {
        _dbContext = dbContext;
        _logger = logger;
        _parsingEngine = parsingEngine;
    }

    public async Task<Result<ReplayAnalysis>> AnalyzeReplayAsync(
        ReplayAnalysisRequest request, 
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Starting replay analysis for {FilePath}", request.ReplayFilePath);

            // Validate file exists
            if (!File.Exists(request.ReplayFilePath))
            {
                return Result<ReplayAnalysis>.Failure(
                    $"Replay file not found: {request.ReplayFilePath}", 
                    ErrorType.NotFound);
            }

            // Calculate file hash for deduplication
            var fileHash = await CalculateFileHashAsync(request.ReplayFilePath, ct);

            // Check if already analyzed
            var existing = await _dbContext.ReplayAnalyses
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.FileHash == fileHash, ct);

            if (existing != null)
            {
                _logger.LogInformation("Replay already analyzed with ID {AnalysisId}", existing.Id);
                return Result<ReplayAnalysis>.Success(existing);
            }

            // Parse replay file
            var (metadata, events) = await ParseReplayFileAsync(request.ReplayFilePath, ct);

            // Create analysis
            var analysis = new ReplayAnalysis
            {
                ReplayFilePath = request.ReplayFilePath,
                Name = request.Name ?? Path.GetFileNameWithoutExtension(request.ReplayFilePath),
                Description = request.Description,
                Platform = metadata.Game ?? "MUGEN",
                ReplayDate = metadata.RecordedAt?.DateTime ?? File.GetCreationTime(request.ReplayFilePath),
                AnalyzedAt = DateTime.UtcNow,
                Duration = metadata.Duration ?? TimeSpan.FromSeconds(events.Count / 60.0),
                Player1Character = metadata.Player1 ?? "Unknown",
                Player2Character = metadata.Player2 ?? "Unknown",
                Player1Name = metadata.Player1,
                Player2Name = metadata.Player2,
                Winner = ParseWinner(metadata.Winner),
                TotalFrames = events.Count > 0 ? events.Max(e => e.Frame ?? 0) : 0,
                FileHash = fileHash,
                AnalysisVersion = "1.0"
            };

            var options = request.Options ?? new ReplayAnalysisOptions();

            // Detect combos
            if (options.DetectCombos)
            {
                analysis.Combos = DetectCombos(events, options.MinComboHits, options.MinComboDamage);
                _logger.LogInformation("Detected {ComboCount} combos", analysis.Combos.Count);
            }

            // Calculate stats
            (analysis.Player1Stats, analysis.Player2Stats) = CalculateCombatStats(events, analysis.Combos);

            // Detect comebacks
            if (options.DetectComebacks)
            {
                analysis.Comebacks = DetectComebacks(events, analysis.Duration);
                _logger.LogInformation("Detected {ComebackCount} comebacks", analysis.Comebacks.Count);
            }

            // Generate highlights
            if (options.GenerateHighlights)
            {
                analysis.Highlights = GenerateHighlights(analysis, events, options.MinHighlightIntensity);
                _logger.LogInformation("Generated {HighlightCount} highlights", analysis.Highlights.Count);
            }

            // Capture frame data if requested
            if (options.CaptureFrameData)
            {
                analysis.FrameData = CaptureFrameData(events);
            }

            // Save to database
            _dbContext.ReplayAnalyses.Add(analysis);
            await _dbContext.SaveChangesAsync(ct);

            _logger.LogInformation("Replay analysis completed with ID {AnalysisId}", analysis.Id);
            return Result<ReplayAnalysis>.Success(analysis);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze replay {FilePath}", request.ReplayFilePath);
            return Result<ReplayAnalysis>.Failure(
                $"Analysis failed: {ex.Message}", 
                ErrorType.Internal);
        }
    }

    public async Task<Result<ReplayAnalysis>> GetAnalysisAsync(Guid analysisId, CancellationToken ct = default)
    {
        try
        {
            var analysis = await _dbContext.ReplayAnalyses
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == analysisId, ct);

            if (analysis == null)
            {
                return Result<ReplayAnalysis>.Failure(
                    $"Analysis {analysisId} not found", 
                    ErrorType.NotFound);
            }

            return Result<ReplayAnalysis>.Success(analysis);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get analysis {AnalysisId}", analysisId);
            return Result<ReplayAnalysis>.Failure(
                $"Query failed: {ex.Message}", 
                ErrorType.Internal);
        }
    }

    public async Task<Result<List<ReplayAnalysisSummary>>> GetAnalysesAsync(
        ReplayAnalysisFilter? filter = null, 
        CancellationToken ct = default)
    {
        try
        {
            var query = _dbContext.ReplayAnalyses.AsNoTracking().AsQueryable();

            if (filter != null)
            {
                if (!string.IsNullOrEmpty(filter.Character))
                {
                    query = query.Where(r => r.Player1Character == filter.Character || 
                                             r.Player2Character == filter.Character);
                }

                if (filter.FromDate.HasValue)
                {
                    query = query.Where(r => r.ReplayDate >= filter.FromDate.Value);
                }

                if (filter.ToDate.HasValue)
                {
                    query = query.Where(r => r.ReplayDate <= filter.ToDate.Value);
                }

                if (filter.HasComebacks.HasValue)
                {
                    query = query.Where(r => r.Comebacks.Count > 0 == filter.HasComebacks.Value);
                }
            }

            var summaries = await query
                .OrderByDescending(r => r.AnalyzedAt)
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

            return Result<List<ReplayAnalysisSummary>>.Success(summaries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get analyses");
            return Result<List<ReplayAnalysisSummary>>.Failure(
                $"Query failed: {ex.Message}", 
                ErrorType.Internal);
        }
    }

    public async Task<Result> DeleteAnalysisAsync(Guid analysisId, CancellationToken ct = default)
    {
        try
        {
            var analysis = await _dbContext.ReplayAnalyses
                .FirstOrDefaultAsync(r => r.Id == analysisId, ct);

            if (analysis == null)
            {
                return Result.Failure(
                    $"Analysis {analysisId} not found", 
                    ErrorType.NotFound);
            }

            _dbContext.ReplayAnalyses.Remove(analysis);
            await _dbContext.SaveChangesAsync(ct);

            _logger.LogInformation("Deleted analysis {AnalysisId}", analysisId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete analysis {AnalysisId}", analysisId);
            return Result.Failure(
                $"Delete failed: {ex.Message}", 
                ErrorType.Internal);
        }
    }

    public async Task<Result<List<DetectedCombo>>> GetCombosAsync(
        Guid analysisId, 
        int? player = null,
        int? minHits = null,
        CancellationToken ct = default)
    {
        try
        {
            var analysis = await _dbContext.ReplayAnalyses
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == analysisId, ct);

            if (analysis == null)
            {
                return Result<List<DetectedCombo>>.Failure(
                    $"Analysis {analysisId} not found", 
                    ErrorType.NotFound);
            }

            var combos = analysis.Combos.AsEnumerable();

            if (player.HasValue)
            {
                combos = combos.Where(c => c.Player == player.Value);
            }

            if (minHits.HasValue)
            {
                combos = combos.Where(c => c.HitCount >= minHits.Value);
            }

            return Result<List<DetectedCombo>>.Success(combos.ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get combos for {AnalysisId}", analysisId);
            return Result<List<DetectedCombo>>.Failure(
                $"Query failed: {ex.Message}", 
                ErrorType.Internal);
        }
    }

    public async Task<Result<List<HighlightMoment>>> GetHighlightsAsync(
        Guid analysisId, 
        HighlightType? type = null,
        int? minIntensity = null,
        CancellationToken ct = default)
    {
        try
        {
            var analysis = await _dbContext.ReplayAnalyses
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == analysisId, ct);

            if (analysis == null)
            {
                return Result<List<HighlightMoment>>.Failure(
                    $"Analysis {analysisId} not found", 
                    ErrorType.NotFound);
            }

            var highlights = analysis.Highlights.AsEnumerable();

            if (type.HasValue)
            {
                highlights = highlights.Where(h => h.Type == type.Value);
            }

            if (minIntensity.HasValue)
            {
                highlights = highlights.Where(h => h.IntensityScore >= minIntensity.Value);
            }

            return Result<List<HighlightMoment>>.Success(highlights.OrderByDescending(h => h.IntensityScore).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get highlights for {AnalysisId}", analysisId);
            return Result<List<HighlightMoment>>.Failure(
                $"Query failed: {ex.Message}", 
                ErrorType.Internal);
        }
    }

    public async Task<Result<List<ComebackMoment>>> GetComebacksAsync(
        Guid analysisId, 
        ComebackSeverity? minSeverity = null,
        CancellationToken ct = default)
    {
        try
        {
            var analysis = await _dbContext.ReplayAnalyses
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == analysisId, ct);

            if (analysis == null)
            {
                return Result<List<ComebackMoment>>.Failure(
                    $"Analysis {analysisId} not found", 
                    ErrorType.NotFound);
            }

            var comebacks = analysis.Comebacks.AsEnumerable();

            if (minSeverity.HasValue)
            {
                comebacks = comebacks.Where(c => c.Severity >= minSeverity.Value);
            }

            return Result<List<ComebackMoment>>.Success(
                comebacks.OrderByDescending(c => c.ComebackScore).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get comebacks for {AnalysisId}", analysisId);
            return Result<List<ComebackMoment>>.Failure(
                $"Query failed: {ex.Message}", 
                ErrorType.Internal);
        }
    }

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

    public Task<Result<HighlightReel>> GenerateHighlightReelAsync(
        Guid analysisId,
        List<Guid> highlightIds,
        HighlightReelOptions options,
        CancellationToken ct = default)
    {
        try
        {
            var reel = new HighlightReel
            {
                Id = Guid.NewGuid(),
                SourceAnalysisId = analysisId,
                Name = options.Name,
                Description = options.Description,
                CreatedAt = DateTime.UtcNow,
                BackgroundMusic = options.BackgroundMusicPath,
                Moments = new List<HighlightMoment>(),
                Transitions = new List<TransitionEffect>()
            };

            _reels[reel.Id] = reel;

            _logger.LogInformation("Generated highlight reel {ReelId} with {Count} moments", 
                reel.Id, highlightIds.Count);

            return Task.FromResult(Result<HighlightReel>.Success(reel));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate highlight reel");
            return Task.FromResult(Result<HighlightReel>.Failure(
                $"Generation failed: {ex.Message}", 
                ErrorType.Internal));
        }
    }

    public async Task<Result<HighlightReel>> AutoGenerateHighlightReelAsync(
        Guid analysisId,
        int maxDurationSeconds = 60,
        CancellationToken ct = default)
    {
        try
        {
            var highlightsResult = await GetHighlightsAsync(analysisId, null, 70, ct);
            
            if (highlightsResult.IsFailure)
            {
                return Result<HighlightReel>.Failure(highlightsResult.Error!, highlightsResult.ErrorType);
            }

            var topHighlights = highlightsResult.Value!
                .OrderByDescending(h => h.IntensityScore)
                .Take(10)
                .Select(h => h.Id)
                .ToList();

            var options = new HighlightReelOptions
            {
                Name = "Auto-Generated Highlights",
                MaxDuration = TimeSpan.FromSeconds(maxDurationSeconds),
                AddTransitions = true,
                IncludeSlowMotion = true
            };

            return await GenerateHighlightReelAsync(analysisId, topHighlights, options, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to auto-generate highlight reel");
            return Result<HighlightReel>.Failure(
                $"Auto-generation failed: {ex.Message}", 
                ErrorType.Internal);
        }
    }

    public Task<Result<string>> ExportHighlightReelAsync(
        Guid reelId,
        string outputPath,
        ExportFormat format,
        CancellationToken ct = default)
    {
        try
        {
            if (!_reels.ContainsKey(reelId))
            {
                return Task.FromResult(Result<string>.Failure(
                    $"Reel {reelId} not found", 
                    ErrorType.NotFound));
            }

            var extension = format switch
            {
                ExportFormat.Mp4 => "mp4",
                ExportFormat.WebM => "webm",
                ExportFormat.Gif => "gif",
                ExportFormat.Avi => "avi",
                ExportFormat.Mov => "mov",
                _ => "mp4"
            };

            var fullPath = Path.ChangeExtension(outputPath, extension);

            // Note: Actual video export would require ffmpeg or similar
            // This is a placeholder implementation
            _logger.LogInformation("Exporting highlight reel {ReelId} to {Path}", reelId, fullPath);

            return Task.FromResult(Result<string>.Success(fullPath));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export highlight reel");
            return Task.FromResult(Result<string>.Failure(
                $"Export failed: {ex.Message}", 
                ErrorType.Internal));
        }
    }

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

    public async Task<Result> TagAnalysisAsync(
        Guid analysisId, 
        List<string> tags,
        CancellationToken ct = default)
    {
        try
        {
            var analysis = await _dbContext.ReplayAnalyses
                .FirstOrDefaultAsync(r => r.Id == analysisId, ct);

            if (analysis == null)
            {
                return Result.Failure(
                    $"Analysis {analysisId} not found", 
                    ErrorType.NotFound);
            }

            foreach (var tag in tags.Where(t => !analysis.Tags.Contains(t)))
            {
                analysis.Tags.Add(tag);
            }

            await _dbContext.SaveChangesAsync(ct);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to tag analysis {AnalysisId}", analysisId);
            return Result.Failure(
                $"Tagging failed: {ex.Message}", 
                ErrorType.Internal);
        }
    }

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

    public async Task<Result<List<FrameSnapshot>>> GetFrameRangeAsync(
        Guid analysisId,
        int startFrame,
        int endFrame,
        CancellationToken ct = default)
    {
        try
        {
            var analysis = await _dbContext.ReplayAnalyses
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == analysisId, ct);

            if (analysis == null)
            {
                return Result<List<FrameSnapshot>>.Failure(
                    $"Analysis {analysisId} not found", 
                    ErrorType.NotFound);
            }

            if (analysis.FrameData == null)
            {
                return Result<List<FrameSnapshot>>.Failure(
                    "Frame data not captured for this analysis", 
                    ErrorType.NotFound);
            }

            var frames = analysis.FrameData
                .Where(f => f.FrameNumber >= startFrame && f.FrameNumber <= endFrame)
                .OrderBy(f => f.FrameNumber)
                .ToList();

            return Result<List<FrameSnapshot>>.Success(frames);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get frame range");
            return Result<List<FrameSnapshot>>.Failure(
                $"Query failed: {ex.Message}", 
                ErrorType.Internal);
        }
    }

    // Helper methods

    private async Task<string> CalculateFileHashAsync(string filePath, CancellationToken ct)
    {
        await using var stream = File.OpenRead(filePath);
        var hash = await SHA256.HashDataAsync(stream, ct);
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

    private static int ParseWinner(string? winner)
    {
        if (string.IsNullOrEmpty(winner)) return 0;
        if (winner.Contains("1") || winner.Equals("p1", StringComparison.OrdinalIgnoreCase)) return 1;
        if (winner.Contains("2") || winner.Equals("p2", StringComparison.OrdinalIgnoreCase)) return 2;
        return 0;
    }

    private static List<DetectedCombo> DetectCombos(List<ReplayEvent> events, int minHits, int minDamage)
    {
        var combos = new List<DetectedCombo>();
        var currentCombo = new List<ReplayEvent>();
        var lastFrame = 0;
        const int comboGapThreshold = 45; // frames (approx 0.75s at 60fps)

        foreach (var evt in events.Where(e => e.Type == ReplayEventType.Hit || e.Type == ReplayEventType.Move))
        {
            if (currentCombo.Count == 0)
            {
                currentCombo.Add(evt);
                lastFrame = evt.Frame ?? 0;
                continue;
            }

            var frame = evt.Frame ?? lastFrame;
            if (frame - lastFrame <= comboGapThreshold && evt.PlayerIndex == currentCombo[0].PlayerIndex)
            {
                currentCombo.Add(evt);
                lastFrame = frame;
            }
            else
            {
                // End of combo
                if (currentCombo.Count >= minHits)
                {
                    var combo = CreateComboFromEvents(currentCombo);
                    if (combo.TotalDamage >= minDamage)
                    {
                        combos.Add(combo);
                    }
                }
                currentCombo = new List<ReplayEvent> { evt };
                lastFrame = frame;
            }
        }

        // Handle final combo
        if (currentCombo.Count >= minHits)
        {
            var combo = CreateComboFromEvents(currentCombo);
            if (combo.TotalDamage >= minDamage)
            {
                combos.Add(combo);
            }
        }

        return combos;
    }

    private static DetectedCombo CreateComboFromEvents(List<ReplayEvent> events)
    {
        var firstFrame = events.Min(e => e.Frame ?? 0);
        var lastFrame = events.Max(e => e.Frame ?? 0);
        var totalDamage = events.Sum(e => e.Damage ?? 0);

        var moves = events.Select(e => new ComboMove
        {
            MoveName = e.Move ?? "Unknown",
            Input = e.Command ?? "",
            Frame = e.Frame ?? 0,
            Damage = e.Damage ?? 0,
            IsCounterHit = e.Type == ReplayEventType.Hit
        }).ToList();

        return new DetectedCombo
        {
            Player = events.First().PlayerIndex,
            Character = $"Player{events.First().PlayerIndex}",
            StartFrame = firstFrame,
            EndFrame = lastFrame,
            HitCount = events.Count,
            TotalDamage = totalDamage,
            Moves = moves,
            QualityScore = CalculateComboQuality(events.Count, totalDamage),
            Difficulty = DetermineComboDifficulty(events.Count, moves.Count)
        };
    }

    private static int CalculateComboQuality(int hitCount, int damage)
    {
        var hitScore = Math.Min(hitCount * 5, 40);
        var damageScore = Math.Min(damage / 50, 40);
        var lengthBonus = hitCount >= 10 ? 20 : hitCount >= 5 ? 10 : 0;
        return Math.Min(hitScore + damageScore + lengthBonus, 100);
    }

    private static ComboDifficulty DetermineComboDifficulty(int hitCount, int uniqueMoves)
    {
        if (hitCount >= 20) return ComboDifficulty.TOD;
        if (hitCount >= 15 || uniqueMoves >= 8) return ComboDifficulty.VeryHard;
        if (hitCount >= 10 || uniqueMoves >= 5) return ComboDifficulty.Hard;
        if (hitCount >= 5) return ComboDifficulty.Medium;
        return ComboDifficulty.Easy;
    }

    private static (PlayerCombatStats P1, PlayerCombatStats P2) CalculateCombatStats(
        List<ReplayEvent> events, 
        List<DetectedCombo> combos)
    {
        var p1Stats = new PlayerCombatStats();
        var p2Stats = new PlayerCombatStats();

        foreach (var evt in events)
        {
            var stats = evt.PlayerIndex == 1 ? p1Stats : p2Stats;

            switch (evt.Type)
            {
                case ReplayEventType.Hit:
                    stats.SuccessfulHits++;
                    stats.TotalAttacks++;
                    if (evt.Damage.HasValue)
                        stats.TotalDamageDealt += evt.Damage.Value;
                    break;
                case ReplayEventType.Block:
                    stats.BlockedAttacks++;
                    break;
                case ReplayEventType.Whiff:
                    stats.WhiffedAttacks++;
                    stats.TotalAttacks++;
                    break;
                case ReplayEventType.Throw:
                    stats.ThrowsAttempted++;
                    stats.ThrowsSuccessful++;
                    break;
                case ReplayEventType.AntiAir:
                    stats.AntiAirs++;
                    break;
            }
        }

        // Calculate combo stats
        foreach (var combo in combos)
        {
            var stats = combo.Player == 1 ? p1Stats : p2Stats;
            stats.CombosPerformed++;
            stats.TotalComboHits += combo.HitCount;
            stats.MaxComboHits = Math.Max(stats.MaxComboHits, combo.HitCount);
            stats.MaxComboDamage = Math.Max(stats.MaxComboDamage, combo.TotalDamage);
        }

        p1Stats.AverageComboDamage = p1Stats.CombosPerformed > 0 
            ? p1Stats.TotalComboHits * 50 // Approximate
            : 0;
        p2Stats.AverageComboDamage = p2Stats.CombosPerformed > 0 
            ? p2Stats.TotalComboHits * 50 
            : 0;

        return (p1Stats, p2Stats);
    }

    private static List<ComebackMoment> DetectComebacks(List<ReplayEvent> events, TimeSpan duration)
    {
        var comebacks = new List<ComebackMoment>();
        // Simplified comeback detection based on damage patterns
        // A real implementation would track health state over time
        return comebacks;
    }

    private static List<HighlightMoment> GenerateHighlights(
        ReplayAnalysis analysis, 
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

    private static List<FrameSnapshot> CaptureFrameData(List<ReplayEvent> events)
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

    private static string GenerateComparisonAnalysis(ReplayAnalysis r1, ReplayAnalysis r2)
    {
        var analysis = new StringBuilder();
        
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
}
