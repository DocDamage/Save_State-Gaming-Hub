using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Mugen.ComboDatabase;
using SaveState.Core.Mugen.ComboDatabase.Services;
using ComboEntryModel = SaveState.Core.Mugen.ComboDatabase.ComboEntry;

namespace SaveState.Application.Mugen.ComboDatabase.Queries;

/// <summary>
/// Query to get a combo by ID.
/// </summary>
public sealed record GetComboQuery(Guid ComboId) : IRequest<Result<ComboEntryModel>>;

/// <summary>
/// Handler for GetComboQuery.
/// </summary>
public sealed class GetComboQueryHandler : IRequestHandler<GetComboQuery, Result<ComboEntryModel>>
{
    private readonly IComboDatabaseService _comboService;

    public GetComboQueryHandler(IComboDatabaseService comboService)
    {
        _comboService = comboService;
    }

    public async Task<Result<ComboEntryModel>> Handle(GetComboQuery request, CancellationToken cancellationToken)
    {
        return await _comboService.GetComboAsync(request.ComboId, cancellationToken);
    }
}
