using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Mugen.ComboDatabase;
using SaveState.Core.Mugen.ComboDatabase.Services;
using ComboEntryModel = SaveState.Core.Mugen.ComboDatabase.ComboEntry;

namespace SaveState.Application.Mugen.ComboDatabase.Queries;

/// <summary>
/// Query to discover combos from replay analysis.
/// </summary>
public sealed record DiscoverCombosFromReplayQuery(
    Guid ReplayAnalysisId) : IRequest<Result<List<ComboEntryModel>>>;

/// <summary>
/// Handler for DiscoverCombosFromReplayQuery.
/// </summary>
public sealed class DiscoverCombosFromReplayQueryHandler : IRequestHandler<DiscoverCombosFromReplayQuery, Result<List<ComboEntryModel>>>
{
    private readonly IComboDatabaseService _comboService;

    public DiscoverCombosFromReplayQueryHandler(IComboDatabaseService comboService)
    {
        _comboService = comboService;
    }

    public async Task<Result<List<ComboEntryModel>>> Handle(DiscoverCombosFromReplayQuery request, CancellationToken cancellationToken)
    {
        return await _comboService.DiscoverCombosFromReplayAsync(
            request.ReplayAnalysisId,
            cancellationToken);
    }
}
