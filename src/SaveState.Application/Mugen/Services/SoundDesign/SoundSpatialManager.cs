using SaveState.Core.Common;
using Microsoft.Extensions.Logging;

namespace SaveState.Application.Mugen.Services.SoundDesign;

/// <summary>
/// Manages spatial audio and 3D positioning.
/// </summary>
public class SoundSpatialManager
{
    private readonly ILogger<SoundSpatialManager> _logger;
    private readonly SpatialAudioEngine _spatialEngine;

    public SoundSpatialManager(
        ILogger<SoundSpatialManager> logger,
        ILoggerFactory loggerFactory)
    {
        _logger = logger;
        _spatialEngine = new SpatialAudioEngine(loggerFactory.CreateLogger<SpatialAudioEngine>());
    }

    /// <summary>
    /// Creates a 3D audio spatial setup.
    /// </summary>
    /// <param name="request">The spatial audio setup request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the spatial audio setup.</returns>
    public async Task<Result<SoundDesignStudioSpatialAudioSetup>> CreateSpatialAudioSetupAsync(
        SoundDesignStudioSpatialAudioRequest request, 
        CancellationToken ct = default)
    {
        try
        {
            var setup = await _spatialEngine.CreateSpatialSetupAsync(request, ct);
            return Result.Success(setup);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating spatial audio setup");
            return Result.Failure<SoundDesignStudioSpatialAudioSetup>($"Failed to create spatial setup: {ex.Message}");
        }
    }
}

/// <summary>
/// Spatial audio engine for 3D audio positioning.
/// </summary>
public class SpatialAudioEngine
{
    private readonly ILogger<SpatialAudioEngine> _logger;

    public SpatialAudioEngine(ILogger<SpatialAudioEngine> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Creates a spatial audio setup based on the request.
    /// </summary>
    /// <param name="request">The spatial audio setup request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created spatial audio setup.</returns>
    public async Task<SoundDesignStudioSpatialAudioSetup> CreateSpatialSetupAsync(
        SoundDesignStudioSpatialAudioRequest request, 
        CancellationToken ct = default)
    {
        var setup = new SoundDesignStudioSpatialAudioSetup
        {
            SetupId = Guid.NewGuid().ToString(),
            Name = request.Name,
            ListenerPosition = request.ListenerPosition,
            AudioSources = request.AudioSources,
            SoundDesignStudioEnvironmentPreset = request.SoundDesignStudioEnvironmentPreset,
            SoundDesignStudioReverbSettings = request.SoundDesignStudioReverbSettings,
            SoundDesignStudioOcclusionSettings = request.SoundDesignStudioOcclusionSettings
        };

        return setup;
    }
}
