namespace SaveState.Application.Mugen.Services.UiUxEnhancement;

/// <summary>
/// UI user preferences data.
/// </summary>
public class UiUserPreferences
{
    public string Theme { get; set; } = default!;
    public bool HighContrast { get; set; } = default!;
    public bool ReducedMotion { get; set; } = default!;
}
