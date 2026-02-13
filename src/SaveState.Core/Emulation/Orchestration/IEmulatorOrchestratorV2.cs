using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.RomManagement.Entities;

namespace SaveState.Core.Emulation.Orchestration;

/// <summary>
/// Next-generation emulator orchestrator with per-game profiles and auto-configuration.
/// </summary>
public interface IEmulatorOrchestratorV2
{
    /// <summary>
    /// Detects optimal emulator for a ROM.
    /// </summary>
    Task<Result<EmulatorRecommendation>> DetectOptimalEmulatorAsync(RomFile romFile, CancellationToken ct = default);

    /// <summary>
    /// Creates a game-specific emulator profile.
    /// </summary>
    Task<Result<EmulatorProfile>> CreateProfileAsync(string gameId, CreateProfileRequest request, CancellationToken ct = default);

    /// <summary>
    /// Gets an emulator profile.
    /// </summary>
    Task<Result<EmulatorProfile>> GetProfileAsync(string profileId, CancellationToken ct = default);

    /// <summary>
    /// Updates an emulator profile.
    /// </summary>
    Task<Result<EmulatorProfile>> UpdateProfileAsync(string profileId, UpdateProfileRequest request, CancellationToken ct = default);

    /// <summary>
    /// Deletes an emulator profile.
    /// </summary>
    Task<Result> DeleteProfileAsync(string profileId, CancellationToken ct = default);

    /// <summary>
    /// Lists profiles for a game.
    /// </summary>
    Task<Result<IReadOnlyList<EmulatorProfile>>> GetProfilesForGameAsync(string gameId, CancellationToken ct = default);

    /// <summary>
    /// Auto-configures emulator settings based on hardware.
    /// </summary>
    Task<Result<HardwareBasedConfig>> AutoConfigureAsync(EmulatorType emulatorType, CancellationToken ct = default);

    /// <summary>
    /// Gets hardware capabilities for emulation.
    /// </summary>
    Task<Result<HardwareCapabilities>> GetHardwareCapabilitiesAsync(CancellationToken ct = default);

    /// <summary>
    /// Launches a game with the specified profile.
    /// </summary>
    Task<Result<GameLaunchResult>> LaunchGameAsync(Game game, string? profileId = null, CancellationToken ct = default);

    /// <summary>
    /// Launches a ROM file with optimal settings.
    /// </summary>
    Task<Result<GameLaunchResult>> LaunchRomAsync(RomFile romFile, string? profileId = null, CancellationToken ct = default);

    /// <summary>
    /// Applies a profile to an emulator configuration.
    /// </summary>
    Task<Result> ApplyProfileAsync(string profileId, string configPath, CancellationToken ct = default);

    /// <summary>
    /// Validates a profile configuration.
    /// </summary>
    Task<Result<ProfileValidationResult>> ValidateProfileAsync(string profileId, CancellationToken ct = default);

    /// <summary>
    /// Benchmarks emulator performance with current settings.
    /// </summary>
    Task<Result<BenchmarkResult>> BenchmarkAsync(string profileId, int durationSeconds = 60, CancellationToken ct = default);

    /// <summary>
    /// Gets the default profile for a game.
    /// </summary>
    Task<Result<EmulatorProfile?>> GetDefaultProfileAsync(string gameId, CancellationToken ct = default);

    /// <summary>
    /// Sets the default profile for a game.
    /// </summary>
    Task<Result> SetDefaultProfileAsync(string gameId, string profileId, CancellationToken ct = default);

    /// <summary>
    /// Clones a profile for a different game.
    /// </summary>
    Task<Result<EmulatorProfile>> CloneProfileAsync(string sourceProfileId, string targetGameId, string? newName = null, CancellationToken ct = default);

    /// <summary>
    /// Imports a profile from file.
    /// </summary>
    Task<Result<EmulatorProfile>> ImportProfileAsync(string filePath, CancellationToken ct = default);

    /// <summary>
    /// Exports a profile to file.
    /// </summary>
    Task<Result<string>> ExportProfileAsync(string profileId, string outputPath, CancellationToken ct = default);

    /// <summary>
    /// Event raised when a game is launched.
    /// </summary>
    event EventHandler<GameLaunchedEventArgs>? GameLaunched;

    /// <summary>
    /// Event raised when emulator configuration is applied.
    /// </summary>
    event EventHandler<ProfileAppliedEventArgs>? ProfileApplied;
}

