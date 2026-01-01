using MediatR;
using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Services;

namespace SaveState.Application.GameLibrary.Commands;

public record AddToBacklogCommand(Guid GameId, int Priority = 50) : IRequest<Result>;

public class AddToBacklogCommandHandler : IRequestHandler<AddToBacklogCommand, Result>
{
    private readonly IBacklogService _backlogService;

    public AddToBacklogCommandHandler(IBacklogService backlogService)
    {
        _backlogService = backlogService;
    }

    public async Task<Result> Handle(AddToBacklogCommand request, CancellationToken ct)
    {
        return await _backlogService.AddToBacklogAsync(request.GameId, request.Priority, ct).ConfigureAwait(false);
    }
}