using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Mugen.ComboDatabase;
using SaveState.Core.Mugen.ComboDatabase.Services;
using ComboEntryModel = SaveState.Core.Mugen.ComboDatabase.ComboEntry;

namespace SaveState.Application.Mugen.ComboDatabase.Queries;

/// <summary>
/// Query to search combos with filtering and sorting.
/// </summary>
public sealed record SearchCombosQuery(
    string? CharacterName = null,
    ComboDifficulty? Difficulty = null,
    int? MinDamage = null,
    int? MaxDamage = null,
    int? MinHits = null,
    string? StartingPosition = null,
    int? MaxMeterRequired = null,
    List<string>? Tags = null,
    bool? IsVerified = null,
    bool? IsOptimal = null,
    bool? IsTouchOfDeath = null,
    string? SearchTerm = null,
    ComboSortOption SortBy = ComboSortOption.Damage,
    bool SortDescending = true,
    int Page = 1,
    int PageSize = 20) : IRequest<Result<List<ComboEntryModel>>>;

/// <summary>
/// Handler for SearchCombosQuery.
/// </summary>
public sealed class SearchCombosQueryHandler : IRequestHandler<SearchCombosQuery, Result<List<ComboEntryModel>>>
{
    private readonly IComboDatabaseService _comboService;

    public SearchCombosQueryHandler(IComboDatabaseService comboService)
    {
        _comboService = comboService;
    }

    public async Task<Result<List<ComboEntryModel>>> Handle(SearchCombosQuery request, CancellationToken cancellationToken)
    {
        var filter = new ComboFilter
        {
            CharacterName = request.CharacterName,
            Difficulty = request.Difficulty,
            MinDamage = request.MinDamage,
            MaxDamage = request.MaxDamage,
            MinHits = request.MinHits,
            StartingPosition = request.StartingPosition,
            MaxMeterRequired = request.MaxMeterRequired,
            Tags = request.Tags,
            IsVerified = request.IsVerified,
            IsOptimal = request.IsOptimal,
            IsTouchOfDeath = request.IsTouchOfDeath,
            SearchTerm = request.SearchTerm,
            SortBy = request.SortBy,
            SortDescending = request.SortDescending
        };

        return await _comboService.SearchCombosAsync(
            filter,
            request.Page,
            request.PageSize,
            cancellationToken);
    }
}
