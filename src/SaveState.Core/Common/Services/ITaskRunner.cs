using System;
using System.Threading.Tasks;

namespace SaveState.Core.Common.Services;

/// <summary>
/// Service for executing fire-and-forget background tasks with centralized logging and error handling.
/// </summary>
public interface ITaskRunner
{
    /// <summary>
    /// Executes a task in the background without waiting for its completion.
    /// Errors are caught and logged automatically.
    /// </summary>
    /// <param name="taskFactory">The factory to create the task.</param>
    /// <param name="taskName">A descriptive name for the task for logging purposes.</param>
    void Run(Func<Task> taskFactory, string taskName);

    /// <summary>
    /// Executes a task in the background without waiting for its completion.
    /// Errors are caught and logged automatically.
    /// </summary>
    /// <param name="task">The task to execute.</param>
    /// <param name="taskName">A descriptive name for the task for logging purposes.</param>
    void Run(Task task, string taskName);
}
