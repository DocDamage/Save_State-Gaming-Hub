namespace SaveState.Application.GameLibrary.Queries.Handlers;

using MediatR;
using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Services;

public class GetCoachingReportQueryHandler : IRequestHandler<GetCoachingReportQuery, Result<CoachingReport>>
{
    private readonly IAiCoachService _aiCoachService;

    public GetCoachingReportQueryHandler(IAiCoachService aiCoachService)
    {
        _aiCoachService = aiCoachService;
    }

    public async Task<Result<CoachingReport>> Handle(GetCoachingReportQuery request, CancellationToken ct)
    {
        return await _aiCoachService.GenerateSessionReportAsync(request.SessionId, ct);
    }
}