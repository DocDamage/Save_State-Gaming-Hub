using MediatR;
using SaveState.Core.Analytics.Services;
using SaveState.Core.Common;

namespace SaveState.Application.Analytics.Commands;

public sealed record CancelGoalCommand(Guid GoalId) : IRequest<Result>;

public sealed class CancelGoalCommandHandler : IRequestHandler<CancelGoalCommand, Result>
{
    private readonly IGoalService _goalService;

    public CancelGoalCommandHandler(IGoalService goalService)
    {
        _goalService = goalService;
    }

    public async Task<Result> Handle(CancelGoalCommand request, CancellationToken ct)
    {
        return await _goalService.CancelGoalAsync(request.GoalId, ct);
    }
}