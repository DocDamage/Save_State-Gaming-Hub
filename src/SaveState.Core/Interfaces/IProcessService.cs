using System.Diagnostics;

namespace SaveState.Core.Interfaces;

public interface IProcessService
{
    /// <summary>
    /// Gets a list of all running processes.
    /// </summary>
    IEnumerable<Process> GetProcesses();

    /// <summary>
    /// Gets a specific process by ID.
    /// </summary>
    Process? GetProcessById(int processId);

    /// <summary>
    /// Gets processes by name.
    /// </summary>
    IEnumerable<Process> GetProcessesByName(string processName);
}
