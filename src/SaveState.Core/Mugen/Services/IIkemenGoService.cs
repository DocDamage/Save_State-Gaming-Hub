using SaveState.Core.Common;

namespace SaveState.Core.Mugen.Services;

/// <summary>
/// Service for IKEMEN GO integration - a modern MUGEN-compatible engine with enhanced features.
/// Provides compatibility checking, migration tools, network play, and advanced engine features.
/// </summary>
public interface IIkemenGoService
{
    #region Engine Detection & Compatibility

    /// <summary>
    /// Detects IKEMEN GO installation on the system.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Detection result with installation details.</returns>
    Task<Result<IkemenGoDetectionResult>> DetectInstallationAsync(CancellationToken ct = default);

    /// <summary>
    /// Checks compatibility of a MUGEN character with IKEMEN GO.
    /// </summary>
    /// <param name="characterPath">Path to character files.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Compatibility report with issues and recommendations.</returns>
    Task<Result<IkemenGoCompatibilityReport>> CheckCharacterCompatibilityAsync(
        string characterPath,
        CancellationToken ct = default);

    /// <summary>
    /// Checks compatibility of a MUGEN stage with IKEMEN GO.
    /// </summary>
    /// <param name="stagePath">Path to stage files.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Compatibility report.</returns>
    Task<Result<IkemenGoCompatibilityReport>> CheckStageCompatibilityAsync(
        string stagePath,
        CancellationToken ct = default);

    /// <summary>
    /// Validates select.def file for IKEMEN GO compatibility.
    /// </summary>
    /// <param name="selectDefPath">Path to select.def.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Validation result with required changes.</returns>
    Task<Result<SelectDefValidationResult>> ValidateSelectDefAsync(
        string selectDefPath,
        CancellationToken ct = default);

    #endregion

    #region Migration Tools

