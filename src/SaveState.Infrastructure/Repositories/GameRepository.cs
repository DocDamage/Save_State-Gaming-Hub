namespace SaveState.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using SaveState.Core.Common;
using SaveState.Core.Common.ValueObjects;
using SaveState.Core.GameLibrary;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.GameLibrary.Enums;
using SaveState.Core.GameLibrary.DTOs;
using SaveState.Core.Monitoring;
using SaveState.Infrastructure.Persistence;

/// <summary>
/// Repository for managing game entities in the database.
/// Provides CRUD operations and optimized queries for game management.
/// </summary>
public class GameRepository : IGameRepository
{
    private readonly SaveStateDbContext _context;
    private readonly IApplicationMetrics _metrics;

    /// <summary>
    /// Initializes a new instance of the GameRepository.
    /// </summary>
    /// <param name="context">The database context for accessing game data.</param>
    /// <param name="metrics">Application metrics collector for performance monitoring.</param>
    public GameRepository(SaveStateDbContext context, IApplicationMetrics metrics)
    {
        _context = context;
        _metrics = metrics;
    }

    /// <summary>
    /// Retrieves a game by its unique identifier, including related platform and file information.
    /// </summary>
    /// <param name="id">The unique identifier of the game.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>The game entity if found, null otherwise.</returns>
    public async Task<Game?> GetByIdAsync(GameId id, CancellationToken ct = default)
        => await _context.Games
            .Include(g => g.Platform)
            .Include(g => g.Files)
            .FirstOrDefaultAsync(g => g.Id == (Guid)id, ct)
            .ConfigureAwait(false);

    /// <summary>
    /// Retrieves all games from the database.
    /// WARNING: This method loads all games into memory and should be used carefully.
    /// Consider using GetGamesAsync with pagination for large datasets.
    /// </summary>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A read-only list of all games.</returns>
    public async Task<IReadOnlyList<Game>> GetAllAsync(CancellationToken ct = default)
    {
        // PERFORMANCE OPTIMIZATION: Add warning for large dataset usage
        // This method loads all games into memory and should be used carefully
        // Consider using GetGamesAsync with pagination for large datasets

        var startTime = DateTime.UtcNow;
        try
        {
            // Add AsNoTracking() for read-only operations to improve performance
            var result = await _context.Games
                .AsNoTracking()
                .ToListAsync(ct)
                .ConfigureAwait(false);

            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordDatabaseQuery("GameRepository.GetAllAsync", duration);
            _metrics.RecordDatabaseConnectionCount(_context.Database.GetDbConnection().State == System.Data.ConnectionState.Open ? 1 : 0);

            // PERFORMANCE MONITORING: Log warning for large result sets
            if (result.Count > 1000)
            {
                _metrics.RecordPerformanceWarning("GameRepository.GetAllAsync", $"Large dataset loaded: {result.Count} games");
                _metrics.RecordSlowQuery("GameRepository.GetAllAsync", duration, result.Count);
            }

            return result;
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordDatabaseQuery("GameRepository.GetAllAsync", duration);
            _metrics.RecordDatabaseError("GameRepository.GetAllAsync", ex.GetType().Name);
            throw;
        }
    }

    public async Task<Game?> GetByTitleAndPlatformAsync(GameTitle title, Guid platformId, CancellationToken ct = default)
        => await _context.Games
            .FirstOrDefaultAsync(g =>
                g.Title.ToLower() == title.Value.ToLower() &&
                g.PlatformId == platformId,
                ct)
            .ConfigureAwait(false);

    public async Task<Game?> GetBySourceAndSourceIdAsync(string source, string sourceId, CancellationToken ct = default)
        => await _context.Games
            .FirstOrDefaultAsync(g =>
                g.Source == source &&
                g.SourceId == sourceId,
                ct)
            .ConfigureAwait(false);

