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
/// ViewModel for the main RetroArch tab interface.
/// </summary>
public partial class RetroArchTabViewModel : ObservableObject
{
    private readonly IMediator _mediator;
    private readonly ILogger<RetroArchTabViewModel> _logger;
    private readonly INotificationService _notificationService;

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

    public RetroArchTabViewModel(
        IMediator mediator,
        ILogger<RetroArchTabViewModel> logger,
        INotificationService notificationService)
    {
        _mediator = mediator;
        _logger = logger;
        _notificationService = notificationService;
    }

    /// <summary>
    /// Launches a RetroArch game.
    /// </summary>
    [RelayCommand]
    private async Task LaunchGameAsync(RetroArchGame? game)
    {
        if (game is null) return;

        try
        {
            var result = await _mediator.Send(new LaunchRetroArchGameCommand(game.Id, game.CoreName ?? string.Empty));
            if (result.IsSuccess)
            {
                _notificationService.ShowSuccess($"Launched {game.Title}", "RetroArch");
            }
            else
            {
                _notificationService.ShowError(result.Error ?? "Failed to launch game", "Launch Failed");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to launch RetroArch game: {GameTitle}", game.Title);
            _notificationService.ShowError("Failed to launch game. Please check RetroArch configuration.", "Launch Failed");
        }
    }

    /// <summary>
    /// Scans the RetroArch library for new games.
    /// </summary>
    [RelayCommand]
    private async Task ScanLibraryAsync()
    {
        try
        {
            IsScanning = true;
            ScanStatus = "Scanning...";

            var result = await _mediator.Send(new ScanLibraryCommand());
            if (result.IsSuccess)
            {
                _notificationService.ShowSuccess($"Found {result.Value} new games", "Scan Complete");
            }
            else
            {
                _notificationService.ShowWarning(result.Error ?? "Scan completed with warnings", "Scan Warning");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to scan RetroArch library");
            _notificationService.ShowError("Failed to scan library. Please try again.", "Scan Failed");
        }
        finally
        {
            IsScanning = false;
            ScanStatus = "Ready";
        }
    }

    /// <summary>
    /// Installs a RetroArch core.
    /// </summary>
    [RelayCommand]
    private async Task InstallCoreAsync(RetroArchCore? core)
    {
        if (core is null) return;

        try
        {
            var result = await _mediator.Send(new InstallCoreCommand(core.Name));
            if (result.IsSuccess)
            {
                _notificationService.ShowSuccess($"Installed {core.Name}", "Core Installed");
            }
            else
            {
                _notificationService.ShowError(result.Error ?? "Failed to install core", "Installation Failed");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to install RetroArch core: {CoreName}", core.Name);
            _notificationService.ShowError("Failed to install core. Please check your internet connection.", "Installation Failed");
        }
    }

    /// <summary>
    /// Refreshes the list of available and installed RetroArch cores.
    /// </summary>
    [RelayCommand]
    private async Task RefreshCoresAsync()
    {
        try
        {
            IsScanning = true;
            ScanStatus = "Refreshing cores...";

            // Get installed cores
            var installedResult = await _mediator.Send(new GetInstalledCoresQuery());
            if (installedResult.IsSuccess && installedResult.Value is not null)
            {
                foreach (var core in installedResult.Value)
                {
                    if (!Cores.Any(c => c.Name == core.Name))
                    {
                        Cores.Add(core);
                    }
                }
            }

            // Get available cores
            var availableResult = await _mediator.Send(new GetAvailableCoresQuery());
            if (availableResult.IsSuccess && availableResult.Value is not null)
            {
                foreach (var core in availableResult.Value)
                {
                    if (!Cores.Any(c => c.Name == core.Name))
                    {
                        Cores.Add(core);
                    }
                }
            }

            _notificationService.ShowSuccess($"Refreshed {Cores.Count} cores", "Cores Updated");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh RetroArch cores");
            _notificationService.ShowError("Failed to refresh cores. Please try again.", "Refresh Failed");
        }
        finally
        {
            IsScanning = false;
            ScanStatus = "Ready";
        }
    }

    /// <summary>
    /// Joins a Netplay lobby.
    /// </summary>
    [RelayCommand]
    private async Task JoinNetplayAsync(NetplayLobby? lobby)
    {
        if (lobby is null) return;

        try
        {
            var result = await _mediator.Send(new JoinNetplayLobbyCommand(lobby.Id));
            if (result.IsSuccess)
            {
                _notificationService.ShowSuccess($"Joined {lobby.HostName}'s lobby", "Netplay");
            }
            else
            {
                _notificationService.ShowError(result.Error ?? "Failed to join lobby", "Join Failed");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to join Netplay lobby: {LobbyId}", lobby.Id);
            _notificationService.ShowError("Failed to join lobby. Please try again.", "Join Failed");
        }
    }

    /// <summary>
    /// Hosts a new Netplay game.
    /// </summary>
    [RelayCommand]
    private async Task HostNetplayAsync()
    {
        try
        {
            // For hosting, we need to select a game first
            var selectedGame = Games.FirstOrDefault();
            if (selectedGame is null)
            {
                _notificationService.ShowWarning("Please select a game first", "No Game Selected");
                return;
            }

            var result = await _mediator.Send(new HostNetplayGameCommand(
                selectedGame.Id,
                selectedGame.CoreName ?? string.Empty));

            if (result.IsSuccess)
            {
                _notificationService.ShowSuccess($"Lobby created: {result.Value}", "Netplay Host");
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
