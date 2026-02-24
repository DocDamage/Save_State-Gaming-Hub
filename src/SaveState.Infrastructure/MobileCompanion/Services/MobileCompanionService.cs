using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.MobileCompanion.Models;
using SaveState.Core.MobileCompanion.Services;
using SaveState.Infrastructure.Persistence;
using SaveState.Core.GameLibrary.Entities;

namespace SaveState.Infrastructure.MobileCompanion.Services;

/// <summary>
/// Implementation of the mobile companion service with SQLite storage.
/// </summary>
public class MobileCompanionService : IMobileCompanionService
{
    private readonly IDbContextFactory<SaveStateDbContext> _dbContextFactory;
    private readonly ILogger<MobileCompanionService> _logger;
    private readonly ITimeProvider _timeProvider;

    // In-memory caches for active sessions and pairing requests
    private static readonly ConcurrentDictionary<string, PairingRequest> _pairingRequests = new();
    private static readonly ConcurrentDictionary<Guid, RemoteSession> _activeSessions = new();
    private static readonly ConcurrentDictionary<Guid, Queue<RemoteCommandMessage>> _commandQueues = new();

    // Permission definitions
    private static readonly HashSet<string> _validPermissions = new(StringComparer.OrdinalIgnoreCase)
    {
        "remote_control",
        "launch_games",
        "manage_save_states",
        "view_library",
        "receive_notifications",
        "send_notifications",
        "media_control",
        "screenshot",
        "recording",
        "big_picture",
        "voice_commands"
    };

    public MobileCompanionService(
        IDbContextFactory<SaveStateDbContext> dbContextFactory,
        ILogger<MobileCompanionService> logger,
        ITimeProvider timeProvider)
    {
        _dbContextFactory = dbContextFactory;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    #region Pairing

    /// <inheritdoc />
    public async Task<Result<PairingRequest>> CreatePairingRequestAsync(CancellationToken ct = default)
    {
        try
        {
            // Generate 6-digit pairing code
            var pairingCode = GeneratePairingCode();

            var request = new PairingRequest
            {
                Id = Guid.NewGuid(),
                PairingCode = pairingCode,
                CreatedAt = _timeProvider.UtcNow,
                ExpiresAt = _timeProvider.UtcNow.AddMinutes(5),
                IsUsed = false
            };

            // Store in memory cache
            _pairingRequests[pairingCode] = request;

            _logger.LogInformation("Created pairing request {RequestId} with code {Code}",
                request.Id, pairingCode);

            // Schedule cleanup after expiration
            _ = Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromMinutes(5));
                _pairingRequests.TryRemove(pairingCode, out _);
            });