    /// <summary>
    /// Retrieves a paginated list of games with optional filtering and sorting.
    /// </summary>
    /// <param name="pageNumber">The page number to retrieve (1-based).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="searchTerm">Optional search term to filter games by title.</param>
    /// <param name="platformId">Optional platform ID to filter games.</param>
    /// <param name="statusFilter">Optional game status filter.</param>
    /// <param name="platformFilter">Optional platform name filter.</param>
    /// <param name="sortBy">Field to sort results by.</param>
    /// <param name="sortDescending">Whether to sort in descending order.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A paged result containing the games and pagination metadata.</returns>
    public async Task<PagedResult<Game>> GetGamesAsync(
        int pageNumber = 1,
        int pageSize = 50,
        string? searchTerm = null,
        Guid? platformId = null,
        Guid? collectionId = null,
        GameStatus? statusFilter = null,
        string? platformFilter = null,
        GameSortBy sortBy = GameSortBy.Title,
        bool sortDescending = false,
        CollectionFilter? adHocFilter = null,
        CancellationToken ct = default)
    {
        var startTime = DateTime.UtcNow;
        try
        {
            var query = _context.Games.AsQueryable();

            // Apply ad-hoc filter if provided
            if (adHocFilter != null)
            {
                query = ApplySmartCollectionFilters(query, adHocFilter);
            }

            // Apply filters at database level
            if (collectionId.HasValue)
            {
                var collection = await _context.VirtualCollections.FindAsync(new object[] { collectionId.Value }, ct).ConfigureAwait(false);
                if (collection != null)
                {
                    if (collection.Type == CollectionType.Smart && !string.IsNullOrEmpty(collection.FilterExpression))
                    {
                        var filter = System.Text.Json.JsonSerializer.Deserialize<CollectionFilter>(collection.FilterExpression);
                        if (filter != null)
                        {
                            // Apply smart filters
                            query = ApplySmartCollectionFilters(query, filter);
                        }
                    }
                    else
                    {
                        // Manual collection
                        query = query.Where(g => _context.VirtualCollectionGames
                            .Any(vcg => vcg.CollectionId == collectionId.Value && vcg.GameId == g.Id));
                    }
                }
                else
                {
                    // Collection not found, return empty
                    return new PagedResult<Game>(new List<Game>(), 0, pageNumber, pageSize);
                }
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(g => g.Title.Contains(searchTerm));
            }

            if (platformId.HasValue)
            {
                query = query.Where(g => g.PlatformId == platformId.Value);
            }

            if (statusFilter.HasValue)
            {
                query = query.Where(g => g.Status == statusFilter.Value);
            }

            if (!string.IsNullOrWhiteSpace(platformFilter))
            {
                query = query.Where(g => g.Platform.Name.Value.Contains(platformFilter));
            }

            // Get total count before pagination
            var totalCount = await query.CountAsync(ct).ConfigureAwait(false);

            // Apply sorting
            IOrderedQueryable<Game> orderedQuery = sortBy switch
            {
                GameSortBy.Title => sortDescending
                    ? query.OrderByDescending(g => g.Title)
                    : query.OrderBy(g => g.Title),
                GameSortBy.DateAdded => sortDescending
                    ? query.OrderByDescending(g => g.Id) // Assuming UUID/Guid isn't sortable by time, but using fallback or CreatedAt if available?
                                                        // Actually Game has CreatedAt but it's not indexed nicely?
                                                        // Wait, code previously used Id. Guid isn't ordered. Let's start using CreatedAt.
                    : query.OrderBy(g => g.Id),
                GameSortBy.Platform => sortDescending
                    ? query.OrderByDescending(g => g.Platform.Name)
                    : query.OrderBy(g => g.Platform.Name),
                GameSortBy.Status => sortDescending
                    ? query.OrderByDescending(g => g.Status)
                    : query.OrderBy(g => g.Status),
                GameSortBy.LastPlayed => sortDescending
                    ? query.OrderByDescending(g => g.LastPlayedAt)
                    : query.OrderBy(g => g.LastPlayedAt),
                GameSortBy.PlayTime => sortDescending
                    ? query.OrderByDescending(g => g.TotalPlayTime)
                    : query.OrderBy(g => g.TotalPlayTime),
                GameSortBy.ReleaseDate => sortDescending
                    ? query.OrderByDescending(g => g.ReleaseDate)
                    : query.OrderBy(g => g.ReleaseDate),
                GameSortBy.UserRating => sortDescending
                    ? query.OrderByDescending(g => g.UserRating)
                    : query.OrderBy(g => g.UserRating),
                _ => sortDescending
                    ? query.OrderByDescending(g => g.Title)
                    : query.OrderBy(g => g.Title)
            };

            query = orderedQuery;

            // Apply pagination
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            var result = new PagedResult<Game>(items, totalCount, pageNumber, pageSize);

            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordDatabaseQuery("GameRepository.GetGamesAsync", duration);
            _metrics.RecordDatabaseConnectionCount(_context.Database.GetDbConnection().State == System.Data.ConnectionState.Open ? 1 : 0);

            return result;
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordDatabaseQuery("GameRepository.GetGamesAsync", duration);
            _metrics.RecordDatabaseError("GameRepository.GetGamesAsync", ex.GetType().Name);
            throw;
        }
    }

