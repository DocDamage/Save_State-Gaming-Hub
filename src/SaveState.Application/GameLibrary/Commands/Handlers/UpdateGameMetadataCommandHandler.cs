using MediatR;
using Microsoft.Extensions.Logging;
using SaveState.Application.Common;
using SaveState.Core.Common.ValueObjects;
using SaveState.Core.GameLibrary;

namespace SaveState.Application.GameLibrary.Commands.Handlers;

/// <summary>
/// Handler for updating game metadata.
/// Modifies game information like titles, descriptions, and other metadata.
/// </summary>
public class UpdateGameMetadataCommandHandler : IRequestHandler<UpdateGameMetadataCommand, Result>
{
    private readonly IGameRepository _gameRepository;
    private readonly ILogger<UpdateGameMetadataCommandHandler> _logger;

    public UpdateGameMetadataCommandHandler(
        IGameRepository gameRepository,
        ILogger<UpdateGameMetadataCommandHandler> logger)
    {
        _gameRepository = gameRepository;
        _logger = logger;
    }

    /// <summary>
    /// Handles the command to update game metadata.
    /// </summary>
    /// <param name="request">The update game metadata command with new metadata.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result indicating success or failure.</returns>
    public async Task<Result> Handle(UpdateGameMetadataCommand request, CancellationToken ct)
    {
        var game = await _gameRepository.GetByIdAsync(request.GameId, ct).ConfigureAwait(false);
        if (game is null)
            return Result.Failure("Game not found");

        try
        {
            game.Update(null, request.Description, request.CoverImageUrl);
            // Note: Tags not yet implemented in Game entity
            await _gameRepository.UpdateAsync(game, ct).ConfigureAwait(false);

            _logger.LogInformation("Updated metadata for game {GameId}: {Title}",
                game.Id, game.Title);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update metadata for game {GameId}", request.GameId);
            return Result.Failure($"Failed to update game metadata: {ex.Message}");
        }
    }
}
