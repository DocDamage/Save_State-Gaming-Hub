using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Models.AiCoach;

namespace SaveState.Infrastructure.GameLibrary.Services.AiCoach.Engines;

/// <summary>
/// Engine responsible for generating coaching tips.
/// </summary>
public interface ITipGenerationEngine
{
    /// <summary>
    /// Generates contextual tips based on the current game situation.
    /// </summary>
    Task<Result<IReadOnlyList<CoachingTip>>> GenerateContextualTipsAsync(CoachingSession session, string context, int maxTips, CancellationToken ct = default);

    /// <summary>
    /// Generates tips for a specific skill area.
    /// </summary>
    Task<Result<IReadOnlyList<CoachingTip>>> GenerateTipsForSkillAreaAsync(SkillArea area, SkillLevel targetLevel, CancellationToken ct = default);

    /// <summary>
    /// Gets hints for the current game state.
    /// </summary>
    Task<Result<IReadOnlyList<Hint>>> GetHintsAsync(Guid sessionId, GameStateSnapshot gameState, CancellationToken ct = default);
}
