// Copyright (c) 2026 SaveStateReborn. All rights reserved.

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.WebBrowser.Models;
using SaveState.Core.WebBrowser.Services;

namespace SaveState.Presentation.ViewModels.WebBrowser;

/// <summary>
/// ViewModel for the bookmarks manager.
/// </summary>
public sealed partial class BookmarksManagerViewModel : ObservableObject
{
    private readonly IBrowserService _browserService;
    private readonly ILogger<BookmarksManagerViewModel> _logger;

    [ObservableProperty]
    private ObservableCollection<BrowserBookmark> _bookmarks = new();

    [ObservableProperty]
    private ObservableCollection<string> _folders = new();

    [ObservableProperty]
    private string _selectedFolder = "All Bookmarks";

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private BrowserBookmark? _selectedBookmark;

    [ObservableProperty]
    private string _newFolderName = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    public ObservableCollection<BrowserBookmark> FilteredBookmarks => new(
        Bookmarks.Where(b =>
            (SelectedFolder == "All Bookmarks" || b.Folder == SelectedFolder) &&
            (string.IsNullOrWhiteSpace(SearchQuery) ||
             b.Title.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ||
             b.Url.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase))));

    public BookmarksManagerViewModel(
        IBrowserService browserService,
        ILogger<BookmarksManagerViewModel> logger)
    {
        _browserService = browserService ?? throw new ArgumentNullException(nameof(browserService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _ = LoadBookmarksAsync();
        _ = LoadFoldersAsync();
    }

    [RelayCommand]
    private async Task LoadBookmarksAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            var result = await _browserService.GetBookmarksAsync(
                SelectedFolder == "All Bookmarks" ? null : SelectedFolder);

            if (result.IsSuccess && result.Value != null)
            {
                Bookmarks.Clear();
                foreach (var bookmark in result.Value.OrderByDescending(b => b.LastVisited ?? b.CreatedAt))
                {
                    Bookmarks.Add(bookmark);
                }
                OnPropertyChanged(nameof(FilteredBookmarks));
            }
            else
            {
                ErrorMessage = result.Error ?? "Failed to load bookmarks";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load bookmarks");
            ErrorMessage = "An error occurred while loading bookmarks";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task LoadFoldersAsync()
    {
        try
        {
            var result = await _browserService.GetBookmarkFoldersAsync();

            if (result.IsSuccess && result.Value != null)
            {
                Folders.Clear();
                Folders.Add("All Bookmarks");
                Folders.Add("Bookmarks Bar");
                Folders.Add("Other Bookmarks");
                foreach (var folder in result.Value.Where(f =>
                    f != "Bookmarks Bar" && f != "Other Bookmarks").OrderBy(f => f))
                {
                    Folders.Add(folder);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load bookmark folders");
        }
    }

    [RelayCommand]
    private async Task AddBookmarkAsync()
    {
        try
        {
            // This would typically open a dialog to get the bookmark details
            // For now, we'll add a placeholder that can be edited
            var result = await _browserService.AddBookmarkAsync(
                "New Bookmark",
                "https://example.com",
                SelectedFolder == "All Bookmarks" ? null : SelectedFolder);

            if (result.IsSuccess)
            {
                await LoadBookmarksAsync();
            }
            else
            {
                ErrorMessage = result.Error ?? "Failed to add bookmark";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add bookmark");
            ErrorMessage = "An error occurred while adding the bookmark";
        }
    }

    [RelayCommand]
    private async Task EditBookmarkAsync(BrowserBookmark? bookmark)
    {
        if (bookmark == null) return;

        try
        {
            SelectedBookmark = bookmark;
            // In a real implementation, this would open an edit dialog
            _logger.LogInformation("Editing bookmark: {BookmarkId}", bookmark.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to edit bookmark");
        }
    }

    [RelayCommand]
    private async Task DeleteBookmarkAsync(BrowserBookmark? bookmark)
    {
        if (bookmark == null) return;

        try
        {
            var result = await _browserService.RemoveBookmarkAsync(bookmark.Id);

            if (result.IsSuccess)
            {
                Bookmarks.Remove(bookmark);
                OnPropertyChanged(nameof(FilteredBookmarks));
            }
            else
            {
                ErrorMessage = result.Error ?? "Failed to delete bookmark";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete bookmark");
            ErrorMessage = "An error occurred while deleting the bookmark";
        }
    }

    [RelayCommand]
    private async Task CreateFolderAsync()
    {
        if (string.IsNullOrWhiteSpace(NewFolderName)) return;

        try
        {
            Folders.Add(NewFolderName.Trim());
            NewFolderName = string.Empty;
            _logger.LogInformation("Created new folder: {FolderName}", NewFolderName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create folder");
        }
    }

    [RelayCommand]
    private async Task DeleteFolderAsync(string? folder)
    {
        if (string.IsNullOrWhiteSpace(folder) || folder == "All Bookmarks") return;

        try
        {
            // Move bookmarks from deleted folder to "Other Bookmarks"
            var bookmarksInFolder = Bookmarks.Where(b => b.Folder == folder).ToList();
            foreach (var bookmark in bookmarksInFolder)
            {
                await _browserService.AddBookmarkAsync(bookmark.Title, bookmark.Url, "Other Bookmarks");
                await _browserService.RemoveBookmarkAsync(bookmark.Id);
            }

            Folders.Remove(folder);
            if (SelectedFolder == folder)
            {
                SelectedFolder = "All Bookmarks";
            }

            await LoadBookmarksAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete folder");
        }
    }

    [RelayCommand]
    private async Task ImportBookmarksAsync()
    {
        try
        {
            // This would open a file picker dialog for importing bookmarks
            _logger.LogInformation("Import bookmarks requested");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import bookmarks");
        }
    }

    [RelayCommand]
    private async Task ExportBookmarksAsync()
    {
        try
        {
            // This would open a file picker dialog for exporting bookmarks
            _logger.LogInformation("Export bookmarks requested");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export bookmarks");
        }
    }

    [RelayCommand]
    private void SortByAsync(string? sortBy)
    {
        if (string.IsNullOrWhiteSpace(sortBy)) return;

        var sorted = sortBy switch
        {
            "Name" => Bookmarks.OrderBy(b => b.Title).ToList(),
            "Url" => Bookmarks.OrderBy(b => b.Url).ToList(),
            "DateAdded" => Bookmarks.OrderByDescending(b => b.CreatedAt).ToList(),
            "LastVisited" => Bookmarks.OrderByDescending(b => b.LastVisited ?? DateTime.MinValue).ToList(),
            "VisitCount" => Bookmarks.OrderByDescending(b => b.VisitCount).ToList(),
            _ => Bookmarks.ToList()
        };

        Bookmarks.Clear();
        foreach (var bookmark in sorted)
        {
            Bookmarks.Add(bookmark);
        }

        OnPropertyChanged(nameof(FilteredBookmarks));
    }

    [RelayCommand]
    private async Task OpenBookmarkAsync(BrowserBookmark? bookmark)
    {
        if (bookmark == null) return;

        try
        {
            // Navigate to the bookmark URL in a new tab
            await _browserService.CreateTabAsync(bookmark.Url, true);
            _logger.LogInformation("Opened bookmark: {Url}", bookmark.Url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open bookmark");
        }
    }

    [RelayCommand]
    private async Task OpenAllInFolderAsync(string? folder)
    {
        if (string.IsNullOrWhiteSpace(folder)) return;

        try
        {
            var bookmarksInFolder = Bookmarks.Where(b => b.Folder == folder).ToList();
            foreach (var bookmark in bookmarksInFolder)
            {
                await _browserService.CreateTabAsync(bookmark.Url, false);
            }

            _logger.LogInformation("Opened {Count} bookmarks from folder: {Folder}",
                bookmarksInFolder.Count, folder);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open bookmarks in folder");
        }
    }

    partial void OnSearchQueryChanged(string value) => OnPropertyChanged(nameof(FilteredBookmarks));

    partial void OnSelectedFolderChanged(string value) => OnPropertyChanged(nameof(FilteredBookmarks));
}
