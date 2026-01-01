using MediatR;
using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Services;

namespace SaveState.Application.GameLibrary.Queries.Categorization;

public sealed record SuggestTagsQuery(string GameTitle, string? Description = null) : IRequest<Result<IReadOnlyList<string>>>;

public sealed class SuggestTagsQueryHandler : IRequestHandler<SuggestTagsQuery, Result<IReadOnlyList<string>>>
{
    private readonly ISmartCategorizationService _categorizationService;

    public SuggestTagsQueryHandler(ISmartCategorizationService categorizationService)
    {
        _categorizationService = categorizationService;
    }

    public async Task<Result<IReadOnlyList<string>>> Handle(SuggestTagsQuery request, CancellationToken ct)
    {
        return await _categorizationService.SuggestTagsAsync(request.GameTitle, request.Description, ct);
    }
}