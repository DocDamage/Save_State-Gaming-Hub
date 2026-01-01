namespace SaveState.Application.Input.Commands.Handlers;

using MediatR;
using SaveState.Core.Common;

public class StartVoiceListeningCommandHandler : IRequestHandler<StartVoiceListeningCommand, Result<bool>>
{
    public Task<Result<bool>> Handle(StartVoiceListeningCommand request, CancellationToken ct)
    {
        return Task.FromResult(Result<bool>.Success(true));
    }
}
