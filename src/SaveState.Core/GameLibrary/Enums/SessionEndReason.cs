namespace SaveState.Core.GameLibrary.Enums;

/// <summary>
/// Represents the reason a game session ended.
/// </summary>
public enum SessionEndReason
{
    /// <summary>User manually closed the game.</summary>
    UserClosed,

    /// <summary>Game process crashed or terminated unexpectedly.</summary>
    ProcessCrashed,

    /// <summary>System shutdown or restart.</summary>
    SystemShutdown,

    /// <summary>Session timed out due to inactivity.</summary>
    Timeout,

    /// <summary>Application was closed while game was running.</summary>
    ApplicationExit,

    /// <summary>Unknown reason.</summary>
    Unknown
}
