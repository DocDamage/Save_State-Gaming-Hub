using SaveState.Core.Common;
using SaveState.Core.Common.Services;

namespace SaveState.Core.Social.MobileRemote;

/// <summary>
/// Service for managing second screen content on mobile devices.
/// </summary>
public interface ISecondScreenService
{
    /// <summary>
    /// Creates a second screen session.
    /// </summary>
    Task<Result<SecondScreenSession>> CreateSessionAsync(string deviceId, SecondScreenConfiguration config, CancellationToken ct = default);

    /// <summary>
    /// Closes a second screen session.
    /// </summary>
    Task<Result> CloseSessionAsync(string sessionId, CancellationToken ct = default);

    /// <summary>
    /// Updates the content displayed on the second screen.
    /// </summary>
    Task<Result> UpdateContentAsync(string sessionId, ScreenContent content, CancellationToken ct = default);

    /// <summary>
    /// Gets the current session.
    /// </summary>
    Task<Result<SecondScreenSession?>> GetSessionAsync(string sessionId, CancellationToken ct = default);

    /// <summary>
    /// Lists active sessions.
    /// </summary>
    Task<Result<IReadOnlyList<SecondScreenSession>>> GetActiveSessionsAsync(CancellationToken ct = default);

    /// <summary>
    /// Sets the display mode for a session.
    /// </summary>
    Task<Result> SetDisplayModeAsync(string sessionId, DisplayMode mode, CancellationToken ct = default);

    /// <summary>
    /// Sends an interactive element update.
    /// </summary>
    Task<Result> UpdateInteractiveElementAsync(string sessionId, string elementId, ElementState state, CancellationToken ct = default);

    /// <summary>
    /// Gets available second screen templates.
    /// </summary>
    Task<Result<IReadOnlyList<ScreenTemplate>>> GetTemplatesAsync(ScreenType type, CancellationToken ct = default);

    /// <summary>
    /// Event raised when content is interacted with on the second screen.
    /// </summary>
    event EventHandler<ScreenInteractionEventArgs>? ScreenInteraction;

    /// <summary>
    /// Event raised when the session state changes.
    /// </summary>
    event EventHandler<SessionStateChangedEventArgs>? SessionStateChanged;
}

/// <summary>
/// Second screen configuration.
/// </summary>
public sealed record SecondScreenConfiguration(
    string TemplateId,
    ScreenType Type,
    bool EnableTouch,
    bool EnableVibration,
    int RefreshRateHz,
    ScreenOrientation Orientation,
    Dictionary<string, object>? CustomData = null);

/// <summary>
/// Second screen session.
/// </summary>
public sealed record SecondScreenSession(
    string Id,
    string DeviceId,
    SecondScreenConfiguration Configuration,
    ScreenContent CurrentContent,
    DisplayMode DisplayMode,
    SecondScreenState State,
    DateTime CreatedAt,
    DateTime? LastActivityAt = null);

/// <summary>
/// Screen content to display.
/// </summary>
public sealed record ScreenContent(
    string Title,
    string? Subtitle,
    string? ImageUrl,
    string? BackgroundUrl,
    IReadOnlyList<ContentSection>? Sections,
    IReadOnlyList<InteractiveElement>? InteractiveElements,
    ContentStyle? Style = null);

/// <summary>
/// Content section.
/// </summary>
public sealed record ContentSection(
    string Id,
    string Title,
    SectionType Type,
    string Content,
    int Order,
    bool IsVisible = true);

/// <summary>
/// Interactive element on the screen.
/// </summary>
public sealed record InteractiveElement(
    string Id,
    string Type,
    string Label,
    int X,
    int Y,
    int Width,
    int Height,
    ElementState State,
    string? IconUrl = null,
    string? Action = null);

/// <summary>
/// Element state.
/// </summary>
public sealed record ElementState(
    bool IsEnabled,
    bool IsVisible,
    string? Value = null,
    double? Progress = null,
    string? Status = null);

/// <summary>
/// Content styling options.
/// </summary>
public sealed record ContentStyle(
    string? BackgroundColor,
    string? TextColor,
    string? AccentColor,
    string? FontFamily,
    int? FontSize,
    ContentAlignment Alignment = ContentAlignment.Left);

/// <summary>
/// Screen template.
/// </summary>
public sealed record ScreenTemplate(
    string Id,
    string Name,
    ScreenType Type,
    string ThumbnailUrl,
    string Description,
    IReadOnlyList<string> SupportedElements,
    bool IsCustomizable = true);

/// <summary>
/// Second screen states.
/// </summary>
public enum SecondScreenState
{
    Initializing,
    Active,
    Paused,
    Disconnected,
    Closed
}

/// <summary>
/// Screen types.
/// </summary>
public enum ScreenType
{
    Map,
    Inventory,
    Stats,
    Chat,
    Menu,
    MiniGame,
    Companion,
    Info,
    Custom
}

/// <summary>
/// Screen orientations.
/// </summary>
public enum ScreenOrientation
{
    Portrait,
    Landscape,
    Auto
}

/// <summary>
/// Display modes.
/// </summary>
public enum DisplayMode
{
    Mirrored,
    Extended,
    Independent
}

/// <summary>
/// Content alignment.
/// </summary>
public enum ContentAlignment
{
    Left,
    Center,
    Right
}

/// <summary>
/// Section types.
/// </summary>
public enum SectionType
{
    Text,
    Image,
    Stats,
    List,
    Grid,
    Progress,
    Alert
}

/// <summary>
/// Event args for screen interaction events.
/// </summary>
public sealed class ScreenInteractionEventArgs : EventArgs
{
    public string SessionId { get; }
    public string ElementId { get; }
    public string InteractionType { get; }
    public Dictionary<string, object>? Data { get; }
    public DateTime OccurredAt { get; }

    public ScreenInteractionEventArgs(string sessionId, string elementId, string interactionType, Dictionary<string, object>? data = null, ITimeProvider? timeProvider = null)
    {
        SessionId = sessionId;
        ElementId = elementId;
        InteractionType = interactionType;
        Data = data;
        OccurredAt = (timeProvider ?? SystemTimeProvider.Instance).UtcNow;
    }
}

/// <summary>
/// Event args for session state changed events.
/// </summary>
public sealed class SessionStateChangedEventArgs : EventArgs
{
    public string SessionId { get; }
    public SecondScreenState OldState { get; }
    public SecondScreenState NewState { get; }
    public DateTime ChangedAt { get; }

    public SessionStateChangedEventArgs(string sessionId, SecondScreenState oldState, SecondScreenState newState, ITimeProvider? timeProvider = null)
    {
        SessionId = sessionId;
        OldState = oldState;
        NewState = newState;
        ChangedAt = (timeProvider ?? SystemTimeProvider.Instance).UtcNow;
    }
}
