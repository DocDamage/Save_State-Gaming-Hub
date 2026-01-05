using Microsoft.Extensions.Logging;
using SaveState.Core.Common.Services;
using System;
using System.Threading.Tasks;

namespace SaveState.Infrastructure.Common;

/// <summary>
/// Implementation of ITaskRunner that uses ILogger to report background task errors.
/// </summary>
public class TaskRunner : ITaskRunner
{
    private readonly ILogger<TaskRunner> _logger;

    public TaskRunner(ILogger<TaskRunner> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public void Run(Func<Task> taskFactory, string taskName)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await taskFactory().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Background task '{TaskName}' failed", taskName);
            }
        });
    }

    /// <inheritdoc />
    public void Run(Task task, string taskName)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await task.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Background task '{TaskName}' failed", taskName);
            }
        });
    }
}
