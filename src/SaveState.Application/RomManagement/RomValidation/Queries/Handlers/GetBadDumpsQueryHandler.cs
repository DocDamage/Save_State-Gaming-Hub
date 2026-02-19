using MediatR;
using SaveState.Core.Common;
using SaveState.Core.RomManagement.RomValidation;
using SaveState.Core.RomManagement.RomValidation.Services;

namespace SaveState.Application.RomManagement.RomValidation.Queries.Handlers;

/// <summary>
/// Handler for getting bad dump ROMs.
/// </summary>
public sealed class GetBadDumpsQueryHandler
    : IRequestHandler<GetBadDumpsQuery, Result<List<BadDumpInfo>>>
{
    private readonly IRomValidationReportRepository _reportRepository;

    public GetBadDumpsQueryHandler(IRomValidationReportRepository reportRepository)
    {
        _reportRepository = reportRepository;
    }

    public async Task<Result<List<BadDumpInfo>>> Handle(
        GetBadDumpsQuery request,
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

        var badDumps = reports
            .Where(r => r.Status == ValidationStatus.BadDump)
            .Select(r => new BadDumpInfo
            {
                RomFileId = r.RomFileId,
                FileName = r.SuggestedName ?? "Unknown",
                IssueDescription = r.Issues?.FirstOrDefault()?.Message ?? "Identified as bad dump",
                ExpectedHash = r.MatchResult?.MatchedEntry?.Name
            })
            .ToList();

        return Result<List<BadDumpInfo>>.Success(badDumps);
    }
}
