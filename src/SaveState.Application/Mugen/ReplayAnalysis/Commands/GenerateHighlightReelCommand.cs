using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Mugen.ReplayAnalysis;
using SaveState.Core.Mugen.ReplayAnalysis.Services;

namespace SaveState.Application.Mugen.ReplayAnalysis.Commands;

/// <summary>
/// Command to generate a highlight reel from selected moments.
/// </summary>
public sealed record GenerateHighlightReelCommand(
    Guid AnalysisId,
    List<Guid> HighlightIds,
    string Name,
    string? Description = null,
    TimeSpan? MaxDuration = null,
    bool AddTransitions = true,
    bool IncludeSlowMotion = true,
    VideoQuality Quality = VideoQuality.High) : IRequest<Result<HighlightReel>>;

/// <summary>
/// Handler for GenerateHighlightReelCommand.
/// </summary>
public sealed class GenerateHighlightReelCommandHandler : IRequestHandler<GenerateHighlightReelCommand, Result<HighlightReel>>
{
    private readonly IReplayAnalysisService _analysisService;

    public GenerateHighlightReelCommandHandler(IReplayAnalysisService analysisService)
    {
        _analysisService = analysisService;
    }

    public async Task<Result<HighlightReel>> Handle(GenerateHighlightReelCommand request, CancellationToken cancellationToken)
    {
        var options = new HighlightReelOptions
        {
            Name = request.Name,
            Description = request.Description,
            MaxDuration = request.MaxDuration,
            AddTransitions = request.AddTransitions,
            IncludeSlowMotion = request.IncludeSlowMotion,
            Quality = request.Quality
        };

        return await _analysisService.GenerateHighlightReelAsync(
            request.AnalysisId, 
            request.HighlightIds, 
            options, 
            cancellationToken);
    }
}
