using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Mugen.ComboDatabase;
using SaveState.Infrastructure.Persistence;

namespace SaveState.Infrastructure.Mugen.ComboDatabase.Managers;

/// <summary>
/// Manages combo collections.
/// </summary>
public class ComboCollectionManager
{
    private readonly SaveStateDbContext _dbContext;
    private readonly ILogger<ComboCollectionManager> _logger;
    private readonly ITimeProvider _timeProvider;

    public ComboCollectionManager(
        SaveStateDbContext dbContext,
        ILogger<ComboCollectionManager> logger,
        ITimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Creates a new combo collection.
    /// </summary>
    public async Task<Result<ComboCollection>> CreateCollectionAsync(
        string name,
        string? description,
        string? characterName,
        string creator,
        bool isPublic = true,
        CancellationToken ct = default)
    {
        try
        {
            var collection = new ComboCollection
            {
                Name = name,
                Description = description,
                CharacterName = characterName,
                Creator = creator,
                IsPublic = isPublic,
                CreatedAt = _timeProvider.UtcNow,
                UpdatedAt = _timeProvider.UtcNow,
                ComboIds = new List<Guid>()
            };

            _dbContext.ComboCollections.Add(collection);
            await _dbContext.SaveChangesAsync(ct);

            return Result<ComboCollection>.Success(collection);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create collection");
            return Result<ComboCollection>.Failure($"Failed to create: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Adds a combo to a collection.
    /// </summary>
    public async Task<Result> AddToCollectionAsync(
        Guid collectionId,
        Guid comboId,
        CancellationToken ct = default)
    {
        try
        {
            var collection = await _dbContext.ComboCollections
                .FirstOrDefaultAsync(c => c.Id == collectionId, ct);

            if (collection == null)
                return Result.Failure($"Collection {collectionId} not found", ErrorType.NotFound);

            if (!collection.ComboIds.Contains(comboId))
            {
                collection.ComboIds.Add(comboId);
                collection.UpdatedAt = _timeProvider.UtcNow;
                await _dbContext.SaveChangesAsync(ct);
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add to collection");
            return Result.Failure($"Failed to add: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Gets a collection by ID and increments its view count.
    /// </summary>
    public async Task<Result<ComboCollection>> GetCollectionAsync(
        Guid collectionId,
        CancellationToken ct = default)
    {
        try
        {
            var collection = await _dbContext.ComboCollections
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == collectionId, ct);

            if (collection == null)
                return Result<ComboCollection>.Failure($"Collection {collectionId} not found", ErrorType.NotFound);

            collection.ViewCount++;
            await _dbContext.SaveChangesAsync(ct);

            return Result<ComboCollection>.Success(collection);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get collection");
            return Result<ComboCollection>.Failure($"Query failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Gets collections for a character, optionally including private collections.
    /// </summary>
    public async Task<Result<List<ComboCollection>>> GetCharacterCollectionsAsync(
        string characterName,
        bool includePrivate = false,
        CancellationToken ct = default)
    {
        try
        {
            var query = _dbContext.ComboCollections
                .AsNoTracking()
                .Where(c => c.CharacterName == characterName);

            if (!includePrivate)
                query = query.Where(c => c.IsPublic);

            var collections = await query
                .OrderByDescending(c => c.LikeCount)
                .ToListAsync(ct);

            return Result<List<ComboCollection>>.Success(collections);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get character collections");
            return Result<List<ComboCollection>>.Failure($"Query failed: {ex.Message}", ErrorType.Internal);
        }
    }
}
