using MediatR;
using SaveState.Application.Common;
using SaveState.Core.Common;
using SaveState.Application.GameLibrary.DTOs;
using SaveState.Core.GameLibrary;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.GameLibrary.Enums;

namespace SaveState.Application.GameLibrary.Queries.Handlers;

public class GetLibraryStatisticsQueryHandler : IRequestHandler<GetLibraryStatisticsQuery, Result<LibraryStatisticsDto>>
{
    private readonly IGameRepository _gameRepository;

    public GetLibraryStatisticsQueryHandler(IGameRepository gameRepository)
    {
        _gameRepository = gameRepository;
    }

    public async Task<Result<LibraryStatisticsDto>> Handle(GetLibraryStatisticsQuery request, CancellationToken ct)
    {
        // Get efficient aggregate data from the database
        var totalGames = await _gameRepository.CountAsync(ct).ConfigureAwait(false);
        var platformStats = await _gameRepository.GetPlatformStatisticsAsync(ct).ConfigureAwait(false);

        // For status-based statistics, we still need some data but can be more selective
        // This is a compromise - we could add more aggregate methods if needed
        var gamesForStatusStats = await _gameRepository.GetGamesAsync(
            pageNumber: 1,
            pageSize: 1000, // Reasonable limit for status statistics
            ct: ct).ConfigureAwait(false);

        var gamesList = gamesForStatusStats.Items;

        // Filter out hidden/deleted games if requested
        if (!request.IncludeHidden)
        {
            gamesList = gamesList.Where(g => !g.IsDeleted).ToList();
        }

        var stats = new LibraryStatisticsDto
        {
            TotalGames = request.IncludeHidden ? totalGames : gamesList.Count,
            InstalledGames = gamesList.Count(g => g.Status == GameStatus.Installed),
            RunningGames = gamesList.Count(g => g.Status == GameStatus.Running),
            GamesByStatus = gamesList
                .GroupBy(g => g.Status)
                .ToDictionary(g => g.Key, g => g.Count()),
            GamesByPlatform = platformStats.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
            TotalDiskSpaceUsed = CalculateTotalDiskSpace(gamesList),
            TotalPlayTime = TimeSpan.Zero, // Would be tracked separately
            LastGameAdded = gamesList.Any() ? gamesList.Max(g => g.CreatedAt) : null
        };

        return Result<LibraryStatisticsDto>.Success(stats);
    }

    private static long CalculateTotalDiskSpace(IEnumerable<Game> games)
    {
        // This is a simplified calculation - in reality, you'd scan actual file sizes
        long totalSize = 0;

        foreach (var game in games)
        {
            // Add size of game files
            foreach (var file in game.Files)
            {
                totalSize += file.FileSize ?? 0;
            }

            // Estimate additional space for the game installation
            if (!string.IsNullOrEmpty(game.InstallPath))
            {
                // Rough estimate: add 100MB per installed game
                totalSize += 100 * 1024 * 1024;
            }
        }

        return totalSize;
    }
}
