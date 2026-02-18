using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Mugen.CharacterFrameAnalysis;
using SaveState.Core.Mugen.CharacterFrameAnalysis.Services;

namespace SaveState.Application.Mugen.FrameData.Commands;

/// <summary>
/// Command to refresh frame data for a character.
/// </summary>
public sealed record RefreshFrameDataCommand(string CharacterPath) : IRequest<Result<CharacterFrameData>>;

/// <summary>
/// Handler for RefreshFrameDataCommand.
/// </summary>
public sealed class RefreshFrameDataCommandHandler : IRequestHandler<RefreshFrameDataCommand, Result<CharacterFrameData>>
{
    private readonly IFrameDataService _frameDataService;

    public RefreshFrameDataCommandHandler(IFrameDataService frameDataService)
    {
        _frameDataService = frameDataService;
    }

    public async Task<Result<CharacterFrameData>> Handle(RefreshFrameDataCommand request, CancellationToken cancellationToken)
    {
        return await _frameDataService.RefreshFrameDataAsync(request.CharacterPath, cancellationToken);
    }
}
