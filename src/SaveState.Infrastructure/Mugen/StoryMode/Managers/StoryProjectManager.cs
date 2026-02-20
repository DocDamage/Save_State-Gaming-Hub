using System.Collections.Concurrent;
using System.Text;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Mugen.Services;

namespace SaveState.Infrastructure.Mugen.StoryMode.Managers;

/// <summary>
/// Manages story projects: creation, loading, saving, and export.
/// </summary>
public class StoryProjectManager
{
    private readonly ILogger<StoryProjectManager> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly ConcurrentDictionary<Guid, StoryProject> _projects;
    private StoryProject? _currentProject;

    public StoryProjectManager(
        ILogger<StoryProjectManager> logger,
        ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
        _projects = new ConcurrentDictionary<Guid, StoryProject>();
    }

    public StoryProject? CurrentProject => _currentProject;
    public ConcurrentDictionary<Guid, StoryProject> Projects => _projects;

    /// <summary>
    /// Creates a new story project.
    /// </summary>
    public Task<Result<StoryProject>> CreateProjectAsync(
        string title,
        string? description = null,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Creating story project: {Title}", title);

            var project = new StoryProject(
                Guid.NewGuid(),
                title,
                description,
                $"{title.Replace(" ", "_")}.story",
                _timeProvider.UtcNow,
                _timeProvider.UtcNow,
                new StoryVersion(1, 0, 0),
                new List<StoryChapter>(),
                new List<StoryCharacter>(),
                new StorySettings(
                    "default_bg.def",
                    "default.mp3",
                    TextSpeed.Normal,
                    true,
                    true));

            _projects[project.Id] = project;
            _currentProject = project;

            return Task.FromResult(Result<StoryProject>.Success(project));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create story project");
            return Task.FromResult(Result<StoryProject>.Failure($"Create project failed: {ex.Message}", ErrorType.Internal));
        }
    }

    /// <summary>
    /// Opens an existing story project.
    /// </summary>
    public Task<Result<StoryProject>> OpenProjectAsync(
        string projectPath,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Opening story project: {Path}", projectPath);

            if (_currentProject == null)
            {
                return Task.FromResult(Result<StoryProject>.Failure("No project loaded", ErrorType.NotFound));
            }

            return Task.FromResult(Result<StoryProject>.Success(_currentProject));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open story project");
            return Task.FromResult(Result<StoryProject>.Failure($"Open project failed: {ex.Message}", ErrorType.Internal));
        }
    }

    /// <summary>
    /// Saves the current story project.
    /// </summary>
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

            _logger.LogInformation("Saving story project: {Title}", _currentProject.Title);

            _currentProject = _currentProject with
            {
                ModifiedAt = _timeProvider.UtcNow,
                Version = new StoryVersion(
                    _currentProject.Version.Major,
                    _currentProject.Version.Minor,
                    _currentProject.Version.Patch + 1)
            };

            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save story project");
            return Task.FromResult(Result.Failure($"Save project failed: {ex.Message}", ErrorType.Internal));
        }
    }

    /// <summary>
    /// Gets project statistics.
    /// </summary>
    public Task<Result<StoryProjectStats>> GetProjectStatsAsync(
        int chapterCount,
        int sceneCount,
        int dialogueCount,
        int battleCount,
        int choiceCount,
        int castCount,
        long totalAssetSize,
        CancellationToken ct = default)
    {
        try
        {
            if (_currentProject == null)
            {
                return Task.FromResult(Result<StoryProjectStats>.Failure("No project is open", ErrorType.Validation));
            }

            var stats = new StoryProjectStats(
                chapterCount,
                sceneCount,
                dialogueCount,
                battleCount,
                choiceCount,
                castCount,
                totalAssetSize,
                TimeSpan.FromMinutes(sceneCount * 2));

            return Task.FromResult(Result<StoryProjectStats>.Success(stats));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get project stats");
            return Task.FromResult(Result<StoryProjectStats>.Failure($"Get stats failed: {ex.Message}", ErrorType.Internal));
        }
    }

    /// <summary>
    /// Exports story mode for MUGEN.
    /// </summary>
    public async Task<Result<string>> ExportForMugenAsync(
        string outputDirectory,
        MugenStoryExportOptions options,
        int chapterCount,
        int sceneCount,
        int castCount,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Exporting story for MUGEN to: {Output}", outputDirectory);

            Directory.CreateDirectory(outputDirectory);

            // Generate MUGEN-compatible story files
            var storyFile = Path.Combine(outputDirectory, "story.def");
            await File.WriteAllTextAsync(storyFile, GenerateStoryDef(chapterCount, sceneCount, castCount), ct);

            return Result<string>.Success(storyFile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export for MUGEN");
            return Result<string>.Failure($"Export failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Sets the current project (used when chapters are updated).
    /// </summary>
    public void SetCurrentProject(StoryProject project)
    {
        _currentProject = project;
        _projects[project.Id] = project;
    }

    private string GenerateStoryDef(int chapterCount, int sceneCount, int castCount)
    {
        var sb = new StringBuilder();
        sb.AppendLine("; Story Mode Definition");
        sb.AppendLine($"; Title: {_currentProject?.Title ?? "Untitled"}");
        sb.AppendLine($"; Version: {_currentProject?.Version.Major}.{_currentProject?.Version.Minor}.{_currentProject?.Version.Patch}");
        sb.AppendLine();
        sb.AppendLine("[Story]");
        sb.AppendLine($"title = \"{_currentProject?.Title ?? "Untitled"}\"");
        sb.AppendLine($"chapters = {chapterCount}");
        sb.AppendLine($"scenes = {sceneCount}");
        sb.AppendLine($"characters = {castCount}");

        return sb.ToString();
    }
}
