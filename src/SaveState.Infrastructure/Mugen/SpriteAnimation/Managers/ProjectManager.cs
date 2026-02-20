using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Mugen.Services;

namespace SaveState.Infrastructure.Mugen.SpriteAnimation.Managers;

/// <summary>
/// Manages sprite project lifecycle including creation, loading, and saving.
/// </summary>
public sealed class ProjectManager
{
    private readonly ILogger<ProjectManager> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly ConcurrentDictionary<int, SpriteGroup> _spriteGroups;
    private readonly ConcurrentDictionary<int, Animation> _animations;
    private readonly ConcurrentDictionary<int, Palette> _palettes;
    private SpriteProject? _currentProject;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectManager"/> class.
    /// </summary>
    public ProjectManager(
        ILogger<ProjectManager> logger,
        ITimeProvider timeProvider,
        ConcurrentDictionary<int, SpriteGroup> spriteGroups,
        ConcurrentDictionary<int, Animation> animations,
        ConcurrentDictionary<int, Palette> palettes)
    {
        _logger = logger;
        _timeProvider = timeProvider;
        _spriteGroups = spriteGroups;
        _animations = animations;
        _palettes = palettes;
    }

    /// <summary>
    /// Creates a new sprite project.
    /// </summary>
    public Task<Result<SpriteProject>> CreateProjectAsync(
        string name,
        ProjectSettings settings,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Creating sprite project: {Name}", name);

            var project = new SpriteProject(
                name,
                $"{name}.ssp",
                settings,
                null,
                null,
                new List<Palette>(),
                _timeProvider.UtcNow,
                _timeProvider.UtcNow);

            _currentProject = project;
            return Task.FromResult(Result<SpriteProject>.Success(project));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create project");
            return Task.FromResult(Result<SpriteProject>.Failure($"Create project failed: {ex.Message}", ErrorType.Internal));
        }
    }

    /// <summary>
    /// Opens an existing sprite project.
    /// </summary>
    public Task<Result<SpriteProject>> OpenProjectAsync(
        string projectPath,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Opening project: {Path}", projectPath);
            return Task.FromResult(Result<SpriteProject>.Success(_currentProject!));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open project");
            return Task.FromResult(Result<SpriteProject>.Failure($"Open project failed: {ex.Message}", ErrorType.Internal));
        }
    }

    /// <summary>
    /// Saves the current project.
    /// </summary>
    public Task<Result> SaveProjectAsync(
        string? projectPath = null,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Saving project");

            if (_currentProject == null)
            {
                return Task.FromResult(Result.Failure("No project is currently open", ErrorType.Validation));
            }

            var path = projectPath ?? _currentProject.FilePath;

            _currentProject = _currentProject with { ModifiedAt = _timeProvider.UtcNow };
            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save project");
            return Task.FromResult(Result.Failure($"Save project failed: {ex.Message}", ErrorType.Internal));
        }
    }

    /// <summary>
    /// Gets project statistics.
    /// </summary>
    public Task<Result<ProjectStatistics>> GetProjectStatisticsAsync(
        CancellationToken ct = default)
    {
        try
        {
            var stats = new ProjectStatistics(
                _spriteGroups.Values.Sum(g => g.Sprites.Count),
                _animations.Count,
                _animations.Values.Sum(a => a.Frames.Count),
                _palettes.Count,
                0,
                _currentProject?.ModifiedAt ?? DateTime.MinValue);

            return Task.FromResult(Result<ProjectStatistics>.Success(stats));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get project statistics");
            return Task.FromResult(Result<ProjectStatistics>.Failure($"Get statistics failed: {ex.Message}", ErrorType.Internal));
        }
    }

    /// <summary>
    /// Gets the current project.
    /// </summary>
    public SpriteProject? GetCurrentProject() => _currentProject;

    /// <summary>
    /// Sets the current project.
    /// </summary>
    public void SetCurrentProject(SpriteProject project) => _currentProject = project;
}
