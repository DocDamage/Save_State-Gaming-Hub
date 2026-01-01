using MediatR;
using SaveState.Core.Assistant.Services;
using SaveState.Core.Common;

namespace SaveState.Application.Assistant.Commands;

public sealed record ClearAssistantContextCommand(Guid GameId) : IRequest<Result>;

public sealed class ClearAssistantContextCommandHandler : IRequestHandler<ClearAssistantContextCommand, Result>
{
    private readonly IGameAssistantService _assistantService;

    public ClearAssistantContextCommandHandler(IGameAssistantService assistantService)
    {
        _assistantService = assistantService;
    }

    public async Task<Result> Handle(ClearAssistantContextCommand request, CancellationToken ct)
    {
        return await _assistantService.ClearContextAsync(request.GameId, ct);
    }
}