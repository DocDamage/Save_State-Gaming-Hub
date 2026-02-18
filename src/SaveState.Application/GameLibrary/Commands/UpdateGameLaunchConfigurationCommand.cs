// Copyright (c) 2026 SaveStateReborn. All rights reserved.

using MediatR;
using SaveState.Application.Common;
using SaveState.Core.Common;
using SaveState.Core.Common.ValueObjects;
using SaveState.Core.GameLibrary;

namespace SaveState.Application.GameLibrary.Commands;

/// <summary>
/// Command to update a game's launch configuration (executable path and arguments).
/// </summary>
public record UpdateGameLaunchConfigurationCommand : IRequest<Result>
{
    public required GameId GameId { get; init; }
    public string? ExecutablePath { get; init; }
    public string? LaunchArguments { get; init; }
}

/// <summary>
/// Handler for updating game launch configuration.
/// </summary>
public sealed class UpdateGameLaunchConfigurationCommandHandler : IRequestHandler<UpdateGameLaunchConfigurationCommand, Result>
{
    private readonly IGameRepository _gameRepository;

    public UpdateGameLaunchConfigurationCommandHandler(IGameRepository gameRepository)
    {
        _gameRepository = gameRepository ?? throw new ArgumentNullException(nameof(gameRepository));
    }

    public async Task<Result> Handle(UpdateGameLaunchConfigurationCommand request, CancellationToken cancellationToken)
    {
        var game = await _gameRepository.GetByIdAsync(request.GameId, cancellationToken);
        if (game == null)
        {
            return Result.Failure($"Game {request.GameId} not found");
        }

        // Update executable path if provided
        if (request.ExecutablePath != null)
        {
            game.SetExecutablePath(request.ExecutablePath);
        }

        // Update launch arguments if provided
        if (request.LaunchArguments != null)
        {
            game.UpdateLaunchConfiguration(request.LaunchArguments);
        }

        await _gameRepository.UpdateAsync(game, cancellationToken);
        return Result.Success();
    }
}
