namespace SaveState.Presentation.Models.QuickActions;

/// <summary>
/// Predefined quick action IDs.
/// </summary>
public static class QuickActionIds
{
    // Game actions
    public const string GameLaunch = "game.launch";

    // Save State actions
    public const string SaveStateQuickSave = "savestate.quicksave";
    public const string SaveStateQuickLoad = "savestate.quickload";

    // Screenshot actions
    public const string ScreenshotTake = "screenshot.take";

    // Recording actions
    public const string RecordingStart = "recording.start";
    public const string RecordingStop = "recording.stop";
    public const string RecordingToggle = "recording.toggle";
}
