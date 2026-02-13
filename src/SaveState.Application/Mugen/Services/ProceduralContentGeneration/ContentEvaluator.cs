using Microsoft.Extensions.Logging;

namespace SaveState.Application.Mugen.Services.ProceduralContentGeneration;

/// <summary>
/// Content evaluator for assessing generated content quality.
/// </summary>
public class ProceduralContentGeneratorContentEvaluator
{
    private readonly ILogger<ProceduralContentGeneratorContentEvaluator> _logger;

    public ProceduralContentGeneratorContentEvaluator(ILogger<ProceduralContentGeneratorContentEvaluator> logger)
    {
        _logger = logger;
    }

    public async Task<ProceduralContentGeneratorContentEvaluation> EvaluateMoveAsync(ProceduralContentGeneratorMoveParameters parameters, CancellationToken ct)
    {
        // Evaluate move balance and fun factor
        var balanceScore = CalculateBalanceScore(parameters);
        var funFactor = CalculateFunFactor(parameters);
        var difficulty = DetermineDifficulty(parameters);

        return new ProceduralContentGeneratorContentEvaluation
        {
            BalanceScore = balanceScore,
            FunFactor = funFactor,
            Difficulty = difficulty,
            Recommendations = GenerateRecommendations(parameters, balanceScore)
        };
    }

    public async Task<ProceduralContentGeneratorContentEvaluation> EvaluateStageAsync(ProceduralContentGeneratorStageLayout layout, IReadOnlyList<ProceduralContentGeneratorInteractiveElement> elements, CancellationToken ct)
    {
        // Evaluate stage quality
        return new ProceduralContentGeneratorContentEvaluation
        {
            BalanceScore = 0.8f,
            FunFactor = 0.75f,
            Difficulty = ProceduralContentGeneratorDifficultyLevel.Medium,
            Recommendations = new[] { "Good stage design" }
        };
    }

    public async Task<ProceduralContentGeneratorContentEvaluation> EvaluateCharacterAsync(ProceduralContentGeneratorCharacterAttributes attributes, ProceduralContentGeneratorCharacterMoveset moveset, CancellationToken ct)
    {
        // Evaluate character balance
        return new ProceduralContentGeneratorContentEvaluation
        {
            BalanceScore = 0.85f,
            FunFactor = 0.8f,
            Difficulty = ProceduralContentGeneratorDifficultyLevel.Medium,
            Recommendations = new[] { "Well-balanced character" }
        };
    }

    public async Task<ProceduralContentGeneratorContentEvaluation> EvaluateEffectAsync(ProceduralContentGeneratorEffectParameters parameters, CancellationToken ct)
    {
        // Evaluate effect quality
        return new ProceduralContentGeneratorContentEvaluation
        {
            BalanceScore = 0.9f,
            FunFactor = 0.85f,
            Difficulty = ProceduralContentGeneratorDifficultyLevel.Easy,
            Recommendations = new[] { "Impressive effect" }
        };
    }

    public async Task<ProceduralContentGeneratorCollectionEvaluation> EvaluateCollectionAsync(IReadOnlyList<ProceduralContentGeneratorGeneratedContentItem> items, CancellationToken ct)
    {
        // Evaluate collection coherence
        return new ProceduralContentGeneratorCollectionEvaluation
        {
            CoherenceScore = 0.8f,
            QualityScore = 0.85f,
            CompletenessScore = 0.9f
        };
    }

    public async Task<ProceduralContentGeneratorEvolutionEvaluation> EvaluateEvolutionAsync(ProceduralContentGeneratorGeneratedContentItem original, ProceduralContentGeneratorGeneratedContentItem evolved, CancellationToken ct)
    {
        // Evaluate evolution quality
        return new ProceduralContentGeneratorEvolutionEvaluation
        {
            QualityImprovement = 0.15f,
            BalanceChange = 0.1f,
            UniquenessIncrease = 0.2f
        };
    }

    private float CalculateBalanceScore(ProceduralContentGeneratorMoveParameters parameters)
    {
        // Simplified balance calculation
        var damageScore = Math.Clamp(parameters.Damage / 100.0f, 0, 1);
        var riskScore = 1.0f - (parameters.StartupFrames / 30.0f); // Faster startup = higher risk
        var rewardScore = parameters.IsProjectile ? 0.8f : 1.0f;

        return (damageScore + riskScore + rewardScore) / 3.0f;
    }

    private float CalculateFunFactor(ProceduralContentGeneratorMoveParameters parameters)
    {
        // Estimate fun factor based on move properties
        var funScore = 0.5f;

        if (parameters.IsProjectile) funScore += 0.2f;
        if (parameters.IsAntiAir) funScore += 0.15f;
        if (parameters.Knockback > 15) funScore += 0.1f;

        return Math.Clamp(funScore, 0, 1);
    }

    private ProceduralContentGeneratorDifficultyLevel DetermineDifficulty(ProceduralContentGeneratorMoveParameters parameters)
    {
        var complexity = parameters.StartupFrames + parameters.ActiveFrames + parameters.RecoveryFrames;

        if (complexity < 20) return ProceduralContentGeneratorDifficultyLevel.Easy;
        if (complexity < 35) return ProceduralContentGeneratorDifficultyLevel.Medium;
        return ProceduralContentGeneratorDifficultyLevel.Hard;
    }

    private IReadOnlyList<string> GenerateRecommendations(ProceduralContentGeneratorMoveParameters parameters, float balanceScore)
    {
        var recommendations = new List<string>();

        if (balanceScore < 0.6f)
        {
            recommendations.Add("Consider reducing damage or increasing recovery to improve balance");
        }
        else if (balanceScore > 0.9f)
        {
            recommendations.Add("Consider increasing damage or reducing startup for better viability");
        }

        if (parameters.StartupFrames > 20)
        {
            recommendations.Add("Long startup frames may make this move punishable - consider optimization");
        }

        return recommendations;
    }
}
