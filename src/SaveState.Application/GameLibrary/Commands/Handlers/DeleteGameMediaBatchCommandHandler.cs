using MediatR;
using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Services;

namespace SaveState.Application.GameLibrary.Commands.Handlers;

public class DeleteGameMediaBatchCommandHandler : IRequestHandler<DeleteGameMediaBatchCommand, Result>
{
    private readonly IGameMediaService _mediaService;

    public DeleteGameMediaBatchCommandHandler(IGameMediaService mediaService)
    {
        _mediaService = mediaService;
    }

    public async Task<Result> Handle(DeleteGameMediaBatchCommand request, CancellationToken cancellationToken)
    {
        return await _mediaService.DeleteMediaBatchAsync(request.MediaIds, cancellationToken);
    }
}
