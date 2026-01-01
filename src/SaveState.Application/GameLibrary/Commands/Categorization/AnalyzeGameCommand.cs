using MediatR;
using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Services;

namespace SaveState.Application.GameLibrary.Commands.Categorization;

public sealed record AnalyzeGameCommand(Guid GameId) : IRequest<Result<GameTags>>;

public sealed class AnalyzeGameCommandHandler : IRequestHandler<AnalyzeGameCommand, Result<GameTags>>
{
    private readonly ISmartCategorizationService _categorizationService;

    public AnalyzeGameCommandHandler(ISmartCategorizationService categorizationService)
    {
        _categorizationService = categorizationService;
    }

    public async Task<Result<GameTags>> Handle(AnalyzeGameCommand request, CancellationToken ct)
    {
        return await _categorizationService.AnalyzeGameAsync(request.GameId, ct);
    }
}