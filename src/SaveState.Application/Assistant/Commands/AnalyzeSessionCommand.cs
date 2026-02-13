using MediatR;
using SaveState.Core.Assistant.Services;
using SaveState.Core.Common;

namespace SaveState.Application.Assistant.Commands;

public sealed record AnalyzeSessionCommand(SessionContext Context) : IRequest<Result<AssistantRecommendation>>;

public sealed class AnalyzeSessionCommandHandler : IRequestHandler<AnalyzeSessionCommand, Result<AssistantRecommendation>>
{
    private readonly IGameAssistantService _assistantService;

    public AnalyzeSessionCommandHandler(IGameAssistantService assistantService)
    {
        _assistantService = assistantService;
    }

    public async Task<Result<AssistantRecommendation>> Handle(AnalyzeSessionCommand request, CancellationToken ct)
    {
        return await _assistantService.AnalyzeSessionAsync(request.Context, ct);
    }
}
