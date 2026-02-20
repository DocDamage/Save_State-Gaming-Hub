using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;

namespace SaveState.Application.Mugen.Services.PredictiveAnalytics.Managers;

/// <summary>
/// Manages player skill ratings and skill assessment operations.
/// </summary>
public sealed class PlayerSkillManager
{
    private readonly ILogger<PlayerSkillManager> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly Dictionary<string, PlayerSkill> _playerSkills = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="PlayerSkillManager"/> class.
    /// </summary>
    public PlayerSkillManager(
        ILogger<PlayerSkillManager> logger,
        ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Gets player skill data.
    /// </summary>
    public Task<Result<PlayerSkill>> GetPlayerSkillAsync(string playerId, CancellationToken ct = default)
    {
        if (!_playerSkills.TryGetValue(playerId, out var skill))
        {
            skill = new PlayerSkill
            {
                PlayerId = playerId,
                Rating = 1500,
                Volatility = 0.06,
                LastUpdated = _timeProvider.UtcNow
            };
            _playerSkills[playerId] = skill;
        }

        return Task.FromResult(Result<PlayerSkill>.Success(skill));
    }

    /// <summary>
    /// Gets current player rating.
    /// </summary>
    public async Task<double> GetCurrentRatingAsync(string playerId, CancellationToken ct)
    {
        var skill = await GetPlayerSkillAsync(playerId, ct);
        return skill.IsSuccess ? skill.Value.Rating : 1500;
    }

    /// <summary>
    /// Updates skill model based on performance analysis.
    /// </summary>
    public async Task<SkillUpdateResult> UpdateSkillModelAsync(
        string playerId,
        PerformanceAnalysis analysis,
        CancellationToken ct)
    {
        var currentSkill = await GetPlayerSkillAsync(playerId, ct);
        if (!currentSkill.IsSuccess)
        {
            throw new InvalidOperationException("Unable to retrieve player skill");
        }

        var skill = currentSkill.Value;
        var oldRating = skill.Rating;

        var expectedPerformance = 1.0 / (1.0 + Math.Pow(10, (1500 - skill.Rating) / 400.0));
        var actualPerformance = analysis.WinRate;

        var ratingChange = 32 * (actualPerformance - expectedPerformance);
        skill.Rating += ratingChange;
        skill.LastUpdated = _timeProvider.UtcNow;

        skill.Volatility = Math.Max(0.03, skill.Volatility * (1.0 - analysis.Consistency * 0.1));

        return new SkillUpdateResult
        {
            Rating = skill.Rating,
            RatingChange = ratingChange,
            Volatility = skill.Volatility,
            Confidence = Math.Min(0.95, analysis.Consistency + 0.5)
        };
    }

    /// <summary>
    /// Updates models with training data.
    /// </summary>
    public Task UpdateModelsWithTrainingDataAsync(IReadOnlyList<TrainingData> trainingData, CancellationToken ct)
    {
        foreach (var data in trainingData)
        {
            if (!string.IsNullOrEmpty(data.PlayerId) && !_playerSkills.ContainsKey(data.PlayerId))
            {
                _playerSkills[data.PlayerId] = new PlayerSkill
                {
                    PlayerId = data.PlayerId,
                    Rating = 1500,
                    Volatility = 0.06,
                    LastUpdated = _timeProvider.UtcNow
                };
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Determines skill tier from rating.
    /// </summary>
    public SkillTier DetermineSkillTier(double rating)
    {
        return rating switch
        {
            >= 2500 => SkillTier.Grandmaster,
            >= 2200 => SkillTier.Master,
            >= 2000 => SkillTier.Diamond,
            >= 1800 => SkillTier.Platinum,
            >= 1600 => SkillTier.Gold,
            >= 1400 => SkillTier.Silver,
            >= 1200 => SkillTier.Bronze,
            _ => SkillTier.Unranked
        };
    }

    /// <summary>
    /// Projects future rating based on trend.
    /// </summary>
    public async Task<double> ProjectFutureRatingAsync(
        string playerId,
        PerformanceAnalysis analysis,
        CancellationToken ct)
    {
        var currentRating = await GetCurrentRatingAsync(playerId, ct);
        var trendMultiplier = analysis.Trend switch
        {
            SkillTrend.Improving => 1.02,
            SkillTrend.Declining => 0.98,
            _ => 1.0
        };

        return currentRating * trendMultiplier;
    }

    /// <summary>
    /// Analyzes match performance for a player.
    /// </summary>
    public PerformanceAnalysis AnalyzeMatchPerformance(
        IReadOnlyList<PredictiveMatchResult> matches,
        string playerId)
    {
        var playerMatches = matches.Where(m => m.Player1Id == playerId || m.Player2Id == playerId).ToList();

        var wins = playerMatches.Count(m =>
            (m.Player1Id == playerId && m.Result == MatchResult.Player1Win) ||
            (m.Player2Id == playerId && m.Result == MatchResult.Player2Win));

        var winRate = playerMatches.Any() ? (double)wins / playerMatches.Count : 0.5;

        var recentMatches = playerMatches.TakeLast(10).ToList();
        var recentWinRate = recentMatches.Any() ?
            recentMatches.Count(m =>
                (m.Player1Id == playerId && m.Result == MatchResult.Player1Win) ||
                (m.Player2Id == playerId && m.Result == MatchResult.Player2Win)) / (double)recentMatches.Count : 0.5;

        var trend = recentWinRate > winRate ? SkillTrend.Improving :
                   recentWinRate < winRate ? SkillTrend.Declining : SkillTrend.Stable;

        return new PerformanceAnalysis
        {
            TotalMatches = playerMatches.Count,
            WinRate = winRate,
            AverageMatchDuration = TimeSpan.FromMinutes(3.5),
            Strengths = IdentifyStrengths(playerMatches, playerId),
            Weaknesses = IdentifyWeaknesses(playerMatches, playerId),
            Trend = trend,
            Consistency = CalculateConsistency(playerMatches, playerId)
        };
    }

    private IReadOnlyList<string> IdentifyStrengths(IReadOnlyList<PredictiveMatchResult> matches, string playerId)
    {
        var strengths = new List<string>();

        var fastMatches = matches.Where(m => m.MatchDuration < TimeSpan.FromMinutes(2)).ToList();
        if (fastMatches.Count > matches.Count * 0.6)
        {
            strengths.Add("Fast and decisive playstyle");
        }

        var comebackMatches = matches.Where(m => m.Comeback == true).ToList();
        if (comebackMatches.Count > matches.Count * 0.3)
        {
            strengths.Add("Strong comeback ability");
        }

        if (!strengths.Any())
        {
            strengths.Add("Consistent performance");
        }

        return strengths;
    }

    private IReadOnlyList<string> IdentifyWeaknesses(IReadOnlyList<PredictiveMatchResult> matches, string playerId)
    {
        var weaknesses = new List<string>();

        var longMatches = matches.Where(m => m.MatchDuration > TimeSpan.FromMinutes(5)).ToList();
        if (longMatches.Count > matches.Count * 0.4)
        {
            weaknesses.Add("Struggles with prolonged matches");
        }

        var comebackAttempts = matches.Where(m => m.ComebackAttempted == true && m.Comeback == false).ToList();
        if (comebackAttempts.Count > matches.Count * 0.2)
        {
            weaknesses.Add("Difficulty executing comebacks");
        }

        if (!weaknesses.Any())
        {
            weaknesses.Add("Areas for improvement identified");
        }

        return weaknesses;
    }

    private double CalculateConsistency(IReadOnlyList<PredictiveMatchResult> matches, string playerId)
    {
        var winRates = new List<double>();
        for (int i = 0; i < matches.Count; i += 5)
        {
            var batch = matches.Skip(i).Take(5).ToList();
            if (batch.Count >= 3)
            {
                var batchWins = batch.Count(m =>
                    (m.Player1Id == playerId && m.Result == MatchResult.Player1Win) ||
                    (m.Player2Id == playerId && m.Result == MatchResult.Player2Win));
                winRates.Add(batchWins / (double)batch.Count);
            }
        }

        if (!winRates.Any()) return 0.5;

        var average = winRates.Average();
        var variance = winRates.Sum(rate => Math.Pow(rate - average, 2)) / winRates.Count;

        return Math.Max(0, 1.0 - variance * 4);
    }
}

/// <summary>
/// Player skill data.
/// </summary>
public class PlayerSkill
{
    public string PlayerId { get; set; } = default!;
    public double Rating { get; set; }
    public double Volatility { get; set; }
    public DateTime LastUpdated { get; set; }
}

/// <summary>
/// Skill update result.
/// </summary>
public class SkillUpdateResult
{
    public double Rating { get; set; }
    public double RatingChange { get; set; }
    public double Volatility { get; set; }
    public double Confidence { get; set; }
}

/// <summary>
/// Performance analysis data.
/// </summary>
public class PerformanceAnalysis
{
    public int TotalMatches { get; set; }
    public double WinRate { get; set; }
    public TimeSpan AverageMatchDuration { get; set; }
    public IReadOnlyList<string> Strengths { get; set; } = default!;
    public IReadOnlyList<string> Weaknesses { get; set; } = default!;
    public SkillTrend Trend { get; set; }
    public double Consistency { get; set; }
}

/// <summary>
/// Skill tier enumeration.
/// </summary>
public enum SkillTier
{
    Unranked,
    Bronze,
    Silver,
    Gold,
    Platinum,
    Diamond,
    Master,
    Grandmaster
}

/// <summary>
/// Skill trend enumeration.
/// </summary>
public enum SkillTrend
{
    Improving,
    Stable,
    Declining
}

/// <summary>
/// Training data for models.
/// </summary>
public class TrainingData
{
    public string PlayerId { get; set; } = default!;
    public double[] Features { get; set; } = default!;
    public double Label { get; set; }
}

/// <summary>
/// Match result data.
/// </summary>
public class PredictiveMatchResult
{
    public string Player1Id { get; set; } = default!;
    public string Player2Id { get; set; } = default!;
    public MatchResult Result { get; set; }
    public TimeSpan MatchDuration { get; set; }
    public bool Comeback { get; set; }
    public bool ComebackAttempted { get; set; }
}

/// <summary>
/// Match result enumeration.
/// </summary>
public enum MatchResult
{
    Player1Win,
    Player2Win,
    Draw
}