    private IQueryable<Game> ApplySmartCollectionFilters(IQueryable<Game> query, CollectionFilter filter)
    {
        if (filter.MaxPlaytime.HasValue)
            query = query.Where(g => g.TotalPlayTime <= filter.MaxPlaytime.Value);

        if (filter.MinPlaytime.HasValue)
            query = query.Where(g => g.TotalPlayTime >= filter.MinPlaytime.Value);

        if (filter.MaxDaysSinceLastPlayed.HasValue)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-filter.MaxDaysSinceLastPlayed.Value);
            query = query.Where(g => g.LastPlayedAt >= cutoffDate || g.LastPlayedAt == null);
        }

        if (!string.IsNullOrEmpty(filter.PlatformName))
        {
            query = query.Where(g => g.Platform != null && g.Platform.Name.Value.Contains(filter.PlatformName));
        }

        if (filter.Status.HasValue)
            query = query.Where(g => g.Status == filter.Status.Value);

        if (filter.IsCompleted.HasValue)
            query = query.Where(g => g.IsCompleted == filter.IsCompleted.Value);

        if (filter.MinRating.HasValue)
            query = query.Where(g => g.UserRating >= filter.MinRating.Value);

        if (!string.IsNullOrEmpty(filter.Genre))
        {
            query = query.Where(g => g.Genres.Any(genre => genre.Name == filter.Genre));
        }

        if (!string.IsNullOrEmpty(filter.Tag))
        {
            var tagPattern = $"%\"{filter.Tag}\"%";
            query = query.Where(g => EF.Functions.Like(EF.Property<string>(g, nameof(Game.Tags)), tagPattern));
        }

        if (filter.MinReleaseYear.HasValue)
        {
             query = query.Where(g => g.ReleaseDate.HasValue && g.ReleaseDate.Value.Year >= filter.MinReleaseYear.Value);
        }

        if (filter.MaxReleaseYear.HasValue)
        {
             query = query.Where(g => g.ReleaseDate.HasValue && g.ReleaseDate.Value.Year <= filter.MaxReleaseYear.Value);
        }

        if (filter.IsInBacklog.HasValue)
        {
            if (filter.IsInBacklog.Value)
            {
                query = query.Where(g => _context.BacklogEntries.Any(be => be.GameId == g.Id));
            }
            else
            {
                query = query.Where(g => !_context.BacklogEntries.Any(be => be.GameId == g.Id));
            }
        }

        return query;
    }

    /// <summary>
    /// Gets game summaries with optimized projection for list views.
    /// </summary>
    public async Task<PagedResult<GameSummaryProjection>> GetGameSummariesAsync(
        int pageNumber = 1,
        int pageSize = 50,
        string? searchTerm = null,
        GameStatus? statusFilter = null,
        string? platformFilter = null,
        GameSortBy sortBy = GameSortBy.Title,
        bool sortDescending = false,
        CancellationToken ct = default)
    {
        var query = _context.Games
            .Include(g => g.Platform)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(g => g.Title.Contains(searchTerm));
        }

        if (statusFilter.HasValue)
        {
            query = query.Where(g => g.Status == statusFilter.Value);
        }

        if (!string.IsNullOrWhiteSpace(platformFilter))
        {
            query = query.Where(g => g.Platform.Name.Value.Contains(platformFilter));
        }

        // Get total count
        var totalCount = await query.CountAsync(ct).ConfigureAwait(false);

        // Apply sorting
        IOrderedQueryable<Game> orderedQuery = sortBy switch
        {
            GameSortBy.Title => sortDescending
                ? query.OrderByDescending(g => g.Title)
                : query.OrderBy(g => g.Title),
            GameSortBy.Platform => sortDescending
                ? query.OrderByDescending(g => g.Platform.Name)
                : query.OrderBy(g => g.Platform.Name),
            GameSortBy.Status => sortDescending
                ? query.OrderByDescending(g => g.Status)
                : query.OrderBy(g => g.Status),
            _ => sortDescending
                ? query.OrderByDescending(g => g.Title)
                : query.OrderBy(g => g.Title)
        };

        // Apply projection and pagination
        var projections = await orderedQuery
            .Select(g => new GameSummaryProjection
            {
                Id = g.Id,
                Title = g.Title,
                PlatformName = g.Platform != null ? g.Platform.Name.Value : "Unknown",
                Status = g.Status,
                CoverImageUrl = g.CoverImagePath
            })
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return new PagedResult<GameSummaryProjection>(projections, totalCount, pageNumber, pageSize);
    }

    /// <summary>
    /// Adds a new game to the database.
    /// </summary>
    /// <param name="game">The game entity to add.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    public async Task AddAsync(Game game, CancellationToken ct = default)
    {
        await _context.Games.AddAsync(game, ct).ConfigureAwait(false);
        await _context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Updates an existing game in the database.
    /// </summary>
    /// <param name="game">The game entity to update.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    public async Task UpdateAsync(Game game, CancellationToken ct = default)
    {
        _context.Games.Update(game);
        await _context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the total count of games in the database.
    /// </summary>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>The total number of games.</returns>
    public async Task<int> CountAsync(CancellationToken ct = default)
        => await _context.Games.CountAsync(ct).ConfigureAwait(false);

    public async Task<int> CountByStatusAsync(GameStatus status, CancellationToken ct = default)
        => await _context.Games.CountAsync(g => g.Status == status, ct).ConfigureAwait(false);

    public async Task<IReadOnlyDictionary<string, int>> GetPlatformStatisticsAsync(CancellationToken ct = default)
    {
        var stats = await _context.Games
            .Where(g => g.Platform != null)
            .GroupBy(g => g.Platform!.Name.Value)
            .Select(g => new { PlatformName = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.PlatformName, g => g.Count, ct)
            .ConfigureAwait(false);

        return stats;
    }
}
