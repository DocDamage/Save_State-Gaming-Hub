using MediatR;
using SaveState.Core.Api.DTOs;
using SaveState.Core.Common;
using SaveState.Core.GameLibrary;

namespace SaveState.Application.Api.Queries;

/// <summary>
/// Handler for getting game library through API.
/// </summary>
public class GetGameLibraryApiQueryHandler : IRequestHandler<GetGameLibraryApiQuery, Result<List<ApiGameDto>>>
{
    private readonly IGameRepository _gameRepository;

    public GetGameLibraryApiQueryHandler(IGameRepository gameRepository)
    {
        _gameRepository = gameRepository;
    }

    public async Task<Result<List<ApiGameDto>>> Handle(GetGameLibraryApiQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var games = await _gameRepository.GetAllAsync(cancellationToken);

            var gameDtos = games.Select(g => new ApiGameDto(
                Id: g.Id,
                Title: g.Title,
                Description: g.Description,
                CoverImagePath: g.CoverImagePath,
                Platform: g.Platform?.Name.ToString(),
                Status: g.Status.ToString(),
                LastPlayedAt: g.LastPlayedAt,
                TotalPlayTime: g.TotalPlayTime.ToString(@"hh\:mm"),
                UserRating: g.UserRating,
                IsCompleted: g.IsCompleted,
                Tags: g.Tags.ToList()
            )).ToList();

            return Result.Success(gameDtos);
        }
        catch (Exception ex)
        {
            return Result.Failure<List<ApiGameDto>>($"Failed to get game library: {ex.Message}");
        }
    }
}

