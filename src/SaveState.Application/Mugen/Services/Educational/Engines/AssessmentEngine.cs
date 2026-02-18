namespace SaveState.Application.Mugen.Services.Educational.Engines;

using Microsoft.Extensions.Logging;
using SaveState.Application.Mugen.Models.Educational;

public class AssessmentEngine
{
    private readonly ILogger<AssessmentEngine> _logger;
    private readonly Dictionary<string, PracticeSession> _practiceSessions;
    private readonly Dictionary<string, MatchAnalysis> _matchAnalyses;

    public AssessmentEngine(ILogger<AssessmentEngine> logger)
    {
        _logger = logger;
        _practiceSessions = new Dictionary<string, PracticeSession>();
        _matchAnalyses = new Dictionary<string, MatchAnalysis>();
    }

    /// <summary>
    /// Creates a new practice session for a user.
    /// </summary>
    public Task<PracticeSession> CreatePracticeSessionAsync(string userId, CancellationToken ct = default)
    {
        _logger.LogInformation("Creating practice session for user {UserId}", userId);

        var sessionId = Guid.NewGuid().ToString();
        var session = new PracticeSession
        {
            SessionId = sessionId,
            UserId = userId,
            Topic = "General Practice",
            Difficulty = DifficultyLevel.Beginner,
            StartedAt = DateTime.UtcNow,
            Status = PracticeSessionStatus.InProgress,
            Exercises = new List<PracticeExercise>
            {
                new()
                {
                    ExerciseId = "ex-1",
                    Title = "Basic Combo Practice",
                    Goal = "Execute a 3-hit combo 5 times",
                    Completed = false
                },
                new()
                {
                    ExerciseId = "ex-2",
                    Title = "Blocking Drill",
                    Goal = "Block 10 attacks in a row",
                    Completed = false
                },
                new()
                {
                    ExerciseId = "ex-3",
                    Title = "Movement Training",
                    Goal = "Perform 20 dashes",
                    Completed = false
                }
            }
        };

        _practiceSessions[sessionId] = session;

        return Task.FromResult(session);
    }

    /// <summary>
    /// Analyzes a match and generates insights.
    /// </summary>
    public Task<MatchAnalysis> AnalyzeMatchAsync(MatchData matchData, CancellationToken ct = default)
    {
        _logger.LogInformation("Analyzing match {MatchId} for user {UserId}", matchData.MatchId, matchData.UserId);

        var analysisId = Guid.NewGuid().ToString();

        // Calculate overall performance
        var overallPerformance = CalculateOverallPerformance(matchData);

        // Identify strengths
        var strengths = IdentifyStrengths(matchData);

        // Identify weaknesses
        var weaknesses = IdentifyWeaknesses(matchData);

        // Generate suggestions
        var suggestions = GenerateSuggestions(matchData, strengths, weaknesses);

        // Calculate skill ratings
        var skillRatings = CalculateSkillRatings(matchData);

        var analysis = new MatchAnalysis
        {
            AnalysisId = analysisId,
            MatchId = matchData.MatchId,
            UserId = matchData.UserId,
            OverallPerformance = overallPerformance,
            Strengths = strengths,
            Weaknesses = weaknesses,
            Suggestions = suggestions,
            SkillRatings = skillRatings,
            AnalyzedAt = DateTime.UtcNow
        };

        _matchAnalyses[analysisId] = analysis;

        return Task.FromResult(analysis);
    }

    /// <summary>
    /// Gets a practice session by ID.
    /// </summary>
    public Task<PracticeSession?> GetPracticeSessionAsync(string sessionId, CancellationToken ct = default)
    {
        _practiceSessions.TryGetValue(sessionId, out var session);
        return Task.FromResult(session);
    }

    /// <summary>
    /// Gets a match analysis by ID.
    /// </summary>
    public Task<MatchAnalysis?> GetMatchAnalysisAsync(string analysisId, CancellationToken ct = default)
    {
        _matchAnalyses.TryGetValue(analysisId, out var analysis);
        return Task.FromResult(analysis);
    }

