using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Mugen.ComboDatabase;
using SaveState.Infrastructure.Persistence;

namespace SaveState.Infrastructure.Mugen.ComboDatabase.Managers;

/// <summary>
/// Manages CRUD operations for combo entries.
/// </summary>
public class ComboCrudManager
{
    private readonly SaveStateDbContext _dbContext;
    private readonly ILogger<ComboCrudManager> _logger;
    private readonly ITimeProvider _timeProvider;

    public ComboCrudManager(
        SaveStateDbContext dbContext,
        ILogger<ComboCrudManager> logger,
        ITimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Creates a new combo entry with auto-calculation of IsOptimal.
    /// </summary>
    public async Task<Result<ComboEntry>> AddComboAsync(
        AddComboRequest request,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Adding combo {ComboName} for {Character}",
                request.Name, request.CharacterName);

            var combo = new ComboEntry
            {
                CharacterName = request.CharacterName,
                Name = request.Name,
                Description = request.Description,
                Difficulty = request.Difficulty,
                HitCount = request.HitCount,
                Damage = request.Damage,
                StartingPosition = request.StartingPosition,
                MeterRequired = request.MeterRequired,
                Moves = request.Moves ?? new List<ComboMoveEntry>(),
                InputNotation = request.InputNotation,
                VideoUrl = request.VideoUrl,
                Creator = request.Creator,
                Tags = request.Tags ?? new List<string>(),
                IsTouchOfDeath = request.IsTouchOfDeath,
                GameVersion = request.GameVersion,
                CreatedAt = _timeProvider.UtcNow,
                UpdatedAt = _timeProvider.UtcNow
            };

            // Auto-calculate properties
            combo.IsOptimal = await DetermineIfOptimalAsync(combo, ct);

            _dbContext.ComboEntries.Add(combo);
            await _dbContext.SaveChangesAsync(ct);

            _logger.LogInformation("Added combo {ComboId} for {Character}",
                combo.Id, combo.CharacterName);

            return Result<ComboEntry>.Success(combo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add combo for {Character}", request.CharacterName);
            return Result<ComboEntry>.Failure($"Failed to add combo: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Updates an existing combo entry.
    /// </summary>
    public async Task<Result<ComboEntry>> UpdateComboAsync(
        Guid comboId,
        UpdateComboRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var combo = await _dbContext.ComboEntries
                .FirstOrDefaultAsync(c => c.Id == comboId, ct);

            if (combo == null)
            {
                return Result<ComboEntry>.Failure($"Combo {comboId} not found", ErrorType.NotFound);
            }

            if (request.Name != null) combo.Name = request.Name;
            if (request.Description != null) combo.Description = request.Description;
            if (request.Difficulty.HasValue) combo.Difficulty = request.Difficulty.Value;
            if (request.HitCount.HasValue) combo.HitCount = request.HitCount.Value;
            if (request.Damage.HasValue) combo.Damage = request.Damage.Value;
            if (request.Moves != null) combo.Moves = request.Moves;
            if (request.InputNotation != null) combo.InputNotation = request.InputNotation;
            if (request.VideoUrl != null) combo.VideoUrl = request.VideoUrl;
            if (request.Tags != null) combo.Tags = request.Tags;
            if (request.IsVerified.HasValue) combo.IsVerified = request.IsVerified.Value;
            if (request.IsOptimal.HasValue) combo.IsOptimal = request.IsOptimal.Value;

            combo.UpdatedAt = _timeProvider.UtcNow;

            await _dbContext.SaveChangesAsync(ct);

            _logger.LogInformation("Updated combo {ComboId}", comboId);
            return Result<ComboEntry>.Success(combo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update combo {ComboId}", comboId);
            return Result<ComboEntry>.Failure($"Failed to update combo: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Gets a combo by ID and increments the view count.
    /// </summary>
    public async Task<Result<ComboEntry>> GetComboAsync(
        Guid comboId,
        CancellationToken ct = default)
    {
        try
        {
            var combo = await _dbContext.ComboEntries
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == comboId, ct);

            if (combo == null)
            {
                return Result<ComboEntry>.Failure($"Combo {comboId} not found", ErrorType.NotFound);
            }

            // Increment view count
            combo.UsageStats.ViewCount++;
            await _dbContext.SaveChangesAsync(ct);

            return Result<ComboEntry>.Success(combo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get combo {ComboId}", comboId);
            return Result<ComboEntry>.Failure($"Failed to get combo: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Deletes a combo entry.
    /// </summary>
    public async Task<Result> DeleteComboAsync(
        Guid comboId,
        CancellationToken ct = default)
    {
        try
        {
            var combo = await _dbContext.ComboEntries
                .FirstOrDefaultAsync(c => c.Id == comboId, ct);

            if (combo == null)
            {
                return Result.Failure($"Combo {comboId} not found", ErrorType.NotFound);
            }

            _dbContext.ComboEntries.Remove(combo);
            await _dbContext.SaveChangesAsync(ct);

            _logger.LogInformation("Deleted combo {ComboId}", comboId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete combo {ComboId}", comboId);
            return Result.Failure($"Failed to delete combo: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Determines if a combo is optimal (highest damage for its starter).
    /// </summary>
    private async Task<bool> DetermineIfOptimalAsync(ComboEntry combo, CancellationToken ct)
    {
        var sameStarter = await _dbContext.ComboEntries
            .AsNoTracking()
            .Where(c => c.CharacterName == combo.CharacterName &&
                       c.StartingPosition == combo.StartingPosition &&
                       c.Id != combo.Id)
            .ToListAsync(ct);

        if (!sameStarter.Any()) return true;

        return combo.Damage >= sameStarter.Max(c => c.Damage);
    }
}