    /// <summary>
    /// Migrates a MUGEN character to IKEMEN GO format.
    /// </summary>
    /// <param name="characterPath">Source character path.</param>
    /// <param name="outputPath">Destination path.</param>
    /// <param name="options">Migration options.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Migration result with details.</returns>
    Task<Result<CharacterMigrationResult>> MigrateCharacterAsync(
        string characterPath,
        string outputPath,
        IkemenGoMigrationOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Migrates a MUGEN stage to IKEMEN GO format.
    /// </summary>
    /// <param name="stagePath">Source stage path.</param>
    /// <param name="outputPath">Destination path.</param>
    /// <param name="options">Migration options.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Migration result.</returns>
    Task<Result<StageMigrationResult>> MigrateStageAsync(
        string stagePath,
        string outputPath,
        IkemenGoMigrationOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Migrates entire MUGEN roster to IKEMEN GO.
    /// </summary>
    /// <param name="mugenPath">Source MUGEN directory.</param>
    /// <param name="ikemenPath">Destination IKEMEN directory.</param>
    /// <param name="options">Batch migration options.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Batch migration result.</returns>
    Task<Result<BatchMigrationResult>> MigrateFullRosterAsync(
        string mugenPath,
        string ikemenPath,
        IkemenGoBatchMigrationOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Converts MUGEN screenpack to IKEMEN GO format.
    /// </summary>
    /// <param name="screenpackPath">Source screenpack path.</param>
    /// <param name="outputPath">Destination path.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Conversion result.</returns>
    Task<Result<ScreenpackConversionResult>> ConvertScreenpackAsync(
        string screenpackPath,
        string outputPath,
        CancellationToken ct = default);

    #endregion

    #region Configuration Management

    /// <summary>
    /// Loads IKEMEN GO configuration (config.json).
    /// </summary>
    /// <param name="configPath">Path to config.json.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Configuration object.</returns>
    Task<Result<IkemenGoConfig>> LoadConfigAsync(
        string configPath,
        CancellationToken ct = default);

    /// <summary>
    /// Saves IKEMEN GO configuration.
    /// </summary>
    /// <param name="config">Configuration to save.</param>
    /// <param name="configPath">Path to save to.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Success/failure result.</returns>
    Task<Result> SaveConfigAsync(
        IkemenGoConfig config,
        string configPath,
        CancellationToken ct = default);

    /// <summary>
    /// Updates specific configuration options.
    /// </summary>
    /// <param name="configPath">Path to config.json.</param>
    /// <param name="updates">Key-value pairs to update.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Update result with applied changes.</returns>
    Task<Result<ConfigUpdateResult>> UpdateConfigOptionsAsync(
        string configPath,
        IReadOnlyDictionary<string, object> updates,
        CancellationToken ct = default);

    /// <summary>
    /// Validates IKEMEN GO configuration.
    /// </summary>
    /// <param name="config">Configuration to validate.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Validation result with issues.</returns>
    Task<Result<IkemenGoConfigValidation>> ValidateConfigAsync(
        IkemenGoConfig config,
        CancellationToken ct = default);

    #endregion

    #region Network & Online Features

    /// <summary>
    /// Configures online play settings.
    /// </summary>
    /// <param name="settings">Network settings.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Configuration result.</returns>
    Task<Result> ConfigureOnlinePlayAsync(
        IkemenGoNetworkSettings settings,
        CancellationToken ct = default);

    /// <summary>
    /// Tests network connectivity for online play.
    /// </summary>
    /// <param name="host">Host to test.</param>
    /// <param name="port">Port to test.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Connection test result.</returns>
    Task<Result<NetworkTestResult>> TestNetworkConnectionAsync(
        string host,
        int port,
        CancellationToken ct = default);

    /// <summary>
    /// Gets available online lobby servers.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of available servers.</returns>
    Task<Result<IReadOnlyList<IkemenGoServer>>> GetLobbyServersAsync(CancellationToken ct = default);

    /// <summary>
    /// Configures rollback netcode settings.
    /// </summary>
    /// <param name="settings">Rollback settings.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Configuration result.</returns>
    Task<Result> ConfigureRollbackNetcodeAsync(
        RollbackNetcodeSettings settings,
        CancellationToken ct = default);

    /// <summary>
    /// Validates port forwarding for hosting.
    /// </summary>
    /// <param name="port">Port to validate.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Validation result.</returns>
    Task<Result<PortValidationResult>> ValidatePortForwardingAsync(
        int port,
        CancellationToken ct = default);

    #endregion

    #region Module System

    /// <summary>
    /// Gets installed Lua modules.
    /// </summary>
    /// <param name="ikemenPath">IKEMEN installation path.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of installed modules.</returns>
    Task<Result<IReadOnlyList<IkemenGoModule>>> GetInstalledModulesAsync(
        string ikemenPath,
        CancellationToken ct = default);

    /// <summary>
    /// Installs a Lua module.
    /// </summary>
    /// <param name="modulePath">Path to module file/directory.</param>
    /// <param name="ikemenPath">IKEMEN installation path.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Installation result.</returns>
    Task<Result<ModuleInstallResult>> InstallModuleAsync(
        string modulePath,
        string ikemenPath,
        CancellationToken ct = default);

    /// <summary>
    /// Uninstalls a Lua module.
    /// </summary>
    /// <param name="moduleName">Name of module to uninstall.</param>
    /// <param name="ikemenPath">IKEMEN installation path.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Uninstallation result.</returns>
    Task<Result> UninstallModuleAsync(
        string moduleName,
        string ikemenPath,
        CancellationToken ct = default);

    /// <summary>
    /// Validates a Lua module.
    /// </summary>
    /// <param name="modulePath">Path to module.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Validation result.</returns>
    Task<Result<ModuleValidationResult>> ValidateModuleAsync(
        string modulePath,
        CancellationToken ct = default);

    /// <summary>
    /// Enables/disables a module.
    /// </summary>
    /// <param name="moduleName">Module name.</param>
    /// <param name="enabled">Enable or disable.</param>
    /// <param name="ikemenPath">IKEMEN installation path.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Operation result.</returns>
    Task<Result> ToggleModuleAsync(
        string moduleName,
        bool enabled,
        string ikemenPath,
        CancellationToken ct = default);

    #endregion

    #region Launch & Execution

    /// <summary>
    /// Launches IKEMEN GO with specified options.
    /// </summary>
    /// <param name="options">Launch options.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Process information.</returns>
    Task<Result<IkemenGoProcess>> LaunchAsync(
        IkemenGoLaunchOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Launches IKEMEN GO in training mode.
    /// </summary>
    /// <param name="character1">First character.</param>
    /// <param name="character2">Second character.</param>
    /// <param name="stage">Stage to use.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Process information.</returns>
    Task<Result<IkemenGoProcess>> LaunchTrainingModeAsync(
        string character1,
        string character2,
        string? stage = null,
        CancellationToken ct = default);

    /// <summary>
    /// Launches IKEMEN GO in online versus mode.
    /// </summary>
    /// <param name="connectionString">Connection string (host:port).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Process information.</returns>
    Task<Result<IkemenGoProcess>> LaunchOnlineVersusAsync(
        string connectionString,
        CancellationToken ct = default);

    /// <summary>
    /// Monitors running IKEMEN GO process.
    /// </summary>
    /// <param name="processId">Process ID to monitor.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Process status.</returns>
    Task<Result<IkemenGoProcessStatus>> GetProcessStatusAsync(
        int processId,
        CancellationToken ct = default);

    /// <summary>
    /// Terminates IKEMEN GO process.
    /// </summary>
    /// <param name="processId">Process ID to terminate.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Termination result.</returns>
    Task<Result> TerminateAsync(
        int processId,
        CancellationToken ct = default);

    #endregion

    #region Replay System

    /// <summary>
    /// Gets list of saved replays.
    /// </summary>
    /// <param name="ikemenPath">IKEMEN installation path.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of replays.</returns>
    Task<Result<IReadOnlyList<IkemenGoReplay>>> GetReplaysAsync(
        string ikemenPath,
        CancellationToken ct = default);

    /// <summary>
    /// Exports replay to video format.
    /// </summary>
    /// <param name="replayPath">Path to replay file.</param>
    /// <param name="outputPath">Output video path.</param>
    /// <param name="options">Export options.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Export result.</returns>
    Task<Result<ReplayExportResult>> ExportReplayToVideoAsync(
        string replayPath,
        string outputPath,
        IkemenGoReplayExportOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Converts MUGEN replay to IKEMEN GO format.
    /// </summary>
    /// <param name="mugenReplayPath">MUGEN replay path.</param>
    /// <param name="outputPath">Output path.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Conversion result.</returns>
    Task<Result> ConvertMugenReplayAsync(
        string mugenReplayPath,
        string outputPath,
        CancellationToken ct = default);

    /// <summary>
    /// Analyzes replay data.
    /// </summary>
    /// <param name="replayPath">Path to replay file.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Analysis results.</returns>
    Task<Result<IkemenGoReplayAnalysis>> AnalyzeReplayAsync(
        string replayPath,
        CancellationToken ct = default);

    #endregion

    #region Statistics & Analytics

    /// <summary>
    /// Gets player statistics from IKEMEN GO save data.
    /// </summary>
    /// <param name="saveDataPath">Path to save data.</param>
    /// <param name="playerName">Player name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Player statistics.</returns>
    Task<Result<IkemenGoPlayerStats>> GetPlayerStatsAsync(
        string saveDataPath,
        string playerName,
        CancellationToken ct = default);

    /// <summary>
    /// Gets match history.
    /// </summary>
    /// <param name="saveDataPath">Path to save data.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Match history.</returns>
    Task<Result<IReadOnlyList<IkemenGoMatchRecord>>> GetMatchHistoryAsync(
        string saveDataPath,
        CancellationToken ct = default);

    /// <summary>
    /// Generates compatibility report for entire content library.
    /// </summary>
    /// <param name="contentPaths">Paths to content directories.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Library compatibility report.</returns>
    Task<Result<IkemenGoLibraryCompatibilityReport>> AnalyzeLibraryCompatibilityAsync(
        IReadOnlyList<string> contentPaths,
        CancellationToken ct = default);

    #endregion
}

#region Result Types

/// <summary>
/// IKEMEN GO detection result.
/// </summary>
public record IkemenGoDetectionResult(
    bool IsInstalled,
    string? InstallationPath,
    IkemenGoVersion? Version,
    IReadOnlyList<string> AvailableExecutables,
    IReadOnlyList<string> ContentPaths);

/// <summary>
/// IKEMEN GO version information.
/// </summary>
public record IkemenGoVersion(
    int Major,
    int Minor,
    int Patch,
    string? BuildLabel,
    DateTime ReleaseDate);

/// <summary>
/// Compatibility report for content.
/// </summary>
public record IkemenGoCompatibilityReport(
    string ContentPath,
    string ContentName,
    IkemenGoCompatibilityLevel CompatibilityLevel,
    IReadOnlyList<IkemenGoCompatibilityIssue> Issues,
    IReadOnlyList<IkemenGoMigrationSuggestion> Suggestions);

/// <summary>
/// Compatibility level.
/// </summary>
public enum IkemenGoCompatibilityLevel
{
    Full,
    Partial,
    RequiresMigration,
    Incompatible
}

/// <summary>
/// Compatibility issue.
/// </summary>
public record IkemenGoCompatibilityIssue(
    IkemenGoIssueSeverity Severity,
    string Code,
    string Message,
    string? FilePath,
    int? LineNumber);

/// <summary>
/// Issue severity.
/// </summary>
public enum IkemenGoIssueSeverity
{
    Info,
    Warning,
    Error,
    Critical
}

/// <summary>
/// Migration suggestion.
/// </summary>
public record IkemenGoMigrationSuggestion(
    string IssueCode,
    string Suggestion,
    bool CanAutoFix,
    string? DocumentationUrl);

/// <summary>
/// Select.def validation result.
/// </summary>
public record SelectDefValidationResult(
    bool IsValid,
    IReadOnlyList<string> InvalidEntries,
    IReadOnlyList<string> MissingFiles,
    IReadOnlyList<string> IkemenGoSpecificOptions);

/// <summary>
/// Character migration result.
/// </summary>
public record CharacterMigrationResult(
    bool Success,
    string SourcePath,
    string DestinationPath,
    IReadOnlyList<string> FilesMigrated,
    IReadOnlyList<IkemenGoCompatibilityIssue> Issues,
    IReadOnlyList<string> AppliedFixes);

/// <summary>
/// Stage migration result.
/// </summary>
public record StageMigrationResult(
    bool Success,
    string SourcePath,
    string DestinationPath,
    IReadOnlyList<string> FilesMigrated,
    IReadOnlyList<IkemenGoCompatibilityIssue> Issues);

/// <summary>
/// Batch migration result.
/// </summary>
public record BatchMigrationResult(
    int TotalCharacters,
    int SuccessfulMigrations,
    int FailedMigrations,
    int SkippedMigrations,
    TimeSpan Duration,
    IReadOnlyList<CharacterMigrationResult> Results);

/// <summary>
/// Screenpack conversion result.
/// </summary>
public record ScreenpackConversionResult(
    bool Success,
    string SourcePath,
    string DestinationPath,
    IReadOnlyList<string> ConvertedFiles,
    IReadOnlyList<string> ManualStepsRequired);

/// <summary>
/// Migration options.
/// </summary>
public record IkemenGoMigrationOptions(
    bool AutoFixIssues,
    bool BackupOriginals,
    bool UpdateAnimations,
    bool ConvertTriggers,
    MigrationStrictness Strictness);

/// <summary>
/// Migration strictness level.
/// </summary>
public enum MigrationStrictness
{
    Lenient,
    Moderate,
    Strict
}

/// <summary>
/// Batch migration options.
/// </summary>
public record IkemenGoBatchMigrationOptions(
    IkemenGoMigrationOptions CharacterOptions,
    IkemenGoMigrationOptions StageOptions,
    bool MigrateScreenpack,
    bool ParallelProcessing,
    int MaxParallelism,
    IReadOnlyList<string>? CharacterFilter,
    IReadOnlyList<string>? ExcludeCharacters);

/// <summary>
/// IKEMEN GO configuration.
/// </summary>
public record IkemenGoConfig(
    IkemenGoVideoSettings Video,
    IkemenGoAudioSettings Audio,
    IkemenGoGameplaySettings Gameplay,
    IkemenGoNetworkSettings Network,
    IkemenGoDebugSettings Debug,
    IkemenGoModuleSettings Modules);

/// <summary>
/// Video settings.
/// </summary>
public record IkemenGoVideoSettings(
    int Width,
    int Height,
    bool Fullscreen,
    bool VSync,
    int FpsLimit,
    string Renderer);

/// <summary>
/// Audio settings.
/// </summary>
public record IkemenGoAudioSettings(
    int MasterVolume,
    int BgmVolume,
    int SfxVolume,
    bool AudioEffects);

/// <summary>
/// Gameplay settings.
/// </summary>
public record IkemenGoGameplaySettings(
    int Difficulty,
    int GameSpeed,
    int RoundTime,
    int RoundCount,
    bool AutoGuard,
    IReadOnlyList<string> DefaultCharacters);

/// <summary>
/// Network settings.
/// </summary>
public record IkemenGoNetworkSettings(
    string PlayerName,
    int ListenPort,
    int MaxPing,
    bool UseLobby,
    string? LobbyServer,
    RollbackNetcodeSettings Rollback);

/// <summary>
/// Rollback netcode settings.
/// </summary>
public record RollbackNetcodeSettings(
    bool Enabled,
    int InputDelay,
    int RollbackFrames,
    bool DesyncDetection);

/// <summary>
/// Debug settings.
/// </summary>
public record IkemenGoDebugSettings(
    bool DebugMode,
    bool ShowFps,
    bool ShowInputs,
    bool LogToFile);

/// <summary>
/// Module settings.
/// </summary>
public record IkemenGoModuleSettings(
    bool AllowModules,
    IReadOnlyList<string> EnabledModules,
    IReadOnlyList<string> DisabledModules);

/// <summary>
/// Config update result.
/// </summary>
public record ConfigUpdateResult(
    IReadOnlyList<string> UpdatedKeys,
    IReadOnlyList<string> FailedKeys,
    IReadOnlyList<string> ValidationErrors);

/// <summary>
/// Config validation result.
/// </summary>
public record IkemenGoConfigValidation(
    bool IsValid,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> Warnings);

/// <summary>
/// Network test result.
/// </summary>
public record NetworkTestResult(
    bool Success,
    int PingMs,
    int PacketLoss,
    string? ErrorMessage);

/// <summary>
/// IKEMEN GO server info.
/// </summary>
public record IkemenGoServer(
    string Name,
    string Host,
    int Port,
    string Region,
    int CurrentPlayers,
    int MaxPlayers,
    int PingMs);

/// <summary>
/// Port validation result.
/// </summary>
public record PortValidationResult(
    bool IsOpen,
    string PublicIp,
    string? ErrorMessage);

/// <summary>
/// IKEMEN GO module.
/// </summary>
public record IkemenGoModule(
    string Name,
    string Version,
    string Author,
    string Description,
    bool IsEnabled,
    bool IsOfficial,
    IReadOnlyList<string> Dependencies);

/// <summary>
/// Module installation result.
/// </summary>
public record ModuleInstallResult(
    bool Success,
    string ModuleName,
    string InstallationPath,
    IReadOnlyList<string> InstalledFiles,
    IReadOnlyList<string> Conflicts);

/// <summary>
/// Module validation result.
/// </summary>
public record ModuleValidationResult(
    bool IsValid,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> MissingDependencies);

/// <summary>
/// Launch options.
/// </summary>
public record IkemenGoLaunchOptions(
    string IkemenPath,
    string? ConfigPath,
    bool QuickVersus,
    bool TrainingMode,
    bool OnlineMode,
    string? ConnectionString,
    IReadOnlyList<string>? Characters,
    string? Stage);

/// <summary>
/// Process information.
/// </summary>
public record IkemenGoProcess(
    int ProcessId,
    string ExecutablePath,
    DateTime StartTime,
    IkemenGoLaunchOptions LaunchOptions);

/// <summary>
/// Process status.
/// </summary>
public record IkemenGoProcessStatus(
    int ProcessId,
    bool IsRunning,
    TimeSpan CpuTime,
    long MemoryUsed,
    int? Fps,
    string? CurrentScene);

/// <summary>
/// IKEMEN GO replay.
/// </summary>
public record IkemenGoReplay(
    string FilePath,
    string FileName,
    DateTime RecordedAt,
    string GameVersion,
    string Player1Name,
    string Player2Name,
    string Player1Character,
    string Player2Character,
    TimeSpan Duration,
    long FileSize);

/// <summary>
/// Replay export options.
/// </summary>
public record IkemenGoReplayExportOptions(
    string Format,
    int Width,
    int Height,
    int Fps,
    int Quality,
    bool IncludeOverlay);

/// <summary>
/// Replay export result.
/// </summary>
public record ReplayExportResult(
    bool Success,
    string OutputPath,
    TimeSpan Duration,
    long FileSize);

/// <summary>
/// Replay analysis.
/// </summary>
public record IkemenGoReplayAnalysis(
    TimeSpan Duration,
    int TotalFrames,
    IReadOnlyList<IkemenGoRoundAnalysis> Rounds,
    IkemenGoCharacterStats Player1Stats,
    IkemenGoCharacterStats Player2Stats);

/// <summary>
/// Round analysis.
/// </summary>
public record IkemenGoRoundAnalysis(
    int RoundNumber,
    string Winner,
    TimeSpan Duration,
    int TotalHitsP1,
    int TotalHitsP2,
    int MaxComboP1,
    int MaxComboP2);

/// <summary>
/// Character stats from replay.
/// </summary>
public record IkemenGoCharacterStats(
    string CharacterName,
    int TotalDamageDealt,
    int TotalDamageReceived,
    int HitsConnected,
    int HitsBlocked,
    int SpecialMovesUsed,
    int SuperMovesUsed);

/// <summary>
/// Player statistics.
/// </summary>
public record IkemenGoPlayerStats(
    string PlayerName,
    int TotalMatches,
    int Wins,
    int Losses,
    int Draws,
    TimeSpan TotalPlayTime,
    string FavoriteCharacter,
    IReadOnlyList<IkemenGoCharacterUsage> CharacterUsage);

/// <summary>
/// Character usage statistics.
/// </summary>
public record IkemenGoCharacterUsage(
    string CharacterName,
    int MatchesPlayed,
    int Wins,
    float WinRate);

/// <summary>
/// Match record.
/// </summary>
public record IkemenGoMatchRecord(
    DateTime Timestamp,
    string Mode,
    string Player1Name,
    string Player2Name,
    string Player1Character,
    string Player2Character,
    string Result,
    TimeSpan Duration);

/// <summary>
/// Library compatibility report.
/// </summary>
public record IkemenGoLibraryCompatibilityReport(
    int TotalCharacters,
    int FullyCompatible,
    int PartiallyCompatible,
    int RequiresMigration,
    int Incompatible,
    IReadOnlyList<IkemenGoCompatibilityReport> DetailedReports);

#endregion