    /// <summary>
    /// Gets the count of practice sessions.
    /// </summary>
    public int GetPracticeSessionCount()
    {
        return _practiceSessions.Count;
    }

    /// <summary>
    /// Gets the count of match analyses.
    /// </summary>
    public int GetMatchAnalysisCount()
    {
        return _matchAnalyses.Count;
    }

    private double CalculateOverallPerformance(MatchData matchData)
    {
        double score = 0;

        // Win/loss factor (40% weight)
        score += matchData.IsWin ? 40 : 0;

        // Round performance (30% weight)
        var totalRounds = matchData.RoundsWon + matchData.RoundsLost;
        if (totalRounds > 0)
        {
            score += 30.0 * matchData.RoundsWon / totalRounds;
        }

        // Combo execution (20% weight)
        var successfulCombos = matchData.CombosExecuted.Count(c => c.WasSuccessful);
        var totalCombos = matchData.CombosExecuted.Count;
        if (totalCombos > 0)
        {
            score += 20.0 * successfulCombos / totalCombos;
        }

        // Defense (10% weight)
        var totalBlocks = matchData.BlocksSuccessful + matchData.BlocksMissed;
        if (totalBlocks > 0)
        {
            score += 10.0 * matchData.BlocksSuccessful / totalBlocks;
        }

        return Math.Min(100, Math.Max(0, score));
    }

    private List<StrengthArea> IdentifyStrengths(MatchData matchData)
    {
        var strengths = new List<StrengthArea>();

        // Check combo execution
        var comboSuccessRate = matchData.CombosExecuted.Any()
            ? (double)matchData.CombosExecuted.Count(c => c.WasSuccessful) / matchData.CombosExecuted.Count
            : 0;

        if (comboSuccessRate > 0.7)
        {
            strengths.Add(new StrengthArea
            {
                Skill = "Combo Execution",
                Description = "Consistent combo execution with high success rate",
                Score = comboSuccessRate * 100
            });
        }

        // Check defense
        var totalBlocks = matchData.BlocksSuccessful + matchData.BlocksMissed;
        var blockSuccessRate = totalBlocks > 0 ? (double)matchData.BlocksSuccessful / totalBlocks : 0;

        if (blockSuccessRate > 0.6)
        {
            strengths.Add(new StrengthArea
            {
                Skill = "Defense",
                Description = "Good blocking and defensive skills",
                Score = blockSuccessRate * 100
            });
        }

        // Check match outcome
        if (matchData.IsWin && matchData.RoundsWon > matchData.RoundsLost)
        {
            strengths.Add(new StrengthArea
            {
                Skill = "Match Control",
                Description = "Strong performance in match situations",
                Score = 85
            });
        }

        return strengths;
    }

    private List<WeaknessArea> IdentifyWeaknesses(MatchData matchData)
    {
        var weaknesses = new List<WeaknessArea>();

        // Check combo execution
        var comboSuccessRate = matchData.CombosExecuted.Any()
            ? (double)matchData.CombosExecuted.Count(c => c.WasSuccessful) / matchData.CombosExecuted.Count
            : 1;

        if (comboSuccessRate < 0.5)
        {
            weaknesses.Add(new WeaknessArea
            {
                Skill = "Combo Execution",
                Description = "Combo execution needs improvement",
                Score = comboSuccessRate * 100,
                Priority = "High"
            });
        }

        // Check defense
        var totalBlocks = matchData.BlocksSuccessful + matchData.BlocksMissed;
        var blockSuccessRate = totalBlocks > 0 ? (double)matchData.BlocksSuccessful / totalBlocks : 1;

        if (blockSuccessRate < 0.5)
        {
            weaknesses.Add(new WeaknessArea
            {
                Skill = "Defense",
                Description = "Blocking timing and defensive positioning need work",
                Score = blockSuccessRate * 100,
                Priority = "High"
            });
        }

        // Check damage taken
        var totalDamageTaken = matchData.CombosTaken.Sum(c => c.Damage);
        if (totalDamageTaken > 500)
        {
            weaknesses.Add(new WeaknessArea
            {
                Skill = "Damage Mitigation",
                Description = "Taking too much damage from opponent combos",
                Score = 40,
                Priority = "Medium"
            });
        }

        return weaknesses;
    }

