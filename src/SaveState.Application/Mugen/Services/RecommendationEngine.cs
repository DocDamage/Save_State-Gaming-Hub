using Microsoft.Extensions.Logging;
using SaveState.Core.Mugen.Services;
using SaveState.Application.Mugen.Models.Educational;
using SaveState.Core.Common;

namespace SaveState.Application.Mugen.Services;

/// <summary>
/// AI-powered recommendation engine that analyzes player data and provides
/// personalized improvement suggestions based on performance patterns.
/// </summary>
public class RecommendationEngine
{
    private readonly ILogger<RecommendationEngine> _logger;
    private readonly ICacheService _cacheService;

    public RecommendationEngine(ILogger<RecommendationEngine> logger, ICacheService cacheService)
    {
        _logger = logger;
        _cacheService = cacheService;
    }

    public Task<IReadOnlyList<RecommendedContent>> GetRecommendationsAsync(string userId, CancellationToken ct = default)
    {
        _logger.LogInformation("Generating recommendations for user {UserId}", userId);

        var recommendations = new List<RecommendedContent>
        {
            new RecommendedContent
            {
                ContentId = "tutorial-basic-controls",
                Title = "Basic Controls Tutorial",
                Category = "Basics",
                RecommendationReason = "Start with the fundamentals"
            },
            new RecommendedContent
            {
                ContentId = "tutorial-combo-basics",
                Title = "Combo Basics",
                Category = "Combos",
                RecommendationReason = "Build on your current skills"
            },
            new RecommendedContent
            {
                ContentId = "strategy-guide-offense",
                Title = "Offensive Strategies",
                Category = "Strategy",
                RecommendationReason = "Expand your gameplay knowledge"
            }
        };

        return Task.FromResult<IReadOnlyList<RecommendedContent>>(recommendations);
    }

    public Task<IReadOnlyList<RecommendedContent>> GetRecommendationsAsync(
        string playerId,
        DifficultyLevel currentLevel,
        DifficultyLevel targetLevel)
    {
        _logger.LogInformation(
            "Generating recommendations for player {PlayerId} from {CurrentLevel} to {TargetLevel}",
            playerId, currentLevel, targetLevel);

        var recommendations = new List<RecommendedContent>
        {
            new RecommendedContent
            {
                ContentId = $"tutorial-{currentLevel}-to-{targetLevel}",
                Title = $"Progressing from {currentLevel} to {targetLevel}",
                Category = "Progression",
                RecommendationReason = $"Tailored path to advance from {currentLevel} to {targetLevel}"
            }
        };

        return Task.FromResult<IReadOnlyList<RecommendedContent>>(recommendations);
    }

    public UserEngagement GetEngagementMetrics(string contentId)
    {
        _logger.LogInformation("Getting engagement metrics for content {ContentId}", contentId);

        return new UserEngagement
        {
            AverageSessionLength = TimeSpan.FromMinutes(25),
            TotalSessions = 1250,
            UniqueUsers = 380,
            ReturnRate = 0.65
        };
    }

    public ContentQuality GetContentQualityMetrics(string contentId)
    {
        _logger.LogInformation("Getting quality metrics for content {ContentId}", contentId);

        return new ContentQuality
        {
            AverageRating = 4.2,
            TotalRatings = 450,
            HighlyRatedContent = 85,
            ContentFreshness = 0.75
        };
    }

    // Legacy overloads for backward compatibility
    public UserEngagement GetEngagementMetrics(TimeSpan period)
    {
        return new UserEngagement
        {
            AverageSessionLength = TimeSpan.FromMinutes(25),
            TotalSessions = 1250,
            UniqueUsers = 380,
            ReturnRate = 0.65
        };
    }

    public ContentQuality GetContentQualityMetrics(TimeSpan period)
    {
        return new ContentQuality
        {
            AverageRating = 4.2,
            TotalRatings = 450,
            HighlyRatedContent = 85,
            ContentFreshness = 0.75
        };
    }

