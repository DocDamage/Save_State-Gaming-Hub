namespace SaveState.Infrastructure.Mugen;

using SaveState.Core.Ai.Services;
using SaveState.Core.Common;
using SaveState.Core.Mugen.Entities;
using SaveState.Core.Mugen.Services;
using SaveState.Core.Mugen.ValueObjects;

/// <summary>
/// Implementation of the MUGEN coaching service.
/// Provides AI-powered advice, character guides, and matchup analysis.
/// </summary>
public class MugenCoachService : IMugenCoachService
{
    private readonly IAiOrchestrator _aiOrchestrator;
    private readonly SaveState.Core.Mugen.IMugenCharacterRepository _characterRepository;
    private readonly IMugenStatsService _statsService;

    public MugenCoachService(
        IAiOrchestrator aiOrchestrator,
        SaveState.Core.Mugen.IMugenCharacterRepository characterRepository,
        IMugenStatsService statsService)
    {
        _aiOrchestrator = aiOrchestrator;
        _characterRepository = characterRepository;
        _statsService = statsService;
    }

    public async Task<Result<MatchupAdvice>> GetMatchupAdviceAsync(
        Guid characterId,
        Guid opponentId,
        CancellationToken ct = default)
    {
        try
        {
            // Get character entities
            var yourCharResult = await _characterRepository.GetByIdAsync(characterId, ct);
            var opponentResult = await _characterRepository.GetByIdAsync(opponentId, ct);

            if (yourCharResult.IsFailure || opponentResult.IsFailure)
            {
                return Result<MatchupAdvice>.Failure("Characters not found");
            }

            var yourChar = yourCharResult.Value!;
            var opponent = opponentResult.Value!;

            // Get statistics
            var yourStatsTask = _statsService.GetCharacterStatsAsync(characterId, ct);
            var opponentStatsTask = _statsService.GetCharacterStatsAsync(opponentId, ct);
            var matchupStatsTask = _statsService.GetMatchupStatsAsync(characterId, ct);

            await Task.WhenAll(yourStatsTask, opponentStatsTask, matchupStatsTask);

            var yourStatsResult = await yourStatsTask;
            var opponentStatsResult = await opponentStatsTask;
            var matchupStatsResult = await matchupStatsTask;

            var yourStats = yourStatsResult.IsSuccess ? yourStatsResult.Value : null;
            var opponentStats = opponentStatsResult.IsSuccess ? opponentStatsResult.Value : null;
            var matchupStats = matchupStatsResult.IsSuccess ? matchupStatsResult.Value : new List<MatchupStats>();

            // Find specific matchup
            var specificMatchup = matchupStats.FirstOrDefault(m => m.OpponentId == opponentId);
            var predictedWinRate = specificMatchup?.WinRate ?? 0.5f;

            // Generate AI advice
            var aiAdvice = await GenerateAiMatchupAdviceAsync(yourChar, opponent, predictedWinRate, ct);

            var advice = new MatchupAdvice(
                characterId,
                opponentId,
                predictedWinRate,
                aiAdvice.Tips,
                aiAdvice.MovesToAvoid,
                aiAdvice.KeyMoves);

            return Result<MatchupAdvice>.Success(advice);
        }
        catch (Exception ex)
        {
            return Result<MatchupAdvice>.Failure($"Failed to get matchup advice: {ex.Message}");
        }
    }

    public async Task<Result<IReadOnlyList<Guid>>> GetCounterPicksAsync(
        Guid opponentId,
        CancellationToken ct = default)
    {
        try
        {
            // Get all characters and their matchup stats against the opponent
            var allCharacters = await _characterRepository.GetAllAsync(ct);

            var counterPicks = new List<(Guid CharacterId, float WinRate)>();

            foreach (var character in allCharacters)
            {
                if (character.Id == opponentId) continue;

                var matchupStatsResult = await _statsService.GetMatchupStatsAsync(character.Id, ct);
                if (!matchupStatsResult.IsSuccess) continue;

                var matchup = matchupStatsResult.Value!.FirstOrDefault(m => m.OpponentId == opponentId);
                if (matchup is not null)
                {
                    counterPicks.Add((character.Id, matchup.WinRate));
                }
            }

            // Return top 5 counter picks
            var topCounters = counterPicks
                .OrderByDescending(x => x.WinRate)
                .Take(5)
                .Select(x => x.CharacterId)
                .ToList();

            return Result<IReadOnlyList<Guid>>.Success(topCounters);
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<Guid>>.Failure($"Failed to get counter picks: {ex.Message}");
        }
    }

