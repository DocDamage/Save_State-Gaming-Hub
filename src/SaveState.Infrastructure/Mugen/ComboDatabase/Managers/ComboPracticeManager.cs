using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Mugen.ComboDatabase;
using SaveState.Infrastructure.Persistence;

namespace SaveState.Infrastructure.Mugen.ComboDatabase.Managers;

/// <summary>
/// Manages combo practice sessions.
/// </summary>
public class ComboPracticeManager
{
    private readonly SaveStateDbContext _dbContext;
    private readonly ILogger<ComboPracticeManager> _logger;
    private readonly ITimeProvider _timeProvider;

    public ComboPracticeManager(
        SaveStateDbContext dbContext,
        ILogger<ComboPracticeManager> logger,
        ITimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Starts a new practice session for a combo.
    /// </summary>
    public Task<Result<ComboPracticeSession>> StartPracticeSessionAsync(
        Guid comboId,
        CancellationToken ct = default)
    {
        try
        {
            var session = new ComboPracticeSession
            {
                ComboId = comboId,
                StartedAt = _timeProvider.UtcNow,
                Attempts = 0,
                Successes = 0
            };

            _dbContext.ComboPracticeSessions.Add(session);

            return Task.FromResult(Result<ComboPracticeSession>.Success(session));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start practice session");
            return Task.FromResult(Result<ComboPracticeSession>.Failure(
                $"Failed to start session: {ex.Message}", ErrorType.Internal));
        }
    }

    /// <summary>
    /// Records a practice attempt for a session.
    /// </summary>
    public async Task<Result> RecordPracticeAttemptAsync(
        Guid sessionId,
        PracticeAttempt attempt,
        CancellationToken ct = default)
    {
        try
        {
            var session = await _dbContext.ComboPracticeSessions
                .FirstOrDefaultAsync(s => s.Id == sessionId, ct);

            if (session == null)
                return Result.Failure($"Session {sessionId} not found", ErrorType.NotFound);

            session.Attempts++;
            if (attempt.Success) session.Successes++;
            session.AttemptsLog.Add(attempt);

            await _dbContext.SaveChangesAsync(ct);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record practice attempt");
            return Result.Failure($"Failed to record attempt: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Completes a practice session and calculates final statistics.
    /// </summary>
    public async Task<Result<ComboPracticeSession>> CompletePracticeSessionAsync(
        Guid sessionId,
        CancellationToken ct = default)
    {
        try
        {
            var session = await _dbContext.ComboPracticeSessions
                .FirstOrDefaultAsync(s => s.Id == sessionId, ct);

            if (session == null)
                return Result<ComboPracticeSession>.Failure($"Session {sessionId} not found", ErrorType.NotFound);

            session.IsCompleted = true;
            session.CompletedAt = _timeProvider.UtcNow;
            session.TotalPracticeTime = session.CompletedAt.Value - session.StartedAt;

            // Calculate consistency rating
            if (session.Attempts > 0)
            {
                var rate = (double)session.Successes / session.Attempts;
                session.ConsistencyRating = rate switch
                {
                    >= 0.9 => 10,
                    >= 0.8 => 8,
                    >= 0.6 => 6,
                    >= 0.4 => 4,
                    _ => 2
                };
            }

            await _dbContext.SaveChangesAsync(ct);

            return Result<ComboPracticeSession>.Success(session);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to complete practice session");
            return Result<ComboPracticeSession>.Failure(
                $"Failed to complete session: {ex.Message}", ErrorType.Internal);
        }
    }
}
