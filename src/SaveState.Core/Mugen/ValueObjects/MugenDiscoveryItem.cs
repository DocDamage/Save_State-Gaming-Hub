namespace SaveState.Core.Mugen.ValueObjects;

/// <summary>
/// Represents a discoverable character or stage package.
/// </summary>
public sealed record MugenDiscoveryItem(
    string Name,
    MugenDiscoveryType ContentType,
    string Source,
    string? DownloadUrl,
    string? HomepageUrl,
    string? Description);

public enum MugenDiscoveryType
{
    Character,
    Stage,
    Screenpack,
    Tool
}
