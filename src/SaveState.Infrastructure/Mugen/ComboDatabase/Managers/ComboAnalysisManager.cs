using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Mugen.ComboDatabase;
using SaveState.Core.Mugen.ComboDatabase.Services;
using SaveState.Infrastructure.Persistence;

namespace SaveState.Infrastructure.Mugen.ComboDatabase.Managers;

/// <summary>
/// Manages combo analysis, optimization, and relationship operations.
/// </summary>
public class ComboAnalysisManager
{
    private readonly SaveStateDbContext _dbContext;
    private readonly ILogger<ComboAnalysisManager> _logger;

    public ComboAnalysisManager(
        SaveStateDbContext dbContext,
        ILogger<ComboAnalysisManager> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Gets optimization suggestions for a combo.
    /// </summary>
    public Task<Result<List<DamageOptimizationSuggestion>>> GetOptimizationSuggestionsAsync(
        Guid comboId,
        CancellationToken ct = default)
    {
        // Placeholder - would analyze combo for optimization opportunities
        return Task.FromResult(Result<List<DamageOptimizationSuggestion>>.Success(new List<DamageOptimizationSuggestion>()));
    }

    /// <summary>
    /// Suggests an improvement to a combo.
    /// </summary>
    public Task<Result<DamageOptimizationSuggestion>> SuggestImprovementAsync(
        Guid comboId,
        string suggestion,
        int potentialDamage,
        string method,
        CancellationToken ct = default)
    {
        try
        {
            var opt = new DamageOptimizationSuggestion
            {
                ComboId = comboId,
                Suggestion = suggestion,
                PotentialExtraDamage = potentialDamage,
                Method = method,
                Verified = false
            };

            return Task.FromResult(Result<DamageOptimizationSuggestion>.Success(opt));
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result<DamageOptimizationSuggestion>.Failure(
                $"Failed to suggest: {ex.Message}", ErrorType.Internal));
        }
    }

    /// <summary>
    /// Finds similar combos based on character and starting position.
    /// </summary>
    public async Task<Result<List<ComboEntry>>> FindSimilarCombosAsync(
        Guid comboId,
        int maxResults = 10,
        CancellationToken ct = default)
    {
        try
        {
            var combo = await _dbContext.ComboEntries
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == comboId, ct);

            if (combo == null)
                return Result<List<ComboEntry>>.Failure($"Combo {comboId} not found", ErrorType.NotFound);

            var similar = await _dbContext.ComboEntries
                .AsNoTracking()
                .Where(c => c.Id != comboId &&
                           c.CharacterName == combo.CharacterName &&
                           c.StartingPosition == combo.StartingPosition)
                .OrderBy(c => Math.Abs(c.Damage - combo.Damage))
                .Take(maxResults)
                .ToListAsync(ct);

            return Result<List<ComboEntry>>.Success(similar);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to find similar combos");
            return Result<List<ComboEntry>>.Failure($"Search failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Gets combo routes analysis for a character.
    /// </summary>
    public async Task<Result<ComboRoutesAnalysis>> GetComboRoutesAsync(
        string characterName,
        CancellationToken ct = default)
    {
        try
        {
            var combos = await _dbContext.ComboEntries
                .AsNoTracking()
                .Where(c => c.CharacterName == characterName)
                .ToListAsync(ct);

            var analysis = new ComboRoutesAnalysis
            {
                CharacterName = characterName
            };

            // Analyze common starters
            analysis.CommonStarters = combos
                .GroupBy(c => c.Moves.FirstOrDefault()?.Name ?? "Unknown")
                .Select(g => new RouteInfo
                {
                    Move = g.Key,
                    OccurrenceCount = g.Count(),
                    AverageDamage = (int)g.Average(c => c.Damage)
                })
                .OrderByDescending(r => r.OccurrenceCount)
                .Take(5)
                .ToList();

            // Analyze common enders
            analysis.CommonEnders = combos
                .GroupBy(c => c.Moves.LastOrDefault()?.Name ?? "Unknown")
                .Select(g => new RouteInfo
                {
                    Move = g.Key,
                    OccurrenceCount = g.Count(),
                    AverageDamage = (int)g.Average(c => c.Damage)
                })
                .OrderByDescending(r => r.OccurrenceCount)
                .Take(5)
                .ToList();

            return Result<ComboRoutesAnalysis>.Success(analysis);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get combo routes");
            return Result<ComboRoutesAnalysis>.Failure($"Analysis failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Links two combos as related.
    /// </summary>
    public async Task<Result> LinkRelatedCombosAsync(
        Guid comboId1,
        Guid comboId2,
        CancellationToken ct = default)
    {
        try
        {
            var combo1 = await _dbContext.ComboEntries
                .FirstOrDefaultAsync(c => c.Id == comboId1, ct);

            if (combo1 == null)
                return Result.Failure($"Combo {comboId1} not found", ErrorType.NotFound);

            if (!combo1.RelatedComboIds.Contains(comboId2))
            {
                combo1.RelatedComboIds.Add(comboId2);
                await _dbContext.SaveChangesAsync(ct);
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to link combos");
            return Result.Failure($"Failed to link: {ex.Message}", ErrorType.Internal);
        }
    }
}
