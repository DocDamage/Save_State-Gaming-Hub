using SaveState.Core.Achievements;
using SaveState.Core.Common;
using SaveState.Core.RetroArch.Services;

namespace SaveState.Infrastructure.RetroArch.Services.RetroArch;

/// <summary>
/// Engine for RetroAchievements integration.
/// </summary>
public interface IRetroAchievementsEngine
{
    /// <summary>
    /// Initializes authentication with RetroAchievements.
    /// </summary>
    void Initialize(string? username, string? apiKey);

    /// <summary>
    /// Gets achievements for a game by hash.
    /// </summary>
    Task<Result<IReadOnlyList<Achievement>>> GetAchievementsAsync(string gameHash, CancellationToken ct = default);

    /// <summary>
    /// Gets whether the client is authenticated.
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Gets whether the engine is configured.
    /// </summary>
    bool IsConfigured { get; }
}
