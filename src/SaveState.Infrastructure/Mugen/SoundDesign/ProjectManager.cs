using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Mugen.Services;

namespace SaveState.Infrastructure.Mugen.SoundDesign;

/// <summary>
/// Manages sound project operations for the SoundDesignService.
/// </summary>
public class ProjectManager
{
    private readonly ILogger<ProjectManager> _logger;
    private readonly ITimeProvider _timeProvider;
    private SoundProject? _currentProject;

    public ProjectManager(
        ILogger<ProjectManager> logger,
        ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public Task<Result<SoundProject>> CreateProjectAsync(
        string name,
        SoundProjectSettings settings,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Creating sound project: {Name}", name);

            var project = new SoundProject(
                name,
                $"{name}.sndproj",
                settings,
                new List<SoundEffect>(),
                new List<BackgroundMusic>(),
                new List<SoundCategory>(),
                _timeProvider.UtcNow,
                _timeProvider.UtcNow);

            _currentProject = project;
            return Task.FromResult(Result<SoundProject>.Success(project));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create project");
            return Task.FromResult(Result<SoundProject>.Failure($"Create project failed: {ex.Message}", ErrorType.Internal));
        }
    }

    public Task<Result<SoundProject>> OpenProjectAsync(
        string projectPath,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Opening sound project: {Path}", projectPath);

            if (_currentProject == null)
            {
                return Task.FromResult(Result<SoundProject>.Failure("No project loaded", ErrorType.NotFound));
            }

            return Task.FromResult(Result<SoundProject>.Success(_currentProject));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open project");
            return Task.FromResult(Result<SoundProject>.Failure($"Open project failed: {ex.Message}", ErrorType.Internal));
        }
    }

    public Task<Result> SaveProjectAsync(
        string? projectPath = null,
        CancellationToken ct = default)
    {
        try
        {
            if (_currentProject == null)
            {
                return Task.FromResult(Result.Failure("No project is currently open", ErrorType.Validation));
            }

            _currentProject = _currentProject with
            {
                ModifiedAt = _timeProvider.UtcNow
            };

            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save project");
            return Task.FromResult(Result.Failure($"Save project failed: {ex.Message}", ErrorType.Internal));
        }
    }
}
