using SaveState.Core.GameLibrary.Entities;

namespace SaveState.Core.GameLibrary;

/// <summary>
/// Repository interface for game session persistence.
/// </summary>
public interface IGameSessionRepository
{
    Task<GameSession?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<GameSession>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<GameSession?> GetActiveSessionAsync(Guid gameId, CancellationToken ct = default);
    Task<IReadOnlyList<GameSession>> GetAllActiveSessionsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<GameSession>> GetByGameIdAsync(Guid gameId, int limit = 50, CancellationToken ct = default);
    Task AddAsync(GameSession session, CancellationToken ct = default);
    Task UpdateAsync(GameSession session, CancellationToken ct = default);
    Task<int> CountByGameIdAsync(Guid gameId, CancellationToken ct = default);
    Task<int> CountByGameIdSinceAsync(Guid gameId, DateTime since, CancellationToken ct = default);
    Task<TimeSpan> GetTotalPlaytimeAsync(Guid gameId, CancellationToken ct = default);
    Task<GameSession?> GetLongestSessionAsync(Guid gameId, CancellationToken ct = default);
    Task<GameSession?> GetFirstSessionAsync(Guid gameId, CancellationToken ct = default);
    Task<IReadOnlyList<GameSession>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<GameSession>> GetByDateRangeAsync(DateTime start, DateTime end, CancellationToken ct = default);
    Task<IReadOnlyList<GameSession>> GetRecentSessionsAsync(int limit, CancellationToken ct = default);

    /// <summary>
    /// Gets recent sessions within the specified time period.
    /// </summary>
    /// <param name="timeSpan">The time period to look back from now.</param>
    /// <param name="limit">Maximum number of sessions to return.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of recent game sessions.</returns>
    Task<IReadOnlyList<GameSession>> GetRecentSessionsAsync(TimeSpan timeSpan, int limit, CancellationToken ct = default);
}
