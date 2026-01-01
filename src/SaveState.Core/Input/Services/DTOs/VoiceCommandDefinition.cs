using SaveState.Core.Input.Services.DTOs;
using SaveState.Core.Sync.Services.DTOs;

namespace SaveState.Core.Input.Services.DTOs;

/// <summary>
/// Defines a voice command with its associated action.
/// </summary>
public sealed record VoiceCommandDefinition(
    string CommandPhrase,
    string Description,
    VoiceCommandAction Action,
    object? Parameters = null,
    IReadOnlyList<string>? AlternativePhrases = null);

/// <summary>
/// Available voice command actions.
/// </summary>
public enum VoiceCommandAction
{
    // Game Control
    LaunchGame,
    CloseGame,
    PauseGame,
    ResumeGame,
    SaveGame,
    LoadGame,

    // Navigation
    OpenLibrary,
    OpenSettings,
    GoBack,
    GoHome,

    // Save States
    CreateSaveState,
    LoadSaveState,
    ListSaveStates,

    // System Control
    AdjustVolume,
    MuteAudio,
    ChangeDisplayMode,

    // Cloud Gaming
    StartCloudSession,
    EndCloudSession,
    CheckNetworkQuality,

    // Voice Control
    StopListening,
    StartListening,
    ShowCommands,

    // Custom Actions
    ExecuteMacro,
    RunSystemCommand,

    // AI Features
    AskAssistant,
    GetGameTips,
    GenerateBriefing
}

/// <summary>
/// Parameters for voice command actions.
/// </summary>
public abstract record VoiceCommandParameters;

public sealed record LaunchGameParameters(Guid GameId) : VoiceCommandParameters;
public sealed record LoadSaveStateParameters(Guid SaveStateId) : VoiceCommandParameters;
public sealed record ExecuteMacroParameters(Guid MacroId) : VoiceCommandParameters;
public sealed record AdjustVolumeParameters(int VolumeLevel) : VoiceCommandParameters;
public sealed record StartCloudSessionParameters(Guid GameId, CloudGamingProvider Provider) : VoiceCommandParameters;
public sealed record AskAssistantParameters(string Question) : VoiceCommandParameters;