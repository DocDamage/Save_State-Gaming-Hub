using System;
using System.Collections.Generic;

namespace SaveState.Core.Mugen.ValueObjects;

public sealed class PlayerSkill
{
    public PlayerSkill(string playerId, double rating, double volatility, IReadOnlyDictionary<string, double> characterRatings, DateTime lastUpdated)
    {
        PlayerId = playerId;
        Rating = rating;
        Volatility = volatility;
        CharacterRatings = characterRatings;
        LastUpdated = lastUpdated;
    }

    public string PlayerId { get; }
    public double Rating { get; }
    public double Volatility { get; }
    public IReadOnlyDictionary<string, double> CharacterRatings { get; }
    public DateTime LastUpdated { get; }
}
