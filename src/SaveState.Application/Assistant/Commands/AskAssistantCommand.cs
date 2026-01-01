using MediatR;
using SaveState.Core.Assistant.Services;
using SaveState.Core.Common;

namespace SaveState.Application.Assistant.Commands;

public sealed record AskAssistantCommand(Guid GameId, string Question) : IRequest<Result<AssistantResponse>>;

public sealed class AskAssistantCommandHandler : IRequestHandler<AskAssistantCommand, Result<AssistantResponse>>
{
    private readonly IGameAssistantService _assistantService;

    public AskAssistantCommandHandler(IGameAssistantService assistantService)
    {
        _assistantService = assistantService;
    }

    public async Task<Result<AssistantResponse>> Handle(AskAssistantCommand request, CancellationToken ct)
    {
        return await _assistantService.AskAsync(request.GameId, request.Question, ct);
    }
}