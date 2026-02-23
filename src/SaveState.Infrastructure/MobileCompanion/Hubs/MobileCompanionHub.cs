using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.MobileCompanion.Models;
using SaveState.Core.MobileCompanion.Services;

namespace SaveState.Infrastructure.MobileCompanion.Hubs;

/// <summary>
/// SignalR hub for real-time communication between mobile companion app and desktop application.
/// </summary>
public interface IMobileCompanionClient
{
    /// <summary>
    /// Called when a command is received from the server.
    /// </summary>
    Task ReceiveCommand(RemoteCommandMessage command);

    /// <summary>
    /// Called when the control mode changes.
    /// </summary>
    Task ControlModeChanged(RemoteControlMode mode);

    /// <summary>
    /// Called when system status is updated.
    /// </summary>
    Task SystemStatusUpdated(SystemStatus status);

    /// <summary>
    /// Called when a notification is sent to the device.
    /// </summary>
    Task ReceiveNotification(CompanionNotification notification);

    /// <summary>
    /// Called when library sync info is updated.
    /// </summary>
    Task LibrarySyncUpdated(LibrarySyncInfo syncInfo);

    /// <summary>
    /// Called when the session is ended.
    /// </summary>
    Task SessionEnded(string reason);

    /// <summary>
    /// Called when an error occurs.
    /// </summary>
    Task Error(string errorMessage);
}

/// <summary>
/// SignalR hub for mobile companion real-time communication.
/// </summary>
public class MobileCompanionHub : Hub<IMobileCompanionClient>
{
    private readonly IMobileCompanionService _companionService;
    private readonly ILogger<MobileCompanionHub> _logger;

    public MobileCompanionHub(
        IMobileCompanionService companionService,
        ILogger<MobileCompanionHub> logger)
    {
        _companionService = companionService;
        _logger = logger;
    }

