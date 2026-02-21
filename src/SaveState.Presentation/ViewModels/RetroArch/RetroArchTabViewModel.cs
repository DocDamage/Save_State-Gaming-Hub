using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Presentation.Models.RetroArch;
using System.Collections.ObjectModel;

namespace SaveState.Presentation.ViewModels.RetroArch;

/// <summary>
/// ViewModel for the main RetroArch tab interface.
/// </summary>
public partial class RetroArchTabViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<RetroArchGame> _games = new();

    [ObservableProperty]
    private ObservableCollection<RetroArchPlaylist> _playlists = new();

    [ObservableProperty]
    private ObservableCollection<RetroArchCore> _cores = new();

    [ObservableProperty]
    private ObservableCollection<RetroArchGame> _recentGames = new();

    [ObservableProperty]
    private ObservableCollection<NetplayLobby> _netplayLobbies = new();

    [ObservableProperty]
    private RetroArchPlaylist? _selectedPlaylist;

    [ObservableProperty]
    private string _scanStatus = "Ready";

    [ObservableProperty]
    private bool _isScanning;

    /// <summary>
    /// Launches a RetroArch game.
    /// </summary>
    [RelayCommand]
    private async Task LaunchGameAsync(RetroArchGame? game)
    {
        if (game is null) return;
        // TODO: Implement launch via mediator
        await Task.CompletedTask;
    }

    /// <summary>
    /// Scans the RetroArch library for new games.
    /// </summary>
    [RelayCommand]
    private async Task ScanLibraryAsync()
    {
        IsScanning = true;
        ScanStatus = "Scanning...";
        // TODO: Implement scan via mediator
        await Task.Delay(1000);
        IsScanning = false;
        ScanStatus = "Ready";
    }

    /// <summary>
    /// Installs a RetroArch core.
    /// </summary>
    [RelayCommand]
    private async Task InstallCoreAsync(RetroArchCore? core)
    {
        if (core is null) return;
        // TODO: Implement install via mediator
        await Task.CompletedTask;
    }

    /// <summary>
    /// Joins a Netplay lobby.
    /// </summary>
    [RelayCommand]
    private async Task JoinNetplayAsync(NetplayLobby? lobby)
    {
        if (lobby is null) return;
        // TODO: Implement join via mediator
        await Task.CompletedTask;
    }

    /// <summary>
    /// Hosts a new Netplay game.
    /// </summary>
    [RelayCommand]
    private async Task HostNetplayAsync()
    {
        // TODO: Implement host dialog via mediator
        await Task.CompletedTask;
    }
}
