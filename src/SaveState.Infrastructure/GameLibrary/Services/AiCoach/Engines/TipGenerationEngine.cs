using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.GameLibrary.Models.AiCoach;

namespace SaveState.Infrastructure.GameLibrary.Services.AiCoach.Engines;

/// <summary>
/// Implementation of tip generation engine.
/// </summary>
public sealed class TipGenerationEngine : ITipGenerationEngine
{
    private readonly ILogger<TipGenerationEngine> _logger;
    private readonly ITimeProvider _timeProvider;

    public TipGenerationEngine(ILogger<TipGenerationEngine> logger, ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public Task<Result<IReadOnlyList<CoachingTip>>> GenerateContextualTipsAsync(
        CoachingSession session,
        string context,
        int maxTips,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "Generating contextual tips for session {SessionId}",
            session.Id);

        var tips = new List<CoachingTip>
        {
            new CoachingTip(
                Title: "Stay Focused",
                Description: "Maintain concentration during intense moments",
                Category: TipCategory.Mindset,
                Difficulty: TipDifficulty.Easy,
                Prerequisites: new List<string>())
        };

        return Task.FromResult(Result.Success<IReadOnlyList<CoachingTip>>(tips.Take(maxTips).ToList()));
    }

    public Task<Result<IReadOnlyList<CoachingTip>>> GenerateTipsForSkillAreaAsync(
        SkillArea area,
        SkillLevel targetLevel,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "Generating tips for skill area {SkillArea} at level {TargetLevel}",
            area, targetLevel);

        var tips = new List<CoachingTip>
        {
            new CoachingTip(
                Title: $"Improve {area}",
                Description: $"Practice exercises to enhance your {area} skills",
                Category: TipCategory.Strategy,
                Difficulty: TipDifficulty.Medium,
                Prerequisites: new List<string>())
        };

        return Task.FromResult(Result.Success<IReadOnlyList<CoachingTip>>(tips));
    }

    public Task<Result<IReadOnlyList<Hint>>> GetHintsAsync(
        Guid sessionId,
        GameStateSnapshot gameState,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "Getting hints for session {SessionId}",
            sessionId);

        var hints = new List<Hint>
        {
            new Hint(
                Id: Guid.NewGuid(),
                Content: "Consider your next move carefully",
                Type: HintType.Contextual,
                RelevanceScore: 0.8,
                Tags: new List<string> { "strategy" },
                GeneratedAt: _timeProvider.UtcNow)
        };

        return Task.FromResult(Result.Success<IReadOnlyList<Hint>>(hints));
    }
}
