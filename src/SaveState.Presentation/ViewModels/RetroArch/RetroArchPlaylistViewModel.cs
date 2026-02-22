using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using Microsoft.Extensions.Logging;
using SaveState.Application.RetroArch.Commands;
using SaveState.Core.RetroArch;
using SaveState.Presentation.Services;
using System.Collections.ObjectModel;

namespace SaveState.Presentation.ViewModels.RetroArch;

/// <summary>
/// ViewModel for managing RetroArch playlists.
/// </summary>
public partial class RetroArchPlaylistViewModel : ObservableObject
{
    private readonly IMediator _mediator;
    private readonly IDialogService _dialogService;
    private readonly ILogger<RetroArchPlaylistViewModel> _logger;
    private readonly INotificationService _notificationService;

    [ObservableProperty]
    private ObservableCollection<RetroArchPlaylist> _playlists = new();

    [ObservableProperty]
    private RetroArchPlaylist? _selectedPlaylist;

    [ObservableProperty]
    private bool _isGridView = true;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    public RetroArchPlaylistViewModel(
        IMediator mediator,
        IDialogService dialogService,
        ILogger<RetroArchPlaylistViewModel> logger,
        INotificationService notificationService)
    {
        _mediator = mediator;
        _dialogService = dialogService;
        _logger = logger;
        _notificationService = notificationService;
    }

    /// <summary>
    /// Toggles between grid and list view.
    /// </summary>
    [RelayCommand]
    private void ToggleView()
    {
        IsGridView = !IsGridView;
    }

    /// <summary>
    /// Adds a game to the selected playlist.
    /// </summary>
    [RelayCommand]
    private async Task AddGameToPlaylistAsync(RetroArchGame? game)
    {
        if (game is null || SelectedPlaylist is null) return;

        try
        {
            var result = await _mediator.Send(new AddGameToPlaylistCommand(
                SelectedPlaylist.Path,
                game.Id,
                game.Title));

            if (result.IsSuccess)
            {
                _notificationService.ShowSuccess($"Added {game.Title} to {SelectedPlaylist.Name}", "Playlist Updated");
            }
            else
            {
                _notificationService.ShowError(result.Error ?? "Failed to add game", "Add Failed");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add game to playlist: {GameTitle}", game.Title);
            _notificationService.ShowError("Failed to add game to playlist.", "Add Failed");
        }
    }

    /// <summary>
    /// Removes a game from the selected playlist.
    /// </summary>
    [RelayCommand]
    private async Task RemoveGameFromPlaylistAsync(RetroArchGame? game)
    {
        if (game is null || SelectedPlaylist is null) return;

        try
        {
            var result = await _mediator.Send(new RemoveGameFromPlaylistCommand(
                SelectedPlaylist.Path,
                game.Id));

            if (result.IsSuccess)
            {
                _notificationService.ShowSuccess($"Removed {game.Title} from {SelectedPlaylist.Name}", "Playlist Updated");
            }
            else
            {
                _notificationService.ShowError(result.Error ?? "Failed to remove game", "Remove Failed");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove game from playlist: {GameTitle}", game.Title);
            _notificationService.ShowError("Failed to remove game from playlist.", "Remove Failed");
        }
    }

    /// <summary>
    /// Creates a new playlist.
    /// </summary>
    [RelayCommand]
    private async Task CreatePlaylistAsync()
    {
        try
        {
            var playlistName = await _dialogService.ShowInputDialogAsync(
                "Create Playlist",
                "Enter a name for the new playlist:",
                "My Playlist");

            if (string.IsNullOrWhiteSpace(playlistName))
                return;

            var result = await _mediator.Send(new CreatePlaylistCommand(playlistName));
            if (result.IsSuccess)
            {
                _notificationService.ShowSuccess($"Created playlist: {playlistName}", "Playlist Created");
            }
            else
            {
                _notificationService.ShowError(result.Error ?? "Failed to create playlist", "Create Failed");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create RetroArch playlist");
            _notificationService.ShowError("Failed to create playlist. Please try again.", "Create Failed");
        }
    }
}
