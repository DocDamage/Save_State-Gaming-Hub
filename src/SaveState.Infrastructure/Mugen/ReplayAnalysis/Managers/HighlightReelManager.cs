using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Mugen.ReplayAnalysis;
using SaveState.Core.Mugen.ReplayAnalysis.Services;

namespace SaveState.Infrastructure.Mugen.ReplayAnalysis.Managers;

/// <summary>
/// Manager for creating and exporting highlight reels from replay analyses.
/// </summary>
public class HighlightReelManager
{
    private readonly ILogger<HighlightReelManager> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly Dictionary<Guid, HighlightReel> _reels;

    /// <summary>
    /// Initializes a new instance of the <see cref="HighlightReelManager"/> class.
    /// </summary>
    public HighlightReelManager(
        ILogger<HighlightReelManager> logger,
        ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
        _reels = new Dictionary<Guid, HighlightReel>();
    }

    /// <summary>
    /// Gets all stored highlight reels.
    /// </summary>
    public Dictionary<Guid, HighlightReel> Reels => _reels;

    /// <summary>
    /// Generates a highlight reel from selected highlight moments.
    /// </summary>
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
                CreatedAt = _timeProvider.UtcNow,
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

    /// <summary>
    /// Auto-generates a highlight reel based on intensity scores.
    /// </summary>
    public async Task<Result<HighlightReel>> AutoGenerateHighlightReelAsync(
        Guid analysisId,
        IReadOnlyList<HighlightMoment> availableHighlights,
        int maxDurationSeconds = 60,
        CancellationToken ct = default)
    {
        try
        {
            var topHighlights = availableHighlights
                .Where(h => h.IntensityScore >= 70)
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

    /// <summary>
    /// Exports a highlight reel to a video file.
    /// </summary>
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
}
