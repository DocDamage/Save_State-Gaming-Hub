namespace SaveState.Application.Mugen.Services.UiUxEnhancement;

/// <summary>
/// UI notification data.
/// </summary>
public class UiNotification
{
    public string SessionId { get; set; } = default!;
    public string Type { get; set; } = default!;
    public string Data { get; set; } = default!;
    public int Priority { get; set; } = default!;
    public DateTime Timestamp { get; set; } = default!;
}
