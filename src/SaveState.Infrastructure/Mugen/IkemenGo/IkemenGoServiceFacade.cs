using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Mugen.Services;
using SaveState.Infrastructure.Mugen.IkemenGo.Managers;

namespace SaveState.Infrastructure.Mugen.IkemenGo;

/// <summary>
/// Coordinator service for IKEMEN GO operations.
/// Delegates all operations to specialized manager classes.
/// </summary>
public class IkemenGoService : IIkemenGoService
{
    private readonly IkemenGoInstallationManager _installationManager;
    private readonly IkemenGoMigrationManager _migrationManager;
    private readonly IkemenGoConfigurationManager _configurationManager;
    private readonly IkemenGoNetworkManager _networkManager;
    private readonly IkemenGoModuleManager _moduleManager;
    private readonly IkemenGoLaunchManager _launchManager;
    private readonly IkemenGoReplayManager _replayManager;
    private readonly IkemenGoAnalyticsManager _analyticsManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="IkemenGoService"/> class.
    /// </summary>
    public IkemenGoService(
        IkemenGoInstallationManager installationManager,
        IkemenGoMigrationManager migrationManager,
        IkemenGoConfigurationManager configurationManager,
        IkemenGoNetworkManager networkManager,
        IkemenGoModuleManager moduleManager,
        IkemenGoLaunchManager launchManager,
        IkemenGoReplayManager replayManager,
        IkemenGoAnalyticsManager analyticsManager)
    {
        _installationManager = installationManager;
        _migrationManager = migrationManager;
        _configurationManager = configurationManager;
        _networkManager = networkManager;
        _moduleManager = moduleManager;
        _launchManager = launchManager;
        _replayManager = replayManager;
        _analyticsManager = analyticsManager;
    }

    #region Engine Detection & Compatibility

    /// <inheritdoc />
    public Task<Result<IkemenGoDetectionResult>> DetectInstallationAsync(CancellationToken ct = default) =>
        _installationManager.DetectInstallationAsync(ct);

    /// <inheritdoc />
    public Task<Result<IkemenGoCompatibilityReport>> CheckCharacterCompatibilityAsync(
        string characterPath,
        CancellationToken ct = default) =>
        _installationManager.CheckCharacterCompatibilityAsync(characterPath, ct);

    /// <inheritdoc />
    public Task<Result<IkemenGoCompatibilityReport>> CheckStageCompatibilityAsync(
        string stagePath,
        CancellationToken ct = default) =>
        _installationManager.CheckStageCompatibilityAsync(stagePath, ct);

    /// <inheritdoc />
    public Task<Result<SelectDefValidationResult>> ValidateSelectDefAsync(
        string selectDefPath,
        CancellationToken ct = default) =>
        _installationManager.ValidateSelectDefAsync(selectDefPath, ct);

    #endregion

    #region Migration Tools

    /// <inheritdoc />
    public Task<Result<CharacterMigrationResult>> MigrateCharacterAsync(
        string characterPath,
        string outputPath,
        IkemenGoMigrationOptions options,
        CancellationToken ct = default) =>
        _migrationManager.MigrateCharacterAsync(characterPath, outputPath, options, ct);

    /// <inheritdoc />
    public Task<Result<StageMigrationResult>> MigrateStageAsync(
        string stagePath,
        string outputPath,
        IkemenGoMigrationOptions options,
        CancellationToken ct = default) =>
        _migrationManager.MigrateStageAsync(stagePath, outputPath, options, ct);

    /// <inheritdoc />
    public Task<Result<BatchMigrationResult>> MigrateFullRosterAsync(
        string mugenPath,
        string ikemenPath,
        IkemenGoBatchMigrationOptions options,
        CancellationToken ct = default) =>
        _migrationManager.MigrateFullRosterAsync(mugenPath, ikemenPath, options, progress: null, ct);

    /// <inheritdoc />
    public Task<Result<ScreenpackConversionResult>> ConvertScreenpackAsync(
        string screenpackPath,
        string outputPath,
        CancellationToken ct = default) =>
        _migrationManager.ConvertScreenpackAsync(screenpackPath, outputPath, ct);

    #endregion

    #region Configuration Management

    /// <inheritdoc />
    public Task<Result<IkemenGoConfig>> LoadConfigAsync(
        string configPath,
        CancellationToken ct = default) =>
        _configurationManager.LoadConfigAsync(configPath, ct);

