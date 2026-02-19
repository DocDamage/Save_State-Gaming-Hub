using MediatR;
using SaveState.Core.Common;
using SaveState.Core.RomManagement.RomValidation;
using SaveState.Core.RomManagement.RomValidation.Services;

namespace SaveState.Application.RomManagement.RomValidation.Queries.Handlers;

/// <summary>
/// Handler for getting ROM rename suggestions.
/// </summary>
public sealed class GetRomRenameSuggestionsQueryHandler
    : IRequestHandler<GetRomRenameSuggestionsQuery, Result<List<RomRenameSuggestion>>>
{
    private readonly IRomValidationReportRepository _reportRepository;

    public GetRomRenameSuggestionsQueryHandler(IRomValidationReportRepository reportRepository)
    {
        _reportRepository = reportRepository;
    }

    public async Task<Result<List<RomRenameSuggestion>>> Handle(
        GetRomRenameSuggestionsQuery request,
        CancellationToken cancellationToken)
    {
        IEnumerable<RomValidationReport> reports;

        if (request.PlatformId.HasValue)
        {
            reports = await _reportRepository.GetByPlatformIdAsync(
                request.PlatformId.Value,
                cancellationToken);
        }
        else
        {
            reports = await _reportRepository.GetAllAsync(cancellationToken);
        }

        var suggestions = reports
            .Where(r => !string.IsNullOrEmpty(r.SuggestedName) &&
                       r.MatchResult?.Confidence == MatchConfidence.Exact)
            .Select(r => new RomRenameSuggestion
            {
                RomFileId = r.RomFileId,
                CurrentName = r.SuggestedName!,
                SuggestedName = r.MatchResult?.MatchedEntry?.Name ?? r.SuggestedName!,
                SourceDat = r.MatchResult?.MatchedEntry?.SourceDat ?? "Unknown DAT"
            })
            .ToList();

        return Result<List<RomRenameSuggestion>>.Success(suggestions);
    }
}
