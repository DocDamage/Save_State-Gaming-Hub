using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Mugen.Services;

namespace SaveState.Infrastructure.Mugen.IkemenGo;

/// <summary>
/// Thin facade for IKEMEN GO operations.
/// </summary>
public class IkemenGoService : IIkemenGoService
{
    private readonly IkemenGoServiceOperations _operations;

    public IkemenGoService(ILogger<IkemenGoService> logger, ITimeProvider timeProvider)
    {
        _operations = new IkemenGoServiceOperations(logger, timeProvider);
    }

    public Task<Result<IkemenGoDetectionResult>> DetectInstallationAsync(CancellationToken ct = default) =>
        _operations.DetectInstallationAsync(ct);

    public Task<Result<IkemenGoCompatibilityReport>> CheckCharacterCompatibilityAsync(
        string characterPath,
        CancellationToken ct = default) =>
        _operations.CheckCharacterCompatibilityAsync(characterPath, ct);

    public Task<Result<IkemenGoCompatibilityReport>> CheckStageCompatibilityAsync(
        string stagePath,
        CancellationToken ct = default) =>
        _operations.CheckStageCompatibilityAsync(stagePath, ct);

    public Task<Result<SelectDefValidationResult>> ValidateSelectDefAsync(
        string selectDefPath,
        CancellationToken ct = default) =>
        _operations.ValidateSelectDefAsync(selectDefPath, ct);

    public Task<Result<CharacterMigrationResult>> MigrateCharacterAsync(
        string characterPath,
        string outputPath,
        IkemenGoMigrationOptions options,
        CancellationToken ct = default) =>
        _operations.MigrateCharacterAsync(characterPath, outputPath, options, ct);

    public Task<Result<StageMigrationResult>> MigrateStageAsync(
        string stagePath,
        string outputPath,
        IkemenGoMigrationOptions options,
        CancellationToken ct = default) =>
        _operations.MigrateStageAsync(stagePath, outputPath, options, ct);

    public Task<Result<BatchMigrationResult>> MigrateFullRosterAsync(
        string mugenPath,
        string ikemenPath,
        IkemenGoBatchMigrationOptions options,
        CancellationToken ct = default) =>
        _operations.MigrateFullRosterAsync(mugenPath, ikemenPath, options, ct);

    public Task<Result<ScreenpackConversionResult>> ConvertScreenpackAsync(
        string screenpackPath,
        string outputPath,
        CancellationToken ct = default) =>
        _operations.ConvertScreenpackAsync(screenpackPath, outputPath, ct);

    public Task<Result<IkemenGoConfig>> LoadConfigAsync(
        string configPath,
        CancellationToken ct = default) =>
        _operations.LoadConfigAsync(configPath, ct);

    public Task<Result> SaveConfigAsync(
        IkemenGoConfig config,
        string configPath,
        CancellationToken ct = default) =>
        _operations.SaveConfigAsync(config, configPath, ct);

    public Task<Result<ConfigUpdateResult>> UpdateConfigOptionsAsync(
        string configPath,
        IReadOnlyDictionary<string, object> updates,
        CancellationToken ct = default) =>
        _operations.UpdateConfigOptionsAsync(configPath, updates, ct);

    public Task<Result<IkemenGoConfigValidation>> ValidateConfigAsync(
        IkemenGoConfig config,
        CancellationToken ct = default) =>
        _operations.ValidateConfigAsync(config, ct);

    public Task<Result> ConfigureOnlinePlayAsync(
        IkemenGoNetworkSettings settings,
        CancellationToken ct = default) =>
        _operations.ConfigureOnlinePlayAsync(settings, ct);

    public Task<Result<NetworkTestResult>> TestNetworkConnectionAsync(
        string host,
        int port,
        CancellationToken ct = default) =>
        _operations.TestNetworkConnectionAsync(host, port, ct);

    public Task<Result<IReadOnlyList<IkemenGoServer>>> GetLobbyServersAsync(CancellationToken ct = default) =>
        _operations.GetLobbyServersAsync(ct);

    public Task<Result> ConfigureRollbackNetcodeAsync(
        RollbackNetcodeSettings settings,
        CancellationToken ct = default) =>
        _operations.ConfigureRollbackNetcodeAsync(settings, ct);

