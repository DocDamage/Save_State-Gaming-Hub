namespace SaveState.Core.Mugen.ValueObjects;

/// <summary>
/// Value object containing palette information for MUGEN characters.
/// </summary>
public record PaletteInfo(
    int PaletteCount = 1,
    string? PaletteFile = null
)
{
    /// <summary>
    /// Default palette info (single palette, no file).
    /// </summary>
    public static PaletteInfo Default => new(1, null);
}
