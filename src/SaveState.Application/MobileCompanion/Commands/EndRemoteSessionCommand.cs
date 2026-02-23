using MediatR;
using SaveState.Core.Common;
using SaveState.Core.MobileCompanion.Services;

namespace SaveState.Application.MobileCompanion.Commands;

public sealed record EndRemoteSessionCommand(Guid SessionId) : IRequest<Result>;

public sealed class EndRemoteSessionCommandHandler : IRequestHandler<EndRemoteSessionCommand, Result>
{
    private readonly IMobileCompanionService _companionService;

    public EndRemoteSessionCommandHandler(IMobileCompanionService companionService)
    {
        _companionService = companionService;
    }

    public async Task<Result> Handle(EndRemoteSessionCommand request, CancellationToken cancellationToken)
    {
        return await _companionService.EndSessionAsync(request.SessionId, cancellationToken)
            .ConfigureAwait(false);
    }
}
