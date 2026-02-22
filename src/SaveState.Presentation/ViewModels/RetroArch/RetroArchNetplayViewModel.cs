using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using Microsoft.Extensions.Logging;
using SaveState.Application.RetroArch.Commands;
using SaveState.Application.RetroArch.Queries;
using SaveState.Core.RetroArch;
using SaveState.Presentation.Services;
using System.Collections.ObjectModel;

namespace SaveState.Presentation.ViewModels.RetroArch;

/// <summary>
/// ViewModel for RetroArch Netplay multiplayer functionality.
/// </summary>
public partial class RetroArchNetplayViewModel : ObservableObject
{
    private readonly IMediator _mediator;
    private readonly IDialogService _dialogService;
    private readonly ILogger<RetroArchNetplayViewModel> _logger;
    private readonly INotificationService _notificationService;

    [ObservableProperty]
    private ObservableCollection<NetplayLobby> _lobbies = new();

    [ObservableProperty]
    private NetplayLobby? _selectedLobby;

    [ObservableProperty]
    private string _hostIp = string.Empty;

    [ObservableProperty]
    private string _connectionStatus = "Disconnected";

    [ObservableProperty]
    private bool _isConnecting;

    public RetroArchNetplayViewModel(
        IMediator mediator,
        IDialogService dialogService,
        ILogger<RetroArchNetplayViewModel> logger,
        INotificationService notificationService)
    {
        _mediator = mediator;
        _dialogService = dialogService;
        _logger = logger;
        _notificationService = notificationService;
    }

    /// <summary>
    /// Refreshes the list of available lobbies.
    /// </summary>
    [RelayCommand]
    private async Task RefreshLobbiesAsync()
    {
        try
        {
            IsConnecting = true;
            ConnectionStatus = "Refreshing lobbies...";

            var result = await _mediator.Send(new GetNetplayLobbiesQuery());
            if (result.IsSuccess && result.Value is not null)
            {
                Lobbies.Clear();
                foreach (var lobby in result.Value)
                {
                    Lobbies.Add(lobby);
                }
                _notificationService.ShowSuccess($"Found {result.Value.Count} lobbies", "Lobbies Refreshed");
            }
            else
            {
                _notificationService.ShowWarning(result.Error ?? "No lobbies found", "Refresh Warning");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh Netplay lobbies");
            _notificationService.ShowError("Failed to refresh lobbies. Please try again.", "Refresh Failed");
        }
        finally
        {
            IsConnecting = false;
            ConnectionStatus = "Disconnected";
        }
    }

    /// <summary>
    /// Joins the selected lobby.
    /// </summary>
    [RelayCommand]
    private async Task JoinLobbyAsync(NetplayLobby? lobby)
    {
        if (lobby is null) return;

        try
        {
            IsConnecting = true;
            ConnectionStatus = $"Connecting to {lobby.HostName}...";

            var result = await _mediator.Send(new JoinNetplayLobbyCommand(lobby.Id));
            if (result.IsSuccess)
            {
                _notificationService.ShowSuccess($"Joined {lobby.HostName}'s lobby", "Connected");
                ConnectionStatus = $"Connected to {lobby.HostName}";
            }
            else
            {
                _notificationService.ShowError(result.Error ?? "Failed to join lobby", "Join Failed");
                ConnectionStatus = "Disconnected";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to join Netplay lobby: {LobbyId}", lobby.Id);
            _notificationService.ShowError("Failed to join lobby. Please try again.", "Join Failed");
            ConnectionStatus = "Disconnected";
        }
        finally
        {
            IsConnecting = false;
        }
    }

    /// <summary>
    /// Joins a lobby by IP address.
    /// </summary>
    [RelayCommand]
    private async Task JoinByIpAsync()
    {
        if (string.IsNullOrWhiteSpace(HostIp)) return;

        try
        {
            IsConnecting = true;
            ConnectionStatus = $"Connecting to {HostIp}...";

            var result = await _mediator.Send(new JoinNetplayByIpCommand(HostIp));
            if (result.IsSuccess)
            {
                _notificationService.ShowSuccess($"Connected to {HostIp}", "Connected");
                ConnectionStatus = $"Connected to {HostIp}";
            }
            else
            {
                _notificationService.ShowError(result.Error ?? "Failed to connect", "Connection Failed");
                ConnectionStatus = "Disconnected";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to join Netplay by IP: {HostIp}", HostIp);
            _notificationService.ShowError("Failed to connect. Please check the IP address.", "Connection Failed");
            ConnectionStatus = "Disconnected";
        }
        finally
        {
            IsConnecting = false;
        }
    }

    /// <summary>
    /// Hosts a new Netplay game.
    /// </summary>
    [RelayCommand]
    private async Task HostGameAsync()
    {
        try
        {
            // Show a dialog to select game and configure host settings
            var gamePath = await _dialogService.ShowOpenFileDialogAsync(
                "Select Game",
                new[] { "*.zip", "*.smc", "*.sfc", "*.nes", "*.gba", "*.gbc", "*.*" });

            if (string.IsNullOrWhiteSpace(gamePath))
                return;

            var result = await _mediator.Send(new HostNetplayGameCommand(gamePath, string.Empty));
            if (result.IsSuccess)
            {
                _notificationService.ShowSuccess($"Lobby created: {result.Value}", "Hosting Game");
                ConnectionStatus = "Hosting";
            }
            else
            {
                _notificationService.ShowError(result.Error ?? "Failed to host game", "Host Failed");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to host Netplay game");
            _notificationService.ShowError("Failed to host game. Please try again.", "Host Failed");
        }
    }
}
