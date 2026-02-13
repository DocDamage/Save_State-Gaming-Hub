namespace SaveState.Application.Mugen.Services.UiUxEnhancement;

/// <summary>
/// Menu configuration data.
/// </summary>
public class MenuConfiguration
{
    public IEnumerable<string> EnabledFeatures { get; set; } = default!;
    public string Theme { get; set; } = default!;
    public LocalizationSettings LocalizationSettings { get; set; } = default!;
    public bool EnableAnimations { get; set; } = default!;
}
