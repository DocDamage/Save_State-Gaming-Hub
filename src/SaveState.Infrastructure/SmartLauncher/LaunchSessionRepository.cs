// Copyright (c) 2026 SaveStateReborn. All rights reserved.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.SmartLauncher;
using SaveState.Infrastructure.Persistence;

namespace SaveState.Infrastructure.SmartLauncher;

/// <summary>
/// Repository for launch session tracking using Entity Framework.
/// </summary>
public sealed class LaunchSessionRepository : ILaunchSessionRepository
{
    private readonly SaveStateDbContext _dbContext;
    private readonly ILogger<LaunchSessionRepository> _logger;

    public LaunchSessionRepository(SaveStateDbContext dbContext, ILogger<LaunchSessionRepository> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task CreateSessionAsync(LaunchSession session, CancellationToken ct = default)
    {
        try
        {
            await _dbContext.LaunchSessions.AddAsync(session, ct);
            await _dbContext.SaveChangesAsync(ct);
            _logger.LogInformation("Created launch session {SessionId} for game {GameName}",
                session.Id, session.GameName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create launch session");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task UpdateSessionAsync(LaunchSession session, CancellationToken ct = default)
    {
        try
        {
            _dbContext.LaunchSessions.Update(session);
            await _dbContext.SaveChangesAsync(ct);
            _logger.LogDebug("Updated launch session {SessionId}", session.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update launch session {SessionId}", session.Id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<Result<LaunchSession>> GetActiveSessionAsync(CancellationToken ct = default)
    {
        try
        {
            var session = await _dbContext.LaunchSessions
                .AsNoTracking()
                .Where(s => s.EndedAt == null)
                .OrderByDescending(s => s.StartedAt)
                .FirstOrDefaultAsync(ct);

            if (session == null)
            {
                return Result.Failure<LaunchSession>("No active session found", ErrorType.NotFound);
            }

            return Result.Success(session);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get active session");
            return Result.Failure<LaunchSession>($"Database error: {ex.Message}", ErrorType.Database);
        }
    }

    /// <inheritdoc />
    public async Task<Result<LaunchSession>> GetSessionAsync(Guid sessionId, CancellationToken ct = default)
    {
        try
        {
            var session = await _dbContext.LaunchSessions
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == sessionId, ct);

            if (session == null)
            {
                return Result.Failure<LaunchSession>($"Session {sessionId} not found", ErrorType.NotFound);
            }

            return Result.Success(session);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get session {SessionId}", sessionId);
            return Result.Failure<LaunchSession>($"Database error: {ex.Message}", ErrorType.Database);
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<LaunchSession>> GetLaunchHistoryAsync(Guid gameId, int count, CancellationToken ct = default)
    {
        try
        {
            return await _dbContext.LaunchSessions
                .AsNoTracking()
                .Where(s => s.GameId == gameId && s.EndedAt != null)
                .OrderByDescending(s => s.StartedAt)
                .Take(count)
                .ToListAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get launch history for game {GameId}", gameId);
            return new List<LaunchSession>();
        }
    }

    /// <inheritdoc />
    public async Task EndSessionAsync(Guid sessionId, int? exitCode, SessionPerformanceMetrics? metrics, CancellationToken ct = default)
    {
        try
        {
            var session = await _dbContext.LaunchSessions
                .FirstOrDefaultAsync(s => s.Id == sessionId, ct);

            if (session != null)
            {
                session.EndedAt = DateTime.UtcNow;
                session.ExitCode = exitCode;
                session.PerformanceMetrics = metrics;

                await _dbContext.SaveChangesAsync(ct);
                _logger.LogInformation("Ended launch session {SessionId}. Duration: {Duration}",
                    sessionId, session.Duration);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to end launch session {SessionId}", sessionId);
            throw;
        }
    }
}