    private List<ImprovementSuggestion> GenerateSuggestions(MatchData matchData, List<StrengthArea> strengths, List<WeaknessArea> weaknesses)
    {
        var suggestions = new List<ImprovementSuggestion>();
        var priority = 1;

        // Generate suggestions based on weaknesses
        foreach (var weakness in weaknesses.OrderByDescending(w => w.Priority))
        {
            suggestions.Add(new ImprovementSuggestion
            {
                Area = weakness.Skill,
                Suggestion = GetSuggestionForSkill(weakness.Skill),
                RecommendedContent = GetContentForSkill(weakness.Skill),
                Priority = priority++
            });
        }

        // If no major weaknesses, suggest advanced techniques
        if (!weaknesses.Any())
        {
            suggestions.Add(new ImprovementSuggestion
            {
                Area = "Advanced Techniques",
                Suggestion = "Consider learning advanced combo routes and mix-ups",
                RecommendedContent = "Advanced Combo Tutorial",
                Priority = 1
            });
        }

        return suggestions;
    }

    private string GetSuggestionForSkill(string skill)
    {
        return skill switch
        {
            "Combo Execution" => "Practice basic combos in training mode until consistent",
            "Defense" => "Work on blocking timing and defensive positioning",
            "Damage Mitigation" => "Focus on avoiding opponent's combo starters",
            "Match Control" => "Practice maintaining momentum during matches",
            _ => "Practice general fundamentals"
        };
    }

    private string GetContentForSkill(string skill)
    {
        return skill switch
        {
            "Combo Execution" => "Beginner Combo Tutorial",
            "Defense" => "Defense Fundamentals Guide",
            "Damage Mitigation" => "Neutral Game Tutorial",
            "Match Control" => "Advanced Strategy Guide",
            _ => "General Practice Session"
        };
    }

    private List<SkillRating> CalculateSkillRatings(MatchData matchData)
    {
        var ratings = new List<SkillRating>();

        // Combo execution rating
        var comboSuccessRate = matchData.CombosExecuted.Any()
            ? (double)matchData.CombosExecuted.Count(c => c.WasSuccessful) / matchData.CombosExecuted.Count
            : 0;
        ratings.Add(new SkillRating
        {
            SkillName = "Combo Execution",
            Rating = comboSuccessRate * 100,
            MaxRating = 100,
            Category = "Offense"
        });

        // Defense rating
        var totalBlocks = matchData.BlocksSuccessful + matchData.BlocksMissed;
        var blockSuccessRate = totalBlocks > 0 ? (double)matchData.BlocksSuccessful / totalBlocks : 0;
        ratings.Add(new SkillRating
        {
            SkillName = "Defense",
            Rating = blockSuccessRate * 100,
            MaxRating = 100,
            Category = "Defense"
        });

        // Adaptability rating (based on special moves usage)
        ratings.Add(new SkillRating
        {
            SkillName = "Special Moves",
            Rating = Math.Min(100, matchData.SpecialMovesUsed * 10),
            MaxRating = 100,
            Category = "Offense"
        });

        // Consistency rating (based on match duration)
        var consistencyScore = Math.Min(100, matchData.MatchDuration.TotalMinutes * 5);
        ratings.Add(new SkillRating
        {
            SkillName = "Consistency",
            Rating = consistencyScore,
            MaxRating = 100,
            Category = "General"
        });

        return ratings;
    }
}
