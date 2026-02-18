// Copyright (c) 2026 SaveStateReborn. All rights reserved.

using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.GameLibrary;
using SaveState.Core.SmartLauncher;
using SaveState.Core.Common.ValueObjects;

namespace SaveState.Application.SmartLauncher;

/// <summary>
/// Service for smart game launching with system optimization.
/// </summary>
public sealed class SmartLauncherService : ISmartLauncherService
{
    private readonly ILogger<SmartLauncherService> _logger;
    private readonly ILaunchProfileRepository _profileRepository;
    private readonly ILaunchSessionRepository _sessionRepository;
    private readonly ISystemOptimizerService _optimizerService;
    private readonly IGameProcessMonitor _processMonitor;
    private readonly IGameRepository _gameRepository;
    private readonly ITimeProvider _timeProvider;

    public SmartLauncherService(
        ILogger<SmartLauncherService> logger,
        ILaunchProfileRepository profileRepository,
        ILaunchSessionRepository sessionRepository,
        ISystemOptimizerService optimizerService,
        IGameProcessMonitor processMonitor,
        IGameRepository gameRepository,
        ITimeProvider timeProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _profileRepository = profileRepository ?? throw new ArgumentNullException(nameof(profileRepository));
        _sessionRepository = sessionRepository ?? throw new ArgumentNullException(nameof(sessionRepository));
        _optimizerService = optimizerService ?? throw new ArgumentNullException(nameof(optimizerService));
        _processMonitor = processMonitor ?? throw new ArgumentNullException(nameof(processMonitor));
        _gameRepository = gameRepository ?? throw new ArgumentNullException(nameof(gameRepository));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

        _processMonitor.ProcessExited += OnGameProcessExited;
    }

