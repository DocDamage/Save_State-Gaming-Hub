namespace SaveState.Core.Mugen.ValueObjects;

/// <summary>
/// Value object containing directory paths for MUGEN character assets.
/// </summary>
public record CharacterDirectories(
    string? SpriteDirectory = null,
    string? SoundDirectory = null,
    string? PaletteDirectory = null
)
{
    /// <summary>
    /// Empty directories instance.
    /// </summary>
    public static readonly CharacterDirectories Empty = new();
}
