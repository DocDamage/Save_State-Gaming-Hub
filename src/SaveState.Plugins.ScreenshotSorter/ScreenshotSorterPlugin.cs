using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common.Services;
using SaveState.Core.Plugins;
using System.Security.Cryptography;
using System.Text.Json;

namespace SaveState.Plugins.ScreenshotSorter;

/// <summary>
/// Plugin that automatically organizes screenshots by game and date.
/// </summary>
public sealed class ScreenshotSorterPlugin : IPlugin
{
    private IPluginContext? _context;
    private ITimeProvider _timeProvider = null!;
    private FileSystemWatcher? _watcher;
    private ScreenshotSorterSettings _settings = new();
    private readonly HashSet<string> _processedHashes = new();
    private string? _currentGameTitle;

    public string Id => "screenshot-sorter";
    public string Name => "Screenshot Sorter";
    public string Version => "1.0.0";
    public string Author => "SaveState Team";
    public string? Description => "Auto-organize screenshots by game and date with duplicate detection.";
    public PluginCapabilities Capabilities => PluginCapabilities.UIExtension;

    public Task InitializeAsync(IPluginContext context, CancellationToken ct = default)
    {
        _context = context;
        _timeProvider = context.Services.GetRequiredService<ITimeProvider>();
        _context.Logger.LogInformation("Screenshot Sorter plugin initialized");

        LoadSettings();

        // Set up file system watcher
        if (_settings.Enabled && !string.IsNullOrEmpty(_settings.WatchFolder))
        {
            StartWatching();
        }

        // Register event handlers
        _context.EventReceived += OnEventReceived;

        return Task.CompletedTask;
    }

    public Task ShutdownAsync(CancellationToken ct = default)
    {
        StopWatching();
        SaveSettings();

        if (_context != null)
        {
            _context.EventReceived -= OnEventReceived;
        }

        return Task.CompletedTask;
    }

    private void OnEventReceived(object? sender, PluginEventArgs e)
    {
        switch (e.EventType)
        {
            case PluginEventType.GameLaunched:
                _currentGameTitle = e.Data?.ToString();
                _context?.Logger.LogDebug("Current game set to: {Game}", _currentGameTitle);
                break;
            case PluginEventType.GameClosed:
                _currentGameTitle = null;
                break;
        }
    }

    private void StartWatching()
    {
        try
        {
            if (!Directory.Exists(_settings.WatchFolder))
            {
                Directory.CreateDirectory(_settings.WatchFolder);
            }

            _watcher = new FileSystemWatcher(_settings.WatchFolder)
            {
                Filter = "*.*",
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime,
                IncludeSubdirectories = false
            };

            _watcher.Created += OnScreenshotCreated;
            _watcher.EnableRaisingEvents = true;

            _context?.Logger.LogInformation("Watching folder: {Folder}", _settings.WatchFolder);
        }
        catch (Exception ex)
        {
            _context?.Logger.LogError(ex, "Failed to start watching folder");
        }
    }

    private void StopWatching()
    {
        if (_watcher != null)
        {
            _watcher.EnableRaisingEvents = false;
            _watcher.Created -= OnScreenshotCreated;
            _watcher.Dispose();
            _watcher = null;
        }
    }

    private void OnScreenshotCreated(object sender, FileSystemEventArgs e)
    {
        // Fire and forget with explicit exception handling
        _ = ProcessScreenshotAsync(e);
    }

    private async Task ProcessScreenshotAsync(FileSystemEventArgs e)
    {
        try
        {
            // Check if it's an image file
            var extension = Path.GetExtension(e.FullPath).ToLowerInvariant();
            if (!_settings.SupportedExtensions.Contains(extension))
                return;

            // Wait a bit for the file to be fully written
            await Task.Delay(500);

            // Check for duplicates
            var hash = await ComputeFileHashAsync(e.FullPath);
            if (_processedHashes.Contains(hash))
            {
                _context?.Logger.LogInformation("Duplicate screenshot detected: {File}", e.Name);
                if (_settings.DeleteDuplicates)
                {
                    File.Delete(e.FullPath);
                    _context?.Logger.LogInformation("Deleted duplicate: {File}", e.Name);
                }
                return;
            }

            _processedHashes.Add(hash);

            // Organize the screenshot
            await OrganizeScreenshotAsync(e.FullPath);
        }
        catch (Exception ex)
        {
            _context?.Logger.LogError(ex, "Failed to process screenshot: {File}", e.FullPath);
        }
    }

