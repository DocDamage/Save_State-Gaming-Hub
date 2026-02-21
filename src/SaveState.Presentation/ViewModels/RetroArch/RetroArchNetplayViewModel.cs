using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Presentation.Models.RetroArch;
using System.Collections.ObjectModel;

namespace SaveState.Presentation.ViewModels.RetroArch;

/// <summary>
/// ViewModel for RetroArch Netplay multiplayer functionality.
/// </summary>
public partial class RetroArchNetplayViewModel : ObservableObject
{
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

    /// <summary>
    /// Refreshes the list of available lobbies.
    /// </summary>
    [RelayCommand]
    private async Task RefreshLobbiesAsync()
    {
        // TODO: Refresh lobbies via mediator
        await Task.CompletedTask;
    }

    /// <summary>
    /// Joins the selected lobby.
    /// </summary>
    [RelayCommand]
    private async Task JoinLobbyAsync(NetplayLobby? lobby)
    {
        if (lobby is null) return;
        IsConnecting = true;
        ConnectionStatus = $"Connecting to {lobby.HostName}...";
        // TODO: Join lobby via mediator
        await Task.Delay(1000);
        IsConnecting = false;
        ConnectionStatus = "Connected";
    }

    /// <summary>
    /// Joins a lobby by IP address.
    /// </summary>
    [RelayCommand]
    private async Task JoinByIpAsync()
    {
        if (string.IsNullOrWhiteSpace(HostIp)) return;
        IsConnecting = true;
        // TODO: Join by IP via mediator
        await Task.Delay(1000);
        IsConnecting = false;
    }

    /// <summary>
    /// Hosts a new Netplay game.
    /// </summary>
    [RelayCommand]
    private async Task HostGameAsync()
    {
        // TODO: Host game dialog via mediator
        await Task.CompletedTask;
    }
}
