using MediatR;
using SaveState.Core.Common;
using SaveState.Core.MobileCompanion.Models;
using SaveState.Core.MobileCompanion.Services;

namespace SaveState.Application.MobileCompanion.Queries;

public sealed record GetActiveSessionsQuery : IRequest<Result<IReadOnlyList<RemoteSessionDto>>>;

public sealed class GetActiveSessionsQueryHandler : IRequestHandler<GetActiveSessionsQuery, Result<IReadOnlyList<RemoteSessionDto>>>
{
    private readonly IMobileCompanionService _companionService;

    public GetActiveSessionsQueryHandler(IMobileCompanionService companionService)
    {
        _companionService = companionService;
    }

    public async Task<Result<IReadOnlyList<RemoteSessionDto>>> Handle(GetActiveSessionsQuery request, CancellationToken cancellationToken)
    {
        var result = await _companionService.GetActiveSessionsAsync(cancellationToken).ConfigureAwait(false);

        if (result.IsFailure)
        {
            return Result<IReadOnlyList<RemoteSessionDto>>.Failure(result.Error!, result.ErrorType);
        }

        var dtos = result.Value.Select(MapToDto).ToList();
        return Result<IReadOnlyList<RemoteSessionDto>>.Success(dtos);
    }

    private static RemoteSessionDto MapToDto(RemoteSession session)
    {
        return new RemoteSessionDto
        {
            Id = session.Id,
            DeviceId = session.DeviceId,
            StartedAt = session.StartedAt,
            LastActivityAt = session.LastActivityAt,
            CurrentMode = session.CurrentMode,
            IsActive = session.IsActive,
            ConnectionId = session.ConnectionId
        };
    }
}