/// <summary>
/// Emulator profile for a specific game.
/// </summary>
public sealed record EmulatorProfile(
    string Id,
    string GameId,
    string Name,
    string? Description,
    EmulatorType EmulatorType,
    CoreConfiguration CoreConfig,
    VideoConfiguration VideoConfig,
    AudioConfiguration AudioConfig,
    InputConfiguration InputConfig,
    ShaderConfiguration? ShaderConfig,
    CheatConfiguration? CheatConfig,
    bool IsDefault,
    DateTime CreatedAt,
    DateTime? LastModifiedAt = null,
    DateTime? LastUsedAt = null);

/// <summary>
/// Core emulator configuration.
/// </summary>
public sealed record CoreConfiguration(
    string CoreName,
    string CoreVersion,
    string SystemName,
    Dictionary<string, string> CoreOptions,
    bool EnableRewind,
    int RewindBufferSizeMb,
    int SaveStateSlots = 10);

/// <summary>
/// Video configuration.
/// </summary>
public sealed record VideoConfiguration(
    int InternalResolutionWidth,
    int InternalResolutionHeight,
    VideoDriver Driver,
    bool VSync,
    int FrameDelay,
    bool IntegerScaling,
    bool KeepAspectRatio,
    FilterMode FilterMode,
    int RefreshRate = 60);

/// <summary>
/// Audio configuration.
/// </summary>
public sealed record AudioConfiguration(
    int SampleRate,
    AudioDriver Driver,
    int LatencyMs,
    bool SyncAudio,
    bool MuteWhenUnfocused,
    float VolumePercent = 100);

/// <summary>
/// Input configuration.
/// </summary>
public sealed record InputConfiguration(
    int MaxPlayers,
    InputDriver Driver,
    int PollTypeBehavior,
    int MenuToggleGamepadCombo,
    Dictionary<string, InputMapping> DeviceMappings);

/// <summary>
/// Input device mapping.
/// </summary>
public sealed record InputMapping(
    string DeviceName,
    string DeviceGuid,
    Dictionary<string, string> ButtonMappings,
    Dictionary<string, AnalogMapping> AnalogMappings);

/// <summary>
/// Analog input mapping.
/// </summary>
public sealed record AnalogMapping(
    string Axis,
    bool Inverted,
    float Deadzone,
    float Sensitivity);

/// <summary>
/// Shader configuration.
/// </summary>
public sealed record ShaderConfiguration(
    string? ActiveShaderPath,
    ShaderPreset Preset,
    int ShaderPasses,
    List<ShaderPass> Passes,
    Dictionary<string, object> ShaderParameters);

/// <summary>
/// Shader pass definition.
/// </summary>
public sealed record ShaderPass(
    string ShaderPath,
    string FilterMode,
    int ScaleX,
    int ScaleY);

/// <summary>
/// Cheat configuration.
/// </summary>
public sealed record CheatConfiguration(
    bool EnableCheats,
    bool ApplyAfterLoad,
    List<CheatEntry> Cheats);

/// <summary>
/// Cheat entry.
/// </summary>
public sealed record CheatEntry(
    string Id,
    string Name,
    string Code,
    bool Enabled,
    CheatType Type);

/// <summary>
/// Request to create a profile.
/// </summary>
public sealed record CreateProfileRequest(
    string Name,
    string? Description,
    EmulatorType EmulatorType,
    CoreConfiguration CoreConfig,
    VideoConfiguration VideoConfig,
    AudioConfiguration AudioConfig,
    InputConfiguration InputConfig,
    ShaderConfiguration? ShaderConfig = null,
    CheatConfiguration? CheatConfig = null);

/// <summary>
/// Request to update a profile.
/// </summary>
public sealed record UpdateProfileRequest(
    string? Name = null,
    string? Description = null,
    CoreConfiguration? CoreConfig = null,
    VideoConfiguration? VideoConfig = null,
    AudioConfiguration? AudioConfig = null,
    InputConfiguration? InputConfig = null,
    ShaderConfiguration? ShaderConfig = null,
    CheatConfiguration? CheatConfig = null);

/// <summary>
/// Emulator recommendation for a ROM.
/// </summary>
public sealed record EmulatorRecommendation(
    string RomHash,
    EmulatorType RecommendedEmulator,
    string RecommendedCore,
    string Reason,
    int ConfidenceScore,
    IReadOnlyList<EmulatorAlternative> Alternatives);

/// <summary>
/// Alternative emulator option.
/// </summary>
public sealed record EmulatorAlternative(
    EmulatorType Emulator,
    string Core,
    string Reason,
    int ConfidenceScore);

/// <summary>
/// Hardware-based configuration.
/// </summary>
public sealed record HardwareBasedConfig(
    EmulatorType EmulatorType,
    VideoConfiguration VideoConfig,
    AudioConfiguration AudioConfig,
    Dictionary<string, string> PerformanceSettings,
    OptimizationLevel RecommendedOptimization);

