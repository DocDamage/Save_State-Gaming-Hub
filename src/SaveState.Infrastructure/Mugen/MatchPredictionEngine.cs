namespace SaveState.Infrastructure.Mugen;

using SaveState.Core.Ai.Services;
using SaveState.Core.Common;
using SaveState.Core.Mugen.Entities;
using SaveState.Core.Mugen.Services;
using SaveState.Core.Mugen.ValueObjects;

/// <summary>
/// AI-powered match prediction engine.
/// Uses character attributes, historical data, and AI analysis to predict outcomes.
/// </summary>
public class MatchPredictionEngine : IMatchPredictionEngine
{
    private readonly IAiOrchestrator _aiOrchestrator;
    private readonly IMugenStatsService _statsService;
    private readonly SaveState.Core.Mugen.IMugenCharacterRepository _characterRepository;

    public MatchPredictionEngine(
        IAiOrchestrator aiOrchestrator,
        IMugenStatsService statsService,
        SaveState.Core.Mugen.IMugenCharacterRepository characterRepository)
    {
        _aiOrchestrator = aiOrchestrator;
        _statsService = statsService;
        _characterRepository = characterRepository;
    }

    public async Task<Result<MatchPrediction>> PredictMatchAsync(
        MugenCharacter character1,
        MugenCharacter character2,
        CancellationToken ct = default)
    {
        try
        {
            // Get historical statistics for both characters
            var stats1Task = _statsService.GetCharacterStatsAsync(character1.Id, ct);
            var stats2Task = _statsService.GetCharacterStatsAsync(character2.Id, ct);
            var matchupStats1Task = _statsService.GetMatchupStatsAsync(character1.Id, ct);
            var matchupStats2Task = _statsService.GetMatchupStatsAsync(character2.Id, ct);

            await Task.WhenAll(stats1Task, stats2Task, matchupStats1Task, matchupStats2Task);

            var stats1Result = await stats1Task;
            var stats2Result = await stats2Task;
            var matchupStats1Result = await matchupStats1Task;
            var matchupStats2Result = await matchupStats2Task;

            var stats1 = stats1Result.IsSuccess ? stats1Result.Value : null;
            var stats2 = stats2Result.IsSuccess ? stats2Result.Value : null;
            var matchupStats1 = matchupStats1Result.IsSuccess ? matchupStats1Result.Value : new List<MatchupStats>();
            var matchupStats2 = matchupStats2Result.IsSuccess ? matchupStats2Result.Value : new List<MatchupStats>();

            // Calculate basic factors
            var factors = CalculateFactors(character1, character2, stats1, stats2, matchupStats1, matchupStats2);

            // Use AI for enhanced prediction
            var aiPrediction = await GetAiPredictionAsync(character1, character2, factors, ct);

            // Combine statistical and AI predictions
            var combinedPrediction = CombinePredictions(factors, aiPrediction);

            return Result.Success<MatchPrediction>(combinedPrediction);
        }
        catch (Exception ex)
        {
            // Fallback to basic statistical prediction if AI fails
            var factors = CalculateBasicFactors(character1, character2);
            var fallbackPrediction = CreatePredictionFromFactors(factors, "Fallback statistical analysis");
            return Result.Success<MatchPrediction>(fallbackPrediction);
        }
    }

    public async Task<Result> TrainWithResultAsync(
        MugenMatchHistory actualResult,
        CancellationToken ct = default)
    {
        try
        {
            // Get character entities for training
            var char1Result = await _characterRepository.GetByIdAsync(actualResult.Player1CharacterId, ct);
            var char2Result = await _characterRepository.GetByIdAsync(actualResult.Player2CharacterId, ct);

            if (char1Result.IsFailure || char2Result.IsFailure ||
                char1Result.Value is null || char2Result.Value is null)
            {
                return Result.Failure("Characters not found for training");
            }

            var char1 = char1Result.Value;
            var char2 = char2Result.Value;

            // Create training prompt for AI
            var trainingPrompt = CreateTrainingPrompt(
                char1,
                char2,
                actualResult.Result);

            // Send training data to AI (this would typically be stored for future model training)
            var trainingRequest = new AiRequest(
                AiRequestType.Chat,
                Prompt: trainingPrompt);

            var response = await _aiOrchestrator.ProcessRequestAsync(trainingRequest, ct);

            // In a real implementation, this would update a machine learning model
            // For now, we just acknowledge the training data
            return response.IsSuccessful
                ? Result.Success()
                : Result.Failure("Training failed");
        }
        catch (Exception ex)
        {
            return Result.Failure($"Training failed: {ex.Message}");
        }
    }

    private static IReadOnlyList<MatchFactor> CalculateFactors(
        MugenCharacter char1,
        MugenCharacter char2,
        CharacterStats? stats1,
        CharacterStats? stats2,
        IReadOnlyList<MatchupStats> matchupStats1,
        IReadOnlyList<MatchupStats> matchupStats2)
    {
        var factors = new List<MatchFactor>();

        // Win rate factor
        var winRate1 = stats1?.WinRate ?? 0.5f;
        var winRate2 = stats2?.WinRate ?? 0.5f;
        factors.Add(new MatchFactor("Win Rate", winRate1, winRate2, 0.4f));

        // Specific matchup factor
        var specificMatchup = matchupStats1.FirstOrDefault(m => m.OpponentId == char2.Id);
        var matchupWinRate1 = specificMatchup?.WinRate ?? 0.5f;
        var reverseMatchup = matchupStats2.FirstOrDefault(m => m.OpponentId == char1.Id);
        var matchupWinRate2 = reverseMatchup?.WinRate ?? 0.5f;
        factors.Add(new MatchFactor("Matchup History", matchupWinRate1, matchupWinRate2, 0.3f));

        // Experience factor (total matches)
        var experience1 = stats1?.TotalMatches ?? 0;
        var experience2 = stats2?.TotalMatches ?? 0;
        var maxExperience = Math.Max(experience1, experience2);
        var expFactor1 = maxExperience > 0 ? (float)experience1 / maxExperience : 0.5f;
        var expFactor2 = maxExperience > 0 ? (float)experience2 / maxExperience : 0.5f;
        factors.Add(new MatchFactor("Experience", expFactor1, expFactor2, 0.2f));

        // File size as rough power indicator (larger files might be more complex characters)
        var sizeFactor1 = char1.FileSize > 0 ? Math.Min((float)char1.FileSize / (1024 * 1024), 10f) / 10f : 0.5f;
        var sizeFactor2 = char2.FileSize > 0 ? Math.Min((float)char2.FileSize / (1024 * 1024), 10f) / 10f : 0.5f;
        factors.Add(new MatchFactor("Complexity", sizeFactor1, sizeFactor2, 0.1f));

        return factors;
    }

