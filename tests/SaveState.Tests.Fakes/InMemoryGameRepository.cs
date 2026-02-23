using System.Collections.Concurrent;
using SaveState.Core.Common;
using SaveState.Core.Common.ValueObjects;
using SaveState.Core.GameLibrary;
using SaveState.Core.GameLibrary.DTOs;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.GameLibrary.Enums;

namespace SaveState.Tests.Fakes;

/// <summary>
/// In-memory implementation of IGameRepository for integration testing.
/// Provides thread-safe CRUD operations and query capabilities for game entities.
/// </summary>
public class InMemoryGameRepository : IGameRepository
{
    private readonly ConcurrentDictionary<Guid, Game> _games = new();
    private readonly ConcurrentDictionary<string, Game> _sourceKeyIndex = new();

    #region Retrieval

    public Task<Game?> GetByIdAsync(GameId id, CancellationToken ct = default)
    {
        _games.TryGetValue((Guid)id, out var game);
        return Task.FromResult(game);
    }

    public Task<IReadOnlyList<Game>> GetAllAsync(CancellationToken ct = default)
    {
        var games = _games.Values.ToList();
        return Task.FromResult<IReadOnlyList<Game>>(games);
    }

    public Task<PagedResult<Game>> GetGamesAsync(
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
        var query = _games.Values.AsEnumerable();

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(g => g.Title.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
        }

        // Apply platform filter
        if (platformId.HasValue)
        {
            query = query.Where(g => g.PlatformId == platformId.Value);
        }

        // Apply status filter
        if (statusFilter.HasValue)
        {
            query = query.Where(g => g.Status == statusFilter.Value);
        }

        // Apply sorting
        query = sortBy switch
        {
            GameSortBy.Title => sortDescending ? query.OrderByDescending(g => g.Title) : query.OrderBy(g => g.Title),
            GameSortBy.ReleaseDate => sortDescending ? query.OrderByDescending(g => g.ReleaseDate) : query.OrderBy(g => g.ReleaseDate),
            GameSortBy.DateAdded => sortDescending ? query.OrderByDescending(g => g.CreatedAt) : query.OrderBy(g => g.CreatedAt),
            GameSortBy.LastPlayed => sortDescending ? query.OrderByDescending(g => g.LastPlayedAt) : query.OrderBy(g => g.LastPlayedAt),
            GameSortBy.PlayTime => sortDescending ? query.OrderByDescending(g => g.TotalPlayTime) : query.OrderBy(g => g.TotalPlayTime),
            GameSortBy.Platform => sortDescending ? query.OrderByDescending(g => g.Platform?.Name.Value) : query.OrderBy(g => g.Platform?.Name.Value),
            GameSortBy.Status => sortDescending ? query.OrderByDescending(g => g.Status) : query.OrderBy(g => g.Status),
            GameSortBy.UserRating => sortDescending ? query.OrderByDescending(g => g.UserRating) : query.OrderBy(g => g.UserRating),
            _ => sortDescending ? query.OrderByDescending(g => g.Title) : query.OrderBy(g => g.Title)
        };

        var totalCount = query.Count();
        var items = query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Task.FromResult(new PagedResult<Game>(items, totalCount, pageNumber, pageSize));
    }

    public Task<PagedResult<GameSummaryProjection>> GetGameSummariesAsync(
        int pageNumber = 1,
        int pageSize = 50,
        string? searchTerm = null,
        GameStatus? statusFilter = null,
        string? platformFilter = null,
        GameSortBy sortBy = GameSortBy.Title,
        bool sortDescending = false,
        CancellationToken ct = default)
    {
        var query = _games.Values.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(g => g.Title.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
        }

        if (statusFilter.HasValue)
        {
            query = query.Where(g => g.Status == statusFilter.Value);
        }

        query = sortBy switch
        {
            GameSortBy.Title => sortDescending ? query.OrderByDescending(g => g.Title) : query.OrderBy(g => g.Title),
            GameSortBy.DateAdded => sortDescending ? query.OrderByDescending(g => g.CreatedAt) : query.OrderBy(g => g.CreatedAt),
            GameSortBy.LastPlayed => sortDescending ? query.OrderByDescending(g => g.LastPlayedAt) : query.OrderBy(g => g.LastPlayedAt),
            _ => sortDescending ? query.OrderByDescending(g => g.Title) : query.OrderBy(g => g.Title)
        };

        var totalCount = query.Count();
        var items = query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(g => new GameSummaryProjection
            {
                Id = (Guid)g.Id!,
                Title = g.Title,
                CoverImageUrl = g.CoverImagePath,
                PlatformName = g.Platform?.Name.Value ?? "Unknown",
                Status = g.Status
            })
            .ToList();

        return Task.FromResult(new PagedResult<GameSummaryProjection>(items, totalCount, pageNumber, pageSize));
    }

