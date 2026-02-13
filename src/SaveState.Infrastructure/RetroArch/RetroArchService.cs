using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SaveState.Core.Achievements;
using SaveState.Core.Common;
using SaveState.Core.RetroArch;
using SaveState.Core.RetroArch.Services;
using SaveState.Infrastructure.RetroArch.Models;
using SaveState.Infrastructure.RetroArch.RetroArchCloudSync;
using SaveState.Infrastructure.RetroArch.Services.RetroArch;
using System.Security.Cryptography;

namespace SaveState.Infrastructure.RetroArch;

/// <summary>
/// Implementation of RetroArch integration service.
/// Coordinates work across specialized engines.
/// </summary>
public class RetroArchService : IRetroArchService
{
    private readonly RetroArchOptions _options;
    private readonly ISyncEngine? _syncEngine;
    private readonly ILogger<RetroArchService> _logger;

    // Specialized engines
    private readonly IPathDetectionEngine _pathDetection;
    private readonly IGameManagementEngine _gameManagement;
    private readonly ICoreManagementEngine _coreManagement;
    private readonly IConfigurationEngine _configuration;
    private readonly INetworkCommandEngine _networkCommand;
    private readonly ISaveStateEngine _saveState;
    private readonly IRetroAchievementsEngine _retroAchievements;

    private string? _retroArchPath;

    public RetroArchService(
        ILogger<RetroArchService> logger,
        IOptions<RetroArchOptions> options,
        IRetroAchievementsClient? retroAchievementsClient = null,
        ISyncEngine? syncEngine = null,
        IPathDetectionEngine? pathDetection = null,
        IGameManagementEngine? gameManagement = null,
        ICoreManagementEngine? coreManagement = null,
        IConfigurationEngine? configuration = null,
        INetworkCommandEngine? networkCommand = null,
        ISaveStateEngine? saveState = null,
        IRetroAchievementsEngine? retroAchievements = null)
    {
        _logger = logger;
        _options = options.Value;
        _syncEngine = syncEngine;

        // Initialize engines (fallback to default implementations if not provided)
        var loggerFactory = LoggerFactory.Create(b => b.AddProvider(new ForwardingLoggerProvider(logger)));
        _pathDetection = pathDetection ?? new PathDetectionEngine(loggerFactory.CreateLogger<PathDetectionEngine>());
        _gameManagement = gameManagement ?? new GameManagementEngine(loggerFactory.CreateLogger<GameManagementEngine>());
        _coreManagement = coreManagement ?? new CoreManagementEngine(loggerFactory.CreateLogger<CoreManagementEngine>());
        _configuration = configuration ?? new ConfigurationEngine(loggerFactory.CreateLogger<ConfigurationEngine>());
        _networkCommand = networkCommand ?? new NetworkCommandEngine(loggerFactory.CreateLogger<NetworkCommandEngine>(), options);
        _saveState = saveState ?? new SaveStateEngine(loggerFactory.CreateLogger<SaveStateEngine>(), _networkCommand);
        _retroAchievements = retroAchievements ?? new RetroAchievementsEngine(
            loggerFactory.CreateLogger<RetroAchievementsEngine>(), retroAchievementsClient);

        // Use configured path if available
        if (!string.IsNullOrEmpty(_options.InstallPath) && File.Exists(_options.InstallPath))
        {
            _retroArchPath = _options.InstallPath;
            _logger.LogInformation("Using configured RetroArch path: {Path}", _retroArchPath);
        }

        // Initialize RetroAchievements if configured
        _retroAchievements.Initialize(_options.RetroAchievementsUsername, _options.RetroAchievementsApiKey);
    }

    #region Path Detection

    public async Task<Result<string>> DetectRetroArchPathAsync(CancellationToken ct = default)
    {
        var result = await _pathDetection.DetectRetroArchPathAsync(_options, ct);
        if (result.IsSuccess) _retroArchPath = result.Value;
        return result;
    }

    #endregion

    #region Games

