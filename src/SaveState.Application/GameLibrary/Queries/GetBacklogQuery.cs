using MediatR;
using SaveState.Application.Common;
using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.GameLibrary.Services;

namespace SaveState.Application.GameLibrary.Queries;

public record GetBacklogQuery(
    int PageNumber = 1,
    int PageSize = 50,
    BacklogStatus? Status = null) : IRequest<Result<PagedResult<BacklogEntry>>>;

public class GetBacklogQueryHandler : IRequestHandler<GetBacklogQuery, Result<PagedResult<BacklogEntry>>>
{
    private readonly IBacklogService _backlogService;

    public GetBacklogQueryHandler(IBacklogService backlogService)
    {
        _backlogService = backlogService;
    }

    public async Task<Result<PagedResult<BacklogEntry>>> Handle(GetBacklogQuery request, CancellationToken ct)
    {
        return await _backlogService.GetBacklogAsync(
            request.PageNumber,
            request.PageSize,
            request.Status,
            ct).ConfigureAwait(false);
    }
}