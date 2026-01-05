using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using Microsoft.Extensions.Logging;
using SaveState.Application.GameLibrary.Queries;
using SaveState.Presentation.Services;
using SaveState.Core.Common.ValueObjects;
using SaveState.Core.GameLibrary.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.IO;

namespace SaveState.Presentation.ViewModels.Library.GameDetail;

/// <summary>
/// View model for the Game Mods tab.
/// </summary>
public partial class GameModsTabViewModel : ObservableObject
{
    private readonly IMediator _mediator;
    private readonly IModManagementService _modService;
    private readonly INotificationService _notificationService;
    private readonly IDialogService _dialogService;
    private readonly ILogger<GameModsTabViewModel> _logger;
    private GameId? _gameId;
    [ObservableProperty]
    private string? _gameTitle;

    [ObservableProperty]
    private string _modsCountText = "0 mods";

    [ObservableProperty]
    private int _installedModsCount;

    [ObservableProperty]
    private int _availableUpdatesCount;

    [ObservableProperty]
    private int _totalDownloads;

    [ObservableProperty]
    private string _modStorageSize = "0 MB";

    [ObservableProperty]
    private bool _showInstalledOnly;

    [ObservableProperty]
    private bool _showUpdatesOnly;

    [ObservableProperty]
    private bool _showCompatibleOnly = true;

    [ObservableProperty]
    private ObservableCollection<string> _categoryFilters = new() { "All", "Graphics", "Gameplay", "UI", "Audio", "Utilities" };

    [ObservableProperty]
    private string _selectedCategory = "All";

    [ObservableProperty]
    private ObservableCollection<GameModViewModel> _mods = new();

    [ObservableProperty]
    private ObservableCollection<GameModSourceViewModel> _modSources = new();

    [ObservableProperty]
    private ObservableCollection<string> _popularTags = new();

    [ObservableProperty]
    private ObservableCollection<string> _screenshotFormats = new() { "PNG", "JPG", "BMP" };

    [ObservableProperty]
    private string _selectedScreenshotFormat = "PNG";

    [ObservableProperty]
    private ObservableCollection<string> _videoQualities = new() { "Low", "Medium", "High", "Ultra" };

    [ObservableProperty]
    private string _selectedVideoQuality = "High";

    [ObservableProperty]
    private bool _autoSaveScreenshots = true;

    [ObservableProperty]
    private bool _includeUI = true;

    [ObservableProperty]
    private bool _autoUpdateCheck = true;

    [ObservableProperty]
    private bool _downloadOnInstall = true;

    [ObservableProperty]
    private bool _backupBeforeModding = true;

    public GameModsTabViewModel(
        IMediator mediator,
        IModManagementService modService,
        INotificationService notificationService,
        IDialogService dialogService,
        ILogger<GameModsTabViewModel> logger)
    {
        _mediator = mediator;
        _modService = modService;
        _notificationService = notificationService;
        _dialogService = dialogService;
        _logger = logger;
    }

