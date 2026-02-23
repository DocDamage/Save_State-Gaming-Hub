using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using SaveState.Core.MobileCompanion.Models;
using CompanionNotification = SaveState.Core.MobileCompanion.Models.CompanionNotification;
using SystemStatus = SaveState.Core.MobileCompanion.Models.SystemStatus;
using TouchpadInput = SaveState.Core.MobileCompanion.Models.TouchpadInput;

namespace SaveState.Presentation.Services;

public interface IMobileConnectionManager
{
    event EventHandler<RemoteCommandMessage> OnCommandReceived;
    event EventHandler<MobileConnectionStatus> OnStatusChanged;
    event EventHandler<CompanionNotification> OnNotificationReceived;
    event EventHandler<SystemStatus> OnStatusUpdate;
    
    MobileConnectionStatus Status { get; }
    bool IsConnected { get; }
    
    Task ConnectAsync(string hubUrl, string pairingCode);
    Task DisconnectAsync();
    Task SendCommandAsync(RemoteControlCommand command, Dictionary<string, object> parameters);
    Task SendGamepadInputAsync(string button, bool isPressed);
    Task SendTouchpadInputAsync(TouchpadInput input);
    Task<SystemStatus> GetSystemStatusAsync();
    Task<LibrarySyncInfo> SyncLibraryAsync();
}

public enum MobileConnectionStatus
{
    Disconnected,
    Connecting,
    Connected,
    Error
}

public class MobileConnectionManager : IMobileConnectionManager, IAsyncDisposable
{
    private HubConnection _hubConnection;
    private readonly ILogger<MobileConnectionManager> _logger;
    private int _reconnectAttempt;
    private const int MaxReconnectAttempts = 5;
    
    public MobileConnectionStatus Status { get; private set; } = MobileConnectionStatus.Disconnected;
    public bool IsConnected => Status == MobileConnectionStatus.Connected;
    
    public event EventHandler<RemoteCommandMessage> OnCommandReceived;
    public event EventHandler<MobileConnectionStatus> OnStatusChanged;
    public event EventHandler<CompanionNotification> OnNotificationReceived;
    public event EventHandler<SystemStatus> OnStatusUpdate;

    public MobileConnectionManager(ILogger<MobileConnectionManager> logger)
    {
        _logger = logger;
    }

    public async Task ConnectAsync(string hubUrl, string pairingCode)
    {
        if (_hubConnection != null)
        {
            await DisconnectAsync();
        }

        UpdateStatus(MobileConnectionStatus.Connecting);
        _reconnectAttempt = 0;

        try
        {
            _hubConnection = new HubConnectionBuilder()
                .WithUrl($"{hubUrl}/hubs/mobile")
                .WithAutomaticReconnect(new[] { 
                    TimeSpan.Zero, 
                    TimeSpan.FromSeconds(2), 
                    TimeSpan.FromSeconds(10), 
                    TimeSpan.FromSeconds(30) 
                })
                .Build();

            _hubConnection.On<RemoteCommandMessage>("ReceiveCommand", command =>
            {
                _logger.LogDebug("Received command: {Command}", command.Command);
                OnCommandReceived?.Invoke(this, command);
            });

            _hubConnection.On<CompanionNotification>("ReceiveNotification", notification =>
            {
                _logger.LogDebug("Received notification: {Title}", notification.Title);
                OnNotificationReceived?.Invoke(this, notification);
            });

            _hubConnection.On<SystemStatus>("ReceiveStatusUpdate", status =>
            {
                OnStatusUpdate?.Invoke(this, status);
            });

            _hubConnection.Reconnecting += error =>
            {
                _logger.LogWarning("Reconnecting due to error: {Error}", error?.Message);
                UpdateStatus(MobileConnectionStatus.Connecting);
                return Task.CompletedTask;
            };

            _hubConnection.Reconnected += connectionId =>
            {
                _logger.LogInformation("Reconnected with ID: {ConnectionId}", connectionId);
                UpdateStatus(MobileConnectionStatus.Connected);
                _reconnectAttempt = 0;
                return Task.CompletedTask;
            };

            _hubConnection.Closed += error =>
            {
                _logger.LogError("Connection closed: {Error}", error?.Message);
                UpdateStatus(MobileConnectionStatus.Disconnected);
                return Task.CompletedTask;
            };

            await _hubConnection.StartAsync();
            
            var result = await _hubConnection.InvokeAsync<bool>("Authenticate", pairingCode);
            
            if (result)
            {
                UpdateStatus(MobileConnectionStatus.Connected);
                _logger.LogInformation("Connected and authenticated to hub");
            }
            else
            {
                UpdateStatus(MobileConnectionStatus.Error);
                throw new InvalidOperationException("Authentication failed");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to hub");
            UpdateStatus(MobileConnectionStatus.Error);
            throw;
        }
    }

    public async Task DisconnectAsync()
    {
        if (_hubConnection != null)
        {
            await _hubConnection.StopAsync();
            await _hubConnection.DisposeAsync();
            _hubConnection = null;
        }
        
        UpdateStatus(MobileConnectionStatus.Disconnected);
        _logger.LogInformation("Disconnected from hub");
    }

    public async Task SendCommandAsync(RemoteControlCommand command, Dictionary<string, object> parameters)
    {
        if (_hubConnection == null || !IsConnected)
        {
            throw new InvalidOperationException("Not connected to hub");
        }

        var message = new RemoteCommandMessage
        {
            Id = Guid.NewGuid(),
            Command = command,
            Parameters = parameters,
            Timestamp = DateTime.UtcNow
        };

        await _hubConnection.InvokeAsync("SendCommand", message);
    }

    public async Task SendGamepadInputAsync(string button, bool isPressed)
    {
        if (_hubConnection == null || !IsConnected)
        {
            throw new InvalidOperationException("Not connected to hub");
        }

        var input = new GamepadInput
        {
            Button = button,
            IsPressed = isPressed
        };

        await _hubConnection.InvokeAsync("SendGamepadInput", input);
    }

    public async Task SendTouchpadInputAsync(TouchpadInput input)
    {
        if (_hubConnection == null || !IsConnected)
        {
            throw new InvalidOperationException("Not connected to hub");
        }

        await _hubConnection.InvokeAsync("SendTouchpadInput", input);
    }

    public async Task<SystemStatus> GetSystemStatusAsync()
    {
        if (_hubConnection == null || !IsConnected)
        {
            return null;
        }

        return await _hubConnection.InvokeAsync<SystemStatus>("GetSystemStatus");
    }

    public async Task<LibrarySyncInfo> SyncLibraryAsync()
    {
        if (_hubConnection == null || !IsConnected)
        {
            return null;
        }

        return await _hubConnection.InvokeAsync<LibrarySyncInfo>("GetLibrarySyncInfo");
    }

    private void UpdateStatus(MobileConnectionStatus status)
    {
        if (Status != status)
        {
            Status = status;
            OnStatusChanged?.Invoke(this, status);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
    }
}
