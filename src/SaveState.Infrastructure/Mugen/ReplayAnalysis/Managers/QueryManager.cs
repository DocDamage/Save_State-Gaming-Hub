using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Mugen.ReplayAnalysis;
using SaveState.Infrastructure.Persistence;

// Alias to disambiguate the entity type from the namespace
using ReplayAnalysisEntity = SaveState.Core.Mugen.ReplayAnalysis.ReplayAnalysis;

namespace SaveState.Infrastructure.Mugen.ReplayAnalysis.Managers;

/// <summary>
/// Manages query operations for replay analysis data including filtering,
/// retrieval, and tagging of analyses, combos, highlights, comebacks, and frame data.
/// </summary>
public class QueryManager
{
    private readonly SaveStateDbContext _dbContext;
    private readonly ILogger<QueryManager> _logger;

    public QueryManager(
        SaveStateDbContext dbContext,
        ILogger<QueryManager> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Gets a single replay analysis by its ID.
    /// </summary>
    public async Task<Result<ReplayAnalysisEntity>> GetAnalysisAsync(Guid analysisId, CancellationToken ct = default)
    {
        try
        {
            var analysis = await _dbContext.ReplayAnalyses
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == analysisId, ct);

            if (analysis == null)
            {
                return Result<ReplayAnalysisEntity>.Failure(
                    $"Analysis {analysisId} not found",
                    ErrorType.NotFound);
            }

            return Result<ReplayAnalysisEntity>.Success(analysis);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get analysis {AnalysisId}", analysisId);
            return Result<ReplayAnalysisEntity>.Failure(
                $"Query failed: {ex.Message}",
                ErrorType.Internal);
        }
    }

    /// <summary>
    /// Gets replay analyses with optional filtering by character, date range, and comeback presence.
    /// </summary>
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

    /// <summary>
    /// Deletes a replay analysis by its ID.
    /// </summary>
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

    /// <summary>
    /// Gets detected combos for a specific analysis with optional filtering by player and minimum hits.
    /// </summary>
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

    /// <summary>
    /// Gets highlight moments for a specific analysis with optional filtering by type and minimum intensity.
    /// </summary>
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

    /// <summary>
    /// Gets comeback moments for a specific analysis with optional filtering by minimum severity.
    /// </summary>
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

    /// <summary>
    /// Adds tags to an existing replay analysis.
    /// </summary>
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

    /// <summary>
    /// Gets frame snapshots within a specific frame range for an analysis.
    /// </summary>
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
}
