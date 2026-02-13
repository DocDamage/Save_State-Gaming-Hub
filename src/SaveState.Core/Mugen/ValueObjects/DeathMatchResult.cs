namespace SaveState.Core.Mugen.ValueObjects;

/// <summary>
/// Represents the result of running actual MUGEN engine matches.
/// </summary>
public sealed record DeathMatchResult(
    Guid Character1Id,
    string Character1Name,
    Guid Character2Id,
    string Character2Name,
    int TotalMatches,
    int Character1Wins,
    int Character2Wins,
    int Draws,
    TimeSpan TotalDuration,
    IReadOnlyList<string> ReplayPaths)
{
    public double Character1WinRate => TotalMatches == 0 ? 0 : (double)Character1Wins / TotalMatches;
    public double Character2WinRate => TotalMatches == 0 ? 0 : (double)Character2Wins / TotalMatches;
}