    /// <inheritdoc />
    public async Task<LaunchResult> LaunchGameAsync(Guid gameId, Guid? profileId = null, CancellationToken ct = default)
    {
        try
        {
            // Check if there's already an active session
            var activeSessionResult = await _sessionRepository.GetActiveSessionAsync(ct);
            if (activeSessionResult.IsSuccess)
            {
                _logger.LogWarning("Another game is already running in session {SessionId}", activeSessionResult.Value.Id);
                return LaunchResult.Failed("Another game is already running. Please close it first.");
            }

            // Get game info
            var game = await _gameRepository.GetByIdAsync(GameId.From(gameId), ct);
            if (game == null)
            {
                return LaunchResult.Failed("Game not found");
            }

            if (string.IsNullOrEmpty(game.ExecutablePath))
            {
                return LaunchResult.Failed("Game executable not configured");
            }

            // Get launch profile
            LaunchProfile profile;
            if (profileId.HasValue)
            {
                var profileResult = await _profileRepository.GetProfileAsync(profileId.Value, ct);
                if (!profileResult.IsSuccess)
                {
                    return LaunchResult.Failed($"Profile not found: {profileResult.Error}");
                }
                profile = profileResult.Value;
            }
            else
            {
                var profileResult = await _profileRepository.GetDefaultProfileAsync(gameId, ct);
                profile = profileResult.IsSuccess ? profileResult.Value : LaunchProfile.CreateBalancedProfile();
            }

            _logger.LogInformation("Launching {Game} with profile {Profile}", game.Title, profile.Name);

            // Create session
            var session = new LaunchSession
            {
                GameId = gameId,
                GameName = game.Title,
                ProfileId = profile.Id,
                StartedAt = _timeProvider.UtcNow
            };

            await _sessionRepository.CreateSessionAsync(session, ct);

            // Apply optimizations
            var optimizations = new List<string>();
            SystemState? initialState = null;

            try
            {
                initialState = await _optimizerService.ApplyOptimizationsAsync(profile, ct);
                session.InitialSystemState = initialState;
                await _sessionRepository.UpdateSessionAsync(session, ct);

                optimizations.Add($"Priority set to {profile.Priority}");
                if (profile.PerformanceSettings.EnableMemoryOptimization)
                    optimizations.Add("Memory optimized");
                if (profile.ProcessesToSuspend.Any())
                    optimizations.Add($"{initialState.SuspendedProcesses.Count} processes suspended");
                if (profile.ServicesToStop.Any())
                    optimizations.Add($"{initialState.StoppedServices.Count} services stopped");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply some optimizations");
            }

            // Launch the game
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = game.ExecutablePath,
                WorkingDirectory = System.IO.Path.GetDirectoryName(game.ExecutablePath) ?? "",
                UseShellExecute = true
            };

            if (profile.RunAsAdministrator)
            {
                startInfo.Verb = "runas";
            }

            var process = System.Diagnostics.Process.Start(startInfo);
            if (process == null)
            {
                // Restore system state on failure
                if (initialState != null)
                {
                    await _optimizerService.RestoreSystemStateAsync(initialState, ct);
                }
                await _sessionRepository.EndSessionAsync(session.Id, -1, null, ct);
                return LaunchResult.Failed("Failed to start game process");
            }

            // Set process priority
            try
            {
                await _optimizerService.SetProcessPriorityAsync(process.Id, profile.Priority, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to set process priority");
            }

            // Start monitoring
            await _processMonitor.StartMonitoringAsync(process.Id, session.Id, ct);

            _logger.LogInformation("Game launched successfully. Process ID: {ProcessId}, Session: {SessionId}",
                process.Id, session.Id);

            return LaunchResult.Successful(
                process.Id,
                session.Id,
                optimizations,
                profile.EstimatedPerformanceGain);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to launch game");
            return LaunchResult.Failed($"Launch failed: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<LaunchProfile>> GetProfilesAsync(Guid? gameId = null, CancellationToken ct = default)
    {
        return await _profileRepository.GetProfilesAsync(gameId, ct);
    }

    /// <inheritdoc />
    public async Task<Result<LaunchProfile>> CreateProfileAsync(LaunchProfile profile, CancellationToken ct = default)
    {
        try
        {
            profile.Id = Guid.NewGuid();
            profile.CreatedAt = _timeProvider.UtcNow;
            await _profileRepository.SaveProfileAsync(profile, ct);
            return Result.Success(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create profile");
            return Result.Failure<LaunchProfile>("Failed to create profile");
        }
    }

    /// <inheritdoc />
    public async Task<Result> UpdateProfileAsync(LaunchProfile profile, CancellationToken ct = default)
    {
        try
        {
            profile.ModifiedAt = _timeProvider.UtcNow;
            await _profileRepository.SaveProfileAsync(profile, ct);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update profile");
            return Result.Failure("Failed to update profile");
        }
    }

    /// <inheritdoc />
    public async Task<Result> DeleteProfileAsync(Guid profileId, CancellationToken ct = default)
    {
        try
        {
            await _profileRepository.DeleteProfileAsync(profileId, ct);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete profile");
            return Result.Failure("Failed to delete profile");
        }
    }

    /// <inheritdoc />
    public async Task<Result<LaunchProfile>> GetDefaultProfileAsync(Guid gameId, CancellationToken ct = default)
    {
        return await _profileRepository.GetDefaultProfileAsync(gameId, ct);
    }

    /// <inheritdoc />
    public async Task<Result> SetDefaultProfileAsync(Guid gameId, Guid profileId, CancellationToken ct = default)
    {
        try
        {
            await _profileRepository.SetDefaultProfileAsync(gameId, profileId, ct);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set default profile");
            return Result.Failure("Failed to set default profile");
        }
    }

    /// <inheritdoc />
    public async Task<Result<LaunchSession>> GetActiveSessionAsync(CancellationToken ct = default)
    {
        return await _sessionRepository.GetActiveSessionAsync(ct);
    }

    /// <inheritdoc />
    public async Task<Result> EndSessionAsync(Guid sessionId, CancellationToken ct = default)
    {
        try
        {
            var sessionResult = await _sessionRepository.GetSessionAsync(sessionId, ct);
            if (!sessionResult.IsSuccess)
            {
                return Result.Failure($"Session not found: {sessionResult.Error}");
            }
            var session = sessionResult.Value;

            // Stop monitoring
            SessionPerformanceMetrics? metrics = null;
            try
            {
                metrics = await _processMonitor.StopMonitoringAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get final metrics");
            }

            // Restore system state
            if (session.InitialSystemState != null)
            {
                try
                {
                    await _optimizerService.RestoreSystemStateAsync(session.InitialSystemState, ct);
                    _logger.LogInformation("System state restored for session {SessionId}", sessionId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to restore system state");
                }
            }

            // End session
            await _sessionRepository.EndSessionAsync(sessionId, null, metrics, ct);

            _logger.LogInformation("Session {SessionId} ended. Duration: {Duration}",
                sessionId, DateTime.UtcNow - sessionResult.Value.StartedAt);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to end session");
            return Result.Failure("Failed to end session");
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<LaunchSession>> GetLaunchHistoryAsync(Guid gameId, int count = 10, CancellationToken ct = default)
    {
        return await _sessionRepository.GetLaunchHistoryAsync(gameId, count, ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> PreviewOptimizationsAsync(Guid? profileId, CancellationToken ct = default)
    {
        var optimizations = new List<string>();

        LaunchProfile profile;
        if (profileId.HasValue)
        {
            var profileResult = await _profileRepository.GetProfileAsync(profileId.Value, ct);
            if (!profileResult.IsSuccess)
            {
                return optimizations;
            }
            profile = profileResult.Value;
        }
        else
        {
            profile = LaunchProfile.CreateBalancedProfile();
        }

        optimizations.Add($"Process priority: {profile.Priority}");

        if (profile.PerformanceSettings.EnableMemoryOptimization)
            optimizations.Add("Memory optimization");

        if (profile.PerformanceSettings.ClearStandbyList)
            optimizations.Add("Clear standby memory list");

        if (profile.ProcessesToSuspend.Any())
            optimizations.Add($"Suspend {profile.ProcessesToSuspend.Count} background processes");

        if (profile.ServicesToStop.Any())
            optimizations.Add($"Stop {profile.ServicesToStop.Count} services");

        if (!string.IsNullOrEmpty(profile.PowerPlanGuid))
            optimizations.Add("Switch to high-performance power plan");

        return optimizations;
    }

    private async void OnGameProcessExited(object? sender, GameProcessExitedEventArgs e)
    {
        try
        {
            _logger.LogInformation("Game process {ProcessId} exited with code {ExitCode}",
                e.ProcessId, e.ExitCode);

            await EndSessionAsync(e.SessionId, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle game process exit");
        }
    }
}
