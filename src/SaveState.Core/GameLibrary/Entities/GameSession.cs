using SaveState.Core.Common.Base;
using SaveState.Core.Common.Services;
using SaveState.Core.GameLibrary.Enums;

namespace SaveState.Core.GameLibrary.Entities;

/// <summary>
/// Represents a single play session for a game.
/// Tracks when a game was played and for how long.
/// </summary>
public class GameSession : EntityBase
{
    public Guid GameId { get; private set; }
    public Game? Game { get; private set; }
    public DateTime StartedAt { get; private set; }
    public DateTime StartTime => StartedAt;
    public DateTime? EndedAt { get; private set; }
    public SessionEndReason? EndReason { get; private set; }
    public string? Notes { get; private set; }

    /// <summary>
    /// Gets the duration of this session.
    /// If session is still active, returns time since start.
    /// Note: For active sessions, use <see cref="GetDuration(ITimeProvider)"/> for testable code.
    /// </summary>
    public TimeSpan Duration => GetDuration(SystemTimeProvider.Instance.UtcNow);

    /// <summary>
    /// Gets the duration of this session using the provided time provider.
    /// If session is still active, returns time since start using the provider's UTC time.
    /// </summary>
    public TimeSpan GetDuration(ITimeProvider timeProvider)
    {
        Guard.Against.Null(timeProvider, nameof(timeProvider));
        return GetDuration(timeProvider.UtcNow);
    }

    /// <summary>
    /// Creates a new game session for the specified game.
    /// </summary>
    public static GameSession Create(Guid gameId, ITimeProvider timeProvider)
    {
        Guard.Against.Null(timeProvider, nameof(timeProvider));
        return new GameSession
        {
            Id = Guid.NewGuid(),
            GameId = gameId,
            StartedAt = timeProvider.UtcNow
        };
    }

    /// <summary>
    /// Creates a new game session for the specified game with explicit start time.
    /// </summary>
    public static GameSession Create(Guid gameId, DateTime startedAt)
    {
        return new GameSession
        {
            Id = Guid.NewGuid(),
            GameId = gameId,
            StartedAt = startedAt
        };
    }

    [Obsolete("Use Create(Guid, ITimeProvider) or Create(Guid, DateTime) instead")]
    public static GameSession Create(Guid gameId, DateTime? startedAt = null)
    {
        return new GameSession
        {
            Id = Guid.NewGuid(),
            GameId = gameId,
            StartedAt = startedAt ?? SystemTimeProvider.Instance.UtcNow
        };
    }

    /// <summary>
    /// Gets the duration of this session using the specified current time for active sessions.
    /// If session is still active, returns time since start based on provided time.
    /// </summary>
    /// <param name="currentTime">The current time to use for calculating duration of active sessions.</param>
    /// <returns>The duration of the session.</returns>
    public TimeSpan GetDuration(DateTime currentTime) => EndedAt.HasValue
        ? EndedAt.Value - StartedAt
        : currentTime - StartedAt;

    /// <summary>
    /// Indicates whether this session is currently active.
    /// </summary>
    public bool IsActive => !EndedAt.HasValue;

    private GameSession() { } // EF Core



    /// <summary>
    /// Ends this session with the specified reason using the provided time provider.
    /// </summary>
    public void End(SessionEndReason reason, ITimeProvider timeProvider)
    {
        Guard.Against.Null(timeProvider, nameof(timeProvider));
        End(reason, timeProvider.UtcNow);
    }

    /// <summary>
    /// Ends this session with the specified reason and explicit end time.
    /// </summary>
    public void End(SessionEndReason reason, DateTime endedAt)
    {
        if (EndedAt.HasValue)
            return; // Already ended

        EndedAt = endedAt;
        EndReason = reason;
    }

    [Obsolete("Use End(SessionEndReason, ITimeProvider) or End(SessionEndReason, DateTime) instead")]
    public void End(SessionEndReason reason, DateTime? endedAt = null)
    {
        if (EndedAt.HasValue)
            return; // Already ended

        EndedAt = endedAt ?? SystemTimeProvider.Instance.UtcNow;
        EndReason = reason;
    }

    /// <summary>
    /// Updates the session notes.
    /// </summary>
    /// <param name="notes">The notes content.</param>
    public void UpdateNotes(string? notes)
    {
        Notes = notes;
    }
}
