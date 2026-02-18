using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Mugen.ComboDatabase;
using SaveState.Core.Mugen.ComboDatabase.Services;

namespace SaveState.Application.Mugen.ComboDatabase.Queries;

/// <summary>
/// Query to get pending combo submissions.
/// </summary>
public sealed record GetPendingSubmissionsQuery(
    int Page = 1,
    int PageSize = 20) : IRequest<Result<List<ComboSubmission>>>;

/// <summary>
/// Handler for GetPendingSubmissionsQuery.
/// </summary>
public sealed class GetPendingSubmissionsQueryHandler : IRequestHandler<GetPendingSubmissionsQuery, Result<List<ComboSubmission>>>
{
    private readonly IComboDatabaseService _comboService;

    public GetPendingSubmissionsQueryHandler(IComboDatabaseService comboService)
    {
        _comboService = comboService;
    }

    public async Task<Result<List<ComboSubmission>>> Handle(GetPendingSubmissionsQuery request, CancellationToken cancellationToken)
    {
        return await _comboService.GetPendingSubmissionsAsync(
            request.Page,
            request.PageSize,
            cancellationToken);
    }
}
