using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Mugen.ComboDatabase;
using SaveState.Core.Mugen.ComboDatabase.Services;
using SaveState.Infrastructure.Persistence;

namespace SaveState.Infrastructure.Mugen.ComboDatabase.Managers;

/// <summary>
/// Manages combo import, export, and discovery operations.
/// </summary>
public class ComboImportExportManager
{
    private readonly SaveStateDbContext _dbContext;
    private readonly ILogger<ComboImportExportManager> _logger;
    private readonly ITimeProvider _timeProvider;

    public ComboImportExportManager(
        SaveStateDbContext dbContext,
        ILogger<ComboImportExportManager> logger,
        ITimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Discovers combos from replay analysis.
    /// </summary>
    public async Task<Result<List<ComboEntry>>> DiscoverCombosFromReplayAsync(
        Guid replayAnalysisId,
        CancellationToken ct = default)
    {
        try
        {
            var replay = await _dbContext.ReplayAnalyses
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == replayAnalysisId, ct);

            if (replay == null)
                return Result<List<ComboEntry>>.Failure($"Replay {replayAnalysisId} not found", ErrorType.NotFound);

            var discovered = new List<ComboEntry>();

            foreach (var detectedCombo in replay.Combos)
            {
                var combo = new ComboEntry
                {
                    CharacterName = detectedCombo.Character,
                    Name = $"{detectedCombo.HitCount}-hit Combo ({detectedCombo.TotalDamage} dmg)",
                    HitCount = detectedCombo.HitCount,
                    Damage = detectedCombo.TotalDamage,
                    Difficulty = MapDifficulty(detectedCombo.Difficulty),
                    Moves = detectedCombo.Moves.Select((m, i) => new ComboMoveEntry
                    {
                        Name = m.MoveName,
                        Input = m.Input,
                        SequenceOrder = i,
                        Damage = m.Damage
                    }).ToList(),
                    Source = "Replay Analysis",
                    IsTouchOfDeath = detectedCombo.IsTouchOfDeath
                };

                _dbContext.ComboEntries.Add(combo);
                discovered.Add(combo);
            }

            await _dbContext.SaveChangesAsync(ct);

            return Result<List<ComboEntry>>.Success(discovered);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to discover combos from replay");
            return Result<List<ComboEntry>>.Failure($"Discovery failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Imports combos from JSON data.
    /// </summary>
    public Task<Result<int>> ImportCombosAsync(
        string source,
        string data,
        CancellationToken ct = default)
    {
        try
        {
            var combos = JsonSerializer.Deserialize<List<ComboEntry>>(data);
            if (combos == null)
                return Task.FromResult(Result<int>.Failure("Invalid data format", ErrorType.Validation));

            foreach (var combo in combos)
            {
                // Id is set by EntityBase constructor
                combo.CreatedAt = _timeProvider.UtcNow;
                combo.Source = source;
                _dbContext.ComboEntries.Add(combo);
            }

            return Task.FromResult(Result<int>.Success(combos.Count));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import combos");
            return Task.FromResult(Result<int>.Failure($"Import failed: {ex.Message}", ErrorType.Internal));
        }
    }

    /// <summary>
    /// Exports combos for a character in the specified format.
    /// </summary>
    public async Task<Result<string>> ExportCombosAsync(
        string characterName,
        ExportFormat format = ExportFormat.Json,
        CancellationToken ct = default)
    {
        try
        {
            var combos = await _dbContext.ComboEntries
                .AsNoTracking()
                .Where(c => c.CharacterName == characterName)
                .ToListAsync(ct);

            var result = format switch
            {
                ExportFormat.Json => JsonSerializer.Serialize(combos, new JsonSerializerOptions { WriteIndented = true }),
                ExportFormat.Csv => ConvertToCsv(combos),
                ExportFormat.Markdown => ConvertToMarkdown(combos),
                _ => JsonSerializer.Serialize(combos)
            };

            return Result<string>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export combos");
            return Result<string>.Failure($"Export failed: {ex.Message}", ErrorType.Internal);
        }
    }

    private static ComboDifficulty MapDifficulty(Core.Mugen.ReplayAnalysis.ComboDifficulty difficulty)
    {
        return difficulty switch
        {
            Core.Mugen.ReplayAnalysis.ComboDifficulty.Easy => ComboDifficulty.Easy,
            Core.Mugen.ReplayAnalysis.ComboDifficulty.Medium => ComboDifficulty.Medium,
            Core.Mugen.ReplayAnalysis.ComboDifficulty.Hard => ComboDifficulty.Hard,
            Core.Mugen.ReplayAnalysis.ComboDifficulty.VeryHard => ComboDifficulty.VeryHard,
            Core.Mugen.ReplayAnalysis.ComboDifficulty.TOD => ComboDifficulty.TOD,
            _ => ComboDifficulty.Medium
        };
    }

    private static string ConvertToCsv(List<ComboEntry> combos)
    {
        var lines = new List<string> { "Name,Character,Difficulty,Damage,Hits,Meter" };
        lines.AddRange(combos.Select(c =>
            $"\"{c.Name}\",{c.CharacterName},{c.Difficulty},{c.Damage},{c.HitCount},{c.MeterRequired}"));
        return string.Join("\n", lines);
    }

    private static string ConvertToMarkdown(List<ComboEntry> combos)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("# Combo Database");
        sb.AppendLine();

        foreach (var combo in combos)
        {
            sb.AppendLine($"## {combo.Name}");
            sb.AppendLine($"- **Character:** {combo.CharacterName}");
            sb.AppendLine($"- **Damage:** {combo.Damage}");
            sb.AppendLine($"- **Hits:** {combo.HitCount}");
            sb.AppendLine($"- **Difficulty:** {combo.Difficulty}");
            sb.AppendLine($"- **Input:** {combo.InputNotation}");
            sb.AppendLine();
        }

        return sb.ToString();
    }
}
