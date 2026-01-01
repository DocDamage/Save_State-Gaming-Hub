using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Performance.Services;

namespace SaveState.Application.Performance.Commands;

/// <summary>
/// Command to optimize the system for gaming.
/// </summary>
public sealed record OptimizeSystemCommand(
    OptimizationLevel Level,
    IReadOnlyList<string>? ProcessesToClose = null,
    bool SetHighPerformancePowerPlan = true,
    bool DisableOverlays = false) : IRequest<Result>;

/// <summary>
/// Handler for OptimizeSystemCommand.
/// </summary>
public sealed class OptimizeSystemCommandHandler : IRequestHandler<OptimizeSystemCommand, Result>
{
    private readonly ISystemResourceManager _resourceManager;

    public OptimizeSystemCommandHandler(ISystemResourceManager resourceManager)
    {
        _resourceManager = resourceManager;
    }

    public async Task<Result> Handle(OptimizeSystemCommand request, CancellationToken cancellationToken)
    {
        var profile = new OptimizationProfile(
            Name: $"Gaming Optimization - {request.Level}",
            Level: request.Level,
            ProcessesToClose: request.ProcessesToClose ?? Array.Empty<string>(),
            SetGamePriority: true,
            DisableOverlays: request.DisableOverlays,
            DisableWindowsGameMode: false,
            SetHighPerformancePowerPlan: request.SetHighPerformancePowerPlan,
            DisableFullscreenOptimizations: false);

        return await _resourceManager.ApplyOptimizationAsync(profile, cancellationToken);
    }
}

/// <summary>
/// Command to restore the system after gaming optimization.
/// </summary>
public sealed record RestoreSystemCommand : IRequest<Result>;

/// <summary>
/// Handler for RestoreSystemCommand.
/// </summary>
public sealed class RestoreSystemCommandHandler : IRequestHandler<RestoreSystemCommand, Result>
{
    private readonly ISystemResourceManager _resourceManager;

    public RestoreSystemCommandHandler(ISystemResourceManager resourceManager)
    {
        _resourceManager = resourceManager;
    }

    public async Task<Result> Handle(RestoreSystemCommand request, CancellationToken cancellationToken)
    {
        return await _resourceManager.RestoreSystemAsync(cancellationToken);
    }
}

/// <summary>
/// Query to analyze the current system state.
/// </summary>
public sealed record AnalyzeSystemQuery : IRequest<Result<SystemAnalysis>>;

/// <summary>
/// Handler for AnalyzeSystemQuery.
/// </summary>
public sealed class AnalyzeSystemQueryHandler : IRequestHandler<AnalyzeSystemQuery, Result<SystemAnalysis>>
{
    private readonly ISystemResourceManager _resourceManager;

    public AnalyzeSystemQueryHandler(ISystemResourceManager resourceManager)
    {
        _resourceManager = resourceManager;
    }

    public async Task<Result<SystemAnalysis>> Handle(AnalyzeSystemQuery request, CancellationToken cancellationToken)
    {
        return await _resourceManager.AnalyzeSystemAsync(cancellationToken);
    }
}
