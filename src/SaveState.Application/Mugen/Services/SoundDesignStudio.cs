using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using Microsoft.Extensions.Logging;
using SaveState.Application.Mugen.Services.SoundDesign;

namespace SaveState.Application.Mugen.Services;

/// <summary>
/// Professional sound design studio providing advanced audio tools,
/// dynamic mixing, spatial audio, and cinematic soundscapes.
/// </summary>
public class SoundDesignStudio : SoundDesignStudioISoundDesignStudio
{
    private readonly ILogger<SoundDesignStudio> _logger;
    private readonly SoundProjectManager _projectManager;
    private readonly SoundTrackManager _trackManager;
    private readonly SoundEffectManager _effectManager;
    private readonly SoundAnalysisManager _analysisManager;
    private readonly SoundMixingManager _mixingManager;
    private readonly SoundSpatialManager _spatialManager;
    private readonly SoundRenderManager _renderManager;

    public SoundDesignStudio(
        ILogger<SoundDesignStudio> logger,
        ILoggerFactory loggerFactory,
        ICacheService cache,
        ITimeProvider timeProvider,
        SoundProjectManager projectManager,
        SoundTrackManager trackManager,
        SoundEffectManager effectManager,
        SoundAnalysisManager analysisManager,
        SoundMixingManager mixingManager,
        SoundSpatialManager spatialManager,
        SoundRenderManager renderManager)
    {
        _logger = logger;
        _projectManager = projectManager;
        _trackManager = trackManager;
        _effectManager = effectManager;
        _analysisManager = analysisManager;
        _mixingManager = mixingManager;
        _spatialManager = spatialManager;
        _renderManager = renderManager;
    }

    /// <inheritdoc />
    public async Task<Result<SoundDesignStudioAudioProject>> CreateAudioProjectAsync(
        SoundDesignStudioAudioProjectRequest request, 
        CancellationToken ct = default)
    {
        return await _projectManager.CreateAudioProjectAsync(request, _trackManager, ct);
    }

    /// <inheritdoc />
    public async Task<Result<SoundDesignStudioAudioTrack>> CreateTrackAsync(
        SoundDesignStudioAudioTrackRequest request, 
        CancellationToken ct = default)
    {
        return await _trackManager.CreateTrackAsync(request, _effectManager.AudioEffects, ct);
    }

    /// <inheritdoc />
    public async Task<Result<SoundDesignStudioAudioEffect>> CreateAudioEffectAsync(
        SoundDesignStudioAudioEffectRequest request, 
        CancellationToken ct = default)
    {
        return await _effectManager.CreateAudioEffectAsync(request, ct);
    }

    /// <inheritdoc />
    public async Task<Result<SoundDesignStudioAudioClip>> ImportAudioFileAsync(
        SoundDesignStudioAudioImportRequest request, 
        CancellationToken ct = default)
    {
        return await _trackManager.ImportAudioFileAsync(request, ct);
    }

    /// <inheritdoc />
    public async Task<Result<SoundDesignStudioMixSnapshot>> CreateMixSnapshotAsync(
        string projectId, 
        string name, 
        CancellationToken ct = default)
    {
        if (!_projectManager.ActiveProjects.TryGetValue(projectId, out var project))
        {
            return Result<SoundDesignStudioMixSnapshot>.Failure("Project not found");
        }

        return await _mixingManager.CreateMixSnapshotAsync(project, name, ct);
    }

    /// <inheritdoc />
    public async Task<Result> ApplyMixSnapshotAsync(
        string projectId, 
        string snapshotId, 
        CancellationToken ct = default)
    {
        if (!_projectManager.ActiveProjects.TryGetValue(projectId, out var project))
        {
            return Result.Failure("Project not found");
        }

        return await _mixingManager.ApplyMixSnapshotAsync(project, snapshotId, ct);
    }

    /// <inheritdoc />
    public async Task<Result<SoundDesignStudioAudioAnalysis>> AnalyzeAudioContentAsync(
        string projectId, 
        CancellationToken ct = default)
    {
        if (!_projectManager.ActiveProjects.TryGetValue(projectId, out var project))
        {
            return Result<SoundDesignStudioAudioAnalysis>.Failure("Project not found");
        }

        return await _analysisManager.AnalyzeAudioContentAsync(project, ct);
    }

    /// <inheritdoc />
    public async Task<Result<SoundDesignStudioSpatialAudioSetup>> CreateSpatialAudioSetupAsync(
        SoundDesignStudioSpatialAudioRequest request, 
        CancellationToken ct = default)
    {
        return await _spatialManager.CreateSpatialAudioSetupAsync(request, ct);
    }

    /// <inheritdoc />
    public async Task<Result> RenderAudioProjectAsync(
        string projectId, 
        SoundDesignStudioRenderSettings settings, 
        CancellationToken ct = default)
    {
        if (!_projectManager.ActiveProjects.TryGetValue(projectId, out var project))
        {
            return Result.Failure("Project not found");
        }

        // Mix all tracks first
        await _mixingManager.MixProjectAsync(project, ct);

        // Then render the project
        return await _renderManager.RenderAudioProjectAsync(project, settings, ct);
    }
}

/// <summary>
/// Sound Design Studio interface.
/// </summary>
public interface SoundDesignStudioISoundDesignStudio
{
    Task<Result<SoundDesignStudioAudioProject>> CreateAudioProjectAsync(SoundDesignStudioAudioProjectRequest request, CancellationToken ct = default);
    Task<Result<SoundDesignStudioAudioTrack>> CreateTrackAsync(SoundDesignStudioAudioTrackRequest request, CancellationToken ct = default);
    Task<Result<SoundDesignStudioAudioEffect>> CreateAudioEffectAsync(SoundDesignStudioAudioEffectRequest request, CancellationToken ct = default);
    Task<Result<SoundDesignStudioAudioClip>> ImportAudioFileAsync(SoundDesignStudioAudioImportRequest request, CancellationToken ct = default);
    Task<Result<SoundDesignStudioMixSnapshot>> CreateMixSnapshotAsync(string projectId, string name, CancellationToken ct = default);
    Task<Result> ApplyMixSnapshotAsync(string projectId, string snapshotId, CancellationToken ct = default);
    Task<Result<SoundDesignStudioAudioAnalysis>> AnalyzeAudioContentAsync(string projectId, CancellationToken ct = default);
    Task<Result<SoundDesignStudioSpatialAudioSetup>> CreateSpatialAudioSetupAsync(SoundDesignStudioSpatialAudioRequest request, CancellationToken ct = default);
    Task<Result> RenderAudioProjectAsync(string projectId, SoundDesignStudioRenderSettings settings, CancellationToken ct = default);
}
