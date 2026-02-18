using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Mugen.ReplayAnalysis.Services;

namespace SaveState.Application.Mugen.ReplayAnalysis.Queries;

/// <summary>
/// Query to validate if a replay file is supported and readable.
/// </summary>
public sealed record ValidateReplayFileQuery(string FilePath) : IRequest<Result<ReplayFileInfo>>;

/// <summary>
/// Handler for ValidateReplayFileQuery.
/// </summary>
public sealed class ValidateReplayFileQueryHandler : IRequestHandler<ValidateReplayFileQuery, Result<ReplayFileInfo>>
{
    private readonly IReplayAnalysisService _analysisService;

    public ValidateReplayFileQueryHandler(IReplayAnalysisService analysisService)
    {
        _analysisService = analysisService;
    }

    public async Task<Result<ReplayFileInfo>> Handle(ValidateReplayFileQuery request, CancellationToken cancellationToken)
    {
        return await _analysisService.ValidateReplayFileAsync(request.FilePath, cancellationToken);
    }
}