    /// <summary>
    /// Authenticates a mobile device using its device ID.
    /// </summary>
    public async Task<Result> AuthenticateAsync(Guid deviceId)
    {
        try
        {
            _logger.LogInformation("Authenticating device {DeviceId} with connection {ConnectionId}",
                deviceId, Context.ConnectionId);

            var deviceResult = await _companionService.GetDeviceAsync(deviceId).ConfigureAwait(false);
            if (deviceResult.IsFailure)
            {
                _logger.LogWarning("Authentication failed for device {DeviceId}: {Error}",
                    deviceId, deviceResult.Error);
                return Result.Failure($"Device not found: {deviceResult.Error}", ErrorType.NotFound);
            }

            // Store device ID in connection context
            Context.Items["DeviceId"] = deviceId;

            // Start or update session
            var sessionResult = await _companionService.StartSessionAsync(
                deviceId, Context.ConnectionId).ConfigureAwait(false);

            if (sessionResult.IsFailure)
            {
                _logger.LogError("Failed to start session for device {DeviceId}: {Error}",
                    deviceId, sessionResult.Error);
                return Result.Failure($"Failed to start session: {sessionResult.Error}", ErrorType.Internal);
            }

            // Join device-specific group for targeted messages
            await Groups.AddToGroupAsync(Context.ConnectionId, deviceId.ToString()).ConfigureAwait(false);

            _logger.LogInformation("Device {DeviceId} authenticated successfully", deviceId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during authentication for device {DeviceId}", deviceId);
            return Result.Failure($"Authentication error: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Sends a remote control command to the desktop.
    /// </summary>
    public async Task<Result> SendCommandAsync(RemoteControlCommand command, Dictionary<string, object>? parameters = null, string? gameId = null)
    {
        try
        {
            var deviceId = GetDeviceIdFromContext();
            if (deviceId == Guid.Empty)
            {
                return Result.Failure("Not authenticated", ErrorType.Unauthorized);
            }

            var message = new RemoteCommandMessage
            {
                Id = Guid.NewGuid(),
                Command = command,
                Parameters = parameters,
                Timestamp = DateTime.UtcNow,
                GameId = gameId
            };

            _logger.LogDebug("Command received from device {DeviceId}: {Command}", deviceId, command);

            return await _companionService.SendCommandAsync(deviceId, message).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing command {Command}", command);
            return Result.Failure($"Command error: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Sends gamepad input to the desktop.
    /// </summary>
    public async Task<Result> SendGamepadInputAsync(GamepadInput input)
    {
        try
        {
            var deviceId = GetDeviceIdFromContext();
            if (deviceId == Guid.Empty)
            {
                return Result.Failure("Not authenticated", ErrorType.Unauthorized);
            }

            return await _companionService.SendGamepadInputAsync(deviceId, input).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing gamepad input");
            return Result.Failure($"Input error: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Sends touchpad input to the desktop.
    /// </summary>
    public async Task<Result> SendTouchpadInputAsync(TouchpadInput input)
    {
        try
        {
            var deviceId = GetDeviceIdFromContext();
            if (deviceId == Guid.Empty)
            {
                return Result.Failure("Not authenticated", ErrorType.Unauthorized);
            }

            return await _companionService.SendTouchpadInputAsync(deviceId, input).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing touchpad input");
            return Result.Failure($"Input error: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Sends keyboard input to the desktop.
    /// </summary>
    public async Task<Result> SendKeyboardInputAsync(KeyboardInput input)
    {
        try
        {
            var deviceId = GetDeviceIdFromContext();
            if (deviceId == Guid.Empty)
            {
                return Result.Failure("Not authenticated", ErrorType.Unauthorized);
            }

            return await _companionService.SendKeyboardInputAsync(deviceId, input).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing keyboard input");
            return Result.Failure($"Input error: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Changes the remote control mode.
    /// </summary>
    public async Task<Result> SetControlModeAsync(RemoteControlMode mode)
    {
        try
        {
            var deviceId = GetDeviceIdFromContext();
            if (deviceId == Guid.Empty)
            {
                return Result.Failure("Not authenticated", ErrorType.Unauthorized);
            }

            var result = await _companionService.SetControlModeAsync(deviceId, mode).ConfigureAwait(false);

            if (result.IsSuccess)
            {
                // Notify the client about mode change
                await Clients.Caller.ControlModeChanged(mode).ConfigureAwait(false);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting control mode");
            return Result.Failure($"Mode change error: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Requests current system status.
    /// </summary>
    public async Task<Result<SystemStatus>> GetSystemStatusAsync()
    {
        try
        {
            var deviceId = GetDeviceIdFromContext();
            if (deviceId == Guid.Empty)
            {
                return Result<SystemStatus>.Failure("Not authenticated", ErrorType.Unauthorized);
            }

            return await _companionService.GetSystemStatusAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting system status");
            return Result<SystemStatus>.Failure($"Status error: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Requests library sync info.
    /// </summary>
    public async Task<Result<LibrarySyncInfo>> GetLibrarySyncInfoAsync()
    {
        try
        {
            var deviceId = GetDeviceIdFromContext();
            if (deviceId == Guid.Empty)
            {
                return Result<LibrarySyncInfo>.Failure("Not authenticated", ErrorType.Unauthorized);
            }

            return await _companionService.GetLibrarySyncInfoAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting library sync info");
            return Result<LibrarySyncInfo>.Failure($"Library sync error: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Called when a client disconnects.
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        try
        {
            var deviceId = GetDeviceIdFromContext();

            if (deviceId != Guid.Empty)
            {
                _logger.LogInformation("Device {DeviceId} disconnected (Connection: {ConnectionId})",
                    deviceId, Context.ConnectionId);

                // Get active session and end it
                var sessionResult = await _companionService.GetActiveSessionAsync(deviceId).ConfigureAwait(false);
                if (sessionResult.IsSuccess && sessionResult.Value.ConnectionId == Context.ConnectionId)
                {
                    await _companionService.EndSessionAsync(sessionResult.Value.Id).ConfigureAwait(false);
                }

                // Leave device group
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, deviceId.ToString()).ConfigureAwait(false);
            }

            if (exception != null)
            {
                _logger.LogError(exception, "Client disconnected with error");
            }
        }
        finally
        {
            await base.OnDisconnectedAsync(exception).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Called when a client connects.
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync().ConfigureAwait(false);
    }

    private Guid GetDeviceIdFromContext()
    {
        if (Context.Items.TryGetValue("DeviceId", out var value) && value is Guid deviceId)
        {
            return deviceId;
        }
        return Guid.Empty;
    }
}
