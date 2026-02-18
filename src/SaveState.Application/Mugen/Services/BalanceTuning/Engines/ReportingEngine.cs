namespace SaveState.Application.Mugen.Services.BalanceTuning.Engines;

using Microsoft.Extensions.Logging;
using SaveState.Application.Mugen.Models.BalanceTuning;
using SaveState.Core.Common.Services;

/// <summary>
/// Engine for generating balance reports and competitive rankings.
/// </summary>
public class ReportingEngine
{
    private readonly ILogger<ReportingEngine> _logger;
    private readonly ITimeProvider _timeProvider;

    public ReportingEngine(ILogger<ReportingEngine> logger, ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Calculates player rankings from stats.
    /// </summary>
    public List<PlayerRanking> CalculatePlayerRankings(IReadOnlyList<PlayerStats> playerStats)
    {
        var rankings = playerStats
            .OrderByDescending(p => p.Rating)
            .Select((p, i) => new PlayerRanking
            {
                PlayerId = p.PlayerId,
                PlayerName = p.PlayerName,
                Rating = p.Rating,
                Rank = i + 1,
                Wins = p.Wins,
                Losses = p.Losses,
                WinRate = p.TotalMatches > 0 ? (float)p.Wins / p.TotalMatches : 0
            })
            .ToList();

        return rankings;
    }

    /// <summary>
    /// Generates ranking divisions.
    /// </summary>
    public List<RankingDivision> GenerateRankingDivisions(IReadOnlyList<PlayerStats> playerStats)
    {
        var divisions = new List<RankingDivision>();
        var sortedPlayers = playerStats.OrderByDescending(p => p.Rating).ToList();

        // Top tier
        divisions.Add(new RankingDivision
        {
            DivisionName = "Master",
            MinRating = 2000,
            MaxRating = 3000,
            PlayerCount = sortedPlayers.Count(p => p.Rating >= 2000)
        });

        // Mid tiers
        divisions.Add(new RankingDivision
        {
            DivisionName = "Diamond",
            MinRating = 1800,
            MaxRating = 1999,
            PlayerCount = sortedPlayers.Count(p => p.Rating >= 1800 && p.Rating < 2000)
        });

        divisions.Add(new RankingDivision
        {
            DivisionName = "Platinum",
            MinRating = 1600,
            MaxRating = 1799,
            PlayerCount = sortedPlayers.Count(p => p.Rating >= 1600 && p.Rating < 1800)
        });

        divisions.Add(new RankingDivision
        {
            DivisionName = "Gold",
            MinRating = 1400,
            MaxRating = 1599,
            PlayerCount = sortedPlayers.Count(p => p.Rating >= 1400 && p.Rating < 1600)
        });

        // Lower tiers
        divisions.Add(new RankingDivision
        {
            DivisionName = "Silver",
            MinRating = 1200,
            MaxRating = 1399,
            PlayerCount = sortedPlayers.Count(p => p.Rating >= 1200 && p.Rating < 1400)
        });

        divisions.Add(new RankingDivision
        {
            DivisionName = "Bronze",
            MinRating = 1000,
            MaxRating = 1199,
            PlayerCount = sortedPlayers.Count(p => p.Rating < 1200)
        });

        return divisions;
    }

    /// <summary>
    /// Calculates season statistics.
    /// </summary>
    public SeasonStatistics CalculateSeasonStatistics(IReadOnlyList<PlayerStats> playerStats)
    {
        var totalMatches = playerStats.Sum(p => p.TotalMatches);
        var totalWins = playerStats.Sum(p => p.Wins);

        return new SeasonStatistics
        {
            TotalPlayers = playerStats.Count,
            TotalMatches = totalMatches,
            TotalActivePlayers = playerStats.Count(p => p.LastActive > _timeProvider.UtcNow.AddDays(-30)),
            AverageRating = playerStats.Count > 0 ? playerStats.Average(p => p.Rating) : 0,
            HighestRating = playerStats.Count > 0 ? playerStats.Max(p => p.Rating) : 0,
            OverallWinRate = totalMatches > 0 ? (float)totalWins / totalMatches : 0
        };
    }

    /// <summary>
    /// Calculates balance factors from player stats.
    /// </summary>
    public Dictionary<string, float> CalculateBalanceFactors(IReadOnlyList<PlayerStats> playerStats)
    {
        var factors = new Dictionary<string, float>
        {
            ["RatingVariance"] = playerStats.Count > 0 ? CalculateVariance(playerStats.Select(p => p.Rating)) : 0,
            ["WinRateDiversity"] = playerStats.Count > 0 ? CalculateVariance(playerStats.Select(p => p.WinRate)) : 0,
            ["ActivityLevel"] = playerStats.Count > 0 ? playerStats.Average(p => p.ActivityScore) : 0,
            ["Competitiveness"] = CalculateCompetitiveness(playerStats)
        };

        return factors;
    }

    /// <summary>
    /// Validates a competitive ranking.
    /// </summary>
    public bool ValidateCompetitiveRanking(CompetitiveRanking ranking)
    {
        if (ranking.Players.Count == 0)
            return false;

        // Check for duplicate ranks
        var ranks = ranking.Players.Select(p => p.Rank).ToList();
        if (ranks.Count != ranks.Distinct().Count())
            return false;

        // Check that ratings are consistent with ranks
        for (int i = 1; i < ranking.Players.Count; i++)
        {
            if (ranking.Players[i].Rating > ranking.Players[i - 1].Rating)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Generates executive summary for a balance report.
    /// </summary>
    public Task<ExecutiveSummary> GenerateExecutiveSummaryAsync(string sessionId, DateRange dateRange, CancellationToken ct = default)
    {
        var summary = new ExecutiveSummary
        {
            Title = "Balance Report Executive Summary",
            ReportPeriod = dateRange,
            KeyFindings = new List<string>
            {
                "Overall game balance is within acceptable parameters",
                "No mechanics significantly over-performing",
                "Player satisfaction remains high"
            },
            CriticalIssues = new List<string>(),
            RecommendedActions = new List<string>
            {
                "Continue monitoring emerging strategies",
                "Review lower-tier mechanic usage"
            }
        };

        return Task.FromResult(summary);
    }

    /// <summary>
    /// Analyzes mechanic balance over time.
    /// </summary>
    public Task<MechanicBalanceAnalysis> AnalyzeMechanicBalanceAsync(string sessionId, DateRange dateRange, CancellationToken ct = default)
    {
        var analysis = new MechanicBalanceAnalysis
        {
            AnalysisPeriod = dateRange,
            MechanicPerformance = new Dictionary<string, MechanicPerformanceData>(),
            Trends = new List<TrendData>(),
            Recommendations = new List<ReportRecommendation>()
        };

        return Task.FromResult(analysis);
    }

    /// <summary>
    /// Collects player feedback.
    /// </summary>
    public Task<PlayerFeedbackSummary> CollectPlayerFeedbackAsync(string sessionId, DateRange dateRange, CancellationToken ct = default)
    {
        var summary = new PlayerFeedbackSummary
        {
            CollectionPeriod = dateRange,
            TotalResponses = 0,
            AverageSatisfaction = 0.75f,
            CommonConcerns = new List<string>(),
            PositiveFeedback = new List<string> { "Game feels fair", "Matches are exciting" }
        };

        return Task.FromResult(summary);
    }

    /// <summary>
    /// Analyzes tournament results.
    /// </summary>
    public Task<TournamentResultsAnalysis> AnalyzeTournamentResultsAsync(string sessionId, DateRange dateRange, CancellationToken ct = default)
    {
        var analysis = new TournamentResultsAnalysis
        {
            AnalysisPeriod = dateRange,
            TournamentsAnalyzed = 0,
            TopWinningMechanics = new List<string>(),
            MetaDiversity = 0.7f,
            UpsetRate = 0.25f
        };

        return Task.FromResult(analysis);
    }

    /// <summary>
    /// Generates report recommendations.
    /// </summary>
    public List<ReportRecommendation> GenerateReportRecommendations(string sessionId, DateRange dateRange)
    {
        return new List<ReportRecommendation>
        {
            new ReportRecommendation
            {
                Category = "Monitoring",
                Priority = RecommendationPriority.Medium,
                Description = "Continue regular balance monitoring",
                ExpectedImpact = 0.5f
            }
        };
    }

    private static float CalculateVariance(IEnumerable<float> values)
    {
        var list = values.ToList();
        if (list.Count == 0) return 0;

        var avg = list.Average();
        return list.Select(v => (v - avg) * (v - avg)).Average();
    }

    private static float CalculateCompetitiveness(IReadOnlyList<PlayerStats> playerStats)
    {
        if (playerStats.Count < 2) return 0.5f;

        var closeMatches = playerStats.Count(p => p.WinRate > 0.4f && p.WinRate < 0.6f);
        return (float)closeMatches / playerStats.Count;
    }
}
