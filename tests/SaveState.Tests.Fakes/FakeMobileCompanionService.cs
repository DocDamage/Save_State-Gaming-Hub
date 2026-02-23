using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.MobileCompanion.Models;
using SaveState.Core.MobileCompanion.Services;

namespace SaveState.Tests.Fakes;

/// <summary>
/// Fake implementation of IMobileCompanionService for integration testing.
/// </summary>
public class FakeMobileCompanionService : IMobileCompanionService
{
    private readonly ITimeProvider _timeProvider;
    private readonly Dictionary<string, PairingRequest> _pairingRequests = new();
    private readonly Dictionary<Guid, MobileDevice> _devices = new();
    private readonly Dictionary<Guid, RemoteSession> _sessions = new();
    private readonly Dictionary<Guid, Queue<RemoteCommandMessage>> _commandQueues = new();

    public FakeMobileCompanionService(ITimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    public Task<Result<PairingRequest>> CreatePairingRequestAsync(CancellationToken ct = default)
    {
        var random = new Random();
        var code = random.Next(100000, 999999).ToString();

        var request = new PairingRequest
        {
            Id = Guid.NewGuid(),
            PairingCode = code,
            CreatedAt = _timeProvider.UtcNow,
            ExpiresAt = _timeProvider.UtcNow.AddMinutes(5),
            IsUsed = false
        };

        _pairingRequests[code] = request;
        return Task.FromResult(Result<PairingRequest>.Success(request));
    }

    public Task<Result<MobileDevice>> CompletePairingAsync(string pairingCode, DeviceInfo deviceInfo, CancellationToken ct = default)
    {
        if (!_pairingRequests.TryGetValue(pairingCode, out var request))
        {
            return Task.FromResult(Result<MobileDevice>.Failure("Invalid or expired pairing code", ErrorType.NotFound));
        }

        if (request.IsUsed)
        {
            return Task.FromResult(Result<MobileDevice>.Failure("Pairing code already used", ErrorType.Conflict));
        }

        if (request.ExpiresAt < _timeProvider.UtcNow)
        {
            return Task.FromResult(Result<MobileDevice>.Failure("Pairing code has expired", ErrorType.NotFound));
        }

        var device = new MobileDevice
        {
            Id = Guid.NewGuid(),
            DeviceName = deviceInfo.DeviceName,
            DeviceType = deviceInfo.DeviceType,
            DeviceModel = deviceInfo.DeviceModel,
            OsVersion = deviceInfo.OsVersion,
            AppVersion = deviceInfo.AppVersion,
            PairedAt = _timeProvider.UtcNow,
            IsConnected = false,
            Status = ConnectionStatus.Disconnected,
            Permissions = new List<string> { "remote_control", "view_library", "receive_notifications" }
        };

        _devices[device.Id] = device;
        request.IsUsed = true;
        request.PairedDeviceId = device.Id;

        return Task.FromResult(Result<MobileDevice>.Success(device));
    }

    public Task<Result> UnpairDeviceAsync(Guid deviceId, CancellationToken ct = default)
    {
        _devices.Remove(deviceId);
        return Task.FromResult(Result.Success());
    }

    public Task<Result<IReadOnlyList<MobileDevice>>> GetPairedDevicesAsync(CancellationToken ct = default)
    {
        var devices = _devices.Values.ToList();
        return Task.FromResult(Result<IReadOnlyList<MobileDevice>>.Success(devices));
    }

    public Task<Result<MobileDevice>> GetDeviceAsync(Guid deviceId, CancellationToken ct = default)
    {
        if (_devices.TryGetValue(deviceId, out var device))
        {
            return Task.FromResult(Result<MobileDevice>.Success(device));
        }
        return Task.FromResult(Result<MobileDevice>.Failure("Device not found", ErrorType.NotFound));
    }

    public Task<Result<RemoteSession>> StartSessionAsync(Guid deviceId, string connectionId, CancellationToken ct = default)
    {
        if (!_devices.ContainsKey(deviceId))
        {
            return Task.FromResult(Result<RemoteSession>.Failure("Device not found", ErrorType.NotFound));
        }

        var session = new RemoteSession
        {
            Id = Guid.NewGuid(),
            DeviceId = deviceId,
            Device = _devices[deviceId],
            StartedAt = _timeProvider.UtcNow,
            LastActivityAt = _timeProvider.UtcNow,
            CurrentMode = RemoteControlMode.Gamepad,
            IsActive = true,
            ConnectionId = connectionId
        };

        _sessions[session.Id] = session;
        _commandQueues[deviceId] = new Queue<RemoteCommandMessage>();

        _devices[deviceId].IsConnected = true;
        _devices[deviceId].Status = ConnectionStatus.Authenticated;

        return Task.FromResult(Result<RemoteSession>.Success(session));
    }

    public Task<Result> EndSessionAsync(Guid sessionId, CancellationToken ct = default)
    {
        if (_sessions.TryGetValue(sessionId, out var session))
        {
            _sessions.Remove(sessionId);
            _commandQueues.Remove(session.DeviceId);

            if (_devices.TryGetValue(session.DeviceId, out var device))
            {
                device.IsConnected = false;
                device.Status = ConnectionStatus.Disconnected;
            }
        }
        return Task.FromResult(Result.Success());
    }

    public Task<Result<RemoteSession>> GetActiveSessionAsync(Guid deviceId, CancellationToken ct = default)
    {
        var session = _sessions.Values.FirstOrDefault(s => s.DeviceId == deviceId);
        if (session == null)
        {
            return Task.FromResult(Result<RemoteSession>.Failure("No active session", ErrorType.NotFound));
        }
        return Task.FromResult(Result<RemoteSession>.Success(session));
    }

    public Task<Result<IReadOnlyList<RemoteSession>>> GetActiveSessionsAsync(CancellationToken ct = default)
    {
        var sessions = _sessions.Values.ToList();
        return Task.FromResult(Result<IReadOnlyList<RemoteSession>>.Success(sessions));
    }

    public Task<Result> SendCommandAsync(Guid deviceId, RemoteCommandMessage command, CancellationToken ct = default)
    {
        if (!_commandQueues.TryGetValue(deviceId, out var queue))
        {
            return Task.FromResult(Result.Failure("No active session for device", ErrorType.NotFound));
        }

        lock (queue)
        {
            queue.Enqueue(command);
        }

        return Task.FromResult(Result.Success());
    }

    public Task<Result> SendGamepadInputAsync(Guid deviceId, GamepadInput input, CancellationToken ct = default)
    {
        var command = new RemoteCommandMessage
        {
            Id = Guid.NewGuid(),
            Command = RemoteControlCommand.NavigateUp,
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

    public Task<Result> SendTouchpadInputAsync(Guid deviceId, TouchpadInput input, CancellationToken ct = default)
    {
        var command = new RemoteCommandMessage
        {
            Id = Guid.NewGuid(),
            Command = RemoteControlCommand.NavigateUp,
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

    public Task<Result> SendKeyboardInputAsync(Guid deviceId, KeyboardInput input, CancellationToken ct = default)
    {
        var command = new RemoteCommandMessage
        {
            Id = Guid.NewGuid(),
            Command = RemoteControlCommand.NavigateUp,
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

    public Task<Result> SetControlModeAsync(Guid deviceId, RemoteControlMode mode, CancellationToken ct = default)
    {
        var session = _sessions.Values.FirstOrDefault(s => s.DeviceId == deviceId);
        if (session == null)
        {
            return Task.FromResult(Result.Failure("No active session", ErrorType.NotFound));
        }

        session.CurrentMode = mode;
        session.LastActivityAt = _timeProvider.UtcNow;

        return Task.FromResult(Result.Success());
    }

    public Task<Result> SendNotificationAsync(Guid deviceId, CompanionNotification notification, CancellationToken ct = default)
    {
        return Task.FromResult(Result.Success());
    }

    public Task<Result> BroadcastNotificationAsync(CompanionNotification notification, CancellationToken ct = default)
    {
        return Task.FromResult(Result.Success());
    }

    public Task<Result<LibrarySyncInfo>> GetLibrarySyncInfoAsync(CancellationToken ct = default)
    {
        var syncInfo = new LibrarySyncInfo
        {
            TotalGames = 0,
            RecentlyPlayedCount = 0,
            InstalledCount = 0,
            LastSyncAt = _timeProvider.UtcNow,
            RecentlyPlayed = new List<GameSummary>()
        };

        return Task.FromResult(Result<LibrarySyncInfo>.Success(syncInfo));
    }

    public Task<Result<SystemStatus>> GetSystemStatusAsync(CancellationToken ct = default)
    {
        var status = new SystemStatus
        {
            IsOnline = true,
            CpuUsage = 0f,
            MemoryUsage = 0f,
            CurrentlyPlayingGame = null,
            CurrentlyPlayingGameCover = null,
            SessionDuration = TimeSpan.Zero,
            IsRecording = false,
            IsStreaming = false
        };

        return Task.FromResult(Result<SystemStatus>.Success(status));
    }

    public Task<Result<GameSummary>> GetGameDetailsAsync(Guid gameId, CancellationToken ct = default)
    {
        return Task.FromResult(Result<GameSummary>.Failure("Game not found", ErrorType.NotFound));
    }

    public Task<Result> UpdateDevicePermissionsAsync(Guid deviceId, List<string> permissions, CancellationToken ct = default)
    {
        if (_devices.TryGetValue(deviceId, out var device))
        {
            device.Permissions = permissions;
            return Task.FromResult(Result.Success());
        }
        return Task.FromResult(Result.Failure("Device not found", ErrorType.NotFound));
    }

    public Task<Result<bool>> CheckPermissionAsync(Guid deviceId, string permission, CancellationToken ct = default)
    {
        if (_devices.TryGetValue(deviceId, out var device))
        {
            var hasPermission = device.Permissions?.Contains(permission) ?? false;
            return Task.FromResult(Result<bool>.Success(hasPermission));
        }
        return Task.FromResult(Result<bool>.Success(false));
    }
}
