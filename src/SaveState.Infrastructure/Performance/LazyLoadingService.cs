using Microsoft.Extensions.Logging;

namespace SaveState.Infrastructure.Performance;

/// <summary>
/// Service for lazy loading heavy resources to improve startup time.
/// </summary>
public class LazyLoadingService
{
    private readonly ILogger<LazyLoadingService> _logger;
    private readonly Dictionary<string, Lazy<Task>> _lazyTasks = new();

    public LazyLoadingService(ILogger<LazyLoadingService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Registers a task for lazy execution.
    /// </summary>
    public void RegisterLazyTask(string taskName, Func<Task> taskFactory)
    {
        _lazyTasks[taskName] = new Lazy<Task>(taskFactory);
        _logger.LogDebug("Registered lazy task: {TaskName}", taskName);
    }

    /// <summary>
    /// Executes a lazy task if it hasn't been executed yet.
    /// </summary>
    public async Task ExecuteLazyTaskAsync(string taskName)
    {
        if (_lazyTasks.TryGetValue(taskName, out var lazyTask))
        {
            _logger.LogDebug("Executing lazy task: {TaskName}", taskName);
            await lazyTask.Value;
        }
        else
        {
            _logger.LogWarning("Lazy task {TaskName} not found", taskName);
        }
    }

    /// <summary>
    /// Checks if a lazy task has been executed.
    /// </summary>
    public bool IsTaskExecuted(string taskName)
    {
        return _lazyTasks.TryGetValue(taskName, out var lazyTask) && lazyTask.IsValueCreated;
    }

    /// <summary>
    /// Executes all registered lazy tasks in parallel.
    /// </summary>
    public async Task ExecuteAllAsync()
    {
        _logger.LogInformation("Executing all lazy tasks ({Count} tasks)", _lazyTasks.Count);
        var tasks = _lazyTasks.Values.Select(lt => lt.Value);
        await Task.WhenAll(tasks);
        _logger.LogInformation("All lazy tasks completed");
    }
}
