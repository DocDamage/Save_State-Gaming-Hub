// Copyright (c) 2026 SaveStateReborn. All rights reserved.

using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.SmartLauncher;

namespace SaveState.Infrastructure.SmartLauncher;

/// <summary>
/// Service for recovering from crashed or interrupted gaming sessions.
/// </summary>
public sealed class SessionRecoveryService
{
    private readonly ILogger<SessionRecoveryService> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly ILaunchSessionRepository _sessionRepository;
    private readonly ISystemOptimizerService _optimizerService;
    private readonly string _recoveryStatePath;

    public SessionRecoveryService(
        ILogger<SessionRecoveryService> logger,
        ILaunchSessionRepository sessionRepository,
        ISystemOptimizerService optimizerService,
        ITimeProvider timeProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _sessionRepository = sessionRepository ?? throw new ArgumentNullException(nameof(sessionRepository));
        _optimizerService = optimizerService ?? throw new ArgumentNullException(nameof(optimizerService));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        
        _recoveryStatePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SaveStateReborn",
            "SessionRecovery.json");
    }

    /// <summary>
    /// Saves the current session state for potential recovery.
    /// </summary>
    public async Task SaveSessionStateAsync(LaunchSession session, SystemState systemState)
    {
        try
        {
            var recoveryData = new SessionRecoveryData
            {
                SessionId = session.Id,
                GameId = session.GameId,
                GameName = session.GameName,
                ProfileId = session.ProfileId,
                StartedAt = session.StartedAt,
                SystemState = systemState,
                SavedAt = _timeProvider.UtcNow,
                ProcessId = null // Would be populated if we had access to it
            };

            var json = System.Text.Json.JsonSerializer.Serialize(recoveryData, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });

            Directory.CreateDirectory(Path.GetDirectoryName(_recoveryStatePath)!);
            await File.WriteAllTextAsync(_recoveryStatePath, json);

            _logger.LogDebug("Saved session recovery state for session {SessionId}", session.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save session recovery state");
        }
    }

    /// <summary>
    /// Checks if there's a session that needs recovery.
    /// </summary>
    public async Task<Result<SessionRecoveryData>> CheckForRecoveryAsync()
    {
        try
        {
            if (!File.Exists(_recoveryStatePath))
            {
                return Result.Failure<SessionRecoveryData>("No recovery data found", ErrorType.NotFound);
            }

            var json = await File.ReadAllTextAsync(_recoveryStatePath);
            var recoveryData = System.Text.Json.JsonSerializer.Deserialize<SessionRecoveryData>(json);

            if (recoveryData == null)
            {
                return Result.Failure<SessionRecoveryData>("Failed to deserialize recovery data", ErrorType.Validation);
            }

            // Check if the session is still active in the database
            var sessionResult = await _sessionRepository.GetSessionAsync(recoveryData.SessionId);
            
            if (!sessionResult.IsSuccess)
            {
                // Session was properly closed, clean up
                File.Delete(_recoveryStatePath);
                return Result.Failure<SessionRecoveryData>("Session was already closed", ErrorType.NotFound);
            }

            if (sessionResult.Value.EndedAt.HasValue)
            {
                // Session was properly ended, clean up
                File.Delete(_recoveryStatePath);
                return Result.Failure<SessionRecoveryData>("Session was already ended", ErrorType.NotFound);
            }

            // Check if the recovery data is too old (more than 24 hours)
            if (_timeProvider.UtcNow - recoveryData.SavedAt > TimeSpan.FromHours(24))
            {
                _logger.LogWarning("Recovery data is too old, cleaning up");
                File.Delete(_recoveryStatePath);
                return Result.Failure<SessionRecoveryData>("Recovery data is too old (>24 hours)", ErrorType.Validation);
            }

            return Result.Success(recoveryData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking for session recovery");
            return Result.Failure<SessionRecoveryData>($"Error checking recovery: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Recovers from an interrupted session.
    /// </summary>
    public async Task<RecoveryResult> RecoverSessionAsync(SessionRecoveryData recoveryData)
    {
        try
        {
            _logger.LogInformation("Attempting to recover session {SessionId}", recoveryData.SessionId);

            // Restore system state if available
            if (recoveryData.SystemState != null)
            {
                await _optimizerService.RestoreSystemStateAsync(recoveryData.SystemState);
                _logger.LogInformation("Restored system state for recovered session");
            }

            // End the session in the database
            await _sessionRepository.EndSessionAsync(recoveryData.SessionId, -1, null);

            // Clean up recovery file
            if (File.Exists(_recoveryStatePath))
            {
                File.Delete(_recoveryStatePath);
            }

            _logger.LogInformation("Successfully recovered session {SessionId}", recoveryData.SessionId);

            return new RecoveryResult
            {
                Success = true,
                SessionId = recoveryData.SessionId,
                GameName = recoveryData.GameName,
                Duration = _timeProvider.UtcNow - recoveryData.StartedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to recover session {SessionId}", recoveryData.SessionId);
            return new RecoveryResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Cleans up any stale recovery data.
    /// </summary>
    public Task CleanupStaleRecoveryDataAsync()
    {
        try
        {
            if (File.Exists(_recoveryStatePath))
            {
                var fileInfo = new FileInfo(_recoveryStatePath);
                if (_timeProvider.UtcNow - fileInfo.LastWriteTimeUtc > TimeSpan.FromDays(7))
                {
                    File.Delete(_recoveryStatePath);
                    _logger.LogInformation("Cleaned up stale recovery data");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up stale recovery data");
        }

        return Task.CompletedTask;
    }
}

/// <summary>
/// Data for session recovery.
/// </summary>
public class SessionRecoveryData
{
    public Guid SessionId { get; set; }
    public Guid GameId { get; set; }
    public string GameName { get; set; } = string.Empty;
    public Guid? ProfileId { get; set; }
    public DateTime StartedAt { get; set; }
    public SystemState? SystemState { get; set; }
    public DateTime SavedAt { get; set; }
    public int? ProcessId { get; set; }
}

/// <summary>
/// Result of a recovery operation.
/// </summary>
public class RecoveryResult
{
    public bool Success { get; set; }
    public Guid? SessionId { get; set; }
    public string? GameName { get; set; }
    public TimeSpan? Duration { get; set; }
    public string? ErrorMessage { get; set; }
}