    public async Task<Result<IReadOnlyList<RetroArchGame>>> GetGamesAsync(CancellationToken ct = default)
    {
        var pathResult = await EnsureRetroArchPathAsync(ct);
        if (pathResult.IsFailure)
            return Result.Failure<IReadOnlyList<RetroArchGame>>(pathResult.Error!);

        return await _gameManagement.GetGamesAsync(_retroArchPath!, _options.PlaylistsPath, ct);
    }

    public Task<Result> LaunchGameAsync(string gamePath, string corePath, CancellationToken ct = default)
    {
        return _retroArchPath == null
            ? Task.FromResult(Result.Failure("RetroArch path not detected"))
            : _gameManagement.LaunchGameAsync(_retroArchPath, gamePath, corePath, ct);
    }

    #endregion

    #region Cores

    public async Task<Result<IReadOnlyList<RetroArchCore>>> GetInstalledCoresAsync(CancellationToken ct = default)
    {
        var pathResult = await EnsureRetroArchPathAsync(ct);
        return pathResult.IsFailure
            ? Result.Failure<IReadOnlyList<RetroArchCore>>(pathResult.Error!)
            : await _coreManagement.GetInstalledCoresAsync(_retroArchPath!, _options.CoresPath, ct);
    }

    public Task<Result<IReadOnlyList<RetroArchCore>>> GetAvailableCoresAsync(CancellationToken ct = default)
        => _coreManagement.GetAvailableCoresAsync(ct);

    public async Task<Result> InstallCoreAsync(string coreName, CancellationToken ct = default)
    {
        var pathResult = await EnsureRetroArchPathAsync(ct);
        return pathResult.IsFailure
            ? Result.Failure(pathResult.Error!)
            : await _coreManagement.InstallCoreAsync(_retroArchPath!, coreName, ct);
    }

    public async Task<Result> UpdateCoreAsync(string coreName, CancellationToken ct = default)
    {
        var pathResult = await EnsureRetroArchPathAsync(ct);
        return pathResult.IsFailure
            ? Result.Failure(pathResult.Error!)
            : await _coreManagement.UpdateCoreAsync(_retroArchPath!, coreName, ct);
    }

    #endregion

    #region Configuration

    public async Task<Result<RetroArchConfig>> GetConfigAsync(CancellationToken ct = default)
    {
        var pathResult = await EnsureRetroArchPathAsync(ct);
        return pathResult.IsFailure
            ? Result.Failure<RetroArchConfig>(pathResult.Error!)
            : await _configuration.GetConfigAsync(_retroArchPath!, ct);
    }

    #endregion

    #region Cloud Sync

    public async Task<Result> SyncSavesAsync(CancellationToken ct = default)
    {
        if (!_options.CloudSyncEnabled)
            return Result.Failure("Cloud sync is not enabled in configuration");

        if (string.IsNullOrEmpty(_options.CloudSyncConnectionString))
            return Result.Failure("Cloud sync connection string not configured");

        if (_syncEngine == null)
            return Result.Failure($"Cloud sync provider '{_options.CloudSyncProvider}' is not registered");

        var pathResult = await EnsureRetroArchPathAsync(ct);
        if (pathResult.IsFailure) return pathResult;

        var configResult = await _configuration.GetConfigAsync(_retroArchPath!, ct);
        if (configResult.IsFailure || configResult.Value == null)
            return Result.Failure("Could not get RetroArch configuration");

        var config = configResult.Value;
        if (string.IsNullOrEmpty(config.SavefileDirectory))
            return Result.Failure("Save directory not configured in RetroArch");

        _logger.LogInformation("Syncing RetroArch saves from: {Directory}", config.SavefileDirectory);

        var filesToSync = await ScanSaveFilesAsync(config, ct);
        var syncResult = await _syncEngine.SyncAsync(filesToSync, _retroArchPath!, ct);

        if (syncResult.IsSuccess)
            _logger.LogInformation("Successfully synced {Count} files to {Provider}", filesToSync.Count, _options.CloudSyncProvider);

        return syncResult;
    }

