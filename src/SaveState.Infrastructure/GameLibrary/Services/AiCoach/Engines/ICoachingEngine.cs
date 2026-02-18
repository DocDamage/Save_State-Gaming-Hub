using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Models.AiCoach;

namespace SaveState.Infrastructure.GameLibrary.Services.AiCoach.Engines;

/// <summary>
/// Engine responsible for managing coaching sessions.
/// </summary>
public interface ICoachingEngine
{
    /// <summary>
    /// Creates a new coaching session.
    /// </summary>
    Task<Result<CoachingSession>> CreateSessionAsync(Guid gameId, CoachingPreferences preferences, CancellationToken ct = default);

    /// <summary>
    /// Ends an active coaching session.
    /// </summary>
    Task<Result> EndSessionAsync(Guid sessionId, CancellationToken ct = default);

    /// <summary>
    /// Gets a coaching session by ID.
    /// </summary>
    Result<CoachingSession> GetSession(Guid sessionId);

    /// <summary>
    /// Compiles a comprehensive report for a completed session.
    /// </summary>
    Task<Result<CoachingReport>> CompileSessionReportAsync(CoachingSession session, CancellationToken ct = default);
}
