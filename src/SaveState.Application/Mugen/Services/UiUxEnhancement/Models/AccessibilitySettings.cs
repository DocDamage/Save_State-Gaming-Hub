namespace SaveState.Application.Mugen.Services.UiUxEnhancement;

/// <summary>
/// Accessibility settings data.
/// </summary>
public class AccessibilitySettings
{
    public bool HighContrast { get; set; } = default!;
    public bool LargeText { get; set; } = default!;
    public bool ColorBlindSupport { get; set; } = default!;
    public bool ReducedMotion { get; set; } = default!;
}
