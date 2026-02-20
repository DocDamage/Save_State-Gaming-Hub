using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using Microsoft.Extensions.Logging;

namespace SaveState.Application.Mugen.Services.SoundDesign;

/// <summary>
/// Manages audio project lifecycle and operations.
/// </summary>
public class SoundProjectManager
{
    private readonly ILogger<SoundProjectManager> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly Dictionary<string, SoundDesignStudioAudioProject> _activeProjects;

    public SoundProjectManager(
        ILogger<SoundProjectManager> logger,
        ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
        _activeProjects = new Dictionary<string, SoundDesignStudioAudioProject>();
    }

    /// <summary>
    /// Gets the dictionary of active projects for access by other managers.
    /// </summary>
    public IReadOnlyDictionary<string, SoundDesignStudioAudioProject> ActiveProjects => _activeProjects;

    /// <summary>
    /// Creates a new audio project with the specified configuration and initial tracks.
    /// </summary>
    /// <param name="request">The project configuration request.</param>
    /// <param name="trackManager">The track manager for creating initial tracks.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the created project.</returns>
    public async Task<Result<SoundDesignStudioAudioProject>> CreateAudioProjectAsync(
        SoundDesignStudioAudioProjectRequest request,
        SoundTrackManager? trackManager = null,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Creating audio project: {Name}", request.Name);

            var project = new SoundDesignStudioAudioProject
            {
                ProjectId = Guid.NewGuid().ToString(),
                Name = request.Name,
                Description = request.Description,
                SampleRate = request.SampleRate,
                BitDepth = request.BitDepth,
                Channels = request.Channels,
                Tempo = request.Tempo,
                SoundDesignStudioTimeSignature = request.SoundDesignStudioTimeSignature,
                Tracks = new List<SoundDesignStudioAudioTrack>(),
                MasterBus = CreateMasterBus(),
                CreatedAt = _timeProvider.UtcNow,
                LastModified = _timeProvider.UtcNow
            };

            // Add initial tracks if track manager is available
            if (trackManager != null && request.InitialTracks.Count > 0)
            {
                var tracks = new List<SoundDesignStudioAudioTrack>();
                foreach (var trackRequest in request.InitialTracks)
                {
                    var trackResult = await trackManager.CreateTrackAsync(trackRequest, new Dictionary<string, SoundDesignStudioAudioEffect>(), ct);
                    if (trackResult.IsSuccess)
                    {
                        tracks.Add(trackResult.Value);
                    }
                }
                project.Tracks = tracks;
            }

            _activeProjects[project.ProjectId] = project;

            _logger.LogInformation("Audio project created: {ProjectId}", project.ProjectId);
            return Result<SoundDesignStudioAudioProject>.Success(project);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating audio project {Name}", request.Name);
            return Result<SoundDesignStudioAudioProject>.Failure($"Failed to create project: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets a project by its ID.
    /// </summary>
    /// <param name="projectId">The project ID.</param>
    /// <returns>The project if found; otherwise null.</returns>
    public SoundDesignStudioAudioProject? GetProject(string projectId)
    {
        if (_activeProjects.TryGetValue(projectId, out var project))
        {
            return project;
        }

        return null;
    }

    /// <summary>
    /// Creates the master bus with default settings.
    /// </summary>
    /// <returns>The configured master bus.</returns>
    private SoundDesignStudioAudioBus CreateMasterBus()
    {
        return new SoundDesignStudioAudioBus
        {
            BusId = "master",
            Name = "Master",
            Volume = 0.0f,
            Pan = 0.0f,
            Mute = false,
            EffectsChain = new List<SoundDesignStudioAudioEffect>(),
            Sends = new List<SoundDesignStudioAudioSend>()
        };
    }
}
