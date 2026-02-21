using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Presentation.Models.RetroArch;
using System.Collections.ObjectModel;

namespace SaveState.Presentation.ViewModels.RetroArch;

/// <summary>
/// ViewModel for managing RetroArch playlists.
/// </summary>
public partial class RetroArchPlaylistViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<RetroArchPlaylist> _playlists = new();

    [ObservableProperty]
    private RetroArchPlaylist? _selectedPlaylist;

    [ObservableProperty]
    private bool _isGridView = true;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

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
        // TODO: Add to playlist via mediator
        await Task.CompletedTask;
    }

    /// <summary>
    /// Removes a game from the selected playlist.
    /// </summary>
    [RelayCommand]
    private async Task RemoveGameFromPlaylistAsync(RetroArchGame? game)
    {
        if (game is null || SelectedPlaylist is null) return;
        // TODO: Remove from playlist via mediator
        await Task.CompletedTask;
    }

    /// <summary>
    /// Creates a new playlist.
    /// </summary>
    [RelayCommand]
    private async Task CreatePlaylistAsync()
    {
        // TODO: Create playlist dialog via mediator
        await Task.CompletedTask;
    }
}
