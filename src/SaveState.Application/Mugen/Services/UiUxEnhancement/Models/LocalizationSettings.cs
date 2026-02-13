namespace SaveState.Application.Mugen.Services.UiUxEnhancement;

/// <summary>
/// Localization settings data.
/// </summary>
public class LocalizationSettings
{
    public string Language { get; set; } = default!;
    public bool UseSystemLanguage { get; set; } = default!;
    public IReadOnlyDictionary<string, string> CustomTranslations { get; set; } = default!;
}