    private async Task OrganizeScreenshotAsync(string sourcePath)
    {
        try
        {
            var fileName = Path.GetFileName(sourcePath);
            var gameTitle = _currentGameTitle ?? "Unknown Game";
            var date = _timeProvider.Now.ToString("yyyy-MM-dd");

            // Sanitize game title for folder name
            var sanitizedTitle = string.Join("_", gameTitle.Split(Path.GetInvalidFileNameChars()));

            // Build target path
            var targetFolder = Path.Combine(
                _settings.TargetFolder,
                sanitizedTitle,
                date
            );

            Directory.CreateDirectory(targetFolder);

            // Generate new filename with timestamp
            var timestamp = _timeProvider.Now.ToString("HHmmss");
            var extension = Path.GetExtension(fileName);
            var newFileName = _settings.FileNamePattern
                .Replace("{game}", sanitizedTitle)
                .Replace("{date}", date)
                .Replace("{time}", timestamp)
                .Replace("{original}", Path.GetFileNameWithoutExtension(fileName))
                + extension;

            var targetPath = Path.Combine(targetFolder, newFileName);

            // Move the file
            File.Move(sourcePath, targetPath, overwrite: false);

            _context?.Logger.LogInformation("Organized screenshot: {Source} -> {Target}",
                fileName, Path.Combine(sanitizedTitle, date, newFileName));
        }
        catch (Exception ex)
        {
            _context?.Logger.LogError(ex, "Failed to organize screenshot: {File}", sourcePath);
        }
    }

    private static async Task<string> ComputeFileHashAsync(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        var hash = await MD5.HashDataAsync(stream);
        return Convert.ToHexString(hash);
    }

    private void LoadSettings()
    {
        try
        {
            var settingsPath = Path.Combine(_context?.DataDirectory ?? ".", "settings.json");
            if (File.Exists(settingsPath))
            {
                var json = File.ReadAllText(settingsPath);
                _settings = JsonSerializer.Deserialize<ScreenshotSorterSettings>(json) ?? new ScreenshotSorterSettings();
            }
            else
            {
                // Set default paths
                _settings.WatchFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "Screenshots");
                _settings.TargetFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "SaveState Screenshots");
            }
        }
        catch (Exception ex)
        {
            _context?.Logger.LogError(ex, "Failed to load settings");
        }
    }

    private void SaveSettings()
    {
        try
        {
            var settingsPath = Path.Combine(_context?.DataDirectory ?? ".", "settings.json");
            var directory = Path.GetDirectoryName(settingsPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(settingsPath, json);
        }
        catch (Exception ex)
        {
            _context?.Logger.LogError(ex, "Failed to save settings");
        }
    }
}

/// <summary>
/// Settings for the Screenshot Sorter plugin.
/// </summary>
public sealed class ScreenshotSorterSettings
{
    /// <summary>
    /// Whether the sorter is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Folder to watch for new screenshots.
    /// </summary>
    public string WatchFolder { get; set; } = string.Empty;

    /// <summary>
    /// Target folder for organized screenshots.
    /// </summary>
    public string TargetFolder { get; set; } = string.Empty;

    /// <summary>
    /// Filename pattern: {game}, {date}, {time}, {original}
    /// </summary>
    public string FileNamePattern { get; set; } = "{game}_{date}_{time}";

    /// <summary>
    /// Supported image extensions.
    /// </summary>
    public HashSet<string> SupportedExtensions { get; set; } = new()
    {
        ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".webp"
    };

    /// <summary>
    /// Whether to delete duplicate screenshots.
    /// </summary>
    public bool DeleteDuplicates { get; set; } = true;

    /// <summary>
    /// Additional watch folders (Steam, Xbox, etc.)
    /// </summary>
    public List<string> AdditionalWatchFolders { get; set; } = new();
}
