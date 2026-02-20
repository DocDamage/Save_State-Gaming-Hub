using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Mugen.ComboDatabase;
using SaveState.Infrastructure.Persistence;

namespace SaveState.Infrastructure.Mugen.ComboDatabase.Managers;

/// <summary>
/// Manages combo search and query operations.
/// </summary>
public class ComboSearchManager
{
    private readonly SaveStateDbContext _dbContext;
    private readonly ILogger<ComboSearchManager> _logger;

    public ComboSearchManager(
        SaveStateDbContext dbContext,
        ILogger<ComboSearchManager> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Searches combos with filtering, sorting, and pagination.
    /// </summary>
    public async Task<Result<List<ComboEntry>>> SearchCombosAsync(
        ComboFilter filter,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        try
        {
            var query = _dbContext.ComboEntries.AsQueryable();

            if (!string.IsNullOrEmpty(filter.CharacterName))
                query = query.Where(c => c.CharacterName == filter.CharacterName);

            if (filter.Difficulty.HasValue)
                query = query.Where(c => c.Difficulty == filter.Difficulty.Value);

            if (filter.MinDamage.HasValue)
                query = query.Where(c => c.Damage >= filter.MinDamage.Value);

            if (filter.MaxDamage.HasValue)
                query = query.Where(c => c.Damage <= filter.MaxDamage.Value);

            if (filter.MinHits.HasValue)
                query = query.Where(c => c.HitCount >= filter.MinHits.Value);

            if (!string.IsNullOrEmpty(filter.StartingPosition))
                query = query.Where(c => c.StartingPosition == filter.StartingPosition);

            if (filter.MaxMeterRequired.HasValue)
                query = query.Where(c => c.MeterRequired <= filter.MaxMeterRequired.Value);

            if (filter.IsVerified.HasValue)
                query = query.Where(c => c.IsVerified == filter.IsVerified.Value);

            if (filter.IsOptimal.HasValue)
                query = query.Where(c => c.IsOptimal == filter.IsOptimal.Value);

            if (filter.IsTouchOfDeath.HasValue)
                query = query.Where(c => c.IsTouchOfDeath == filter.IsTouchOfDeath.Value);

            if (!string.IsNullOrEmpty(filter.SearchTerm))
            {
                var term = filter.SearchTerm.ToLower();
                query = query.Where(c =>
                    c.Name.ToLower().Contains(term) ||
                    (c.Description != null && c.Description.ToLower().Contains(term)) ||
                    c.InputNotation.ToLower().Contains(term));
            }

            // Apply sorting
            query = filter.SortBy switch
            {
                ComboSortOption.Damage => filter.SortDescending
                    ? query.OrderByDescending(c => c.Damage)
                    : query.OrderBy(c => c.Damage),
                ComboSortOption.HitCount => filter.SortDescending
                    ? query.OrderByDescending(c => c.HitCount)
                    : query.OrderBy(c => c.HitCount),
                ComboSortOption.Difficulty => filter.SortDescending
                    ? query.OrderByDescending(c => c.Difficulty)
                    : query.OrderBy(c => c.Difficulty),
                ComboSortOption.DateAdded => filter.SortDescending
                    ? query.OrderByDescending(c => c.CreatedAt)
                    : query.OrderBy(c => c.CreatedAt),
                ComboSortOption.Rating => filter.SortDescending
                    ? query.OrderByDescending(c => c.Ratings.AverageRating)
                    : query.OrderBy(c => c.Ratings.AverageRating),
                ComboSortOption.Usage => filter.SortDescending
                    ? query.OrderByDescending(c => c.UsageStats.MatchUsageCount)
                    : query.OrderBy(c => c.UsageStats.MatchUsageCount),
                ComboSortOption.MeterEfficiency => filter.SortDescending
                    ? query.OrderByDescending(c => c.Damage / (c.MeterRequired + 1))
                    : query.OrderBy(c => c.Damage / (c.MeterRequired + 1)),
                _ => query.OrderByDescending(c => c.Damage)
            };

            var combos = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return Result<List<ComboEntry>>.Success(combos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search combos");
            return Result<List<ComboEntry>>.Failure($"Search failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Gets all combos for a character with summary statistics.
    /// </summary>
    public async Task<Result<CharacterComboDatabase>> GetCharacterCombosAsync(
        string characterName,
        CancellationToken ct = default)
    {
        try
        {
            var combos = await _dbContext.ComboEntries
                .AsNoTracking()
                .Where(c => c.CharacterName == characterName)
                .ToListAsync(ct);

            var summary = new CharacterComboDatabase
            {
                CharacterName = characterName,
                TotalCombos = combos.Count,
                EasyCombos = combos.Count(c => c.Difficulty == ComboDifficulty.Easy),
                MediumCombos = combos.Count(c => c.Difficulty == ComboDifficulty.Medium),
                HardCombos = combos.Count(c => c.Difficulty == ComboDifficulty.Hard),
                ExpertCombos = combos.Count(c => c.Difficulty == ComboDifficulty.Expert),
                OptimalCombos = combos.Count(c => c.IsOptimal),
                TouchOfDeathCombos = combos.Count(c => c.IsTouchOfDeath),
                AverageDamage = combos.Any() ? (decimal)combos.Average(c => (double)c.Damage) : 0,
                MaxComboHits = combos.Any() ? combos.Max(c => c.HitCount) : 0,
                HighestDamage = combos.Any() ? combos.Max(c => c.Damage) : 0,
                FeaturedCombos = combos
                    .Where(c => c.IsOptimal || c.Ratings.AverageRating >= 4)
                    .Take(10)
                    .ToList(),
                CombosByStarter = combos
                    .GroupBy(c => c.StartingPosition)
                    .ToDictionary(g => g.Key, g => g.Count()),
                CombosByPosition = combos
                    .GroupBy(c => c.EndingPosition)
                    .ToDictionary(g => g.Key, g => g.Count())
            };

            return Result<CharacterComboDatabase>.Success(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get character combos for {Character}", characterName);
            return Result<CharacterComboDatabase>.Failure($"Query failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Gets combos filtered by difficulty for a character.
    /// </summary>
    public async Task<Result<List<ComboEntry>>> GetCombosByDifficultyAsync(
        string characterName,
        ComboDifficulty difficulty,
        CancellationToken ct = default)
    {
        try
        {
            var combos = await _dbContext.ComboEntries
                .AsNoTracking()
                .Where(c => c.CharacterName == characterName && c.Difficulty == difficulty)
                .OrderByDescending(c => c.Damage)
                .ToListAsync(ct);

            return Result<List<ComboEntry>>.Success(combos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get combos by difficulty");
            return Result<List<ComboEntry>>.Failure($"Query failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Gets optimal combos for a character with optional position filter.
    /// </summary>
    public async Task<Result<List<ComboEntry>>> GetOptimalCombosAsync(
        string characterName,
        string? startingPosition = null,
        CancellationToken ct = default)
    {
        try
        {
            var query = _dbContext.ComboEntries
                .AsNoTracking()
                .Where(c => c.CharacterName == characterName && c.IsOptimal);

            if (!string.IsNullOrEmpty(startingPosition))
                query = query.Where(c => c.StartingPosition == startingPosition);

            var combos = await query
                .OrderByDescending(c => c.Damage)
                .ToListAsync(ct);

            return Result<List<ComboEntry>>.Success(combos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get optimal combos");
            return Result<List<ComboEntry>>.Failure($"Query failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Gets Touch of Death combos, optionally filtered by character.
    /// </summary>
    public async Task<Result<List<ComboEntry>>> GetTouchOfDeathCombosAsync(
        string? characterName = null,
        CancellationToken ct = default)
    {
        try
        {
            var query = _dbContext.ComboEntries
                .AsNoTracking()
                .Where(c => c.IsTouchOfDeath);

            if (!string.IsNullOrEmpty(characterName))
                query = query.Where(c => c.CharacterName == characterName);

            var combos = await query
                .OrderByDescending(c => c.Damage)
                .ToListAsync(ct);

            return Result<List<ComboEntry>>.Success(combos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get ToD combos");
            return Result<List<ComboEntry>>.Failure($"Query failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Gets combos filtered by tag, optionally filtered by character.
    /// </summary>
    public async Task<Result<List<ComboEntry>>> GetCombosByTagAsync(
        string tag,
        string? characterName = null,
        CancellationToken ct = default)
    {
        try
        {
            var query = _dbContext.ComboEntries
                .AsNoTracking()
                .Where(c => c.Tags.Contains(tag));

            if (!string.IsNullOrEmpty(characterName))
                query = query.Where(c => c.CharacterName == characterName);

            var combos = await query
                .OrderByDescending(c => c.Ratings.AverageRating)
                .ToListAsync(ct);

            return Result<List<ComboEntry>>.Success(combos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get combos by tag");
            return Result<List<ComboEntry>>.Failure($"Query failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Gets matchup-specific combo recommendations.
    /// </summary>
    public async Task<Result<ComboMatchupInfo>> GetMatchupCombosAsync(
        string characterName,
        string opponentName,
        CancellationToken ct = default)
    {
        try
        {
            var characterCombos = await _dbContext.ComboEntries
                .AsNoTracking()
                .Where(c => c.CharacterName == characterName)
                .ToListAsync(ct);

            // Filter for matchup-specific recommendations
            var recommended = characterCombos
                .Where(c => c.Universal || !c.CharacterExceptions.Contains(opponentName))
                .OrderByDescending(c => c.Damage / (c.MeterRequired + 1))
                .Take(5)
                .ToList();

            var optimal = characterCombos
                .Where(c => c.IsOptimal)
                .Take(3)
                .ToList();

            var meterEfficient = characterCombos
                .Where(c => c.MeterRequired == 0)
                .OrderByDescending(c => c.Damage)
                .Take(3)
                .ToList();

            var info = new ComboMatchupInfo
            {
                CharacterName = characterName,
                OpponentName = opponentName,
                RecommendedCombos = recommended,
                OptimalCombos = optimal,
                MeterEfficientCombos = meterEfficient,
                Analysis = $"Found {characterCombos.Count} combos for {characterName} vs {opponentName}",
                CharacterAdvantage = CalculateCharacterAdvantage(characterCombos)
            };

            return Result<ComboMatchupInfo>.Success(info);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get matchup combos");
            return Result<ComboMatchupInfo>.Failure($"Query failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Calculates character advantage based on combo stats.
    /// </summary>
    private static decimal CalculateCharacterAdvantage(List<ComboEntry> combos)
    {
        if (!combos.Any()) return 0;
        var avgDamage = combos.Average(c => c.Damage);
        var avgMeterEfficiency = combos.Average(c => c.Damage / (c.MeterRequired + 1));
        return (decimal)(avgDamage / 1000.0 + avgMeterEfficiency / 100.0);
    }
}
