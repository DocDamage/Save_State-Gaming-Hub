namespace SaveState.Application.GameLibrary.Queries.Handlers;

using MediatR;
using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Models.AiCoach;
using SaveState.Core.GameLibrary.Services;

/// <summary>
/// Handler for retrieving AI coaching session reports.
/// Provides detailed analysis of gaming performance and improvement suggestions.
/// </summary>
public class GetCoachingReportQueryHandler : IRequestHandler<GetCoachingReportQuery, Result<CoachingReport>>
{
    private readonly IAiCoachService _aiCoachService;

    public GetCoachingReportQueryHandler(IAiCoachService aiCoachService)
    {
        _aiCoachService = aiCoachService;
    }

    /// <summary>
    /// Handles the query to get a coaching session report.
    /// </summary>
    /// <param name="request">The coaching report query with session ID.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result containing the coaching report or an error.</returns>
    public async Task<Result<CoachingReport>> Handle(GetCoachingReportQuery request, CancellationToken ct)
    {
        return await _aiCoachService.GenerateSessionReportAsync(request.SessionId, ct);
    }
}