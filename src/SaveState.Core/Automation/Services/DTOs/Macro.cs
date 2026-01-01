namespace SaveState.Core.Automation.Services.DTOs;

/// <summary>
/// Represents a recorded macro with actions and metadata.
/// </summary>
public sealed record Macro(
    Guid Id,
    string Name,
    string Description,
    Guid GameId,
    string UserId,
    IReadOnlyList<MacroAction> Actions,
    MacroMetadata Metadata,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    bool IsActive = true);

/// <summary>
/// Configuration for macro recording.
/// </summary>
public sealed record MacroRecordingConfig(
    Guid GameId,
    string Name,
    string Description,
    RecordingMode Mode,
    TimeSpan? MaxDuration = null,
    IReadOnlyList<string>? Tags = null);

/// <summary>
/// Recording mode for macros.
/// </summary>
public enum RecordingMode
{
    Manual,
    Automatic,
    Continuous
}

/// <summary>
/// A single action within a macro.
/// </summary>
public abstract record MacroAction(
    string ActionType,
    TimeSpan Timestamp,
    IReadOnlyDictionary<string, object>? Parameters = null);

/// <summary>
/// Mouse action.
/// </summary>
public sealed record MouseAction(
    MouseEventType EventType,
    int X,
    int Y,
    MouseButton Button,
    TimeSpan Timestamp) : MacroAction("Mouse", Timestamp);

/// <summary>
/// Keyboard action.
/// </summary>
public sealed record KeyboardAction(
    Key Key,
    KeyEventType EventType,
    TimeSpan Timestamp) : MacroAction("Keyboard", Timestamp);

/// <summary>
/// Game-specific action.
/// </summary>
public sealed record GameAction(
    string GameCommand,
    IReadOnlyDictionary<string, object> Parameters,
    TimeSpan Timestamp) : MacroAction("Game", Timestamp, Parameters);

/// <summary>
/// System action.
/// </summary>
public sealed record SystemAction(
    string SystemCommand,
    IReadOnlyDictionary<string, object> Parameters,
    TimeSpan Timestamp) : MacroAction("System", Timestamp, Parameters);

/// <summary>
/// Mouse event types.
/// </summary>
public enum MouseEventType
{
    Move,
    Click,
    DoubleClick,
    RightClick,
    MiddleClick,
    Wheel
}

/// <summary>
/// Mouse buttons.
/// </summary>
public enum MouseButton
{
    Left,
    Right,
    Middle
}

/// <summary>
/// Key event types.
/// </summary>
public enum KeyEventType
{
    Press,
    Release
}

/// <summary>
/// Keyboard keys.
/// </summary>
public enum Key
{
    // Add common keys here - this would be a comprehensive enum
    A, B, C, D, E, F, G, H, I, J, K, L, M, N, O, P, Q, R, S, T, U, V, W, X, Y, Z,
    Space, Enter, Escape, Tab, Backspace,
    F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12,
    LeftShift, RightShift, LeftCtrl, RightCtrl, LeftAlt, RightAlt
}

/// <summary>
/// Metadata for macros.
/// </summary>
public sealed record MacroMetadata(
    string Author,
    string Version,
    IReadOnlyList<string> Tags,
    IReadOnlyDictionary<string, string> Properties,
    MacroStatistics? Statistics = null);

/// <summary>
/// Statistics for macro usage.
/// </summary>
public sealed record MacroStatistics(
    int TotalExecutions,
    TimeSpan TotalRuntime,
    TimeSpan AverageRuntime,
    DateTime LastExecuted,
    int SuccessCount,
    int FailureCount);

/// <summary>
/// Categories and tags for macro organization.
/// </summary>
public sealed record MacroCategories(
    IReadOnlyList<string> Categories,
    IReadOnlyList<string> PopularTags,
    IReadOnlyDictionary<string, int> TagUsageCounts);

/// <summary>
/// Search filters for macros.
/// </summary>
public sealed record MacroSearchFilters(
    Guid? GameId = null,
    string? Author = null,
    IReadOnlyList<string>? Tags = null,
    DateTime? CreatedAfter = null,
    DateTime? CreatedBefore = null,
    bool? IsActive = null);