    /// <inheritdoc />
    public Task<Result> SaveConfigAsync(
        IkemenGoConfig config,
        string configPath,
        CancellationToken ct = default) =>
        _configurationManager.SaveConfigAsync(configPath, config, ct);

    /// <inheritdoc />
    public Task<Result<ConfigUpdateResult>> UpdateConfigOptionsAsync(
        string configPath,
        IReadOnlyDictionary<string, object> updates,
        CancellationToken ct = default) =>
        _configurationManager.UpdateConfigOptionsAsync(configPath, updates, ct);

    /// <inheritdoc />
    public Task<Result<IkemenGoConfigValidation>> ValidateConfigAsync(
        IkemenGoConfig config,
        CancellationToken ct = default) =>
        _configurationManager.ValidateConfigAsync(config, ct);

    #endregion

    #region Network & Online Features

    /// <inheritdoc />
    public async Task<Result> ConfigureOnlinePlayAsync(
        IkemenGoNetworkSettings settings,
        CancellationToken ct = default)
    {
        // This method needs a config path - use a default or require detection first
        var detectionResult = await _installationManager.DetectInstallationAsync(ct);
        if (detectionResult.IsFailure || !detectionResult.Value.IsInstalled)
        {
            return Result.Failure("IKEMEN GO not found", ErrorType.NotFound);
        }

        var configPath = Path.Combine(detectionResult.Value.InstallationPath!, "config.json");
        return await _networkManager.ConfigureOnlinePlayAsync(configPath, settings, ct);
    }

    /// <inheritdoc />
    public Task<Result<NetworkTestResult>> TestNetworkConnectionAsync(
        string host,
        int port,
        CancellationToken ct = default) =>
        _networkManager.TestNetworkConnectionAsync(host, port, ct);

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<IkemenGoServer>>> GetLobbyServersAsync(CancellationToken ct = default) =>
        _networkManager.GetLobbyServersAsync(ct);

    /// <inheritdoc />
    public async Task<Result> ConfigureRollbackNetcodeAsync(
        RollbackNetcodeSettings settings,
        CancellationToken ct = default)
    {
        var detectionResult = await _installationManager.DetectInstallationAsync(ct);
        if (detectionResult.IsFailure || !detectionResult.Value.IsInstalled)
        {
            return Result.Failure("IKEMEN GO not found", ErrorType.NotFound);
        }

        var configPath = Path.Combine(detectionResult.Value.InstallationPath!, "config.json");
        return await _networkManager.ConfigureRollbackNetcodeAsync(configPath, settings, ct);
    }

    /// <inheritdoc />
    public Task<Result<PortValidationResult>> ValidatePortForwardingAsync(
        int port,
        CancellationToken ct = default) =>
        _networkManager.ValidatePortForwardingAsync(port, ct);

    #endregion

    #region Module System

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<IkemenGoModule>>> GetInstalledModulesAsync(
        string ikemenPath,
        CancellationToken ct = default) =>
        _moduleManager.GetInstalledModulesAsync(Path.Combine(ikemenPath, "external", "mods"), ct);

    /// <inheritdoc />
    public Task<Result<ModuleInstallResult>> InstallModuleAsync(
        string modulePath,
        string ikemenPath,
        CancellationToken ct = default) =>
        _moduleManager.InstallModuleAsync(modulePath, Path.Combine(ikemenPath, "external", "mods"), null, ct);

    /// <inheritdoc />
    public Task<Result> UninstallModuleAsync(
        string moduleName,
        string ikemenPath,
        CancellationToken ct = default) =>
        _moduleManager.UninstallModuleAsync(Path.Combine(ikemenPath, "external", "mods"), moduleName, ct);

    /// <inheritdoc />
    public Task<Result<ModuleValidationResult>> ValidateModuleAsync(
        string modulePath,
        CancellationToken ct = default) =>
        _moduleManager.ValidateModuleAsync(modulePath, ct);

    /// <inheritdoc />
    public Task<Result> ToggleModuleAsync(
        string moduleName,
        bool enabled,
        string ikemenPath,
        CancellationToken ct = default) =>
        _moduleManager.ToggleModuleAsync(Path.Combine(ikemenPath, "external", "mods"), moduleName, enabled, ct);

    #endregion

    #region Launch & Execution

    /// <inheritdoc />
    public async Task<Result<IkemenGoProcess>> LaunchAsync(
        IkemenGoLaunchOptions options,
        CancellationToken ct = default)
    {
        var exePath = Path.Combine(options.IkemenPath, "Ikemen_GO.exe");
        return await _launchManager.LaunchAsync(exePath, options, ct);
    }

