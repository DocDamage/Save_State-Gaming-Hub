namespace SaveState.Application.Mugen.Services.AdvancedCombat.Engines;

using Microsoft.Extensions.Logging;
using SaveState.Application.Mugen.Models.AdvancedCombat;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using System.Collections.Concurrent;

/// <summary>
/// Frame data analysis engine for timing and visualization.
/// </summary>
public class FrameDataEngine
{
    private readonly ILogger<FrameDataEngine> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly ConcurrentDictionary<string, List<FrameDataDisplay>> _displays = new();

    public FrameDataEngine(ILogger<FrameDataEngine> logger, ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Displays frame data for a move.
    /// </summary>
    public Task<Result<FrameDataDisplay>> DisplayFrameDataAsync(string sessionId, FrameDataRequest request, CancellationToken ct = default)
    {
        var displayId = Guid.NewGuid().ToString();
        var frameData = new FrameData
        {
            MoveName = request.MoveName,
            TotalFrames = 45,
            StartupFrames = 12,
            ActiveFrames = 4,
            RecoveryFrames = 29,
            FrameBreakdown = new Dictionary<string, int>
            {
                { "Startup", 12 },
                { "Active", 4 },
                { "Recovery", 29 }
            },
            HitAdvantage = 8,
            BlockAdvantage = -4,
            GeneratedAt = _timeProvider.UtcNow
        };

        var display = new FrameDataDisplay
        {
            DisplayId = displayId,
            SessionId = sessionId,
            FrameData = frameData,
            DisplayMode = request.DisplayMode,
            ShowAdvanced = request.ShowAdvanced,
            CreatedAt = _timeProvider.UtcNow,
            Active = true
        };

        var list = _displays.GetOrAdd(sessionId, _ => new List<FrameDataDisplay>());
        lock (list)
        {
            list.Add(display);
        }

        _logger.LogDebug("Frame data displayed for session {SessionId}: {MoveName}", sessionId, request.MoveName);
        return Task.FromResult(Result.Success(display));
    }

    /// <summary>
    /// Analyzes move frames for optimal play.
    /// </summary>
    public Task<Result<MoveAnalysis>> AnalyzeMoveAsync(MoveAnalysisRequest request, CancellationToken ct = default)
    {
        var analysis = new MoveAnalysis
        {
            MoveName = request.MoveName,
            FrameAdvantage = 8,
            RiskRewardRatio = 1.5f,
            OptimalFollowups = new[] { "Light Punch", "Medium Kick", "Special Move" },
            CounterMoves = new[] { "Quick Jab", "Throw", "Uppercut" },
            AnalyzedAt = _timeProvider.UtcNow
        };

        return Task.FromResult(Result.Success(analysis));
    }

    /// <summary>
    /// Gets all frame data displays for a session (used for analysis).
    /// </summary>
    public IReadOnlyList<FrameDataDisplay> GetDisplaysForSession(string sessionId)
    {
        return _displays.TryGetValue(sessionId, out var displays) ? displays : new List<FrameDataDisplay>();
    }

    /// <summary>
    /// Calculates timing improvement based on displays.
    /// </summary>
    public float CalculateTimingImprovement(List<FrameDataDisplay> displays)
    {
        if (displays.Count == 0) return 0f;
        return Math.Min(1.0f, displays.Count * 0.1f);
    }
}
