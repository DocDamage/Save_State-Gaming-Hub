using System;
using System.Collections.Generic;

namespace SaveState.Core.Mugen.ValueObjects;

public sealed class PlayerSkill
{
    public PlayerSkill(string playerId, int rating, double trend, IReadOnlyDictionary<string, double> preferences, DateTime lastUpdated)
    {
        PlayerId = playerId;
        Rating = rating;
        Trend = trend;
        Preferences = preferences;
        LastUpdated = lastUpdated;
    }

    public string PlayerId { get; }
    public int Rating { get; }
    public double Trend { get; }
    public IReadOnlyDictionary<string, double> Preferences { get; }
    public DateTime LastUpdated { get; }
}
