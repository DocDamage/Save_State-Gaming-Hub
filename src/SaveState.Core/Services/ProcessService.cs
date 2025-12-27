using System.Diagnostics;
using SaveState.Core.Interfaces;
using Serilog;

namespace SaveState.Core.Services;

public class ProcessService : IProcessService
{
    private readonly ILogger _logger = Log.ForContext<ProcessService>();

    public IEnumerable<Process> GetProcesses()
    {
        try
        {
            return Process.GetProcesses();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to retrieve process list");
            return Enumerable.Empty<Process>();
        }
    }

    public Process? GetProcessById(int processId)
    {
        try
        {
            return Process.GetProcessById(processId);
        }
        catch (Exception ex)
        {
            _logger.Warning("Failed to get process by ID {Id}: {Message}", processId, ex.Message);
            return null;
        }
    }

    public IEnumerable<Process> GetProcessesByName(string processName)
    {
        try
        {
            return Process.GetProcessesByName(processName);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to retrieve processes by name {Name}", processName);
            return Enumerable.Empty<Process>();
        }
    }
}