    public Task<Result<PortValidationResult>> ValidatePortForwardingAsync(
        int port,
        CancellationToken ct = default) =>
        _operations.ValidatePortForwardingAsync(port, ct);

    public Task<Result<IReadOnlyList<IkemenGoModule>>> GetInstalledModulesAsync(
        string ikemenPath,
        CancellationToken ct = default) =>
        _operations.GetInstalledModulesAsync(ikemenPath, ct);

    public Task<Result<ModuleInstallResult>> InstallModuleAsync(
        string modulePath,
        string ikemenPath,
        CancellationToken ct = default) =>
        _operations.InstallModuleAsync(modulePath, ikemenPath, ct);

    public Task<Result> UninstallModuleAsync(
        string moduleName,
        string ikemenPath,
        CancellationToken ct = default) =>
        _operations.UninstallModuleAsync(moduleName, ikemenPath, ct);

    public Task<Result<ModuleValidationResult>> ValidateModuleAsync(
        string modulePath,
        CancellationToken ct = default) =>
        _operations.ValidateModuleAsync(modulePath, ct);

    public Task<Result> ToggleModuleAsync(
        string moduleName,
        bool enabled,
        string ikemenPath,
        CancellationToken ct = default) =>
        _operations.ToggleModuleAsync(moduleName, enabled, ikemenPath, ct);

    public Task<Result<IkemenGoProcess>> LaunchAsync(
        IkemenGoLaunchOptions options,
        CancellationToken ct = default) =>
        _operations.LaunchAsync(options, ct);

    public Task<Result<IkemenGoProcess>> LaunchTrainingModeAsync(
        string character1,
        string character2,
        string? stage = null,
        CancellationToken ct = default) =>
        _operations.LaunchTrainingModeAsync(character1, character2, stage, ct);

    public Task<Result<IkemenGoProcess>> LaunchOnlineVersusAsync(
        string connectionString,
        CancellationToken ct = default) =>
        _operations.LaunchOnlineVersusAsync(connectionString, ct);

    public Task<Result<IkemenGoProcessStatus>> GetProcessStatusAsync(
        int processId,
        CancellationToken ct = default) =>
        _operations.GetProcessStatusAsync(processId, ct);

    public Task<Result> TerminateAsync(
        int processId,
        CancellationToken ct = default) =>
        _operations.TerminateAsync(processId, ct);

    public Task<Result<IReadOnlyList<IkemenGoReplay>>> GetReplaysAsync(
        string ikemenPath,
        CancellationToken ct = default) =>
        _operations.GetReplaysAsync(ikemenPath, ct);

    public Task<Result<ReplayExportResult>> ExportReplayToVideoAsync(
        string replayPath,
        string outputPath,
        IkemenGoReplayExportOptions options,
        CancellationToken ct = default) =>
        _operations.ExportReplayToVideoAsync(replayPath, outputPath, options, ct);

    public Task<Result> ConvertMugenReplayAsync(
        string mugenReplayPath,
        string outputPath,
        CancellationToken ct = default) =>
        _operations.ConvertMugenReplayAsync(mugenReplayPath, outputPath, ct);

    public Task<Result<IkemenGoReplayAnalysis>> AnalyzeReplayAsync(
        string replayPath,
        CancellationToken ct = default) =>
        _operations.AnalyzeReplayAsync(replayPath, ct);

    public Task<Result<IkemenGoPlayerStats>> GetPlayerStatsAsync(
        string saveDataPath,
        string playerName,
        CancellationToken ct = default) =>
        _operations.GetPlayerStatsAsync(saveDataPath, playerName, ct);

    public Task<Result<IReadOnlyList<IkemenGoMatchRecord>>> GetMatchHistoryAsync(
        string saveDataPath,
        CancellationToken ct = default) =>
        _operations.GetMatchHistoryAsync(saveDataPath, ct);

    public Task<Result<IkemenGoLibraryCompatibilityReport>> AnalyzeLibraryCompatibilityAsync(
        IReadOnlyList<string> contentPaths,
        CancellationToken ct = default) =>
        _operations.AnalyzeLibraryCompatibilityAsync(contentPaths, ct);
}
