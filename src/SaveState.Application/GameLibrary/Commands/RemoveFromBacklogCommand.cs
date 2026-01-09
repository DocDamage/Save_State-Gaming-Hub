using MediatR;
using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Services;

namespace SaveState.Application.GameLibrary.Commands;

public record RemoveFromBacklogCommand(Guid GameId) : IRequest<Result>;

public class RemoveFromBacklogCommandHandler : IRequestHandler<RemoveFromBacklogCommand, Result>
{
    private readonly IBacklogService _backlogService;

    public RemoveFromBacklogCommandHandler(IBacklogService backlogService)
    {
        _backlogService = backlogService;
    }

    public async Task<Result> Handle(RemoveFromBacklogCommand request, CancellationToken ct)
    {
        return await _backlogService.RemoveFromBacklogAsync(request.GameId, ct).ConfigureAwait(false);
    }
}
