using MediatR;
using SaveState.Core.Assistant.Services;
using SaveState.Core.Common;

namespace SaveState.Application.Assistant.Commands;

public sealed record AnalyzeDifficultyCommand(Guid GameId, GameplayMetrics Metrics) : IRequest<Result<DifficultySuggestion>>;

public sealed class AnalyzeDifficultyCommandHandler : IRequestHandler<AnalyzeDifficultyCommand, Result<DifficultySuggestion>>
{
    private readonly IGameAssistantService _assistantService;

    public AnalyzeDifficultyCommandHandler(IGameAssistantService assistantService)
    {
        _assistantService = assistantService;
    }

    public async Task<Result<DifficultySuggestion>> Handle(AnalyzeDifficultyCommand request, CancellationToken ct)
    {
        return await _assistantService.AnalyzeDifficultyAsync(request.GameId, request.Metrics, ct);
    }
}
