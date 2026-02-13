namespace SaveState.Application.Mugen.Services.UiUxEnhancement;

/// <summary>
/// Sound library data.
/// </summary>
public class SoundLibrary
{
    public string Theme { get; set; } = default!;
    public IReadOnlyDictionary<string, SoundData> Sounds { get; set; } = default!;
}