    private static IReadOnlyList<MatchFactor> CalculateBasicFactors(MugenCharacter char1, MugenCharacter char2)
    {
        // Simplified factors when stats aren't available
        return new[]
        {
            new MatchFactor("Basic Analysis", 0.5f, 0.5f, 1.0f)
        };
    }

    private async Task<AiPredictionResult> GetAiPredictionAsync(
        MugenCharacter char1,
        MugenCharacter char2,
        IReadOnlyList<MatchFactor> factors,
        CancellationToken ct)
    {
        var prompt = BuildPredictionPrompt(char1, char2, factors);

        var request = new AiRequest(
            AiRequestType.Chat,
            Prompt: prompt);

        var response = await _aiOrchestrator.ProcessRequestAsync(request, ct);

        if (!response.IsSuccessful || string.IsNullOrEmpty(response.Content))
        {
            return new AiPredictionResult(0.5f, 0.5f, "AI prediction unavailable");
        }

        // Parse AI response (simplified - real implementation would use structured output)
        var aiResponse = response.Content.ToLowerInvariant();
        var char1WinProb = aiResponse.Contains($"{char1.Name.ToLower()} wins") ? 0.6f :
                          aiResponse.Contains($"{char2.Name.ToLower()} wins") ? 0.4f : 0.5f;
        var char2WinProb = 1 - char1WinProb;

        return new AiPredictionResult(char1WinProb, char2WinProb, response.Content);
    }

    private static string BuildPredictionPrompt(
        MugenCharacter char1,
        MugenCharacter char2,
        IReadOnlyList<MatchFactor> factors)
    {
        var factorSummary = string.Join(", ",
            factors.Select(f => $"{f.Name}: {char1.Name} {f.Character1Score:F2} vs {char2.Name} {f.Character2Score:F2}"));

        return $@"Predict the outcome of a MUGEN match between {char1.Name} and {char2.Name}.

Character 1: {char1.Name}
- Author: {char1.Author}
- Version: {char1.Version}
- Description: {char1.DisplayName}

Character 2: {char2.Name}
- Author: {char2.Author}
- Version: {char2.Version}
- Description: {char2.DisplayName}

Statistical Factors: {factorSummary}

Based on fighting game knowledge and the provided data, predict who would win and why.
Consider character balance, move sets, and matchup advantages.

Respond with a prediction and reasoning.";
    }

    private static MatchPrediction CombinePredictions(
        IReadOnlyList<MatchFactor> factors,
        AiPredictionResult aiResult)
    {
        // Weighted combination of statistical factors and AI prediction
        var statisticalWinProb1 = factors.Sum(f => f.Character1Score * f.Weight) / factors.Sum(f => f.Weight);
        var aiWinProb1 = aiResult.Character1WinProbability;

        // Blend statistical and AI predictions (70% statistical, 30% AI)
        var blendedWinProb1 = (statisticalWinProb1 * 0.7f) + (aiWinProb1 * 0.3f);
        var blendedWinProb2 = 1 - blendedWinProb1;

        // Adjust for draws (small probability)
        var drawProb = 0.05f;
        blendedWinProb1 *= (1 - drawProb);
        blendedWinProb2 *= (1 - drawProb);

        var reasoning = $"Statistical analysis combined with AI prediction. {aiResult.Reasoning}";

        return new MatchPrediction(
            blendedWinProb1,
            blendedWinProb2,
            drawProb,
            factors,
            reasoning);
    }

    private static MatchPrediction CreatePredictionFromFactors(
        IReadOnlyList<MatchFactor> factors,
        string reasoning)
    {
        var winProb1 = factors.Sum(f => f.Character1Score * f.Weight) / factors.Sum(f => f.Weight);
        var winProb2 = 1 - winProb1;
        var drawProb = 0.05f;

        winProb1 *= (1 - drawProb);
        winProb2 *= (1 - drawProb);

        return new MatchPrediction(winProb1, winProb2, drawProb, factors, reasoning);
    }

    private static string CreateTrainingPrompt(MugenCharacter char1, MugenCharacter char2, MatchResult result)
    {
        var winner = result switch
        {
            MatchResult.Player1Win => char1.Name,
            MatchResult.Player2Win => char2.Name,
            _ => "Draw"
        };

        return $@"Training Data: MUGEN match between {char1.Name} and {char2.Name}.
Result: {winner} won.
Character 1: {char1.Name} ({char1.Author})
Character 2: {char2.Name} ({char2.Author})
Use this data to improve future predictions.";
    }

    private sealed record AiPredictionResult(float Character1WinProbability, float Character2WinProbability, string Reasoning);
}

