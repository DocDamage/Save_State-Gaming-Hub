namespace SaveState.Application.Mugen.Services.UiUxEnhancement;

/// <summary>
/// HUD configuration data.
/// </summary>
public class HudConfiguration
{
    public string SessionId { get; set; } = default!;
    public HudPreferences Preferences { get; set; } = default!;
    public IReadOnlyList<HudElement> Elements { get; set; } = default!;
    public HudLayout Layout { get; set; } = default!;
    public string Theme { get; set; } = default!;
    public AccessibilitySettings AccessibilitySettings { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = default!;
}
