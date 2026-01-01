using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Performance.Services;

namespace SaveState.Application.Performance.Commands;

/// <summary>
/// Command to create a display profile for a game.
/// </summary>
public sealed record CreateDisplayProfileCommand(
    Guid GameId,
    int Width,
    int Height,
    int RefreshRate,
    bool VSync = true,
    bool HdrEnabled = false,
    bool GSync = false) : IRequest<Result<DisplayProfile>>;

/// <summary>
/// Handler for CreateDisplayProfileCommand.
/// </summary>
public sealed class CreateDisplayProfileCommandHandler : IRequestHandler<CreateDisplayProfileCommand, Result<DisplayProfile>>
{
    private readonly IDisplayCalibrator _displayCalibrator;

    public CreateDisplayProfileCommandHandler(IDisplayCalibrator displayCalibrator)
    {
        _displayCalibrator = displayCalibrator;
    }

    public async Task<Result<DisplayProfile>> Handle(CreateDisplayProfileCommand request, CancellationToken cancellationToken)
    {
        var settings = new DisplaySettings(
            Width: request.Width,
            Height: request.Height,
            RefreshRate: request.RefreshRate,
            VSync: request.VSync,
            HdrEnabled: request.HdrEnabled,
            GSync: request.GSync,
            FullscreenOptimizations: true);

        return await _displayCalibrator.CreateGameProfileAsync(request.GameId, settings, cancellationToken);
    }
}

/// <summary>
/// Command to apply a display profile.
/// </summary>
public sealed record ApplyDisplayProfileCommand(Guid ProfileId) : IRequest<Result>;

/// <summary>
/// Handler for ApplyDisplayProfileCommand.
/// </summary>
public sealed class ApplyDisplayProfileCommandHandler : IRequestHandler<ApplyDisplayProfileCommand, Result>
{
    private readonly IDisplayCalibrator _displayCalibrator;

    public ApplyDisplayProfileCommandHandler(IDisplayCalibrator displayCalibrator)
    {
        _displayCalibrator = displayCalibrator;
    }

    public async Task<Result> Handle(ApplyDisplayProfileCommand request, CancellationToken cancellationToken)
    {
        return await _displayCalibrator.ApplyProfileAsync(request.ProfileId, cancellationToken);
    }
}

/// <summary>
/// Command to revert display settings to original.
/// </summary>
public sealed record RevertDisplaySettingsCommand : IRequest<Result>;

/// <summary>
/// Handler for RevertDisplaySettingsCommand.
/// </summary>
public sealed class RevertDisplaySettingsCommandHandler : IRequestHandler<RevertDisplaySettingsCommand, Result>
{
    private readonly IDisplayCalibrator _displayCalibrator;

    public RevertDisplaySettingsCommandHandler(IDisplayCalibrator displayCalibrator)
    {
        _displayCalibrator = displayCalibrator;
    }

    public async Task<Result> Handle(RevertDisplaySettingsCommand request, CancellationToken cancellationToken)
    {
        return await _displayCalibrator.RevertSettingsAsync(cancellationToken);
    }
}

/// <summary>
/// Query to get current display settings.
/// </summary>
public sealed record GetDisplaySettingsQuery : IRequest<Result<DisplaySettings>>;

/// <summary>
/// Handler for GetDisplaySettingsQuery.
/// </summary>
public sealed class GetDisplaySettingsQueryHandler : IRequestHandler<GetDisplaySettingsQuery, Result<DisplaySettings>>
{
    private readonly IDisplayCalibrator _displayCalibrator;

    public GetDisplaySettingsQueryHandler(IDisplayCalibrator displayCalibrator)
    {
        _displayCalibrator = displayCalibrator;
    }

    public async Task<Result<DisplaySettings>> Handle(GetDisplaySettingsQuery request, CancellationToken cancellationToken)
    {
        return await _displayCalibrator.GetCurrentSettingsAsync(cancellationToken);
    }
}

/// <summary>
/// Query to get available display resolutions.
/// </summary>
public sealed record GetAvailableResolutionsQuery : IRequest<Result<IReadOnlyList<DisplayResolution>>>;

/// <summary>
/// Handler for GetAvailableResolutionsQuery.
/// </summary>
public sealed class GetAvailableResolutionsQueryHandler : IRequestHandler<GetAvailableResolutionsQuery, Result<IReadOnlyList<DisplayResolution>>>
{
    private readonly IDisplayCalibrator _displayCalibrator;

    public GetAvailableResolutionsQueryHandler(IDisplayCalibrator displayCalibrator)
    {
        _displayCalibrator = displayCalibrator;
    }

    public async Task<Result<IReadOnlyList<DisplayResolution>>> Handle(GetAvailableResolutionsQuery request, CancellationToken cancellationToken)
    {
        return await _displayCalibrator.GetAvailableResolutionsAsync(cancellationToken);
    }
}
