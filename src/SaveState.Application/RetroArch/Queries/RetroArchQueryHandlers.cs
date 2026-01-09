using MediatR;
using SaveState.Core.Common;
using SaveState.Core.RetroArch;
using SaveState.Core.RetroArch.Services;

namespace SaveState.Application.RetroArch.Queries;

public class GetRetroArchGamesQueryHandler : IRequestHandler<GetRetroArchGamesQuery, Result<IReadOnlyList<RetroArchGame>>>
{
    private readonly IRetroArchService _retroArchService;

    public GetRetroArchGamesQueryHandler(IRetroArchService retroArchService)
    {
        _retroArchService = retroArchService;
    }

    public async Task<Result<IReadOnlyList<RetroArchGame>>> Handle(GetRetroArchGamesQuery request, CancellationToken cancellationToken)
    {
        return await _retroArchService.GetGamesAsync(cancellationToken);
    }
}

public class GetInstalledCoresQueryHandler : IRequestHandler<GetInstalledCoresQuery, Result<IReadOnlyList<RetroArchCore>>>
{
    private readonly IRetroArchService _retroArchService;

    public GetInstalledCoresQueryHandler(IRetroArchService retroArchService)
    {
        _retroArchService = retroArchService;
    }

    public async Task<Result<IReadOnlyList<RetroArchCore>>> Handle(GetInstalledCoresQuery request, CancellationToken cancellationToken)
    {
        return await _retroArchService.GetInstalledCoresAsync(cancellationToken);
    }
}

public class GetAvailableCoresQueryHandler : IRequestHandler<GetAvailableCoresQuery, Result<IReadOnlyList<RetroArchCore>>>
{
    private readonly IRetroArchService _retroArchService;

    public GetAvailableCoresQueryHandler(IRetroArchService retroArchService)
    {
        _retroArchService = retroArchService;
    }

    public async Task<Result<IReadOnlyList<RetroArchCore>>> Handle(GetAvailableCoresQuery request, CancellationToken cancellationToken)
    {
        return await _retroArchService.GetAvailableCoresAsync(cancellationToken);
    }
}

public class GetRetroArchConfigQueryHandler : IRequestHandler<GetRetroArchConfigQuery, Result<RetroArchConfig>>
{
    private readonly IRetroArchService _retroArchService;

    public GetRetroArchConfigQueryHandler(IRetroArchService retroArchService)
    {
        _retroArchService = retroArchService;
    }

    public async Task<Result<RetroArchConfig>> Handle(GetRetroArchConfigQuery request, CancellationToken cancellationToken)
    {
        return await _retroArchService.GetConfigAsync(cancellationToken);
    }
}