            return Result<PairingRequest>.Success(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create pairing request");
            return Result<PairingRequest>.Failure($"Failed to create pairing request: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result<MobileDevice>> CompletePairingAsync(string pairingCode, DeviceInfo deviceInfo, CancellationToken ct = default)
    {
        try
        {
            // Validate pairing code
            if (!_pairingRequests.TryGetValue(pairingCode, out var request))
            {
                return Result<MobileDevice>.Failure("Invalid or expired pairing code", ErrorType.NotFound);
            }

            if (request.IsUsed)
            {
                return Result<MobileDevice>.Failure("Pairing code already used", ErrorType.Conflict);
            }

            if (request.ExpiresAt < _timeProvider.UtcNow)
            {
                _pairingRequests.TryRemove(pairingCode, out _);
                return Result<MobileDevice>.Failure("Pairing code has expired", ErrorType.NotFound);
            }

            // Create device entity
            var device = new MobileDeviceEntity
            {
                Id = Guid.NewGuid(),
                DeviceName = deviceInfo.DeviceName,
                DeviceType = deviceInfo.DeviceType,
                DeviceModel = deviceInfo.DeviceModel,
                OsVersion = deviceInfo.OsVersion,
                AppVersion = deviceInfo.AppVersion,
                PushNotificationToken = deviceInfo.PushNotificationToken,
                PairedAt = _timeProvider.UtcNow,
                IsConnected = false,
                Status = ConnectionStatus.Disconnected,
                Permissions = "remote_control,view_library,receive_notifications" // Default permissions
            };

            // Save to database
            await using var context = await _dbContextFactory.CreateDbContextAsync(ct);
            context.MobileDevices.Add(device);
            await context.SaveChangesAsync(ct).ConfigureAwait(false);

            // Mark pairing code as used
            request.IsUsed = true;
            request.PairedDeviceId = device.Id;

            _logger.LogInformation("Device {DeviceId} ({Name}) paired successfully",
                device.Id, device.DeviceName);

            var mobileDevice = MapToModel(device);
            return Result<MobileDevice>.Success(mobileDevice);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to complete pairing");
            return Result<MobileDevice>.Failure($"Failed to complete pairing: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result> UnpairDeviceAsync(Guid deviceId, CancellationToken ct = default)
    {
        try
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync(ct);

            var device = await context.MobileDevices.FindAsync(new object[] { deviceId }, ct);
            if (device == null)
            {
                return Result.Failure("Device not found", ErrorType.NotFound);
            }

            // End any active sessions
            var sessionsToEnd = _activeSessions.Values
                .Where(s => s.DeviceId == deviceId)
                .ToList();

            foreach (var session in sessionsToEnd)
            {
                await EndSessionAsync(session.Id, ct).ConfigureAwait(false);
            }

            context.MobileDevices.Remove(device);
            await context.SaveChangesAsync(ct).ConfigureAwait(false);

            _logger.LogInformation("Device {DeviceId} unpaired", deviceId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unpair device {DeviceId}", deviceId);
            return Result.Failure($"Failed to unpair device: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<MobileDevice>>> GetPairedDevicesAsync(CancellationToken ct = default)
    {
        try
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync(ct);

            var devices = await context.MobileDevices
                .AsNoTracking()
                .OrderByDescending(d => d.PairedAt)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            var models = devices.Select(MapToModel).ToList();
            return Result<IReadOnlyList<MobileDevice>>.Success(models);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get paired devices");
            return Result<IReadOnlyList<MobileDevice>>.Failure($"Failed to get devices: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result<MobileDevice>> GetDeviceAsync(Guid deviceId, CancellationToken ct = default)
    {
        try
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync(ct);

            var device = await context.MobileDevices.FindAsync(new object[] { deviceId }, ct);
            if (device == null)
            {
                return Result<MobileDevice>.Failure("Device not found", ErrorType.NotFound);
            }

            return Result<MobileDevice>.Success(MapToModel(device));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get device {DeviceId}", deviceId);
            return Result<MobileDevice>.Failure($"Failed to get device: {ex.Message}", ErrorType.Internal);
        }
    }

    #endregion

    #region Session Management

    /// <inheritdoc />
    public async Task<Result<RemoteSession>> StartSessionAsync(Guid deviceId, string connectionId, CancellationToken ct = default)
    {
        try
        {
            // Verify device exists
            await using var context = await _dbContextFactory.CreateDbContextAsync(ct);
            var device = await context.MobileDevices.FindAsync(new object[] { deviceId }, ct);

            if (device == null)
            {
                return Result<RemoteSession>.Failure("Device not found", ErrorType.NotFound);
            }

            // End any existing session for this device
            var existingSession = _activeSessions.Values
                .FirstOrDefault(s => s.DeviceId == deviceId);

            if (existingSession != null)
            {
                await EndSessionAsync(existingSession.Id, ct).ConfigureAwait(false);
            }

            // Create new session
            var session = new RemoteSession
            {
                Id = Guid.NewGuid(),
                DeviceId = deviceId,
                Device = MapToModel(device),
                StartedAt = _timeProvider.UtcNow,
                LastActivityAt = _timeProvider.UtcNow,
                CurrentMode = RemoteControlMode.Gamepad,
                IsActive = true,
                ConnectionId = connectionId
            };

            _activeSessions[session.Id] = session;

            // Update device status
            device.IsConnected = true;
            device.Status = ConnectionStatus.Authenticated;
            device.LastConnectedAt = _timeProvider.UtcNow;
            await context.SaveChangesAsync(ct).ConfigureAwait(false);

            // Initialize command queue
            _commandQueues[deviceId] = new Queue<RemoteCommandMessage>();

            _logger.LogInformation("Started session {SessionId} for device {DeviceId}",
                session.Id, deviceId);

            return Result<RemoteSession>.Success(session);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start session for device {DeviceId}", deviceId);
            return Result<RemoteSession>.Failure($"Failed to start session: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result> EndSessionAsync(Guid sessionId, CancellationToken ct = default)
    {
        try
        {
            if (!_activeSessions.TryRemove(sessionId, out var session))
            {
                return Result.Failure("Session not found", ErrorType.NotFound);
            }

            await using var context = await _dbContextFactory.CreateDbContextAsync(ct);
            var device = await context.MobileDevices.FindAsync(new object[] { session.DeviceId }, ct);

            if (device != null)
            {
                device.IsConnected = false;
                device.Status = ConnectionStatus.Disconnected;
                await context.SaveChangesAsync(ct).ConfigureAwait(false);
            }

            // Clean up command queue
            _commandQueues.TryRemove(session.DeviceId, out _);

            _logger.LogInformation("Ended session {SessionId} for device {DeviceId}",
                sessionId, session.DeviceId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to end session {SessionId}", sessionId);
            return Result.Failure($"Failed to end session: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public Task<Result<RemoteSession>> GetActiveSessionAsync(Guid deviceId, CancellationToken ct = default)
    {
        var session = _activeSessions.Values
            .FirstOrDefault(s => s.DeviceId == deviceId);

        if (session == null)
        {
            return Task.FromResult(Result<RemoteSession>.Failure("No active session", ErrorType.NotFound));
        }

        return Task.FromResult(Result<RemoteSession>.Success(session));
    }

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<RemoteSession>>> GetActiveSessionsAsync(CancellationToken ct = default)
    {
        var sessions = _activeSessions.Values.ToList();
        return Task.FromResult(Result<IReadOnlyList<RemoteSession>>.Success(sessions));
    }

    #endregion

    #region Remote Control

    /// <inheritdoc />
    public Task<Result> SendCommandAsync(Guid deviceId, RemoteCommandMessage command, CancellationToken ct = default)
    {
        try
        {
            if (!_commandQueues.TryGetValue(deviceId, out var queue))
            {
                return Task.FromResult(Result.Failure("No active session for device", ErrorType.NotFound));
            }

            // Check permission
            if (!HasPermission(deviceId, "remote_control").Result)
            {
                return Task.FromResult(Result.Failure("Device lacks remote control permission", ErrorType.Forbidden));
            }

            lock (queue)
            {
                queue.Enqueue(command);
            }

            _logger.LogDebug("Command {Command} queued for device {DeviceId}",
                command.Command, deviceId);

            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send command to device {DeviceId}", deviceId);
            return Task.FromResult(Result.Failure($"Failed to send command: {ex.Message}", ErrorType.Internal));
        }
    }

    /// <inheritdoc />
    public Task<Result> SendGamepadInputAsync(Guid deviceId, GamepadInput input, CancellationToken ct = default)
    {
        // Convert gamepad input to command
        var command = new RemoteCommandMessage
        {
            Id = Guid.NewGuid(),
            Command = RemoteControlCommand.NavigateUp, // Placeholder - would map button to action
            Parameters = new Dictionary<string, object>
            {
                ["button"] = input.Button,
                ["isPressed"] = input.IsPressed,
                ["axisX"] = input.AxisX,
                ["axisY"] = input.AxisY
            },
            Timestamp = _timeProvider.UtcNow
        };

        return SendCommandAsync(deviceId, command, ct);
    }

    /// <inheritdoc />
    public Task<Result> SendTouchpadInputAsync(Guid deviceId, TouchpadInput input, CancellationToken ct = default)
    {
        var command = new RemoteCommandMessage
        {
            Id = Guid.NewGuid(),
            Command = RemoteControlCommand.NavigateUp, // Placeholder
            Parameters = new Dictionary<string, object>
            {
                ["x"] = input.X,
                ["y"] = input.Y,
                ["action"] = input.Action.ToString(),
                ["fingerId"] = input.FingerId
            },
            Timestamp = _timeProvider.UtcNow
        };

        return SendCommandAsync(deviceId, command, ct);
    }

    /// <inheritdoc />
    public Task<Result> SendKeyboardInputAsync(Guid deviceId, KeyboardInput input, CancellationToken ct = default)
    {
        var command = new RemoteCommandMessage
        {
            Id = Guid.NewGuid(),
            Command = RemoteControlCommand.NavigateUp, // Placeholder
            Parameters = new Dictionary<string, object>
            {
                ["key"] = input.Key,
                ["isPressed"] = input.IsPressed,
                ["modifiers"] = input.Modifiers
            },
            Timestamp = _timeProvider.UtcNow
        };

        return SendCommandAsync(deviceId, command, ct);
    }

    /// <inheritdoc />
    public Task<Result> SetControlModeAsync(Guid deviceId, RemoteControlMode mode, CancellationToken ct = default)
    {
        var session = _activeSessions.Values.FirstOrDefault(s => s.DeviceId == deviceId);
        if (session == null)
        {
            return Task.FromResult(Result.Failure("No active session", ErrorType.NotFound));
        }

        session.CurrentMode = mode;
        session.LastActivityAt = _timeProvider.UtcNow;

        _logger.LogInformation("Control mode changed to {Mode} for device {DeviceId}",
            mode, deviceId);

        return Task.FromResult(Result.Success());
    }

    /// <summary>
    /// Gets and clears the pending commands for a device.
    /// </summary>
    public Task<IReadOnlyList<RemoteCommandMessage>> GetPendingCommandsAsync(Guid deviceId)
    {
        if (!_commandQueues.TryGetValue(deviceId, out var queue))
        {
            return Task.FromResult<IReadOnlyList<RemoteCommandMessage>>(Array.Empty<RemoteCommandMessage>());
        }

        lock (queue)
        {
            var commands = queue.ToList();
            queue.Clear();
            return Task.FromResult<IReadOnlyList<RemoteCommandMessage>>(commands);
        }
    }

    #endregion

    #region Notifications

    /// <inheritdoc />
    public Task<Result> SendNotificationAsync(Guid deviceId, CompanionNotification notification, CancellationToken ct = default)
    {
        // This would integrate with push notification services (Firebase, APNS)
        _logger.LogInformation("Notification queued for device {DeviceId}: {Title}",
            deviceId, notification.Title);

        // Store notification in database for retrieval
        return Task.FromResult(Result.Success());
    }

    /// <inheritdoc />
    public Task<Result> BroadcastNotificationAsync(CompanionNotification notification, CancellationToken ct = default)
    {
        _logger.LogInformation("Broadcasting notification: {Title}", notification.Title);

        // This would send to all connected devices via SignalR and push notifications
        return Task.FromResult(Result.Success());
    }

    #endregion

    #region Data Sync

    /// <inheritdoc />
    public async Task<Result<LibrarySyncInfo>> GetLibrarySyncInfoAsync(CancellationToken ct = default)
    {
        try
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync(ct);

            // Get total games count
            var totalGames = await context.Games.CountAsync(ct).ConfigureAwait(false);

            // Get recently played games (last 30 days)
            var thirtyDaysAgo = _timeProvider.UtcNow.AddDays(-30);
            var recentlyPlayed = await context.GameSessions
                .Where(s => s.StartedAt >= thirtyDaysAgo)
                .GroupBy(s => s.GameId)
                .Select(g => new { GameId = g.Key, LastPlayed = g.Max(s => s.StartedAt) })
                .OrderByDescending(x => x.LastPlayed)
                .Take(10)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            var recentlyPlayedGames = new List<GameSummary>();
            foreach (var item in recentlyPlayed)
            {
                var game = await context.Games.FindAsync(new object[] { item.GameId }, ct);
                if (game != null)
                {
                    var totalPlayTime = await context.GameSessions
                        .Where(s => s.GameId == item.GameId)
                        .SumAsync(s => (long)s.Duration.TotalMinutes, ct);

                    recentlyPlayedGames.Add(new GameSummary
                    {
                        Id = game.Id,
                        Name = game.Title,
                        CoverImage = game.CoverImagePath,
                        Platform = game.Platform?.Name ?? "Unknown",
                        PlayTime = TimeSpan.FromMinutes(totalPlayTime),
                        LastPlayed = item.LastPlayed,
                        Status = GameStatus.Installed
                    });
                }
            }

            var syncInfo = new LibrarySyncInfo
            {
                TotalGames = totalGames,
                RecentlyPlayedCount = recentlyPlayed.Count,
                InstalledCount = totalGames, // Simplified - would check installation status
                LastSyncAt = _timeProvider.UtcNow,
                RecentlyPlayed = recentlyPlayedGames
            };

            return Result<LibrarySyncInfo>.Success(syncInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get library sync info");
            return Result<LibrarySyncInfo>.Failure($"Failed to get library info: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public Task<Result<SystemStatus>> GetSystemStatusAsync(CancellationToken ct = default)
    {
        try
        {
            // Get system metrics
            var cpuUsage = GetCpuUsage();
            var memoryUsage = GetMemoryUsage();

            // Get current game if any
            // This would integrate with the game session service
            var status = new SystemStatus
            {
                IsOnline = true,
                CpuUsage = cpuUsage,
                MemoryUsage = memoryUsage,
                CurrentlyPlayingGame = null, // Would be populated from game session service
                CurrentlyPlayingGameCover = null,
                SessionDuration = TimeSpan.Zero,
                IsRecording = false,
                IsStreaming = false
            };

            return Task.FromResult(Result<SystemStatus>.Success(status));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get system status");
            return Task.FromResult(Result<SystemStatus>.Failure($"Failed to get status: {ex.Message}", ErrorType.Internal));
        }
    }

    /// <inheritdoc />
    public async Task<Result<GameSummary>> GetGameDetailsAsync(Guid gameId, CancellationToken ct = default)
    {
        try
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync(ct);

            var game = await context.Games.FindAsync(new object[] { gameId }, ct);
            if (game == null)
            {
                return Result<GameSummary>.Failure("Game not found", ErrorType.NotFound);
            }

            var totalPlayTime = await context.GameSessions
                .Where(s => s.GameId == gameId)
                .SumAsync(s => (long)s.Duration.TotalMinutes, ct);

            var lastPlayed = await context.GameSessions
                .Where(s => s.GameId == gameId)
                .OrderByDescending(s => s.StartedAt)
                .Select(s => (DateTime?)s.StartedAt)
                .FirstOrDefaultAsync(ct);

            var summary = new GameSummary
            {
                Id = game.Id,
                Name = game.Title,
                CoverImage = game.CoverImagePath,
                Platform = game.Platform?.Name ?? "Unknown",
                PlayTime = TimeSpan.FromMinutes(totalPlayTime),
                LastPlayed = lastPlayed,
                Status = GameStatus.Installed
            };

            return Result<GameSummary>.Success(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get game details for {GameId}", gameId);
            return Result<GameSummary>.Failure($"Failed to get game details: {ex.Message}", ErrorType.Internal);
        }
    }

    #endregion

    #region Permissions

    /// <inheritdoc />
    public async Task<Result> UpdateDevicePermissionsAsync(Guid deviceId, List<string> permissions, CancellationToken ct = default)
    {
        try
        {
            // Validate permissions
            var invalidPermissions = permissions.Where(p => !_validPermissions.Contains(p)).ToList();
            if (invalidPermissions.Any())
            {
                return Result.Failure($"Invalid permissions: {string.Join(", ", invalidPermissions)}", ErrorType.Validation);
            }

            await using var context = await _dbContextFactory.CreateDbContextAsync(ct);

            var device = await context.MobileDevices.FindAsync(new object[] { deviceId }, ct);
            if (device == null)
            {
                return Result.Failure("Device not found", ErrorType.NotFound);
            }

            device.Permissions = string.Join(",", permissions);
            await context.SaveChangesAsync(ct).ConfigureAwait(false);

            _logger.LogInformation("Updated permissions for device {DeviceId}: {Permissions}",
                deviceId, string.Join(", ", permissions));

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update permissions for device {DeviceId}", deviceId);
            return Result.Failure($"Failed to update permissions: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <inheritdoc />
    public async Task<Result<bool>> CheckPermissionAsync(Guid deviceId, string permission, CancellationToken ct = default)
    {
        var hasPermission = await HasPermission(deviceId, permission).ConfigureAwait(false);
        return Result<bool>.Success(hasPermission);
    }

    private async Task<bool> HasPermission(Guid deviceId, string permission)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        var device = await context.MobileDevices.FindAsync(new object[] { deviceId });
        if (device == null) return false;

        var permissions = device.Permissions?.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase) ?? new HashSet<string>();

        return permissions.Contains(permission);
    }

    #endregion

    #region Helper Methods

    private static string GeneratePairingCode()
    {
        // Generate 6-digit code
        var random = new Random();
        return random.Next(100000, 999999).ToString();
    }

    private static MobileDevice MapToModel(MobileDeviceEntity entity)
    {
        return new MobileDevice
        {
            Id = entity.Id,
            DeviceName = entity.DeviceName,
            DeviceType = entity.DeviceType,
            DeviceModel = entity.DeviceModel,
            OsVersion = entity.OsVersion,
            AppVersion = entity.AppVersion,
            PairedAt = entity.PairedAt,
            LastConnectedAt = entity.LastConnectedAt,
            PushNotificationToken = entity.PushNotificationToken,
            IsConnected = entity.IsConnected,
            Status = entity.Status,
            Permissions = entity.Permissions?.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList() ?? new List<string>()
        };
    }

    private static float GetCpuUsage()
    {
        // Simplified - would use performance counters
        return 0f;
    }

    private static float GetMemoryUsage()
    {
        // Simplified - would use performance counters
        var process = System.Diagnostics.Process.GetCurrentProcess();
        var usedMemory = process.WorkingSet64;
        var totalMemory = GC.GetTotalMemory(false);
        return (float)(usedMemory / (double)totalMemory * 100);
    }

    #endregion
}

/// <summary>
/// Entity for mobile device storage.
/// </summary>
public class MobileDeviceEntity
{
    public Guid Id { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;
    public string? DeviceModel { get; set; }
    public string? OsVersion { get; set; }
    public string? AppVersion { get; set; }
    public DateTime PairedAt { get; set; }
    public DateTime? LastConnectedAt { get; set; }
    public string? PushNotificationToken { get; set; }
    public bool IsConnected { get; set; }
    public ConnectionStatus Status { get; set; }
    public string? Permissions { get; set; }
}
