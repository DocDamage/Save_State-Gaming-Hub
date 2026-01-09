using MediatR;
using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Services;

namespace SaveState.Application.GameLibrary.Commands.Handlers;

public class DeleteGameMediaCommandHandler : IRequestHandler<DeleteGameMediaCommand, Result>
{
    private readonly IGameMediaService _mediaService;

    public DeleteGameMediaCommandHandler(IGameMediaService mediaService)
    {
        _mediaService = mediaService;
    }

    public async Task<Result> Handle(DeleteGameMediaCommand request, CancellationToken cancellationToken)
    {
        return await _mediaService.DeleteMediaAsync(request.MediaId, cancellationToken);
    }
}
