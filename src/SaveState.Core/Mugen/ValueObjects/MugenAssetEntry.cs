namespace SaveState.Core.Mugen.ValueObjects;

/// <summary>
/// Represents a MUGEN asset file (sprite, animation, sound, etc.).
/// </summary>
public sealed record MugenAssetEntry(
    string FileName,
    string FullPath,
    long SizeBytes,
    MugenAssetType AssetType);

public enum MugenAssetType
{
    Sprite,
    Animation,
    Sound,
    Unknown
}
