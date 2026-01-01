using MediatR;
using SaveState.Core.Assistant.Services;
using SaveState.Core.Common;

namespace SaveState.Application.Assistant.Commands;

public sealed record GetQuickTipsCommand(Guid GameId) : IRequest<Result<IReadOnlyList<string>>>;

public sealed class GetQuickTipsCommandHandler : IRequestHandler<GetQuickTipsCommand, Result<IReadOnlyList<string>>>
{
    private readonly IGameAssistantService _assistantService;

    public GetQuickTipsCommandHandler(IGameAssistantService assistantService)
    {
        _assistantService = assistantService;
    }

    public async Task<Result<IReadOnlyList<string>>> Handle(GetQuickTipsCommand request, CancellationToken ct)
    {
        return await _assistantService.GetQuickTipsAsync(request.GameId, ct);
    }
}