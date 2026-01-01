using MediatR;
using SaveState.Core.Assistant.Services;
using SaveState.Core.Common;

namespace SaveState.Application.Assistant.Commands;

public sealed record GetWalkthroughHintCommand(
    Guid GameId,
    string CurrentLocation,
    bool AvoidSpoilers = true) : IRequest<Result<string>>;

public sealed class GetWalkthroughHintCommandHandler : IRequestHandler<GetWalkthroughHintCommand, Result<string>>
{
    private readonly IGameAssistantService _assistantService;

    public GetWalkthroughHintCommandHandler(IGameAssistantService assistantService)
    {
        _assistantService = assistantService;
    }

    public async Task<Result<string>> Handle(GetWalkthroughHintCommand request, CancellationToken ct)
    {
        return await _assistantService.GetWalkthroughHintAsync(
            request.GameId,
            request.CurrentLocation,
            request.AvoidSpoilers,
            ct);
    }
}