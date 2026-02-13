namespace SaveState.Application.Mugen.Services.UiUxEnhancement;

/// <summary>
/// UI state snapshot data.
/// </summary>
public class UiStateSnapshot
{
    public string SessionId { get; set; } = default!;
    public HudConfiguration? HudConfiguration { get; set; } = default!;
    public MenuSystem? MenuSystem { get; set; } = default!;
    public VisualFeedbackSystem? FeedbackSystem { get; set; } = default!;
    public IReadOnlyList<UiNotification> PendingNotifications { get; set; } = default!;
    public UiState UiState { get; set; } = default!;
    public DateTime CapturedAt { get; set; } = default!;
}