    public Task<Game?> GetByTitleAndPlatformAsync(GameTitle title, Guid platformId, CancellationToken ct = default)
    {
        var game = _games.Values
            .FirstOrDefault(g => g.Title.Equals(title.Value, StringComparison.OrdinalIgnoreCase)
                              && g.PlatformId == platformId);
        return Task.FromResult(game);
    }

    public Task<Game?> GetBySourceAndSourceIdAsync(string source, string sourceId, CancellationToken ct = default)
    {
        var key = $"{source}:{sourceId}";
        _sourceKeyIndex.TryGetValue(key, out var game);
        return Task.FromResult(game);
    }

    public Task<IReadOnlyList<Game>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
    {
        var idSet = ids.ToHashSet();
        var games = _games.Values
            .Where(g => idSet.Contains((Guid)g.Id!))
            .ToList();
        return Task.FromResult<IReadOnlyList<Game>>(games);
    }

    #endregion

    #region Modification

    public Task AddAsync(Game game, CancellationToken ct = default)
    {
        if (game.Id == null)
        {
            throw new InvalidOperationException("Game must have an ID");
        }

        var id = (Guid)game.Id;
        _games[id] = game;

        // Index by source if available
        var sourceKey = GetSourceKey(game);
        if (sourceKey != null)
        {
            _sourceKeyIndex[sourceKey] = game;
        }

        return Task.CompletedTask;
    }

    public Task UpdateAsync(Game game, CancellationToken ct = default)
    {
        if (game.Id == null)
        {
            throw new InvalidOperationException("Game must have an ID");
        }

        var id = (Guid)game.Id;
        if (!_games.ContainsKey(id))
        {
            throw new InvalidOperationException($"Game with ID {id} not found");
        }

        _games[id] = game;

        // Update source index
        var sourceKey = GetSourceKey(game);
        if (sourceKey != null)
        {
            _sourceKeyIndex[sourceKey] = game;
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(GameId id, CancellationToken ct = default)
    {
        var gameId = (Guid)id;

        if (_games.TryRemove(gameId, out var game))
        {
            // Remove from source index
            var sourceKey = GetSourceKey(game);
            if (sourceKey != null)
            {
                _sourceKeyIndex.TryRemove(sourceKey, out _);
            }
        }

        return Task.CompletedTask;
    }

    #endregion

    #region Statistics

    public Task<int> CountAsync(CancellationToken ct = default)
    {
        return Task.FromResult(_games.Count);
    }

    public Task<int> CountByStatusAsync(GameStatus status, CancellationToken ct = default)
    {
        var count = _games.Values.Count(g => g.Status == status);
        return Task.FromResult(count);
    }

    public Task<IReadOnlyDictionary<string, int>> GetPlatformStatisticsAsync(CancellationToken ct = default)
    {
        var stats = _games.Values
            .GroupBy(g => g.Platform?.Name.Value ?? "Unknown")
            .ToDictionary(g => g.Key, g => g.Count())
            as IReadOnlyDictionary<string, int>;

        return Task.FromResult(stats);
    }

    #endregion

    #region Helper Methods

    private static string? GetSourceKey(Game game)
    {
        if (!string.IsNullOrEmpty(game.Source) && !string.IsNullOrEmpty(game.SourceId))
        {
            return $"{game.Source}:{game.SourceId}";
        }
        return null;
    }

    /// <summary>
    /// Clears all games from the repository. Useful for test cleanup.
    /// </summary>
    public void Clear()
    {
        _games.Clear();
        _sourceKeyIndex.Clear();
    }

    #endregion
}
