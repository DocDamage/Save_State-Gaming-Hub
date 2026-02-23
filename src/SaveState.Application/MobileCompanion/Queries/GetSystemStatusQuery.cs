using MediatR;
using SaveState.Core.Common;
using SaveState.Core.MobileCompanion.Services;

namespace SaveState.Application.MobileCompanion.Queries;

public sealed record GetSystemStatusQuery : IRequest<Result<SystemStatusDto>>;

public sealed class GetSystemStatusQueryHandler : IRequestHandler<GetSystemStatusQuery, Result<SystemStatusDto>>
{
    private readonly IMobileCompanionService _companionService;

    public GetSystemStatusQueryHandler(IMobileCompanionService companionService)
    {
        _companionService = companionService;
    }

    public async Task<Result<SystemStatusDto>> Handle(GetSystemStatusQuery request, CancellationToken cancellationToken)
    {
        var result = await _companionService.GetSystemStatusAsync(cancellationToken).ConfigureAwait(false);

        if (result.IsFailure)
        {
            return Result<SystemStatusDto>.Failure(result.Error!, result.ErrorType);
        }

        var dto = new SystemStatusDto
        {
            IsOnline = result.Value.IsOnline,
            CpuUsage = result.Value.CpuUsage,
            MemoryUsage = result.Value.MemoryUsage,
            CurrentlyPlayingGame = result.Value.CurrentlyPlayingGame,
            CurrentlyPlayingGameCover = result.Value.CurrentlyPlayingGameCover,
            SessionDuration = result.Value.SessionDuration,
            IsRecording = result.Value.IsRecording,
            IsStreaming = result.Value.IsStreaming
        };

        return Result<SystemStatusDto>.Success(dto);
    }
}
