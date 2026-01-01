using MediatR;
using SaveState.Core.Analytics.Entities;
using SaveState.Core.Analytics.Services;
using SaveState.Core.Common;

namespace SaveState.Application.Analytics.Queries;

public sealed record GetCompletedGoalsQuery(int Year) : IRequest<Result<IReadOnlyList<GamingGoal>>>;

public sealed class GetCompletedGoalsQueryHandler : IRequestHandler<GetCompletedGoalsQuery, Result<IReadOnlyList<GamingGoal>>>
{
    private readonly IGoalService _goalService;

    public GetCompletedGoalsQueryHandler(IGoalService goalService)
    {
        _goalService = goalService;
    }

    public async Task<Result<IReadOnlyList<GamingGoal>>> Handle(GetCompletedGoalsQuery request, CancellationToken ct)
    {
        return await _goalService.GetCompletedGoalsAsync(request.Year, ct);
    }
}