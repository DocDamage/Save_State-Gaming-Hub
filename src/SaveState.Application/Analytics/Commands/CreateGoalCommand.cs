using MediatR;
using SaveState.Core.Analytics.Entities;
using SaveState.Core.Analytics.Services;
using SaveState.Core.Common;

namespace SaveState.Application.Analytics.Commands;

public sealed record CreateGoalCommand(
    string Title,
    GoalType Type,
    int TargetValue,
    DateOnly? EndDate = null,
    Guid? SpecificGameId = null) : IRequest<Result<GamingGoal>>;

public sealed class CreateGoalCommandHandler : IRequestHandler<CreateGoalCommand, Result<GamingGoal>>
{
    private readonly IGoalService _goalService;

    public CreateGoalCommandHandler(IGoalService goalService)
    {
        _goalService = goalService;
    }

    public async Task<Result<GamingGoal>> Handle(CreateGoalCommand request, CancellationToken ct)
    {
        var createRequest = new CreateGoalRequest(
            request.Title,
            request.Type,
            request.TargetValue,
            request.EndDate,
            request.SpecificGameId);

        return await _goalService.CreateGoalAsync(createRequest, ct);
    }
}