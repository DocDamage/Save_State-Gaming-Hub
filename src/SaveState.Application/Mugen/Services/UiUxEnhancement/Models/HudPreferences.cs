namespace SaveState.Application.Mugen.Services.UiUxEnhancement;

/// <summary>
/// HUD preferences data.
/// </summary>
public class HudPreferences
{
    public ScreenResolution ScreenResolution { get; set; } = default!;
    public IEnumerable<string> EnabledMechanics { get; set; } = default!;
    public string Theme { get; set; } = default!;
    public AccessibilitySettings AccessibilitySettings { get; set; } = default!;
    public bool PerformanceMode { get; set; } = default!;
}
