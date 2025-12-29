namespace SaveState.Core.Mugen.ValueObjects;

/// <summary>
/// Value object containing arcade mode information for MUGEN characters.
/// </summary>
public record ArcadeInfo(
    int IntroStoryboard = 0,
    int EndingStoryboard = 0
)
{
    /// <summary>
    /// Default arcade info (no storyboards).
    /// </summary>
    public static readonly ArcadeInfo Default = new(0, 0);
}
