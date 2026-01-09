using MediatR;
using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Services;

namespace SaveState.Application.GameLibrary.Commands.Handlers;

public class UpdateGameMediaCommandHandler : IRequestHandler<UpdateGameMediaCommand, Result>
{
    private readonly IGameMediaService _mediaService;

    public UpdateGameMediaCommandHandler(IGameMediaService mediaService)
    {
        _mediaService = mediaService;
    }

    public async Task<Result> Handle(UpdateGameMediaCommand request, CancellationToken cancellationToken)
    {
        return await _mediaService.UpdateMediaAsync(
            request.MediaId,
            request.Title,
            request.Description,
            request.IsFavorite,
            cancellationToken);
    }
}
