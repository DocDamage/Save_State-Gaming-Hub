using MediatR;
using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.GameLibrary.Services;

namespace SaveState.Application.GameLibrary.Commands;

public record UpdateBacklogStatusCommand(Guid GameId, BacklogStatus Status) : IRequest<Result>;

public class UpdateBacklogStatusCommandHandler : IRequestHandler<UpdateBacklogStatusCommand, Result>
{
    private readonly IBacklogService _backlogService;

    public UpdateBacklogStatusCommandHandler(IBacklogService backlogService)
    {
        _backlogService = backlogService;
    }

    public async Task<Result> Handle(UpdateBacklogStatusCommand request, CancellationToken ct)
    {
        return await _backlogService.UpdateBacklogStatusAsync(request.GameId, request.Status, ct).ConfigureAwait(false);
    }
}