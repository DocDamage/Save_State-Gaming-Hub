// Copyright (c) 2026 SaveStateReborn. All rights reserved.

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SaveState.Core.WebBrowser.Models;
using SaveState.Core.WebBrowser.Services;

namespace SaveState.Presentation.ViewModels.Settings;

/// <summary>
/// ViewModel for the browser settings page.
/// </summary>
public sealed partial class BrowserSettingsViewModel : ObservableObject
{
    private readonly IBrowserService _browserService;
    private readonly ILogger<BrowserSettingsViewModel> _logger;

    // General Settings
    [ObservableProperty]
    private string _homePage = "https://www.google.com";

    [ObservableProperty]
    private string _selectedSearchEngine = "Google";

    [ObservableProperty]
    private bool _restoreSessionOnStartup;

    [ObservableProperty]
    private bool _showHomeButton = true;

    // Privacy Settings
    [ObservableProperty]
    private bool _clearDataOnExit;

    [ObservableProperty]
    private bool _clearHistoryOnExit;

    [ObservableProperty]
    private bool _clearCookiesOnExit;

    [ObservableProperty]
    private bool _clearCacheOnExit;

    [ObservableProperty]
    private bool _doNotTrack = true;

    [ObservableProperty]
    private bool _blockThirdPartyCookies;

    // Security Settings
    [ObservableProperty]
    private bool _blockPopups = true;

    [ObservableProperty]
    private bool _blockDangerousDownloads = true;

    [ObservableProperty]
    private bool _showSecurityWarnings = true;

    [ObservableProperty]
    private ObservableCollection<string> _blockedDomains = new();

    [ObservableProperty]
    private string _newBlockedDomain = string.Empty;

    // Download Settings
    [ObservableProperty]
    private string _defaultDownloadPath = string.Empty;

    [ObservableProperty]
    private bool _askBeforeDownload = true;

    [ObservableProperty]
    private bool _showDownloadsWhenComplete;

    // Appearance Settings
    [ObservableProperty]
    private bool _showBookmarksBar = true;

    [ObservableProperty]
    private bool _showFullUrls;

    [ObservableProperty]
    private string _selectedFontSize = "Medium";

    [ObservableProperty]
    private string _selectedTheme = "System";

    // Advanced Settings
    [ObservableProperty]
    private string? _proxyAddress;

    [ObservableProperty]
    private int? _proxyPort;

    [ObservableProperty]
    private bool _useSystemProxy = true;

    [ObservableProperty]
    private ObservableCollection<CustomHeaderViewModel> _customHeaders = new();

    [ObservableProperty]
    private string _newHeaderName = string.Empty;

    [ObservableProperty]
    private string _newHeaderValue = string.Empty;

    [ObservableProperty]
    private bool _enableHardwareAcceleration = true;

    [ObservableProperty]
    private bool _enableSmoothScrolling = true;

    // Shortcuts
    [ObservableProperty]
    private ObservableCollection<BrowserShortcutViewModel> _keyboardShortcuts = new();

    public ObservableCollection<string> SearchEngines { get; } = new()
    {
        "Google",
        "Bing",
        "DuckDuckGo",
        "Yahoo",
        "Ecosia"
    };

    public ObservableCollection<string> FontSizes { get; } = new()
    {
        "Very Small",
        "Small",
        "Medium",
        "Large",
        "Very Large"
    };

    public ObservableCollection<string> Themes { get; } = new()
    {
        "System",
        "Light",
        "Dark"
    };

