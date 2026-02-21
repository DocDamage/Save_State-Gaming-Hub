using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Plugins;
using System.IO.Compression;

namespace SaveState.Plugins.GameBackupManager;

public sealed class GameBackupManagerPlugin : IPlugin
{
    private IPluginContext? _context;
    private ITimeProvider? _timeProvider;
    private readonly string _backupRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SaveStateBackups");

    public string Id => "game-backup-manager";
    public string Name => "Game Backup Manager";
    public string Version => "1.0.0";
    public string Author => "SaveState Team";
    public string? Description => "Automatically backup game saves upon closing.";
    public PluginCapabilities Capabilities => PluginCapabilities.CloudStorage; // Leveraging CloudStorage capability for backups

    public Task InitializeAsync(IPluginContext context, CancellationToken ct = default)
    {
        _context = context;
        _timeProvider = context.Services.GetService<ITimeProvider>();
        _context.Logger.LogInformation("Backup Manager Initialized");

        Directory.CreateDirectory(_backupRoot);
        _context.EventReceived += OnEventReceived;

        return Task.CompletedTask;
    }

    private void OnEventReceived(object? sender, PluginEventArgs e)
    {
        if (e.EventType == PluginEventType.GameClosed && e.Data is string gameTitle)
        {
            // Fire and forget with proper exception handling in PerformBackupAsync
            _ = PerformBackupAsync(gameTitle);
        }
    }

    private async Task PerformBackupAsync(string gameTitle)
    {
        try
        {
            // In a real implementation, we'd look up the specific save path for this game
            // from a database (e.g., PCGamingWiki).
            // For this Wave 5 Foundation, we'll log the intent.

            var logger = _context?.Logger;
            if (logger?.IsEnabled(LogLevel.Information) == true)
            {
                logger.LogInformation("Starting backup scan for {Game}...", gameTitle);
            }

            // Mock Example:
            // var savePath = GetSavePath(gameTitle);
            // if (Directory.Exists(savePath)) {
            //     var now = _timeProvider?.Now ?? DateTime.Now;
            //     var zipPath = Path.Combine(_backupRoot, $"{gameTitle}_{now:yyyyMMddHHmmss}.zip");
            //     ZipFile.CreateFromDirectory(savePath, zipPath);
            // }

            await Task.Delay(100);
        }
        catch (Exception ex)
        {
            var logger = _context?.Logger;
            if (logger?.IsEnabled(LogLevel.Error) == true)
            {
                logger.LogError(ex, "Backup failed for {Game}", gameTitle);
            }
        }
    }

    public Task ShutdownAsync(CancellationToken ct = default) => Task.CompletedTask;
}