/// <summary>
/// Hardware capabilities for emulation.
/// </summary>
public sealed record HardwareCapabilities(
    string CpuName,
    int CpuCores,
    int CpuThreads,
    long MemoryMb,
    string GpuName,
    long VramMb,
    bool SupportsVulkan,
    bool SupportsDirectX12,
    bool SupportsOpenGl4,
    int MaxRecommendedResolution,
    bool CanRunParallelCores);

/// <summary>
/// Game launch result.
/// </summary>
public sealed record GameLaunchResult(
    bool Success,
    string? ProcessId,
    string? ProfileId,
    string LaunchCommand,
    DateTime LaunchedAt,
    string? ErrorMessage = null);

/// <summary>
/// Profile validation result.
/// </summary>
public sealed record ProfileValidationResult(
    bool IsValid,
    IReadOnlyList<ValidationMessage> Messages);

/// <summary>
/// Validation message.
/// </summary>
public sealed record ValidationMessage(
    ValidationLevel Level,
    string Message,
    string? Property = null);

/// <summary>
/// Benchmark result.
/// </summary>
public sealed record BenchmarkResult(
    string ProfileId,
    double AverageFps,
    double MinFps,
    double MaxFps,
    double FrameTimeMs,
    double CpuUsagePercent,
    double MemoryUsageMb,
    int DroppedFrames,
    TimeSpan Duration,
    DateTime BenchmarkedAt);

/// <summary>
/// Emulator types.
/// </summary>
public enum EmulatorType
{
    RetroArch,
    Dolphin,
    Pcsx2,
    Rpcs3,
    Cemu,
    Yuzu,
    Ryujinx,
    Xenia,
    Mgba,
    Desmume,
    Melonds,
    Ppsspp,
    Duckstation,
    BeetlePsx,
    Snes9x,
    Mesen,
    Fceux,
    GenesisPlusGx,
    BlastEm,
    Flycast,
    Redream,
    Custom
}

/// <summary>
/// Video drivers.
/// </summary>
public enum VideoDriver
{
    Vulkan,
    OpenGl,
    Direct3D11,
    Direct3D12,
    Metal,
    Software
}

/// <summary>
/// Audio drivers.
/// </summary>
public enum AudioDriver
{
    XAudio,
    Wasapi,
    DirectSound,
    Alsa,
    PulseAudio,
    CoreAudio,
    OpenAL
}

/// <summary>
/// Input drivers.
/// </summary>
public enum InputDriver
{
    XInput,
    DirectInput,
    RawInput,
    SDL,
    DInput
}

/// <summary>
/// Filter modes.
/// </summary>
public enum FilterMode
{
    Nearest,
    Linear,
    Bilinear,
    Trilinear,
    Anisotropic
}

/// <summary>
/// Shader presets.
/// </summary>
public enum ShaderPreset
{
    None,
    CrtRoyale,
    CrtLottes,
    CrtEasymode,
    CrtGeom,
    LcdGrid,
    Smaa,
    Fxaa,
    Sharpen,
    Smooth,
    PixelPerfect,
    Custom
}

/// <summary>
/// Cheat types.
/// </summary>
public enum CheatType
{
    ActionReplay,
    GameGenie,
    GameShark,
    ProActionReplay,
    Raw,
    Custom
}

/// <summary>
/// Optimization levels.
/// </summary>
public enum OptimizationLevel
{
    Conservative,
    Balanced,
    Aggressive,
    Maximum
}

/// <summary>
/// Validation levels.
/// </summary>
public enum ValidationLevel
{
    Info,
    Warning,
    Error
}

/// <summary>
/// Event args for game launched events.
/// </summary>
public sealed class GameLaunchedEventArgs : EventArgs
{
    public string GameId { get; }
    public string? ProfileId { get; }
    public EmulatorType EmulatorType { get; }
    public DateTime LaunchedAt { get; }

    public GameLaunchedEventArgs(string gameId, string? profileId, EmulatorType emulatorType)
    {
        GameId = gameId;
        ProfileId = profileId;
        EmulatorType = emulatorType;
        LaunchedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Event args for profile applied events.
/// </summary>
public sealed class ProfileAppliedEventArgs : EventArgs
{
    public string ProfileId { get; }
    public string GameId { get; }
    public string ConfigPath { get; }
    public DateTime AppliedAt { get; }

    public ProfileAppliedEventArgs(string profileId, string gameId, string configPath)
    {
        ProfileId = profileId;
        GameId = gameId;
        ConfigPath = configPath;
        AppliedAt = DateTime.UtcNow;
    }
}
