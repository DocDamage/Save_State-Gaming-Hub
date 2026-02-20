using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Mugen;
using SaveState.Core.Mugen.Entities;
using SaveState.Core.Mugen.CharacterFusion;
using SaveState.Core.Mugen.CharacterFusion.Services;
using SaveState.Infrastructure.Persistence;
using CoreModels = SaveState.Core.Mugen.CharacterFusion;

namespace SaveState.Infrastructure.Mugen.CharacterFusion;

/// <summary>
/// Implementation of character fusion service.
/// </summary>
public class CharacterFusionService : ICharacterFusionService
{
    private readonly SaveStateDbContext _dbContext;
    private readonly ILogger<CharacterFusionService> _logger;
    private readonly IMugenCharacterRepository _characterRepository;
    private readonly ITimeProvider _timeProvider;

    public CharacterFusionService(
        SaveStateDbContext dbContext,
        ILogger<CharacterFusionService> logger,
        IMugenCharacterRepository characterRepository,
        ITimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _logger = logger;
        _characterRepository = characterRepository;
        _timeProvider = timeProvider;
    }

    public async Task<Result<FusionAnalysis>> AnalyzeFusionPotentialAsync(
        Guid parent1Id,
        Guid parent2Id,
        CancellationToken ct = default)
    {
        try
        {
            var parent1Result = await _characterRepository.GetByIdAsync(parent1Id, ct);
            var parent2Result = await _characterRepository.GetByIdAsync(parent2Id, ct);

            if (parent1Result.IsFailure || parent2Result.IsFailure)
                return Result<FusionAnalysis>.Failure("One or both parents not found", ErrorType.NotFound);

            var parent1 = parent1Result.Value;
            var parent2 = parent2Result.Value;

            var analysis = new FusionAnalysis
            {
                Parent1Id = parent1Id,
                Parent2Id = parent2Id,
                Parent1Name = parent1.Name,
                Parent2Name = parent2.Name,
                CompatibilityScore = CalculateCompatibility(parent1, parent2),
                PredictedStats = PredictFusionStats(parent1, parent2, FusionType.Potara),
                SuggestedFusionName = GenerateFusionName(parent1.Name, parent2.Name),
                PredictedMoves = PredictMoves(parent1, parent2),
                Synergies = FindSynergies(parent1, parent2),
                Conflicts = FindConflicts(parent1, parent2)
            };

            analysis.Compatibility = analysis.CompatibilityScore switch
            {
                >= 90 => FusionCompatibility.Perfect,
                >= 75 => FusionCompatibility.Excellent,
                >= 60 => FusionCompatibility.Good,
                >= 45 => FusionCompatibility.Fair,
                >= 30 => FusionCompatibility.Poor,
                _ => FusionCompatibility.Incompatible
            };

            return Result<FusionAnalysis>.Success(analysis);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze fusion potential");
            return Result<FusionAnalysis>.Failure($"Analysis failed: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<FusedCharacter>> FuseCharactersAsync(
        FusionRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var parent1Result = await _characterRepository.GetByIdAsync(request.Parent1Id, ct);
            var parent2Result = await _characterRepository.GetByIdAsync(request.Parent2Id, ct);

            if (parent1Result.IsFailure || parent2Result.IsFailure)
                return Result<FusedCharacter>.Failure("One or both parents not found", ErrorType.NotFound);

            var parent1 = parent1Result.Value;
            var parent2 = parent2Result.Value;

            var fusionName = request.CustomName ?? GenerateFusionName(parent1.Name, parent2.Name);
            var compatibility = CalculateCompatibility(parent1, parent2);

            var fusedCharacter = new FusedCharacter
            {
                Name = fusionName,
                DisplayName = fusionName,
                Parent1Id = request.Parent1Id,
                Parent1Name = parent1.Name,
                Parent2Id = request.Parent2Id,
                Parent2Name = parent2.Name,
                FusionType = request.FusionType,
                Stats = CalculateFusionStats(parent1, parent2, request.FusionType, request.Customization),
                Moves = FuseMoves(parent1, parent2, request.Customization),
                Appearance = GenerateAppearance(parent1, parent2, request.Customization),
                CreatedAt = DateTime.UtcNow,
                CompatibilityScore = compatibility,
                Tags = new List<string> { "fusion", $"tier-{(compatibility >= 70 ? "high" : "standard")}" }
            };

            _dbContext.FusedCharacters.Add(fusedCharacter);
            await _dbContext.SaveChangesAsync(ct);

            _logger.LogInformation("Created fusion: {FusionName} from {Parent1} + {Parent2}",
                fusionName, parent1.Name, parent2.Name);

            return Result<FusedCharacter>.Success(fusedCharacter);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fuse characters");
            return Result<FusedCharacter>.Failure($"Fusion failed: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<string>> GenerateMugenCharacterAsync(
        Guid fusionId,
        string outputDirectory,
        CancellationToken ct = default)
    {
        try
        {
            var fusion = await _dbContext.FusedCharacters
                .FirstOrDefaultAsync(f => f.Id == fusionId, ct);

            if (fusion == null)
                return Result<string>.Failure("Fusion not found", ErrorType.NotFound);

            var characterFolder = Path.Combine(outputDirectory, fusion.Name);
            Directory.CreateDirectory(characterFolder);

            // Generate .def file
            var defContent = GenerateDefFile(fusion);
            await File.WriteAllTextAsync(Path.Combine(characterFolder, $"{fusion.Name}.def"), defContent, ct);

            // Generate .cmd file
            var cmdContent = GenerateCmdFile(fusion);
            await File.WriteAllTextAsync(Path.Combine(characterFolder, $"{fusion.Name}.cmd"), cmdContent, ct);

            // Generate .cns file (constants)
            var cnsContent = GenerateCnsFile(fusion);
            await File.WriteAllTextAsync(Path.Combine(characterFolder, $"{fusion.Name}.cns"), cnsContent, ct);

            fusion.CharacterFolderPath = characterFolder;
            fusion.IsGenerated = true;
            fusion.MugenDefContent = defContent;
            await _dbContext.SaveChangesAsync(ct);

            _logger.LogInformation("Generated MUGEN character files for {FusionName} at {Path}",
                fusion.Name, characterFolder);

            return Result<string>.Success(characterFolder);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate MUGEN character");
            return Result<string>.Failure($"Generation failed: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<FusedCharacter>> GetFusionAsync(Guid fusionId, CancellationToken ct = default)
    {
        var fusion = await _dbContext.FusedCharacters
            .FirstOrDefaultAsync(f => f.Id == fusionId, ct);

        if (fusion == null)
            return Result<FusedCharacter>.Failure("Fusion not found", ErrorType.NotFound);

        return Result<FusedCharacter>.Success(fusion);
    }

    public async Task<Result<List<FusedCharacter>>> GetUserFusionsAsync(Guid userId, CancellationToken ct = default)
    {
        var fusions = await _dbContext.FusedCharacters
            .Where(f => f.CreatedBy == userId)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync(ct);

        return Result<List<FusedCharacter>>.Success(fusions);
    }

    public async Task<Result<List<FusedCharacter>>> GetFusionsByCharacterAsync(Guid characterId, CancellationToken ct = default)
    {
        var fusions = await _dbContext.FusedCharacters
            .Where(f => f.Parent1Id == characterId || f.Parent2Id == characterId)
            .OrderByDescending(f => f.Stats.PowerLevel)
            .ToListAsync(ct);

        return Result<List<FusedCharacter>>.Success(fusions);
    }

    public async Task<Result> DeleteFusionAsync(Guid fusionId, CancellationToken ct = default)
    {
        var fusion = await _dbContext.FusedCharacters
            .FirstOrDefaultAsync(f => f.Id == fusionId, ct);

        if (fusion == null)
            return Result.Failure("Fusion not found", ErrorType.NotFound);

        // Delete character folder if generated
        if (!string.IsNullOrEmpty(fusion.CharacterFolderPath) && Directory.Exists(fusion.CharacterFolderPath))
        {
            Directory.Delete(fusion.CharacterFolderPath, true);
        }

        _dbContext.FusedCharacters.Remove(fusion);
        await _dbContext.SaveChangesAsync(ct);

        return Result.Success();
    }

    public async Task<Result> RecordBattleResultAsync(Guid fusionId, FusionBattleHistory battle, CancellationToken ct = default)
    {
        var fusion = await _dbContext.FusedCharacters
            .FirstOrDefaultAsync(f => f.Id == fusionId, ct);

        if (fusion == null)
            return Result.Failure("Fusion not found", ErrorType.NotFound);

        fusion.BattleCount++;
        if (battle.Won) fusion.WinRate = ((fusion.WinRate * (fusion.BattleCount - 1)) + 100) / fusion.BattleCount;
        else fusion.WinRate = (fusion.WinRate * (fusion.BattleCount - 1)) / fusion.BattleCount;

        _dbContext.FusionBattleHistories.Add(battle);
        await _dbContext.SaveChangesAsync(ct);

        return Result.Success();
    }

    public async Task<Result<List<FusionBattleHistory>>> GetBattleHistoryAsync(Guid fusionId, CancellationToken ct = default)
    {
        var history = await _dbContext.FusionBattleHistories
            .Where(h => h.FusedCharacterId == fusionId)
            .OrderByDescending(h => h.BattleDate)
            .ToListAsync(ct);

        return Result<List<FusionBattleHistory>>.Success(history);
    }

    public async Task<Result<List<FusionLeaderboardEntry>>> GetLeaderboardAsync(int top = 100, CancellationToken ct = default)
    {
        var leaderboard = await _dbContext.FusedCharacters
            .Where(f => f.BattleCount > 0)
            .OrderByDescending(f => f.Stats.PowerLevel)
            .Take(top)
            .Select(f => new FusionLeaderboardEntry
            {
                FusedCharacterId = f.Id,
                Name = f.Name,
                ParentNames = $"{f.Parent1Name} + {f.Parent2Name}",
                PowerLevel = f.Stats.PowerLevel,
                Tier = f.Stats.Tier,
                TotalBattles = f.BattleCount,
                Wins = (int)(f.BattleCount * (f.WinRate / 100)),
                WinRate = f.WinRate
            })
            .ToListAsync(ct);

        // Assign ranks
        for (int i = 0; i < leaderboard.Count; i++)
        {
            leaderboard[i].Rank = i + 1;
        }

        return Result<List<FusionLeaderboardEntry>>.Success(leaderboard);
    }

    public Task<Result<List<PresetFusion>>> GetPresetFusionsAsync(bool unlockedOnly = true, CancellationToken ct = default)
    {
        // Return default presets
        var presets = new List<PresetFusion>
        {
            new()
            {
                Name = "Vegito",
                DisplayName = "Vegito",
                Parent1Name = "Goku",
                Parent2Name = "Vegeta",
                FusionType = FusionType.Potara,
                BaseStats = new FusionStats { Health = 1500, Attack = 140, Defense = 130, Speed = 120, Power = 150, Special = 140 },
                SignatureMoves = new List<string> { "Final Kamehameha", "Spirit Sword", "Big Bang Attack" },
                Description = "The ultimate fusion using Potara earrings",
                IsUnlocked = true
            },
            new()
            {
                Name = "Gogeta",
                DisplayName = "Gogeta",
                Parent1Name = "Goku",
                Parent2Name = "Vegeta",
                FusionType = FusionType.FusionDance,
                BaseStats = new FusionStats { Health = 1400, Attack = 145, Defense = 125, Speed = 125, Power = 145, Special = 135 },
                SignatureMoves = new List<string> { "Stardust Breaker", "Big Bang Kamehameha", "Soul Punisher" },
                Description = "Fusion through the Fusion Dance",
                IsUnlocked = true
            }
        };

        return Task.FromResult(Result<List<PresetFusion>>.Success(presets));
    }

    public Task<Result> UnlockPresetFusionAsync(Guid presetId, Guid userId, CancellationToken ct = default)
    {
        // Implementation would unlock preset for user
        return Task.FromResult(Result.Success());
    }

    public async Task<Result<List<FusionSuggestion>>> GetFusionSuggestionsAsync(Guid characterId, int count = 5, CancellationToken ct = default)
    {
        var characterResult = await _characterRepository.GetByIdAsync(characterId, ct);
        if (characterResult.IsFailure)
            return Result<List<FusionSuggestion>>.Failure("Character not found", ErrorType.NotFound);

        var character = characterResult.Value;

        // Get all other characters and rank by compatibility
        var allCharacters = await _characterRepository.GetAllAsync(ct);
        var suggestions = new List<FusionSuggestion>();

        foreach (var other in allCharacters.Where(c => c.Id != characterId).Take(20))
        {
            var compatibility = CalculateCompatibility(character, other);
            suggestions.Add(new FusionSuggestion
            {
                CharacterId = other.Id,
                CharacterName = other.Name,
                CompatibilityScore = compatibility,
                Compatibility = compatibility switch
                {
                    >= 90 => FusionCompatibility.Perfect,
                    >= 75 => FusionCompatibility.Excellent,
                    >= 60 => FusionCompatibility.Good,
                    _ => FusionCompatibility.Fair
                },
                SuggestedFusionName = GenerateFusionName(character.Name, other.Name),
                PredictedStats = PredictFusionStats(character, other, FusionType.Potara),
                Reason = compatibility > 75 ? "Excellent synergy detected" : "Moderate compatibility"
            });
        }

        return Result<List<FusionSuggestion>>.Success(
            suggestions.OrderByDescending(s => s.CompatibilityScore).Take(count).ToList());
    }

    public async Task<Result<FusionComparison>> CompareFusionsAsync(Guid fusionId1, Guid fusionId2, CancellationToken ct = default)
    {
        var fusion1 = await GetFusionAsync(fusionId1, ct);
        var fusion2 = await GetFusionAsync(fusionId2, ct);

        if (fusion1.IsFailure) return Result<FusionComparison>.Failure(fusion1.Error!, fusion1.ErrorType);
        if (fusion2.IsFailure) return Result<FusionComparison>.Failure(fusion2.Error!, fusion2.ErrorType);

        var comparison = new FusionComparison
        {
            Fusion1 = fusion1.Value,
            Fusion2 = fusion2.Value,
            StatComparisons = new Dictionary<string, StatComparison>
            {
                ["Health"] = new() { StatName = "Health", Fusion1Value = fusion1.Value.Stats.Health, Fusion2Value = fusion2.Value.Stats.Health },
                ["Attack"] = new() { StatName = "Attack", Fusion1Value = fusion1.Value.Stats.Attack, Fusion2Value = fusion2.Value.Stats.Attack },
                ["Defense"] = new() { StatName = "Defense", Fusion1Value = fusion1.Value.Stats.Defense, Fusion2Value = fusion2.Value.Stats.Defense },
                ["Speed"] = new() { StatName = "Speed", Fusion1Value = fusion1.Value.Stats.Speed, Fusion2Value = fusion2.Value.Stats.Speed },
                ["Power"] = new() { StatName = "Power", Fusion1Value = fusion1.Value.Stats.Power, Fusion2Value = fusion2.Value.Stats.Power }
            }
        };

        comparison.Winner = comparison.StatComparisons.Count(s => s.Value.Winner == "Fusion1") >
                           comparison.StatComparisons.Count(s => s.Value.Winner == "Fusion2") ? 1 :
                           comparison.StatComparisons.Count(s => s.Value.Winner == "Fusion2") >
                           comparison.StatComparisons.Count(s => s.Value.Winner == "Fusion1") ? 2 : 0;

        comparison.PredictedOutcome = comparison.Winner == 1 ? $"{fusion1.Value.Name} is predicted to win" :
                                      comparison.Winner == 2 ? $"{fusion2.Value.Name} is predicted to win" :
                                      "Battle is predicted to be evenly matched";

        return Result<FusionComparison>.Success(comparison);
    }

    public async Task<Result<byte[]>> ExportFusionAsync(Guid fusionId, CancellationToken ct = default)
    {
        var fusion = await GetFusionAsync(fusionId, ct);
        if (fusion.IsFailure) return Result<byte[]>.Failure(fusion.Error!, fusion.ErrorType);

        var json = JsonSerializer.Serialize(fusion.Value, new JsonSerializerOptions { WriteIndented = true });
        return Result<byte[]>.Success(System.Text.Encoding.UTF8.GetBytes(json));
    }

    public async Task<Result<FusedCharacter>> ImportFusionAsync(byte[] data, Guid userId, CancellationToken ct = default)
    {
        try
        {
            var json = System.Text.Encoding.UTF8.GetString(data);
            var importedFusion = JsonSerializer.Deserialize<FusedCharacter>(json);
            
            if (importedFusion == null)
                return Result<FusedCharacter>.Failure("Invalid fusion data", ErrorType.Validation);

            // Create a new fusion with imported data
            var fusion = new FusedCharacter
            {
                Name = $"{importedFusion.Name} (Imported)",
                DisplayName = $"{importedFusion.DisplayName} (Imported)",
                Parent1Id = importedFusion.Parent1Id,
                Parent1Name = importedFusion.Parent1Name,
                Parent2Id = importedFusion.Parent2Id,
                Parent2Name = importedFusion.Parent2Name,
                FusionType = importedFusion.FusionType,
                Stats = importedFusion.Stats,
                Moves = importedFusion.Moves,
                Appearance = importedFusion.Appearance,
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow,
                BattleCount = 0,
                WinRate = 0
            };

            _dbContext.FusedCharacters.Add(fusion);
            await _dbContext.SaveChangesAsync(ct);

            return Result<FusedCharacter>.Success(fusion);
        }
        catch (Exception ex)
        {
            return Result<FusedCharacter>.Failure($"Import failed: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<bool>> CanFuseAsync(Guid parent1Id, Guid parent2Id, CancellationToken ct = default)
    {
        if (parent1Id == parent2Id)
            return Result<bool>.Success(false);

        var parent1 = await _characterRepository.GetByIdAsync(parent1Id, ct);
        var parent2 = await _characterRepository.GetByIdAsync(parent2Id, ct);

        return Result<bool>.Success(parent1 != null && parent2 != null);
    }

    #region Helper Methods

    private int CalculateCompatibility(MugenCharacter parent1, MugenCharacter parent2)
    {
        // Simple compatibility algorithm based on attributes
        var random = new Random();
        return random.Next(30, 100); // Placeholder - would use actual character data
    }

    private string GenerateFusionName(string name1, string name2)
    {
        // Generate creative fusion names
        var strategies = new List<Func<string, string, string>>
        {
            (a, b) => $"{a[..(a.Length / 2)]}{b[(b.Length / 2)..]}",
            (a, b) => $"{a[0]}{b}",
            (a, b) => $"{a}{b[0]}",
            (a, b) => $"Fusion {a}",
            (a, b) => $"Super {a}"
        };

        var random = new Random();
        return strategies[random.Next(strategies.Count)](name1, name2);
    }

    private FusionStats PredictFusionStats(MugenCharacter parent1, MugenCharacter parent2, FusionType type)
    {
        return CalculateFusionStats(parent1, parent2, type, null);
    }

    private FusionStats CalculateFusionStats(
        MugenCharacter parent1,
        MugenCharacter parent2,
        FusionType type,
        FusionCustomizationOptions? customization)
    {
        var p1Percent = customization?.Parent1StatPercentage ?? 50;
        var p2Percent = 100 - p1Percent;

        var stats = new FusionStats
        {
            Health = CombineStat(1000, 1000, p1Percent, p2Percent, type),
            Attack = CombineStat(100, 100, p1Percent, p2Percent, type),
            Defense = CombineStat(100, 100, p1Percent, p2Percent, type),
            Speed = CombineStat(100, 100, p1Percent, p2Percent, type),
            Power = CombineStat(100, 100, p1Percent, p2Percent, type),
            Special = CombineStat(100, 100, p1Percent, p2Percent, type),
            Combo = CombineStat(100, 100, p1Percent, p2Percent, type)
        };

        return stats;
    }

    private int CombineStat(int stat1, int stat2, int p1Percent, int p2Percent, FusionType type)
    {
        var baseValue = (stat1 * p1Percent + stat2 * p2Percent) / 100;
        return type switch
        {
            FusionType.Potara => (int)(baseValue * 1.5),
            FusionType.FusionDance => (int)(baseValue * 1.2),
            FusionType.DNAFusion => (int)(baseValue * 1.1),
            _ => baseValue
        };
    }

    private List<string> PredictMoves(MugenCharacter parent1, MugenCharacter parent2)
    {
        return new List<string> { "Move 1", "Move 2", "Move 3", "Special Attack" };
    }

    private List<string> FindSynergies(MugenCharacter parent1, MugenCharacter parent2)
    {
        return new List<string> { "Complementary fighting styles", "Similar power levels" };
    }

    private List<string> FindConflicts(MugenCharacter parent1, MugenCharacter parent2)
    {
        return new List<string>(); // No conflicts by default
    }

    private List<FusedMove> FuseMoves(MugenCharacter parent1, MugenCharacter parent2, FusionCustomizationOptions? customization)
    {
        var moves = new List<FusedMove>();
        
        // Add moves from both parents
        moves.Add(new FusedMove { Name = "Parent1 Special", Source = MoveSource.Parent1, ParentName = parent1.Name, Damage = 100 });
        moves.Add(new FusedMove { Name = "Parent2 Special", Source = MoveSource.Parent2, ParentName = parent2.Name, Damage = 100 });
        moves.Add(new FusedMove { Name = "Fusion Blast", Source = MoveSource.NewFusionMove, Damage = 200, IsEnhanced = true, EnhancementDescription = "Combined power of both parents" });

        return moves;
    }

    private FusionAppearance GenerateAppearance(MugenCharacter parent1, MugenCharacter parent2, FusionCustomizationOptions? customization)
    {
        return new FusionAppearance
        {
            PrimaryColor = customization?.PrimaryColor ?? "#4A90E2",
            SecondaryColor = customization?.SecondaryColor ?? "#F5A623",
            AuraColor = "#FFD700",
            Parent1VisualDominance = 50,
            UniqueTraits = new List<string> { "Aura glow", "Enhanced musculature" }
        };
    }


    private string GenerateDefFile(CoreModels.FusedCharacter fusion)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"; {fusion.DisplayName} - Fused Character");
        sb.AppendLine($"; Parents: {fusion.Parent1Name} + {fusion.Parent2Name}");
        sb.AppendLine();
        sb.AppendLine("[Info]");
        sb.AppendLine($"name = \"{fusion.DisplayName}\"");
        sb.AppendLine($"displayname = \"{fusion.DisplayName}\"");
        sb.AppendLine($"versiondate = {_timeProvider.UtcNow:MM,dd,yyyy}");
        sb.AppendLine("mugenversion = 1.1");
        sb.AppendLine("author = \"SaveState Fusion System\"");
        sb.AppendLine("pal.defaults = 1");
        sb.AppendLine();
        sb.AppendLine("[Files]");
        sb.AppendLine($"sprite = {fusion.Name}.sff");
        sb.AppendLine($"anim = {fusion.Name}.air");
        sb.AppendLine($"sound = {fusion.Name}.snd");
        sb.AppendLine($"cmd = {fusion.Name}.cmd");
        sb.AppendLine($"cns = {fusion.Name}.cns");
        sb.AppendLine($"st = {fusion.Name}.cns");
        sb.AppendLine("st1 = AI.cns");
        sb.AppendLine();
        sb.AppendLine("[Palette Keymap]");
        sb.AppendLine("a = 1");
        sb.AppendLine();
        sb.AppendLine("[Arcade]");
        sb.AppendLine("intro.storyboard = ");
        sb.AppendLine("ending.storyboard = ");
        return sb.ToString();
    }

    private string GenerateCmdFile(CoreModels.FusedCharacter fusion)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"; {fusion.DisplayName} Command Definitions");
        sb.AppendLine("; Generated by SaveState Fusion System");
        sb.AppendLine();
        sb.AppendLine("[Command]");
        sb.AppendLine("name = \"Fusion Blast\"");
        sb.AppendLine("command = ~D, DF, F, a+b");
        return sb.ToString();
    }

    private string GenerateCnsFile(CoreModels.FusedCharacter fusion)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"; {fusion.DisplayName} Constants");
        sb.AppendLine("; Generated by SaveState Fusion System");
        sb.AppendLine();
        sb.AppendLine("[Data]");
        sb.AppendLine($"life = {fusion.Stats.Health}");
        sb.AppendLine($"attack = {fusion.Stats.Attack}");
        sb.AppendLine($"defence = {fusion.Stats.Defense}");
        sb.AppendLine("power = 3000");
        return sb.ToString();
    }

    #endregion
}
