namespace SaveState.Core.GameLibrary.Entities;

using SaveState.Core.Common.Base;
using SaveState.Core.GameLibrary.Enums;

/// <summary>
/// Represents a single play session for a game.
/// Tracks when a game was played and for how long.
/// </summary>
public class GameSession : EntityBase
{
    public Guid GameId { get; private set; }
    public Game Game { get; private set; } = null!;
    public DateTime StartedAt { get; private set; }
    public DateTime? EndedAt { get; private set; }
    public SessionEndReason? EndReason { get; private set; }

    /// <summary>
    /// Gets the duration of this session.
    /// If session is still active, returns time since start.
    /// </summary>
    public TimeSpan Duration => EndedAt.HasValue
        ? EndedAt.Value - StartedAt
        : DateTime.UtcNow - StartedAt;

    /// <summary>
    /// Indicates whether this session is currently active.
    /// </summary>
    public bool IsActive => !EndedAt.HasValue;

    private GameSession() { } // EF Core

    /// <summary>
    /// Creates a new game session for the specified game.
    /// </summary>
    public static GameSession Create(Guid gameId)
    {
        return new GameSession
        {
            Id = Guid.NewGuid(),
            GameId = gameId,
            StartedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Ends this session with the specified reason.
    /// </summary>
    public void End(SessionEndReason reason)
    {
        if (EndedAt.HasValue)
            return; // Already ended

        EndedAt = DateTime.UtcNow;
        EndReason = reason;
    }
}
