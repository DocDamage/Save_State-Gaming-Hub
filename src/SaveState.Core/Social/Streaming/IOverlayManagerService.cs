using SaveState.Core.Common;

namespace SaveState.Core.Social.Streaming;

/// <summary>
/// Service for managing streaming overlays.
/// </summary>
public interface IOverlayManagerService
{
    /// <summary>
    /// Gets available overlay templates.
    /// </summary>
    Task<Result<IReadOnlyList<OverlayTemplate>>> GetTemplatesAsync(CancellationToken ct = default);

    /// <summary>
    /// Creates a new overlay configuration.
    /// </summary>
    Task<Result<OverlayConfiguration>> CreateOverlayAsync(CreateOverlayRequest request, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing overlay configuration.
    /// </summary>
    Task<Result<OverlayConfiguration>> UpdateOverlayAsync(string overlayId, UpdateOverlayRequest request, CancellationToken ct = default);

    /// <summary>
    /// Deletes an overlay configuration.
    /// </summary>
    Task<Result> DeleteOverlayAsync(string overlayId, CancellationToken ct = default);

    /// <summary>
    /// Gets an overlay configuration.
    /// </summary>
    Task<Result<OverlayConfiguration>> GetOverlayAsync(string overlayId, CancellationToken ct = default);

    /// <summary>
    /// Activates an overlay for a stream session.
    /// </summary>
    Task<Result> ActivateOverlayAsync(string overlayId, string sessionId, CancellationToken ct = default);

    /// <summary>
    /// Deactivates an overlay.
    /// </summary>
    Task<Result> DeactivateOverlayAsync(string overlayId, CancellationToken ct = default);

    /// <summary>
    /// Updates overlay data (e.g., new follower, donation).
    /// </summary>
    Task<Result> UpdateOverlayDataAsync(string overlayId, OverlayDataUpdate update, CancellationToken ct = default);

    /// <summary>
    /// Triggers an overlay animation.
    /// </summary>
    Task<Result> TriggerAnimationAsync(string overlayId, string animationName, CancellationToken ct = default);

    /// <summary>
    /// Gets the overlay HTML/URL for streaming software.
    /// </summary>
    Task<Result<OverlayUrl>> GetOverlayUrlAsync(string overlayId, CancellationToken ct = default);
}

/// <summary>
/// Overlay template information.
/// </summary>
public sealed record OverlayTemplate(
    string Id,
    string Name,
    string Description,
    string ThumbnailUrl,
    OverlayType Type,
    IReadOnlyList<OverlayWidget> DefaultWidgets);

/// <summary>
/// Overlay configuration.
/// </summary>
public sealed record OverlayConfiguration(
    string Id,
    string Name,
    string TemplateId,
    OverlayType Type,
    int Width,
    int Height,
    IReadOnlyList<OverlayWidget> Widgets,
    OverlayTheme Theme,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? LastModifiedAt = null);

/// <summary>
/// Request to create an overlay.
/// </summary>
public sealed record CreateOverlayRequest(
    string Name,
    string TemplateId,
    int Width,
    int Height,
    List<OverlayWidget>? Widgets = null,
    OverlayTheme? Theme = null);

/// <summary>
/// Request to update an overlay.
/// </summary>
public sealed record UpdateOverlayRequest(
    string? Name = null,
    List<OverlayWidget>? Widgets = null,
    OverlayTheme? Theme = null);

/// <summary>
/// Overlay widget definition.
/// </summary>
public sealed record OverlayWidget(
    string Id,
    string Type,
    string Name,
    int X,
    int Y,
    int Width,
    int Height,
    WidgetStyle Style,
    Dictionary<string, object> Properties,
    bool IsVisible = true,
    int ZIndex = 0);

/// <summary>
/// Widget styling options.
/// </summary>
public sealed record WidgetStyle(
    string? BackgroundColor,
    string? TextColor,
    string? FontFamily,
    int? FontSize,
    int? BorderRadius,
    string? BorderColor,
    int? BorderWidth,
    string? BackgroundImageUrl,
    double Opacity = 1.0);

/// <summary>
/// Overlay theme settings.
/// </summary>
public sealed record OverlayTheme(
    string Name,
    string PrimaryColor,
    string SecondaryColor,
    string AccentColor,
    string BackgroundColor,
    string TextColor,
    string FontFamily,
    string? BackgroundImageUrl = null,
    bool UseGradient = false);

/// <summary>
/// Overlay data update.
/// </summary>
public sealed record OverlayDataUpdate(
    string WidgetId,
    string DataType,
    Dictionary<string, object> Data,
    bool TriggerAnimation = false);

/// <summary>
/// Overlay URL information for streaming software.
/// </summary>
public sealed record OverlayUrl(
    string OverlayId,
    string Url,
    int Width,
    int Height,
    bool IsLocal);

/// <summary>
/// Overlay types.
/// </summary>
public enum OverlayType
{
    Standard,
    Minimal,
    Fullscreen,
    WebcamFrame,
    EventAlerts,
    Custom
}

/// <summary>
/// Widget types.
/// </summary>
public static class WidgetTypes
{
    public const string Webcam = "webcam";
    public const string Chat = "chat";
    public const string RecentEvents = "recentEvents";
    public const string Alerts = "alerts";
    public const string Goal = "goal";
    public const string Schedule = "schedule";
    public const string SocialMedia = "socialMedia";
    public const string GameInfo = "gameInfo";
    public const string SessionTimer = "sessionTimer";
    public const string NowPlaying = "nowPlaying";
    public const string CustomHtml = "customHtml";
}
