using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Services.DTOs;

namespace SaveState.Core.GameLibrary.Services;

/// <summary>
/// Service for generating AI-powered game briefings and session summaries.
/// </summary>
public interface IGameBriefingService
{
    /// <summary>
    /// Generates a comprehensive briefing for a game including recent progress,
    /// current objectives, tips, and time since last played.
    /// </summary>
    Task<Result<GameBriefing>> GenerateBriefingAsync(
        Guid gameId,
        CancellationToken ct = default);

    /// <summary>
    /// Generates a summary of the last session played.
    /// </summary>
    Task<Result<string>> GenerateLastSessionSummaryAsync(
        Guid gameId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets suggested objectives for the current play session.
    /// </summary>
    Task<Result<IReadOnlyList<string>>> GetCurrentObjectivesAsync(
        Guid gameId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets AI-generated tips for improving gameplay.
    /// </summary>
    Task<Result<IReadOnlyList<string>>> GetGameTipsAsync(
        Guid gameId,
        CancellationToken ct = default);

    /// <summary>
    /// Generates a briefing optimized for quick reading (mobile/Big Picture mode).
    /// </summary>
    Task<Result<GameBriefing>> GenerateQuickBriefingAsync(
        Guid gameId,
        CancellationToken ct = default);
}