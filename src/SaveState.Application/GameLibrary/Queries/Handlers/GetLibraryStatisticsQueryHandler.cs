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
        var games = await _gameRepository.GetAllAsync(ct).ConfigureAwait(false);

        // Filter out hidden/deleted games if requested
        if (!request.IncludeHidden)
        {
            games = games.Where(g => !g.IsDeleted).ToList();
        }

        var stats = new LibraryStatisticsDto
        {
            TotalGames = games.Count,
            InstalledGames = games.Count(g => g.Status == GameStatus.Installed),
            RunningGames = games.Count(g => g.Status == GameStatus.Running),
            GamesByStatus = games
                .GroupBy(g => g.Status)
                .ToDictionary(g => g.Key, g => g.Count()),
            GamesByPlatform = games
                .Where(g => g.Platform != null)
                .GroupBy(g => g.Platform!.Name.Value)
                .ToDictionary(g => g.Key, g => g.Count()),
            TotalDiskSpaceUsed = CalculateTotalDiskSpace(games),
            TotalPlayTime = TimeSpan.Zero, // Would be tracked separately
            LastGameAdded = games.Any() ? games.Max(g => g.CreatedAt) : null
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