    public async Task<IReadOnlyList<ImprovementRecommendation>> GenerateRecommendationsAsync(
        PlayerStatistics statistics,
        IReadOnlyList<PlayerPattern> patterns,
        PerformanceTrends trends,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Generating recommendations for player {PlayerId}", statistics.PlayerId);

            var recommendations = new List<ImprovementRecommendation>();

            // Analyze based on win rate
            recommendations.AddRange(await AnalyzeWinRateAsync(statistics, ct));

            // Analyze based on patterns
            recommendations.AddRange(await AnalyzePatternsAsync(patterns, ct));

            // Analyze based on trends
            recommendations.AddRange(await AnalyzeTrendsAsync(trends, ct));

            // Analyze character-specific performance
            recommendations.AddRange(await AnalyzeCharacterPerformanceAsync(statistics, ct));

            // Analyze damage and combo efficiency
            recommendations.AddRange(await AnalyzeCombatEfficiencyAsync(statistics, ct));

            // Sort by priority and remove duplicates
            recommendations = recommendations
                .OrderBy(r => r.Priority)
                .GroupBy(r => r.Recommendation)
                .Select(g => g.First())
                .ToList();

            _logger.LogInformation("Generated {Count} recommendations for player {PlayerId}",
                recommendations.Count, statistics.PlayerId);

            return recommendations;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating recommendations for player {PlayerId}", statistics.PlayerId);
            return Array.Empty<ImprovementRecommendation>();
        }
    }

    private async Task<IReadOnlyList<ImprovementRecommendation>> AnalyzeWinRateAsync(
        PlayerStatistics statistics,
        CancellationToken ct)
    {
        var recommendations = new List<ImprovementRecommendation>();

        if (statistics.TotalMatches < 5)
        {
            recommendations.Add(new ImprovementRecommendation(
                Category: "Getting Started",
                Recommendation: "Play more matches to establish performance baseline",
                Rationale: "Insufficient match data for accurate analysis",
                Priority: RecommendationPriority.High,
                ActionSteps: new[] {
                    "Complete at least 10 ranked matches",
                    "Try different characters to find your preferences",
                    "Focus on learning basic mechanics"
                }
            ));
            return recommendations;
        }

        if (statistics.WinRate < 0.3m)
        {
            recommendations.Add(new ImprovementRecommendation(
                Category: "Fundamentals",
                Recommendation: "Focus on basic defensive and offensive fundamentals",
                Rationale: $"Current win rate of {statistics.WinRate:P1} indicates need for basic skill improvement",
                Priority: RecommendationPriority.Critical,
                ActionSteps: new[] {
                    "Practice blocking and basic combos",
                    "Learn character-specific defensive options",
                    "Study frame data and move properties",
                    "Watch tutorial videos for your main characters"
                }
            ));
        }
        else if (statistics.WinRate < 0.5m)
        {
            recommendations.Add(new ImprovementRecommendation(
                Category: "Strategy",
                Recommendation: "Work on match strategy and decision making",
                Rationale: $"Win rate of {statistics.WinRate:P1} suggests strategic improvements needed",
                Priority: RecommendationPriority.High,
                ActionSteps: new[] {
                    "Learn neutral game and footsies",
                    "Practice spacing and positioning",
                    "Study opponent tendencies",
                    "Focus on conditioning and pressure"
                }
            ));
        }
        else if (statistics.WinRate >= 0.7m)
        {
            recommendations.Add(new ImprovementRecommendation(
                Category: "Optimization",
                Recommendation: "Focus on perfecting execution and consistency",
                Rationale: $"Strong win rate of {statistics.WinRate:P1} - focus on refinement",
                Priority: RecommendationPriority.Medium,
                ActionSteps: new[] {
                    "Minimize input errors and mistakes",
                    "Optimize character matchups",
                    "Improve conditioning and pressure",
                    "Study advanced techniques and setups"
                }
            ));
        }

        return recommendations;
    }

    private async Task<IReadOnlyList<ImprovementRecommendation>> AnalyzePatternsAsync(
        IReadOnlyList<PlayerPattern> patterns,
        CancellationToken ct)
    {
        var recommendations = new List<ImprovementRecommendation>();

        foreach (var pattern in patterns)
        {
            switch (pattern.PatternType)
            {
                case "Combo-Heavy Playstyle":
                    if (pattern.Frequency > 0.8m)
                    {
                        recommendations.Add(new ImprovementRecommendation(
                            Category: "Balance",
                            Recommendation: "Diversify gameplay beyond combo focus",
                            Rationale: "Over-reliance on combos makes you predictable",
                            Priority: RecommendationPriority.Medium,
                            ActionSteps: new[] {
                                "Practice spacing and neutral game",
                                "Learn anti-air and defensive options",
                                "Incorporate special moves more strategically",
                                "Work on mix-up patterns"
                            }
                        ));
                    }
                    break;

                case "Special Move Spammer":
                    if (pattern.Frequency > 0.7m)
                    {
                        recommendations.Add(new ImprovementRecommendation(
                            Category: "Strategy",
                            Recommendation: "Balance special move usage with fundamentals",
                            Rationale: "Heavy special usage reduces combo potential and creates openings",
                            Priority: RecommendationPriority.High,
                            ActionSteps: new[] {
                                "Practice linking normals into specials",
                                "Learn special move recovery and gaps",
                                "Focus on combo extensions after specials",
                                "Work on meter management"
                            }
                        ));
                    }
                    break;

                case "Defensive Player":
                    if (pattern.Frequency > 0.8m)
                    {
                        recommendations.Add(new ImprovementRecommendation(
                            Category: "Offense",
                            Recommendation: "Develop offensive pressure and conditioning",
                            Rationale: "Strong defense needs offensive tools to capitalize",
                            Priority: RecommendationPriority.Medium,
                            ActionSteps: new[] {
                                "Practice pressure and frame traps",
                                "Learn tick throws and mix-ups",
                                "Work on offensive resets",
                                "Build meter for super moves"
                            }
                        ));
                    }
                    break;

                case "Rushdown Style":
                    if (pattern.Frequency > 0.7m)
                    {
                        recommendations.Add(new ImprovementRecommendation(
                            Category: "Defense",
                            Recommendation: "Improve defensive recovery and anti-pressure tools",
                            Rationale: "Aggressive style needs strong defense to avoid whiffing",
                            Priority: RecommendationPriority.High,
                            ActionSteps: new[] {
                                "Practice defensive movement and rolls",
                                "Learn reversal options and anti-airs",
                                "Work on spacing and distance control",
                                "Study opponent pressure patterns"
                            }
                        ));
                    }
                    break;

                case "Input-Heavy Playstyle":
                    if (pattern.Frequency > 0.6m)
                    {
                        recommendations.Add(new ImprovementRecommendation(
                            Category: "Execution",
                            Recommendation: "Focus on input accuracy and consistency",
                            Rationale: "Complex inputs require precision to be effective",
                            Priority: RecommendationPriority.Medium,
                            ActionSteps: new[] {
                                "Practice input buffering and linking",
                                "Work on motion-input consistency",
                                "Use training mode for execution drills",
                                "Focus on one technique at a time"
                            }
                        ));
                    }
                    break;
            }
        }

        return recommendations;
    }

    private async Task<IReadOnlyList<ImprovementRecommendation>> AnalyzeTrendsAsync(
        PerformanceTrends trends,
        CancellationToken ct)
    {
        var recommendations = new List<ImprovementRecommendation>();

        // Analyze win rate trend
        if (trends.WinRateTrend.Count >= 2)
        {
            var recentWinRate = trends.WinRateTrend.Last().Value;
            var earlierWinRate = trends.WinRateTrend.First().Value;
            var winRateChange = recentWinRate - earlierWinRate;

            if (winRateChange < -0.1m) // Declining win rate
            {
                recommendations.Add(new ImprovementRecommendation(
                    Category: "Adaptation",
                    Recommendation: "Address recent performance decline",
                    Rationale: $"Win rate decreased by {Math.Abs(winRateChange):P1} over time",
                    Priority: RecommendationPriority.High,
                    ActionSteps: new[] {
                        "Review recent matches for mistakes",
                        "Adapt to meta changes or opponent strategies",
                        "Return to fundamentals if needed",
                        "Consider character or playstyle changes"
                    }
                ));
            }
            else if (winRateChange > 0.1m) // Improving win rate
            {
                recommendations.Add(new ImprovementRecommendation(
                    Category: "Progression",
                    Recommendation: "Continue current improvement trajectory",
                    Rationale: $"Win rate improved by {winRateChange:P1} - maintain momentum",
                    Priority: RecommendationPriority.Low,
                    ActionSteps: new[] {
                        "Build on recent successes",
                        "Challenge yourself with tougher opponents",
                        "Expand character knowledge",
                        "Practice advanced techniques"
                    }
                ));
            }
        }

        // Analyze damage trend
        if (trends.DamageTrend.Count >= 2)
        {
            var recentDamage = trends.DamageTrend.Last().Value;
            var earlierDamage = trends.DamageTrend.First().Value;
            var damageChange = recentDamage - earlierDamage;

            if (damageChange < -30) // Declining damage
            {
                recommendations.Add(new ImprovementRecommendation(
                    Category: "Damage",
                    Recommendation: "Improve damage output and combo efficiency",
                    Rationale: $"Damage output decreased by {Math.Abs(damageChange):F0} points",
                    Priority: RecommendationPriority.Medium,
                    ActionSteps: new[] {
                        "Review combo routes and optimization",
                        "Practice combo extensions",
                        "Focus on meter building",
                        "Work on corner carry and positioning"
                    }
                ));
            }
        }

        return recommendations;
    }

    private async Task<IReadOnlyList<ImprovementRecommendation>> AnalyzeCharacterPerformanceAsync(
        PlayerStatistics statistics,
        CancellationToken ct)
    {
        var recommendations = new List<ImprovementRecommendation>();

        if (!statistics.CharacterStats.Any())
            return recommendations;

        // Find best and worst performing characters
        var bestCharacter = statistics.CharacterStats.MaxBy(c => c.Value.WinRate);
        var worstCharacter = statistics.CharacterStats.MinBy(c => c.Value.WinRate);

        if (bestCharacter.Value.WinRate - worstCharacter.Value.WinRate > 0.2m)
        {
            recommendations.Add(new ImprovementRecommendation(
                Category: "Character Selection",
                Recommendation: $"Focus on {bestCharacter.Key} and improve {worstCharacter.Key}",
                Rationale: $"Large performance gap between characters ({bestCharacter.Value.WinRate:P1} vs {worstCharacter.Value.WinRate:P1})",
                Priority: RecommendationPriority.Medium,
                ActionSteps: new[] {
                    $"Master {bestCharacter.Key} as your main",
                    $"Practice fundamentals with {worstCharacter.Key}",
                    $"Learn matchup-specific strategies",
                    "Consider character switching based on opponent"
                }
            ));
        }

        // Analyze character diversity
        if (statistics.CharacterStats.Count < 3)
        {
            recommendations.Add(new ImprovementRecommendation(
                Category: "Character Pool",
                Recommendation: "Expand character knowledge and versatility",
                Rationale: $"Only {statistics.CharacterStats.Count} characters used - limited adaptability",
                Priority: RecommendationPriority.Medium,
                ActionSteps: new[] {
                    "Learn 2-3 new characters thoroughly",
                    "Practice character matchups",
                    "Understand different playstyles",
                    "Build secondary characters for variety"
                }
            ));
        }

        return recommendations;
    }

    private async Task<IReadOnlyList<ImprovementRecommendation>> AnalyzeCombatEfficiencyAsync(
        PlayerStatistics statistics,
        CancellationToken ct)
    {
        var recommendations = new List<ImprovementRecommendation>();

        // Analyze overall damage efficiency
        var totalDamageDealt = statistics.CharacterStats.Sum(c => c.Value.TotalDamageDealt);
        var avgDamagePerMatch = statistics.TotalMatches > 0 ? (decimal)totalDamageDealt / statistics.TotalMatches : 0;

        if (avgDamagePerMatch < 200)
        {
            recommendations.Add(new ImprovementRecommendation(
                Category: "Damage Optimization",
                Recommendation: "Improve damage conversion and combo efficiency",
                Rationale: $"Average damage per match ({avgDamagePerMatch:F0}) is below optimal",
                Priority: RecommendationPriority.Medium,
                ActionSteps: new[] {
                    "Study optimal combo routes",
                    "Practice corner carry and positioning",
                    "Learn to extend combos with special moves",
                    "Focus on high-damage starters"
                }
            ));
        }

        // Analyze combo length trends
        var avgComboLength = statistics.CharacterStats.Average(c => c.Value.AverageComboLength);
        if (avgComboLength < 3)
        {
            recommendations.Add(new ImprovementRecommendation(
                Category: "Combo Development",
                Recommendation: "Work on combo extensions and linking",
                Rationale: $"Average combo length ({avgComboLength:F1} hits) needs improvement",
                Priority: RecommendationPriority.Medium,
                ActionSteps: new[] {
                    "Practice basic combo routes",
                    "Learn frame data for linking moves",
                    "Work on special move cancels",
                    "Study character-specific combos"
                }
            ));
        }

        return recommendations;
    }
}
