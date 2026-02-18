namespace SaveState.Application.Mugen.Services.MobileCompanion.Engines;

using Microsoft.Extensions.Logging;
using SaveState.Core.Common.Services;

/// <summary>
/// Engine for streaming live data to mobile devices.
/// </summary>
public class DataStreamingEngine
{
    private readonly ILogger<DataStreamingEngine> _logger;
    private readonly ITimeProvider _timeProvider;

    public DataStreamingEngine(ILogger<DataStreamingEngine> logger, ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Gets live statistics for a user.
    /// </summary>
    public Task<MobileCompanionServiceLiveGameStats> GetLiveStatsAsync(string userId, CancellationToken ct = default)
    {
        var now = _timeProvider.UtcNow;
        var liveStats = new LiveStats
        {
            UserId = userId,
            CurrentStreak = 5,
            WinRate = 0.65f,
            Rank = 1234,
            OnlineStatus = true,
            LastActive = now
        };

        return Task.FromResult(liveStats.ToServiceLiveGameStats(now));
    }

    /// <summary>
    /// Gets live match data.
    /// </summary>
    public Task<MobileCompanionServiceLiveMatchData> GetLiveMatchDataAsync(string matchId, CancellationToken ct = default)
    {
        return Task.FromResult(new MobileCompanionServiceLiveMatchData
        {
            MatchId = matchId,
            Timestamp = _timeProvider.UtcNow,
            PlayerData = new MobileCompanionServicePlayerMatchData
            {
                Health = 75,
                Meter = 50,
                Combo = 3,
                Position = new MobileCompanionServiceMobileVector2(100, 200),
                CurrentMove = "Hadouken"
            },
            OpponentData = new MobileCompanionServicePlayerMatchData
            {
                Health = 60,
                Meter = 75,
                Combo = 0,
                Position = new MobileCompanionServiceMobileVector2(300, 200),
                CurrentMove = "Blocking"
            },
            MatchEvents = new List<MobileCompanionServiceMatchEvent>()
        });
    }
}

/// <summary>
/// Live statistics data.
/// </summary>
public class LiveStats
{
    public string UserId { get; set; } = default!;
    public int CurrentStreak { get; set; }
    public float WinRate { get; set; }
    public int Rank { get; set; }
    public bool OnlineStatus { get; set; }
    public DateTime LastActive { get; set; }

    /// <summary>
    /// Converts to MobileCompanionServiceLiveGameStats.
    /// </summary>
    public MobileCompanionServiceLiveGameStats ToServiceLiveGameStats(DateTime timestamp)
    {
        return new MobileCompanionServiceLiveGameStats
        {
            CurrentMatch = new MobileCompanionServiceMatchStats
            {
                MatchId = $"match_{UserId}_{timestamp:yyyyMMdd}",
                PlayerHealth = 100,
                OpponentHealth = 100,
                MatchTime = TimeSpan.FromMinutes(2),
                ComboCount = CurrentStreak,
                MeterLevel = (int)(WinRate * 100)
            },
            MobileCompanionServiceSessionStats = new MobileCompanionServiceSessionStats
            {
                MatchesPlayed = CurrentStreak + 5,
                Wins = (int)((CurrentStreak + 5) * WinRate),
                Losses = (int)((CurrentStreak + 5) * (1 - WinRate)),
                WinRate = WinRate,
                AverageMatchTime = TimeSpan.FromMinutes(3),
                BestCombo = CurrentStreak,
                TotalDamageDealt = Rank
            }
        };
    }
}
