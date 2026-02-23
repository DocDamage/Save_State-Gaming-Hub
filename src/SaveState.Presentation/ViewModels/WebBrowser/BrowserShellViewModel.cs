using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SaveState.Core.WebBrowser.Models;
using SaveState.Core.WebBrowser.Services;

namespace SaveState.Presentation.ViewModels.WebBrowser;

public partial class BrowserShellViewModel : ObservableObject
{
    private readonly IBrowserService _browserService;
    private readonly ILogger<BrowserShellViewModel> _logger;

    [ObservableProperty] private ObservableCollection<BrowserTab> _tabs = new();
    [ObservableProperty] private BrowserTab? _activeTab;
    [ObservableProperty] private string _addressBarText = string.Empty;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private double _loadingProgress;
    [ObservableProperty] private bool _canGoBack;
    [ObservableProperty] private bool _canGoForward;
    [ObservableProperty] private bool _showFindBar;
    [ObservableProperty] private string _findText = string.Empty;
    [ObservableProperty] private bool _showDownloads;
    [ObservableProperty] private ObservableCollection<BrowserDownload> _activeDownloads = new();
    [ObservableProperty] private ObservableCollection<BrowserBookmark> _bookmarkBarItems = new();
    [ObservableProperty] private bool _showBookmarksBar = true;

    public BrowserShellViewModel(IBrowserService browserService, ILogger<BrowserShellViewModel> logger)
    {
        _browserService = browserService;
        _logger = logger;
        _browserService.TabCreated += OnTabCreated;
        _browserService.TabClosed += OnTabClosed;
        _browserService.ActiveTabChanged += OnActiveTabChanged;
        _browserService.AddressChanged += OnAddressChanged;
        _browserService.TitleChanged += OnTitleChanged;
        _browserService.LoadingStateChanged += OnLoadingStateChanged;
        _browserService.LoadingProgressChanged += OnLoadingProgressChanged;
        _browserService.DownloadStarted += OnDownloadStarted;
        _browserService.DownloadProgressChanged += OnDownloadProgressChanged;
        _browserService.DownloadCompleted += OnDownloadCompleted;
    }

    private void OnTabCreated(object? sender, BrowserTab tab) => Tabs.Add(tab);
    private void OnTabClosed(object? sender, BrowserTab tab) => Tabs.Remove(tab);
    private void OnActiveTabChanged(object? sender, BrowserTab tab)
    {
        ActiveTab = tab;
        AddressBarText = tab.Url;
        CanGoBack = tab.CanGoBack;
        CanGoForward = tab.CanGoForward;
    }
    private void OnAddressChanged(object? sender, (Guid TabId, string Url) e)
    {
        if (ActiveTab?.Id == e.TabId) AddressBarText = e.Url;
    }
    private void OnTitleChanged(object? sender, (Guid TabId, string Title) e) { }
    private void OnLoadingStateChanged(object? sender, (Guid TabId, bool IsLoading) e)
    {
        if (ActiveTab?.Id == e.TabId) IsLoading = e.IsLoading;
    }
    private void OnLoadingProgressChanged(object? sender, (Guid TabId, double Progress) e)
    {
        if (ActiveTab?.Id == e.TabId) LoadingProgress = e.Progress;
    }
    private void OnDownloadStarted(object? sender, BrowserDownload download) => ActiveDownloads.Add(download);
    private void OnDownloadProgressChanged(object? sender, BrowserDownload download) { }
    private void OnDownloadCompleted(object? sender, BrowserDownload download) { }

    [RelayCommand] private async Task NewTabAsync(string? url = null)
    {
        var result = await _browserService.CreateTabAsync(url);
        if (result.IsSuccess) await _browserService.ActivateTabAsync(result.Value.Id);
    }

    [RelayCommand] private async Task CloseTabAsync(BrowserTab tab)
    {
        await _browserService.CloseTabAsync(tab.Id);
    }

    [RelayCommand] private async Task ActivateTabAsync(BrowserTab tab)
    {
        await _browserService.ActivateTabAsync(tab.Id);
    }

    [RelayCommand] private async Task NavigateAsync()
    {
        if (ActiveTab != null && !string.IsNullOrWhiteSpace(AddressBarText))
        {
            await _browserService.NavigateAsync(ActiveTab.Id, AddressBarText);
        }
    }

    [RelayCommand] private async Task GoBackAsync()
    {
        if (ActiveTab != null) await _browserService.GoBackAsync(ActiveTab.Id);
    }

    [RelayCommand] private async Task GoForwardAsync()
    {
        if (ActiveTab != null) await _browserService.GoForwardAsync(ActiveTab.Id);
    }

    [RelayCommand] private async Task RefreshAsync()
    {
        if (ActiveTab != null) await _browserService.RefreshAsync(ActiveTab.Id);
    }

    [RelayCommand] private async Task StopAsync()
    {
        if (ActiveTab != null) await _browserService.StopAsync(ActiveTab.Id);
    }

    [RelayCommand] private async Task GoHomeAsync()
    {
        if (ActiveTab != null) await _browserService.NavigateAsync(ActiveTab.Id, _browserService.CurrentSettings.HomePage);
    }

    [RelayCommand] private async Task ToggleFindBarAsync()
    {
        ShowFindBar = !ShowFindBar;
        if (!ShowFindBar && ActiveTab != null) await _browserService.StopFindingAsync(ActiveTab.Id, true);
    }

    [RelayCommand] private async Task FindNextAsync()
    {
        if (ActiveTab != null)
            await _browserService.FindAsync(ActiveTab.Id, new BrowserFindOptions { SearchText = FindText, FindNext = true });
    }

    [RelayCommand] private async Task FindPreviousAsync()
    {
        if (ActiveTab != null)
            await _browserService.FindAsync(ActiveTab.Id, new BrowserFindOptions { SearchText = FindText, Forward = false, FindNext = true });
    }

    [RelayCommand] private async Task AddBookmarkAsync()
    {
        if (ActiveTab != null)
            await _browserService.AddBookmarkAsync(ActiveTab.Title, ActiveTab.Url);
    }

    [RelayCommand] private async Task ZoomInAsync()
    {
        if (ActiveTab != null) await _browserService.ZoomInAsync(ActiveTab.Id);
    }

    [RelayCommand] private async Task ZoomOutAsync()
    {
        if (ActiveTab != null) await _browserService.ZoomOutAsync(ActiveTab.Id);
    }

    [RelayCommand] private async Task ResetZoomAsync()
    {
        if (ActiveTab != null) await _browserService.ResetZoomAsync(ActiveTab.Id);
    }

    [RelayCommand] private async Task ShowDevToolsAsync()
    {
        if (ActiveTab != null) await _browserService.ShowDevToolsAsync(ActiveTab.Id);
    }

    [RelayCommand] private async Task TakeScreenshotAsync()
    {
        if (ActiveTab != null)
        {
            var result = await _browserService.CaptureScreenshotAsync(ActiveTab.Id);
            // Save or copy to clipboard
        }
    }

    [RelayCommand] private async Task PrintAsync()
    {
        if (ActiveTab != null) await _browserService.PrintAsync(ActiveTab.Id);
    }
}
