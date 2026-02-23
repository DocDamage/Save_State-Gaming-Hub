// Copyright (c) 2026 SaveStateReborn. All rights reserved.

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SaveState.Core.WebBrowser.Models;
using SaveState.Core.WebBrowser.Services;

namespace SaveState.Presentation.ViewModels.WebBrowser;

/// <summary>
/// ViewModel for the browser history viewer.
/// </summary>
public sealed partial class HistoryViewModel : ObservableObject
{
    private readonly IBrowserService _browserService;
    private readonly ILogger<HistoryViewModel> _logger;

    [ObservableProperty]
    private ObservableCollection<BrowserHistoryItem> _historyItems = new();

    [ObservableProperty]
    private ObservableCollection<HistoryGroup> _groupedHistory = new();

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private DateTime? _filterDateFrom;

    [ObservableProperty]
    private DateTime? _filterDateTo;

    [ObservableProperty]
    private ObservableCollection<BrowserHistoryItem> _selectedItems = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    public int TotalHistoryCount => HistoryItems.Count;

    public HistoryViewModel(
        IBrowserService browserService,
        ILogger<HistoryViewModel> logger)
    {
        _browserService = browserService ?? throw new ArgumentNullException(nameof(browserService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _ = LoadHistoryAsync();
    }

    [RelayCommand]
    private async Task LoadHistoryAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            var result = await _browserService.GetHistoryAsync(FilterDateFrom, FilterDateTo);

            if (result.IsSuccess && result.Value != null)
            {
                HistoryItems.Clear();
                foreach (var item in result.Value.OrderByDescending(h => h.VisitedAt))
                {
                    HistoryItems.Add(item);
                }

                UpdateGroupedHistory();
            }
            else
            {
                ErrorMessage = result.Error ?? "Failed to load history";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load history");
            ErrorMessage = "An error occurred while loading history";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            await LoadHistoryAsync();
            return;
        }

        IsLoading = true;

        try
        {
            var filtered = HistoryItems.Where(h =>
                h.Title.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ||
                h.Url.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase)).ToList();

            HistoryItems.Clear();
            foreach (var item in filtered)
            {
                HistoryItems.Add(item);
            }

            UpdateGroupedHistory();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search history");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task DeleteItemAsync(BrowserHistoryItem? item)
    {
        if (item == null) return;

        try
        {
            var result = await _browserService.DeleteHistoryItemAsync(item.Id);

            if (result.IsSuccess)
            {
                HistoryItems.Remove(item);
                UpdateGroupedHistory();
            }
            else
            {
                ErrorMessage = result.Error ?? "Failed to delete history item";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete history item");
        }
    }

    [RelayCommand]
    private async Task DeleteSelectedAsync()
    {
        if (SelectedItems.Count == 0) return;

        try
        {
            var itemsToDelete = SelectedItems.ToList();
            foreach (var item in itemsToDelete)
            {
                var result = await _browserService.DeleteHistoryItemAsync(item.Id);
                if (result.IsSuccess)
                {
                    HistoryItems.Remove(item);
                }
            }

            SelectedItems.Clear();
            UpdateGroupedHistory();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete selected history items");
        }
    }

    [RelayCommand]
    private async Task ClearAllHistoryAsync()
    {
        try
        {
            var result = await _browserService.ClearHistoryAsync();

            if (result.IsSuccess)
            {
                HistoryItems.Clear();
                GroupedHistory.Clear();
            }
            else
            {
                ErrorMessage = result.Error ?? "Failed to clear history";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear history");
        }
    }

    [RelayCommand]
    private async Task OpenItemAsync(BrowserHistoryItem? item)
    {
        if (item == null) return;

        try
        {
            await _browserService.CreateTabAsync(item.Url, true);
            _logger.LogInformation("Opened history item: {Url}", item.Url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open history item");
        }
    }

    [RelayCommand]
    private async Task OpenInNewTabAsync(BrowserHistoryItem? item)
    {
        if (item == null) return;

        try
        {
            await _browserService.CreateTabAsync(item.Url, false);
            _logger.LogInformation("Opened history item in new tab: {Url}", item.Url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open history item in new tab");
        }
    }

    [RelayCommand]
    private async Task DeleteByDateRangeAsync()
    {
        if (!FilterDateFrom.HasValue && !FilterDateTo.HasValue) return;

        try
        {
            var from = FilterDateFrom ?? DateTime.MinValue;
            var to = FilterDateTo ?? DateTime.MaxValue;

            var itemsToDelete = HistoryItems.Where(h =>
                h.VisitedAt >= from && h.VisitedAt <= to).ToList();

            foreach (var item in itemsToDelete)
            {
                var result = await _browserService.DeleteHistoryItemAsync(item.Id);
                if (result.IsSuccess)
                {
                    HistoryItems.Remove(item);
                }
            }

            UpdateGroupedHistory();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete history by date range");
        }
    }

    private void UpdateGroupedHistory()
    {
        GroupedHistory.Clear();

        var today = DateTime.Today;
        var yesterday = today.AddDays(-1);
        var lastWeek = today.AddDays(-7);
        var lastMonth = today.AddDays(-30);

        var groups = new Dictionary<string, List<BrowserHistoryItem>>
        {
            ["Today"] = new(),
            ["Yesterday"] = new(),
            ["Last 7 Days"] = new(),
            ["Last 30 Days"] = new(),
            ["Older"] = new()
        };

        foreach (var item in HistoryItems)
        {
            var date = item.VisitedAt.Date;

            if (date == today)
                groups["Today"].Add(item);
            else if (date == yesterday)
                groups["Yesterday"].Add(item);
            else if (date >= lastWeek)
                groups["Last 7 Days"].Add(item);
            else if (date >= lastMonth)
                groups["Last 30 Days"].Add(item);
            else
                groups["Older"].Add(item);
        }

        foreach (var group in groups.Where(g => g.Value.Count > 0))
        {
            GroupedHistory.Add(new HistoryGroup
            {
                Title = $"{group.Key} ({group.Value.Count})",
                Items = new ObservableCollection<BrowserHistoryItem>(group.Value)
            });
        }
    }

    partial void OnFilterDateFromChanged(DateTime? value) => _ = LoadHistoryAsync();
    partial void OnFilterDateToChanged(DateTime? value) => _ = LoadHistoryAsync();
}

/// <summary>
/// Represents a group of history items (e.g., Today, Yesterday, etc.)
/// </summary>
public sealed record HistoryGroup
{
    public string Title { get; set; } = string.Empty;
    public ObservableCollection<BrowserHistoryItem> Items { get; set; } = new();
}
