namespace SaveState.Core.Mugen.ValueObjects;

/// <summary>
/// Represents computed ELO rating and performance for a character.
/// </summary>
public sealed record MugenEloRating(
    Guid CharacterId,
    string CharacterName,
    double Rating,
    int Wins,
    int Losses)
{
    public int Matches => Wins + Losses;
    public double WinRate => Matches == 0 ? 0 : (double)Wins / Matches;
}