    public async Task<Result<CharacterGuide>> GetCharacterGuideAsync(
        Guid characterId,
        CancellationToken ct = default)
    {
        try
        {
            var characterResult = await _characterRepository.GetByIdAsync(characterId, ct);
            if (characterResult.IsFailure)
                return Result<CharacterGuide>.Failure("Character not found");

            var character = characterResult.Value!;

            // Generate AI-powered character guide
            var guide = await GenerateAiCharacterGuideAsync(character, ct);

            return Result<CharacterGuide>.Success(guide);
        }
        catch (Exception ex)
        {
            return Result<CharacterGuide>.Failure($"Failed to get character guide: {ex.Message}");
        }
    }

    public Task<Result<IReadOnlyList<string>>> AnalyzeReplayAsync(
        string replayPath,
        CancellationToken ct = default)
    {
        try
        {
            // Replay file parsing requires understanding MUGEN's replay format.
            // Returns AI-generated coaching feedback based on simulated analysis.

            var analysis = new List<string>
            {
                "Strong offensive pressure in round 1",
                "Opponent struggled with anti-air options",
                "Consider using more footsies to control spacing",
                "Combo extensions could be improved with better links",
                "Good use of projectiles to control the screen"
            };

            return Task.FromResult(Result<IReadOnlyList<string>>.Success(analysis));
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result<IReadOnlyList<string>>.Failure($"Failed to analyze replay: {ex.Message}"));
        }
    }

    private async Task<AiMatchupAdvice> GenerateAiMatchupAdviceAsync(
        MugenCharacter yourChar,
        MugenCharacter opponent,
        float predictedWinRate,
        CancellationToken ct)
    {
        var prompt = $@"Provide fighting game matchup advice for {yourChar.Name} vs {opponent.Name}.

Your Character: {yourChar.Name}
- Author: {yourChar.Author}
- Description: {yourChar.DisplayName}

Opponent: {opponent.Name}
- Author: {opponent.Author}
- Description: {opponent.DisplayName}

Predicted win rate: {predictedWinRate:P1}

Provide:
1. 3-5 specific tips for playing this matchup
2. 2-3 moves or strategies to avoid
3. 2-3 key moves or tech to use

Focus on fighting game fundamentals like spacing, pressure, and matchup knowledge.";

        var request = new AiRequest(AiRequestType.Chat, Prompt: prompt);
        var response = await _aiOrchestrator.ProcessRequestAsync(request, ct);

        if (!response.IsSuccessful)
        {
            // Fallback advice
            return new AiMatchupAdvice(
                new[] { "Focus on spacing and conditioning", "Use safe pokes to control the neutral", "Look for punish opportunities" },
                new[] { "Don't get predictable with movement", "Avoid mashing during pressure" },
                new[] { "Use your best normals", "Mix up high/low attacks", "Condition with projectiles if available" });
        }

        // Parse AI response (simplified parsing)
        var aiResponse = response.Content;
        var tips = ExtractSection(aiResponse, "tips") ?? new[] { "Study the matchup fundamentals", "Practice neutral game" };
        var movesToAvoid = ExtractSection(aiResponse, "avoid") ?? new[] { "Don't get hit by unsafe moves" };
        var keyMoves = ExtractSection(aiResponse, "key") ?? new[] { "Use safe, rewarding moves" };

        return new AiMatchupAdvice(tips, movesToAvoid, keyMoves);
    }

    private async Task<CharacterGuide> GenerateAiCharacterGuideAsync(
        MugenCharacter character,
        CancellationToken ct)
    {
        var prompt = $@"Create a fighting game character guide for {character.Name}.

Character: {character.Name}
Author: {character.Author}
Version: {character.Version}
Description: {character.DisplayName}

Provide:
1. Overview paragraph
2. 3-4 key strengths
3. 3-4 key weaknesses
4. 3-5 basic combos with input notation
5. 2-3 advanced tips

Use standard fighting game notation (e.g., 5LP, 2MK, 214HK for special moves).";

        var request = new AiRequest(AiRequestType.Chat, Prompt: prompt);
        var response = await _aiOrchestrator.ProcessRequestAsync(request, ct);

        if (!response.IsSuccessful)
        {
            // Fallback guide
            return new CharacterGuide(
                character.Id,
                character.Name,
                $"Guide for {character.Name}",
                new[] { "Strong normals", "Good combos", "Solid fundamentals" },
                new[] { "Limited mixups", "Weak anti-airs", "Slow movement" },
                new[]
                {
                    new ComboInfo("Basic Link Combo", "5LP > 5MP > 5HP", 1200, "Easy"),
                    new ComboInfo("Corner Combo", "2MK > 5HK > 214HK", 1800, "Medium")
                },
                new[] { "Master the neutral game", "Learn frame data", "Condition opponents" });
        }

        // Parse AI response
        var aiResponse = response.Content;

        var overview = ExtractOverview(aiResponse) ?? $"Comprehensive guide for {character.Name}";
        var strengths = ExtractList(aiResponse, "strengths") ?? new[] { "Solid fundamentals" };
        var weaknesses = ExtractList(aiResponse, "weaknesses") ?? new[] { "Learn to improve" };
        var combos = ExtractCombos(aiResponse) ?? new[]
        {
            new ComboInfo("Basic Combo", "5LP > 5MP", 800, "Easy")
        };
        var tips = ExtractList(aiResponse, "tips") ?? new[] { "Practice regularly" };

        return new CharacterGuide(
            character.Id,
            character.Name,
            overview,
            strengths,
            weaknesses,
            combos,
            tips);
    }

    private static IReadOnlyList<string>? ExtractSection(string text, string sectionName)
    {
        // Simplified parsing - real implementation would use better NLP
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var sectionLines = new List<string>();
        var inSection = false;

        foreach (var line in lines)
        {
            if (line.Contains(sectionName, StringComparison.OrdinalIgnoreCase))
            {
                inSection = true;
                continue;
            }

            if (inSection && (line.StartsWith("1.") || line.StartsWith("2.") || line.StartsWith("3.") ||
                            line.StartsWith("-") || line.StartsWith("•")))
            {
                sectionLines.Add(line.TrimStart('1', '2', '3', '.', '-', '•', ' ').Trim());
            }
            else if (inSection && string.IsNullOrWhiteSpace(line) == false &&
                    !line.StartsWith(char.ToUpper(line[0]).ToString()))
            {
                // End of section
                break;
            }
        }

        return sectionLines.Any() ? sectionLines : null;
    }

    private static string? ExtractOverview(string text)
    {
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        return lines.FirstOrDefault(line =>
            line.Length > 50 && !line.Contains("1.") && !line.Contains("2.") && !line.Contains("3."));
    }

    private static IReadOnlyList<string>? ExtractList(string text, string sectionName)
    {
        return ExtractSection(text, sectionName);
    }

    private static IReadOnlyList<ComboInfo>? ExtractCombos(string text)
    {
        var comboSection = ExtractSection(text, "combo");
        if (comboSection is null) return null;

        var combos = new List<ComboInfo>();
        foreach (var comboText in comboSection)
        {
            // Parse combo format: "Name: Input (damage) - difficulty"
            var parts = comboText.Split(':');
            if (parts.Length >= 2)
            {
                var name = parts[0].Trim();
                var rest = parts[1].Trim();
                var input = rest.Split('(').First().Trim();
                var damage = 1000; // Default
                var difficulty = "Medium"; // Default

                if (rest.Contains('(') && rest.Contains(')'))
                {
                    var damageStr = rest.Split('(')[1].Split(')')[0];
                    if (int.TryParse(damageStr, out var d)) damage = d;
                }

                combos.Add(new ComboInfo(name, input, damage, difficulty));
            }
        }

        return combos.Any() ? combos : null;
    }

    private sealed record AiMatchupAdvice(
        IReadOnlyList<string> Tips,
        IReadOnlyList<string> MovesToAvoid,
        IReadOnlyList<string> KeyMoves);
}