    public BrowserSettingsViewModel(
        IBrowserService browserService,
        ILogger<BrowserSettingsViewModel> logger)
    {
        _browserService = browserService ?? throw new ArgumentNullException(nameof(browserService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        LoadSettings();
        LoadDefaultShortcuts();
    }

    private void LoadSettings()
    {
        try
        {
            var settings = _browserService.CurrentSettings;
            HomePage = settings.HomePage;
            DefaultDownloadPath = settings.DownloadPath;
            ClearDataOnExit = settings.ClearDataOnExit;
            DoNotTrack = settings.DoNotTrack;
            BlockPopups = settings.BlockPopups;

            foreach (var domain in settings.BlockedDomains)
            {
                BlockedDomains.Add(domain);
            }

            foreach (var header in settings.CustomHeaders)
            {
                CustomHeaders.Add(new CustomHeaderViewModel
                {
                    Name = header.Key,
                    Value = header.Value
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load browser settings");
        }
    }

    private void LoadDefaultShortcuts()
    {
        KeyboardShortcuts.Add(new BrowserShortcutViewModel { Action = "New Tab", Shortcut = "Ctrl+T" });
        KeyboardShortcuts.Add(new BrowserShortcutViewModel { Action = "Close Tab", Shortcut = "Ctrl+W" });
        KeyboardShortcuts.Add(new BrowserShortcutViewModel { Action = "Reopen Closed Tab", Shortcut = "Ctrl+Shift+T" });
        KeyboardShortcuts.Add(new BrowserShortcutViewModel { Action = "Next Tab", Shortcut = "Ctrl+Tab" });
        KeyboardShortcuts.Add(new BrowserShortcutViewModel { Action = "Previous Tab", Shortcut = "Ctrl+Shift+Tab" });
        KeyboardShortcuts.Add(new BrowserShortcutViewModel { Action = "Address Bar", Shortcut = "Ctrl+L" });
        KeyboardShortcuts.Add(new BrowserShortcutViewModel { Action = "Find", Shortcut = "Ctrl+F" });
        KeyboardShortcuts.Add(new BrowserShortcutViewModel { Action = "DevTools", Shortcut = "F12" });
        KeyboardShortcuts.Add(new BrowserShortcutViewModel { Action = "Reload", Shortcut = "Ctrl+R" });
        KeyboardShortcuts.Add(new BrowserShortcutViewModel { Action = "Hard Reload", Shortcut = "Ctrl+Shift+R" });
    }

    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
        try
        {
            var settings = new BrowserSettings
            {
                HomePage = HomePage,
                SearchEngine = GetSearchEngineUrl(SelectedSearchEngine),
                ClearDataOnExit = ClearDataOnExit,
                DoNotTrack = DoNotTrack,
                BlockPopups = BlockPopups,
                DownloadPath = DefaultDownloadPath,
                BlockedDomains = BlockedDomains.ToList(),
                CustomHeaders = CustomHeaders.ToDictionary(h => h.Name, h => h.Value),
                ProxyAddress = UseSystemProxy ? null : ProxyAddress,
                ProxyPort = UseSystemProxy ? null : ProxyPort
            };

            var result = await _browserService.UpdateSettingsAsync(settings);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Browser settings saved successfully");
            }
            else
            {
                _logger.LogWarning("Failed to save browser settings: {Error}", result.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save browser settings");
        }
    }

    private static string GetSearchEngineUrl(string engine) => engine switch
    {
        "Google" => "https://www.google.com/search?q=",
        "Bing" => "https://www.bing.com/search?q=",
        "DuckDuckGo" => "https://duckduckgo.com/?q=",
        "Yahoo" => "https://search.yahoo.com/search?p=",
        "Ecosia" => "https://www.ecosia.org/search?q=",
        _ => "https://www.google.com/search?q="
    };

    [RelayCommand]
    private void AddBlockedDomain()
    {
        if (string.IsNullOrWhiteSpace(NewBlockedDomain)) return;

        var domain = NewBlockedDomain.Trim();
        if (!BlockedDomains.Contains(domain))
        {
            BlockedDomains.Add(domain);
        }
        NewBlockedDomain = string.Empty;
    }

    [RelayCommand]
    private void RemoveBlockedDomain(string? domain)
    {
        if (string.IsNullOrWhiteSpace(domain)) return;

        BlockedDomains.Remove(domain);
    }

    [RelayCommand]
    private void BrowseDownloadPath()
    {
        // This would open a folder picker dialog
        _logger.LogInformation("Browse download path requested");
    }

    [RelayCommand]
    private void AddCustomHeader()
    {
        if (string.IsNullOrWhiteSpace(NewHeaderName)) return;

        CustomHeaders.Add(new CustomHeaderViewModel
        {
            Name = NewHeaderName.Trim(),
            Value = NewHeaderValue.Trim()
        });

        NewHeaderName = string.Empty;
        NewHeaderValue = string.Empty;
    }

    [RelayCommand]
    private void RemoveCustomHeader(CustomHeaderViewModel? header)
    {
        if (header == null) return;

        CustomHeaders.Remove(header);
    }

    [RelayCommand]
    private async Task ClearBrowsingDataAsync()
    {
        try
        {
            if (ClearHistoryOnExit)
            {
                await _browserService.ClearHistoryAsync();
            }

            _logger.LogInformation("Browsing data cleared");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear browsing data");
        }
    }

    [RelayCommand]
    private void ResetToDefaults()
    {
        HomePage = "https://www.google.com";
        SelectedSearchEngine = "Google";
        RestoreSessionOnStartup = false;
        ShowHomeButton = true;
        ClearDataOnExit = false;
        ClearHistoryOnExit = false;
        ClearCookiesOnExit = false;
        ClearCacheOnExit = false;
        DoNotTrack = true;
        BlockThirdPartyCookies = false;
        BlockPopups = true;
        BlockDangerousDownloads = true;
        ShowSecurityWarnings = true;
        BlockedDomains.Clear();
        AskBeforeDownload = true;
        ShowDownloadsWhenComplete = false;
        ShowBookmarksBar = true;
        ShowFullUrls = false;
        SelectedFontSize = "Medium";
        SelectedTheme = "System";
        UseSystemProxy = true;
        EnableHardwareAcceleration = true;
        EnableSmoothScrolling = true;
    }
}

/// <summary>
/// ViewModel for a custom HTTP header.
/// </summary>
public sealed partial class CustomHeaderViewModel : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _value = string.Empty;
}

/// <summary>
/// ViewModel for a browser keyboard shortcut.
/// </summary>
public sealed partial class BrowserShortcutViewModel : ObservableObject
{
    [ObservableProperty]
    private string _action = string.Empty;

    [ObservableProperty]
    private string _shortcut = string.Empty;
}
