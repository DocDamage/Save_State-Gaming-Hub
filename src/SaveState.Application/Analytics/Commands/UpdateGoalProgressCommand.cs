using MediatR;
using SaveState.Core.Analytics.Services;
using SaveState.Core.Common;

namespace SaveState.Application.Analytics.Commands;

public sealed record UpdateGoalProgressCommand : IRequest<Result>;

public sealed class UpdateGoalProgressCommandHandler : IRequestHandler<UpdateGoalProgressCommand, Result>
{
    private readonly IGoalService _goalService;

    public UpdateGoalProgressCommandHandler(IGoalService goalService)
    {
        _goalService = goalService;
    }

    public async Task<Result> Handle(UpdateGoalProgressCommand request, CancellationToken ct)
    {
        return await _goalService.UpdateProgressAsync(ct);
    }
}