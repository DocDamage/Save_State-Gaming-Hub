namespace SaveState.Application.Input.Commands.Handlers;

using MediatR;
using SaveState.Core.Common;
using SaveState.Application.Input.Commands;

public class StopVoiceListeningCommandHandler : IRequestHandler<StopVoiceListeningCommand, Result<bool>>
{
    public Task<Result<bool>> Handle(StopVoiceListeningCommand request, CancellationToken ct)
    {
        return Task.FromResult(Result<bool>.Success(true));
    }
}
