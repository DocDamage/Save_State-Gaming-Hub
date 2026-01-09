using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Common.ValueObjects;
using SaveState.Core.GameLibrary;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.GameLibrary.ValueObjects;

namespace SaveState.Application.GameLibrary.Commands.Handlers;

/// <summary>
/// Handler for creating a new game goal.
/// </summary>
public class CreateGameGoalCommandHandler : IRequestHandler<CreateGameGoalCommand, Result<Guid>>
{
    private readonly IGameGoalRepository _goalRepository;

    public CreateGameGoalCommandHandler(IGameGoalRepository goalRepository)
    {
        _goalRepository = goalRepository;
    }

    public async Task<Result<Guid>> Handle(CreateGameGoalCommand request, CancellationToken cancellationToken)
    {
        var gameId = GameId.From(request.GameId);
        var userId = UserId.From(request.UserId);

        var goal = GameGoal.Create(
            gameId,
            userId,
            request.Title,
            request.Description,
            request.TargetValue,
            request.Unit,
            request.DueDate);

        await _goalRepository.AddAsync(goal, cancellationToken).ConfigureAwait(false);

        return Result.Success<Guid>(goal.Id);
    }
}

