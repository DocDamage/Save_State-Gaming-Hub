using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
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
    private readonly ITimeProvider _timeProvider;

    public CreateGameGoalCommandHandler(IGameGoalRepository goalRepository, ITimeProvider timeProvider)
    {
        _goalRepository = goalRepository;
        _timeProvider = timeProvider;
    }

    public async Task<Result<Guid>> Handle(CreateGameGoalCommand request, CancellationToken cancellationToken)
    {
        var gameId = GameId.From(request.GameId);
        var userId = UserId.From(request.UserId);

        var goal = GameGoal.Create(
            gameId,
            userId,
            request.Title,
            _timeProvider,
            request.Description,
            request.TargetValue,
            request.Unit,
            request.DueDate);

        await _goalRepository.AddAsync(goal, cancellationToken).ConfigureAwait(false);

        return Result.Success<Guid>(goal.Id);
    }
}

