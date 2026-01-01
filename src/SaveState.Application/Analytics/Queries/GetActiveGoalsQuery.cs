using MediatR;
using SaveState.Core.Analytics.Entities;
using SaveState.Core.Analytics.Services;
using SaveState.Core.Common;

namespace SaveState.Application.Analytics.Queries;

public sealed record GetActiveGoalsQuery : IRequest<Result<IReadOnlyList<GamingGoal>>>;

public sealed class GetActiveGoalsQueryHandler : IRequestHandler<GetActiveGoalsQuery, Result<IReadOnlyList<GamingGoal>>>
{
    private readonly IGoalService _goalService;

    public GetActiveGoalsQueryHandler(IGoalService goalService)
    {
        _goalService = goalService;
    }

    public async Task<Result<IReadOnlyList<GamingGoal>>> Handle(GetActiveGoalsQuery request, CancellationToken ct)
    {
        return await _goalService.GetActiveGoalsAsync(ct);
    }
}