    /// <inheritdoc />
    public async Task<Result<IkemenGoProcess>> LaunchTrainingModeAsync(
        string character1,
        string character2,
        string? stage = null,
        CancellationToken ct = default)
    {
        var detectionResult = await _installationManager.DetectInstallationAsync(ct);
        if (detectionResult.IsFailure || !detectionResult.Value.IsInstalled)
        {
            return Result<IkemenGoProcess>.Failure("IKEMEN GO not found", ErrorType.NotFound);
        }

        var exePath = Path.Combine(detectionResult.Value.InstallationPath!, "Ikemen_GO.exe");
        return await _launchManager.LaunchTrainingModeAsync(exePath, character1, character2, stage, ct);
    }

    /// <inheritdoc />
    public async Task<Result<IkemenGoProcess>> LaunchOnlineVersusAsync(
        string connectionString,
        CancellationToken ct = default)
    {
        var detectionResult = await _installationManager.DetectInstallationAsync(ct);
        if (detectionResult.IsFailure || !detectionResult.Value.IsInstalled)
        {
            return Result<IkemenGoProcess>.Failure("IKEMEN GO not found", ErrorType.NotFound);
        }

        var exePath = Path.Combine(detectionResult.Value.InstallationPath!, "Ikemen_GO.exe");
        return await _launchManager.LaunchOnlineVersusAsync(exePath, connectionString, ct);
    }

    /// <inheritdoc />
    public Task<Result<IkemenGoProcessStatus>> GetProcessStatusAsync(
        int processId,
        CancellationToken ct = default) =>
        _launchManager.GetProcessStatusAsync(processId, ct);

    /// <inheritdoc />
    public Task<Result> TerminateAsync(
        int processId,
        CancellationToken ct = default) =>
        _launchManager.TerminateAsync(processId, false, ct);

    #endregion

    #region Replay System

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<IkemenGoReplay>>> GetReplaysAsync(
        string ikemenPath,
        CancellationToken ct = default) =>
        _replayManager.GetReplaysAsync(Path.Combine(ikemenPath, "replays"), ct);

    /// <inheritdoc />
    public Task<Result<ReplayExportResult>> ExportReplayToVideoAsync(
        string replayPath,
        string outputPath,
        IkemenGoReplayExportOptions options,
        CancellationToken ct = default) =>
        _replayManager.ExportReplayToVideoAsync(replayPath, outputPath, options, ct);

    /// <inheritdoc />
    public Task<Result> ConvertMugenReplayAsync(
        string mugenReplayPath,
        string outputPath,
        CancellationToken ct = default) =>
        _replayManager.ConvertMugenReplayAsync(mugenReplayPath, outputPath, ct);

    /// <inheritdoc />
    public Task<Result<IkemenGoReplayAnalysis>> AnalyzeReplayAsync(
        string replayPath,
        CancellationToken ct = default) =>
        _replayManager.AnalyzeReplayAsync(replayPath, ct);

    #endregion

    #region Statistics & Analytics

    /// <inheritdoc />
    public Task<Result<IkemenGoPlayerStats>> GetPlayerStatsAsync(
        string saveDataPath,
        string playerName,
        CancellationToken ct = default) =>
        _analyticsManager.GetPlayerStatsAsync(playerName, saveDataPath, ct);

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<IkemenGoMatchRecord>>> GetMatchHistoryAsync(
        string saveDataPath,
        CancellationToken ct = default) =>
        _analyticsManager.GetMatchHistoryAsync("Player", saveDataPath, 100, ct);

    /// <inheritdoc />
    public async Task<Result<IkemenGoLibraryCompatibilityReport>> AnalyzeLibraryCompatibilityAsync(
        IReadOnlyList<string> contentPaths,
        CancellationToken ct = default)
    {
        // Separate chars and stages paths
        var charsPaths = contentPaths.Where(p => p.EndsWith("chars", StringComparison.OrdinalIgnoreCase)).ToList();
        var stagesPaths = contentPaths.Where(p => p.EndsWith("stages", StringComparison.OrdinalIgnoreCase)).ToList();

        return await _analyticsManager.AnalyzeLibraryCompatibilityAsync(
            charsPaths.FirstOrDefault() ?? string.Empty,
            stagesPaths.FirstOrDefault() ?? string.Empty,
            ct);
    }

    #endregion
}