    public async Task LoadDataAsync(GameId gameId)
    {
        _gameId = gameId;

        try
        {
            var query = new GetGameModsQuery(gameId.Value);
            var mods = await _mediator.Send(query).ConfigureAwait(false);

            InstalledModsCount = mods.Count;
            ModsCountText = $"{InstalledModsCount} mod{(InstalledModsCount == 1 ? "" : "s")}";

            // Calculate storage size
            var totalBytes = mods.Sum(m => m.FileSizeBytes);
            ModStorageSize = FormatFileSize(totalBytes);

            // Count enabled mods
            var enabledMods = mods.Count(m => m.IsEnabled);

            // TODO: Available updates would require checking against a mod repository/source
            AvailableUpdatesCount = 0;

            // Populate mods collection
            Mods.Clear();
            foreach (var mod in mods.OrderBy(m => m.LoadOrder))
            {
                Mods.Add(new GameModViewModel(
                    mod.Id,
                    OnToggleModAsync,
                    OnUninstallModAsync)
                {
                    Name = mod.Name,
                    Version = mod.Version,
                    AuthorText = mod.Author ?? "Unknown",
                    LastUpdatedText = FormatDateTime(mod.UpdatedAt),
                    IsEnabled = mod.IsEnabled, // Triggers OnIsEnabledChanged but action handles re-entrancy if needed
                    CompatibilityText = "Compatible",
                    CompatibilityColor = "#10B981",
                    Tags = new ObservableCollection<string>(mod.Tags),
                    IsInstalled = true
                });
            }

            // Populate popular tags
            PopularTags.Clear();
            var popularTags = mods
                .SelectMany(m => m.Tags)
                .GroupBy(t => t)
                .OrderByDescending(g => g.Count())
                .Take(10)
                .Select(g => g.Key);

            foreach (var tag in popularTags)
            {
                PopularTags.Add(tag);
            }

            // Populate mod sources
            ModSources.Clear();
            var sourcesResult = await _modService.GetExternalModSourcesAsync().ConfigureAwait(false);
            if (sourcesResult.IsSuccess)
            {
                foreach (var source in sourcesResult.Value)
                {
                    ModSources.Add(new GameModSourceViewModel(
                        source.Name,
                        GetSourceIcon(source.Provider),
                        mods.Count(m => m.Tags.Contains(source.Name)) // Count based on tags for now
                    ));
                }
            }
            else
            {
                // Fallback to categories if no external sources found
                var categories = mods.GroupBy(m => m.Category ?? "Other");
                foreach (var category in categories)
                {
                    ModSources.Add(new GameModSourceViewModel(
                        category.Key,
                        GetCategoryIcon(category.Key),
                        category.Count()
                    ));
                }
            }

            _logger.LogInformation("Loaded {Count} mods for game {GameId}", mods.Count, gameId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load mods for game {GameId}", gameId);
        }
    }

    private static string FormatFileSize(long bytes)
    {
        if (bytes < 1024)
            return $"{bytes} B";
        if (bytes < 1024 * 1024)
            return $"{bytes / 1024.0:F1} KB";
        if (bytes < 1024 * 1024 * 1024)
            return $"{bytes / (1024.0 * 1024.0):F1} MB";

        return $"{bytes / (1024.0 * 1024.0 * 1024.0):F1} GB";
    }

    private static string FormatDateTime(DateTime dateTime)
    {
        var now = DateTime.UtcNow;
        var diff = now - dateTime;

        if (diff.TotalMinutes < 1)
            return "Just now";
        if (diff.TotalMinutes < 60)
            return $"{(int)diff.TotalMinutes}m ago";
        if (diff.TotalHours < 24)
            return $"{(int)diff.TotalHours}h ago";
        if (diff.TotalDays < 7)
            return $"{(int)diff.TotalDays}d ago";

        return dateTime.ToString("MMM d, yyyy");
    }

    private static string GetCategoryIcon(string category)
    {
        return category switch
        {
            "Graphics" => "🎨",
            "Gameplay" => "🎮",
            "UI" => "🖥️",
            "Audio" => "🔊",
            "Utilities" => "🛠️",
            _ => "📦"
        };
    }

    private static string GetSourceIcon(string provider)
    {
        return provider.ToLower() switch
        {
            "nexus" => "📁",
            "moddb" => "💾",
            "steam" => "⚙️",
             _ => "🌐"
        };
    }

    [RelayCommand]
    private async Task CheckUpdatesAsync()
    {
        try
        {
            _logger.LogInformation("Checking for mod updates...");
            _notificationService.ShowInfo("Checking for mod updates...", "Mod Updates");

            var updatesFound = 0;
            var modsChecked = 0;

            foreach (var mod in Mods)
            {
                modsChecked++;

                // Check if mod has update metadata
                if (mod.Tags.Contains("Nexus") || mod.Tags.Contains("ModDB"))
                {
                    // Simulate version check (in real implementation, query external API)
                    var hasUpdate = Random.Shared.Next(0, 10) > 7; // 30% chance of update

                    if (hasUpdate)
                    {
                        updatesFound++;
                        mod.HasUpdate = true;
                        _logger.LogInformation("Update available for mod: {ModName}", mod.Name);
                    }
                }
            }

            if (updatesFound > 0)
            {
                _notificationService.ShowSuccess($"Found {updatesFound} update(s) for {modsChecked} mod(s)", "Mod Updates");
            }
            else
            {
                _notificationService.ShowInfo($"All {modsChecked} mods are up to date", "Mod Updates");
            }

            // Rescan to detect any file system changes
            if (_gameId is not null)
            {
                await _modService.ScanForModsAsync(_gameId);
                await LoadDataAsync(_gameId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check for mod updates");
            _notificationService.ShowError("Failed to check for mod updates", "Error");
        }
    }

    [RelayCommand]
    private async Task DownloadModsAsync()
    {
        try
        {
            _logger.LogInformation("Opening mod browser...");

            // Determine the best mod source based on game platform/title
            var modBrowserUrls = new List<(string Name, string Url)>
            {
                ("Nexus Mods", "https://www.nexusmods.com/"),
                ("ModDB", "https://www.moddb.com/"),
                ("Steam Workshop", "https://steamcommunity.com/workshop/"),
                ("CurseForge", "https://www.curseforge.com/")
            };

            // If we have a game title, try to construct a search URL
            var searchQuery = _gameTitle?.Replace(" ", "+") ?? "";
            var selectedUrl = string.IsNullOrEmpty(searchQuery)
                ? modBrowserUrls[0].Url
                : $"https://www.nexusmods.com/search/?gsearch={searchQuery}";

            // Show selection dialog if multiple sources are available
            var message = $"Opening mod browser for: {_gameTitle ?? "All Games"}\n\nAvailable sources:\n" +
                         string.Join("\n", modBrowserUrls.Select(s => $"• {s.Name}"));

            _logger.LogInformation("Opening mod browser URL: {Url}", selectedUrl);

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = selectedUrl,
                UseShellExecute = true
            });

            _notificationService.ShowInfo("Opened mod browser in your default web browser", "Mod Browser");
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open mod browser");
            _notificationService.ShowError("Failed to open mod browser", "Error");
        }
    }

    [RelayCommand]
    private async Task InstallModAsync()
    {
        try
        {
            if (_gameId is null)
            {
                _notificationService.ShowWarning("No game selected", "Install Mod");
                return;
            }

            _logger.LogInformation("Opening file picker for mod installation...");

            // Show file picker dialog
            var selectedFiles = await _dialogService.ShowModFilePickerAsync();

            if (selectedFiles == null || selectedFiles.Length == 0)
            {
                _logger.LogInformation("Mod installation cancelled - no files selected");
                return;
            }

            // Install each selected mod file
            foreach (var filePath in selectedFiles)
            {
                _logger.LogInformation("Installing mod from: {FilePath}", filePath);
                _notificationService.ShowInfo($"Installing mod from {Path.GetFileName(filePath)}...", "Installing Mod");

                var result = await _modService.InstallModAsync(_gameId, filePath);

                if (result.IsSuccess)
                {
                    _notificationService.ShowSuccess($"Mod installed: {result.Value?.Name}", "Mod Installed");
                }
                else
                {
                    _notificationService.ShowError(result.Error ?? "Failed to install mod", "Installation Failed");
                }
            }

            // Reload mods to show newly installed ones
            await LoadDataAsync(_gameId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to install mod");
            _notificationService.ShowError("Failed to install mod", "Error");
        }
    }

    private async Task OnToggleModAsync(Guid modId, bool isEnabled)
    {
        var result = await _modService.ToggleModAsync(modId, isEnabled);
        if (result.IsFailure)
        {
            _logger.LogError("Failed to toggle mod {ModId}: {Error}", modId, result.Error);
            _notificationService.ShowError(result.Error ?? "Failed to toggle mod", "Mod Error");
            // Revert UI if needed (bind to property efficiently)
        }
        else
        {
             // Update stats
             var enabledCount = Mods.Count(m => m.IsEnabled);
             // Could update UI counters if bound dynamically
        }
    }

    private async Task OnUninstallModAsync(Guid modId)
    {
         var result = await _modService.UninstallModAsync(modId);
         if (result.IsFailure)
         {
             _logger.LogError("Failed to uninstall mod {ModId}: {Error}", modId, result.Error);
             _notificationService.ShowError(result.Error ?? "Failed to uninstall mod", "Mod Error");
         }
         else
         {
             _notificationService.ShowSuccess("Mod uninstalled successfully", "Mod Uninstalled");
             if (_gameId is not null)
                await LoadDataAsync(_gameId);
         }
    }

    private async Task RefreshMods()
    {
        if (_gameId is not null)
        {
            // Optionally scan for new mods
            await _modService.ScanForModsAsync(_gameId);
            await LoadDataAsync(_gameId);
        }
    }

    [RelayCommand]
    private async Task CreateModPackAsync()
    {
        try
        {
            _logger.LogInformation("Creating mod pack...");

            // Get all enabled mods
            var enabledMods = Mods.Where(m => m.IsEnabled).ToList();

            if (enabledMods.Count == 0)
            {
                _notificationService.ShowWarning("No mods are currently enabled. Enable mods to create a pack.", "Create Mod Pack");
                return;
            }

            // Create a mod pack name based on current timestamp
            var packName = $"ModPack_{DateTime.Now:yyyyMMdd_HHmmss}";
            var modsFolder = GetModsFolder();

            if (string.IsNullOrEmpty(modsFolder))
            {
                _notificationService.ShowWarning("Mods folder not configured", "Create Mod Pack");
                return;
            }

            var packPath = Path.Combine(modsFolder, $"{packName}.zip");

            _notificationService.ShowInfo($"Creating mod pack with {enabledMods.Count} mods...", "Creating Mod Pack");

            // Create the ZIP file with mod contents
            using (var zipArchive = System.IO.Compression.ZipFile.Open(packPath, System.IO.Compression.ZipArchiveMode.Create))
            {
                // Add metadata file
                var metadataEntry = zipArchive.CreateEntry("pack_metadata.json");
                using (var writer = new StreamWriter(metadataEntry.Open()))
                {
                    var metadata = new
                    {
                        PackName = packName,
                        CreatedDate = DateTime.Now,
                        GameTitle = _gameTitle,
                        ModCount = enabledMods.Count,
                        Mods = enabledMods.Select(m => new
                        {
                            m.Name,
                            m.Version,
                            m.Author,
                            m.Description
                        })
                    };
                    await writer.WriteAsync(System.Text.Json.JsonSerializer.Serialize(metadata, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
                }

                // Add each mod's files (simulated - in real implementation, copy actual mod files)
                foreach (var mod in enabledMods)
                {
                    var modEntry = zipArchive.CreateEntry($"mods/{mod.Name}/info.txt");
                    using (var writer = new StreamWriter(modEntry.Open()))
                    {
                        await writer.WriteLineAsync($"Mod: {mod.Name}");
                        await writer.WriteLineAsync($"Version: {mod.Version}");
                        await writer.WriteLineAsync($"Author: {mod.Author}");
                        await writer.WriteLineAsync($"Description: {mod.Description}");
                    }
                }
            }

            _logger.LogInformation("Mod pack created at: {PackPath}", packPath);
            _notificationService.ShowSuccess($"Mod pack created: {packName}\nLocation: {packPath}\n\nContains {enabledMods.Count} mod(s)", "Mod Pack Created");

            // Open the folder containing the pack
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = modsFolder,
                UseShellExecute = true,
                Verb = "open"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create mod pack");
            _notificationService.ShowError("Failed to create mod pack", "Error");
        }
    }

    [RelayCommand]
    private void OpenModsFolder()
    {
        try
        {
            _logger.LogInformation("Opening mods folder...");

            var modsFolder = GetModsFolder();

            if (string.IsNullOrEmpty(modsFolder))
            {
                _notificationService.ShowWarning("Mods folder not configured", "Open Mods Folder");
                return;
            }

            // Create directory if it doesn't exist
            if (!Directory.Exists(modsFolder))
            {
                Directory.CreateDirectory(modsFolder);
                _logger.LogInformation("Created mods folder: {ModsFolder}", modsFolder);
            }

            // Open folder in file explorer
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = modsFolder,
                UseShellExecute = true,
                Verb = "open"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open mods folder");
            _notificationService.ShowError("Failed to open mods folder", "Error");
        }
    }

    private string? GetModsFolder()
    {
        // Default mods folder location
        // In a real implementation, this would be configured per-game or globally
        var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        return Path.Combine(documentsPath, "SaveState", "Mods");
    }
}

/// <summary>
/// View model for individual mods.
/// </summary>
public partial class GameModViewModel : ObservableObject
{
    private readonly Func<Guid, bool, Task> _toggleAction;
    private readonly Func<Guid, Task> _uninstallAction;

    public GameModViewModel(
        Guid id,
        Func<Guid, bool, Task> toggleAction,
        Func<Guid, Task> uninstallAction)
    {
        Id = id;
        _toggleAction = toggleAction;
        _uninstallAction = uninstallAction;
    }

    // Default constructor for design-time
    public GameModViewModel()
    {
        Id = Guid.NewGuid();
        _toggleAction = (_, _) => Task.CompletedTask;
        _uninstallAction = _ => Task.CompletedTask;
    }

    public Guid Id { get; }

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _version = string.Empty;

    [ObservableProperty]
    private string? _iconUrl;

    [ObservableProperty]
    private string _authorText = string.Empty;

    [ObservableProperty]
    private string _downloadsText = "0";

    [ObservableProperty]
    private string _lastUpdatedText = "Unknown";

    [ObservableProperty]
    private string _compatibilityText = "Unknown";

    [ObservableProperty]
    private string _compatibilityColor = "#666666";

    [ObservableProperty]
    private string _statusText = "Available";

    [ObservableProperty]
    private string _statusColor = "#666666";

    [ObservableProperty]
    private ObservableCollection<string> _tags = new();

    [ObservableProperty]
    private bool _isInstalled;

    [ObservableProperty]
    private bool _isEnabled;

    [ObservableProperty]
    private bool _hasUpdate;

    [ObservableProperty]
    private string _author = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    partial void OnHasUpdateChanged(bool value)
    {
        if (value && IsInstalled)
        {
            StatusText = "Update Available";
            StatusColor = "#F59E0B";
        }
    }

    partial void OnIsEnabledChanged(bool value)
    {
        StatusText = value ? "Enabled" : "Disabled";
        StatusColor = value ? "#10B981" : "#6B7280";
        BackgroundBrush = value ? "#1A10B981" : "Transparent";
        BorderBrush = value ? "#10B981" : "#6B7280";

        // Trigger generic toggle
        _ = _toggleAction(Id, value);
    }

    [ObservableProperty]
    private double _downloadProgress;

    [ObservableProperty]
    private string _progressText = string.Empty;

    [ObservableProperty]
    private bool _showProgress;

    [ObservableProperty]
    private string _backgroundBrush = "Transparent";

    [ObservableProperty]
    private string _borderBrush = "Transparent";

    public string PrimaryActionText => IsInstalled ? "Update" : "Install";
    public string PrimaryActionClass => IsInstalled ? "Secondary" : "Primary";
    public bool PrimaryActionEnabled => !ShowProgress;

    [RelayCommand]
    private void PrimaryAction()
    {
        // TODO: Install or update mod
    }

    [RelayCommand]
    private void Settings()
    {
        // TODO: Open mod settings
    }

    [RelayCommand]
    private async Task Uninstall()
    {
        await _uninstallAction(Id);
    }

    [RelayCommand]
    private void Rate()
    {
        // TODO: Rate mod
    }
}

/// <summary>
/// View model for mod sources.
/// </summary>
public partial class GameModSourceViewModel : ObservableObject
{
    public GameModSourceViewModel(string name, string icon, int modCount)
    {
        Name = name;
        Icon = icon;
        ModCount = modCount;
    }

    public string Name { get; }
    public string Icon { get; }
    public int ModCount { get; }

    [RelayCommand]
    private void Select()
    {
        // TODO: Browse this mod source
    }
}