    private async Task<List<SyncFileInfo>> ScanSaveFilesAsync(RetroArchConfig config, CancellationToken ct)
    {
        var saveFiles = new List<string>();
        if (Directory.Exists(config.SavefileDirectory))
        {
            saveFiles.AddRange(Directory.GetFiles(config.SavefileDirectory, "*.srm", SearchOption.AllDirectories));
            saveFiles.AddRange(Directory.GetFiles(config.SavefileDirectory, "*.state*", SearchOption.AllDirectories));
        }

        if (!string.IsNullOrEmpty(config.SavestateDirectory) && Directory.Exists(config.SavestateDirectory))
            saveFiles.AddRange(Directory.GetFiles(config.SavestateDirectory, "*.state*", SearchOption.AllDirectories));

        _logger.LogInformation("Found {Count} save files to sync", saveFiles.Count);

        var filesToSync = new List<SyncFileInfo>();
        foreach (var file in saveFiles)
        {
            try
            {
                var fileInfo = new FileInfo(file);
                var hash = await CalculateFileHashAsync(file, ct);
                filesToSync.Add(new SyncFileInfo { Path = file, Hash = hash, Modified = fileInfo.LastWriteTimeUtc });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating hash for file: {File}", file);
            }
        }

        return filesToSync;
    }

    private static async Task<string> CalculateFileHashAsync(string filePath, CancellationToken ct)
    {
        using var stream = File.OpenRead(filePath);
        var hashBytes = await SHA256.HashDataAsync(stream, ct);
        return Convert.ToHexString(hashBytes);
    }

    #endregion

    #region Achievements

    public Task<Result<IReadOnlyList<Achievement>>> GetAchievementsAsync(string gameHash, CancellationToken ct = default)
        => _retroAchievements.GetAchievementsAsync(gameHash, ct);

    #endregion

    #region Save States

    public async Task<Result<string>> CreateSaveStateAsync(int slot = -1, CancellationToken ct = default)
    {
        if (!_options.NetworkCommandEnabled)
            return Result.Failure<string>("RetroArch network command interface is not enabled");
        return await _saveState.CreateSaveStateAsync(slot, ct);
    }

    public async Task<Result> LoadSaveStateAsync(int slot, CancellationToken ct = default)
    {
        if (!_options.NetworkCommandEnabled)
            return Result.Failure("RetroArch network command interface is not enabled");
        return await _saveState.LoadSaveStateAsync(slot, ct);
    }

    public async Task<Result> LoadSaveStateFromFileAsync(string filePath, CancellationToken ct = default)
    {
        if (!_options.NetworkCommandEnabled)
            return Result.Failure("RetroArch network command interface is not enabled");
        return await _saveState.LoadSaveStateFromFileAsync(filePath, ct);
    }

    #endregion

    #region Screenshots

    public async Task<Result<string>> CaptureScreenshotAsync(CancellationToken ct = default)
    {
        if (!_options.NetworkCommandEnabled)
            return Result.Failure<string>("RetroArch network command interface is not enabled");

        var screenshotDir = _retroArchPath != null
            ? Path.Combine(Path.GetDirectoryName(_retroArchPath)!, "screenshots")
            : null;

        return await _saveState.CaptureScreenshotAsync(screenshotDir, ct);
    }

    #endregion

    #region Network Commands

    public Task<Result<string>> SendCommandAsync(string command, CancellationToken ct = default)
        => _networkCommand.SendCommandAsync(command, ct);

    public Task<Result<bool>> IsRunningAsync(CancellationToken ct = default)
        => _networkCommand.IsRunningAsync(ct);

    #endregion

    #region Helper Methods

    private async Task<Result> EnsureRetroArchPathAsync(CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(_retroArchPath) && File.Exists(_retroArchPath))
            return Result.Success();

        var detectResult = await DetectRetroArchPathAsync(ct);
        return detectResult.IsFailure
            ? Result.Failure(detectResult.Error ?? "RetroArch not found")
            : Result.Success();
    }

    private class ForwardingLoggerProvider : ILoggerProvider
    {
        private readonly ILogger _logger;
        public ForwardingLoggerProvider(ILogger logger) => _logger = logger;
        public ILogger CreateLogger(string categoryName) => _logger;
        public void Dispose() { }
    }

    #endregion
}
