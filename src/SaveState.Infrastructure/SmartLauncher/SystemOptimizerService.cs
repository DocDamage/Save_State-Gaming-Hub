// Copyright (c) 2026 SaveStateReborn. All rights reserved.

using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using Microsoft.Extensions.Logging;
using SaveState.Core.SmartLauncher;

namespace SaveState.Infrastructure.SmartLauncher;

/// <summary>
/// Windows-specific system optimizer service for gaming.
/// </summary>
public sealed class SystemOptimizerService : ISystemOptimizerService
{
    private readonly ILogger<SystemOptimizerService> _logger;
    private readonly List<ProcessInfo> _suspendedProcesses = new();
    private readonly List<ServiceInfo> _stoppedServices = new();
    private string? _originalPowerPlan;
    private bool _visualEffectsEnabled = true;

    public SystemOptimizerService(ILogger<SystemOptimizerService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<SystemState> ApplyOptimizationsAsync(LaunchProfile profile, CancellationToken ct = default)
    {
        var state = new SystemState
        {
            VisualEffectsEnabled = _visualEffectsEnabled
        };

        try
        {
            // Save current power plan
            _originalPowerPlan = await GetCurrentPowerPlanAsync(ct);
            state.PowerPlanGuid = _originalPowerPlan;

            // Set high performance power plan if specified
            if (!string.IsNullOrEmpty(profile.PowerPlanGuid))
            {
                await SetPowerPlanAsync(profile.PowerPlanGuid, ct);
            }

            // Suspend processes
            if (profile.ProcessesToSuspend.Any())
            {
                var suspended = await SuspendProcessesAsync(profile.ProcessesToSuspend, ct);
                state.SuspendedProcesses = suspended;
                _suspendedProcesses.AddRange(suspended);
            }

            // Stop services
            if (profile.ServicesToStop.Any())
            {
                var stopped = await StopServicesAsync(profile.ServicesToStop, ct);
                state.StoppedServices = stopped;
                _stoppedServices.AddRange(stopped);
            }

            // Apply performance settings
            if (profile.PerformanceSettings.DisableVisualEffects)
            {
                await DisableVisualEffectsAsync(ct);
            }

            if (profile.PerformanceSettings.ClearStandbyList)
            {
                await OptimizeMemoryAsync(ct);
            }

            _logger.LogInformation("System optimizations applied successfully");
            return state;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply system optimizations");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task RestoreSystemStateAsync(SystemState state, CancellationToken ct = default)
    {
        try
        {
            // Restore power plan
            if (!string.IsNullOrEmpty(state.PowerPlanGuid))
            {
                await SetPowerPlanAsync(state.PowerPlanGuid, ct);
            }

            // Resume suspended processes
            if (state.SuspendedProcesses.Any())
            {
                await ResumeProcessesAsync(state.SuspendedProcesses, ct);
            }

            // Start stopped services
            if (state.StoppedServices.Any())
            {
                await StartServicesAsync(state.StoppedServices, ct);
            }

            // Restore visual effects
            if (state.VisualEffectsEnabled)
            {
                await EnableVisualEffectsAsync(ct);
            }

            _logger.LogInformation("System state restored successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore system state");
            throw;
        }
    }

    /// <inheritdoc />
    public Task<List<ProcessInfo>> SuspendProcessesAsync(List<string> processNames, CancellationToken ct = default)
    {
        var suspended = new List<ProcessInfo>();

        if (!OperatingSystem.IsWindows())
        {
            _logger.LogWarning("Process suspension is only supported on Windows");
            return Task.FromResult(suspended);
        }

        try
        {
            foreach (var processName in processNames)
            {
                var processes = Process.GetProcessesByName(processName);
                foreach (var process in processes)
                {
                    try
                    {
                        // Skip system processes and current process
                        if (process.Id == Environment.ProcessId || process.Id == 0)
                            continue;

                        SuspendProcess(process);
                        suspended.Add(new ProcessInfo
                        {
                            Id = process.Id,
                            Name = process.ProcessName,
                            ExecutablePath = process.MainModule?.FileName
                        });

                        _logger.LogDebug("Suspended process: {ProcessName} (PID: {ProcessId})",
                            process.ProcessName, process.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to suspend process {ProcessName}", processName);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error suspending processes");
        }

        return Task.FromResult(suspended);
    }

    /// <inheritdoc />
    public Task ResumeProcessesAsync(List<ProcessInfo> processes, CancellationToken ct = default)
    {
        if (!OperatingSystem.IsWindows())
        {
            _logger.LogWarning("Process resumption is only supported on Windows");
            return Task.CompletedTask;
        }

        try
        {
            foreach (var processInfo in processes)
            {
                try
                {
                    var process = Process.GetProcessById(processInfo.Id);
                    ResumeProcess(process);
                    _logger.LogDebug("Resumed process: {ProcessName} (PID: {ProcessId})",
                        processInfo.Name, processInfo.Id);
                }
                catch (ArgumentException)
                {
                    // Process already exited
                    _logger.LogDebug("Process {ProcessName} (PID: {ProcessId}) already exited",
                        processInfo.Name, processInfo.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to resume process {ProcessName}", processInfo.Name);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resuming processes");
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<List<ServiceInfo>> StopServicesAsync(List<string> serviceNames, CancellationToken ct = default)
    {
        var stopped = new List<ServiceInfo>();

        if (!OperatingSystem.IsWindows())
        {
            _logger.LogWarning("Service management is only supported on Windows");
            return Task.FromResult(stopped);
        }

        try
        {
            foreach (var serviceName in serviceNames)
            {
                try
                {
                    using var sc = new ServiceController(serviceName);
                    if (sc.Status == ServiceControllerStatus.Running)
                    {
                        sc.Stop();
                        sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(10));

                        stopped.Add(new ServiceInfo
                        {
                            Name = serviceName,
                            DisplayName = sc.DisplayName,
                            StartupType = sc.StartType.ToString()
                        });

                        _logger.LogDebug("Stopped service: {ServiceName}", serviceName);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to stop service {ServiceName}", serviceName);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping services");
        }

        return Task.FromResult(stopped);
    }

    /// <inheritdoc />
    public Task StartServicesAsync(List<ServiceInfo> services, CancellationToken ct = default)
    {
        if (!OperatingSystem.IsWindows())
        {
            _logger.LogWarning("Service management is only supported on Windows");
            return Task.CompletedTask;
        }

        try
        {
            foreach (var serviceInfo in services)
            {
                try
                {
                    using var sc = new ServiceController(serviceInfo.Name);
                    if (sc.Status == ServiceControllerStatus.Stopped)
                    {
                        sc.Start();
                        sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(10));
                        _logger.LogDebug("Started service: {ServiceName}", serviceInfo.Name);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to start service {ServiceName}", serviceInfo.Name);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting services");
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SetProcessPriorityAsync(int processId, ProcessPriority priority, CancellationToken ct = default)
    {
        try
        {
            var process = Process.GetProcessById(processId);
            var priorityClass = priority switch
            {
                ProcessPriority.Low => ProcessPriorityClass.Idle,
                ProcessPriority.BelowNormal => ProcessPriorityClass.BelowNormal,
                ProcessPriority.Normal => ProcessPriorityClass.Normal,
                ProcessPriority.AboveNormal => ProcessPriorityClass.AboveNormal,
                ProcessPriority.High => ProcessPriorityClass.High,
                ProcessPriority.RealTime => ProcessPriorityClass.RealTime,
                _ => ProcessPriorityClass.Normal
            };

            process.PriorityClass = priorityClass;
            _logger.LogDebug("Set process {ProcessId} priority to {Priority}", processId, priority);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to set process priority for PID {ProcessId}", processId);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task OptimizeMemoryAsync(CancellationToken ct = default)
    {
        if (!OperatingSystem.IsWindows())
        {
            _logger.LogWarning("Memory optimization is only supported on Windows");
            return Task.CompletedTask;
        }

        try
        {
            // Clear working set for current process
            NativeMethods.EmptyWorkingSet(Process.GetCurrentProcess().Handle);

            // Trigger garbage collection
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, blocking: true, compacting: true);
            GC.WaitForPendingFinalizers();

            _logger.LogInformation("Memory optimization completed");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to optimize memory");
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DisableVisualEffectsAsync(CancellationToken ct = default)
    {
        if (!OperatingSystem.IsWindows())
        {
            return Task.CompletedTask;
        }

        try
        {
            // Set visual effects to best performance
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects", true);
            if (key != null)
            {
                key.SetValue("VisualFXSetting", 2); // Best performance
            }

            _visualEffectsEnabled = false;
            _logger.LogInformation("Visual effects disabled");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to disable visual effects");
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task EnableVisualEffectsAsync(CancellationToken ct = default)
    {
        if (!OperatingSystem.IsWindows())
        {
            return Task.CompletedTask;
        }

        try
        {
            // Restore visual effects to default
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects", true);
            if (key != null)
            {
                key.SetValue("VisualFXSetting", 0); // Let Windows choose
            }

            _visualEffectsEnabled = true;
            _logger.LogInformation("Visual effects enabled");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to enable visual effects");
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task SetPowerPlanAsync(string powerPlanGuid, CancellationToken ct = default)
    {
        if (!OperatingSystem.IsWindows())
        {
            _logger.LogWarning("Power plan management is only supported on Windows");
            return;
        }

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "powercfg",
                Arguments = $"/setactive {powerPlanGuid}",
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process != null)
            {
                await process.WaitForExitAsync(ct);
                _logger.LogInformation("Power plan set to {PowerPlanGuid}", powerPlanGuid);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to set power plan");
        }
    }

    /// <inheritdoc />
    public async Task<string> GetCurrentPowerPlanAsync(CancellationToken ct = default)
    {
        if (!OperatingSystem.IsWindows())
        {
            return string.Empty;
        }

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "powercfg",
                Arguments = "/getactivescheme",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process != null)
            {
                var output = await process.StandardOutput.ReadToEndAsync(ct);
                await process.WaitForExitAsync(ct);

                // Parse GUID from output: "Power Scheme GUID: 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c  (High performance)"
                var match = System.Text.RegularExpressions.Regex.Match(output,
                    @"Power Scheme GUID: ([0-9a-fA-F\-]+)");
                if (match.Success)
                {
                    return match.Groups[1].Value;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get current power plan");
        }

        return string.Empty;
    }

    /// <inheritdoc />
    public Task<OptimizationCheckResult> CanApplyOptimizationsAsync(LaunchProfile profile, CancellationToken ct = default)
    {
        var result = new OptimizationCheckResult
        {
            CanApply = true,
            Warnings = new List<string>(),
            Requirements = new List<string>()
        };

        if (!OperatingSystem.IsWindows())
        {
            result.CanApply = false;
            result.Requirements.Add("Windows operating system");
            return Task.FromResult(result);
        }

        // Check if running with administrator privileges for certain optimizations
        if (profile.RunAsAdministrator || profile.ServicesToStop.Any())
        {
            if (!IsRunningAsAdministrator())
            {
                result.Warnings.Add("Some optimizations require administrator privileges");
            }
        }

        return Task.FromResult(result);
    }

    private static bool IsRunningAsAdministrator()
    {
        if (!OperatingSystem.IsWindows())
            return false;

        using var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
        var principal = new System.Security.Principal.WindowsPrincipal(identity);
        return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
    }

    private static void SuspendProcess(Process process)
    {
        if (!OperatingSystem.IsWindows()) return;

        try
        {
            var handle = NativeMethods.OpenProcess(NativeConstants.PROCESS_SUSPEND_RESUME, false, process.Id);
            if (handle == IntPtr.Zero) return;

            try
            {
                _ = NativeMethods.NtSuspendProcess(handle);
            }
            finally
            {
                _ = NativeMethods.CloseHandle(handle);
            }
        }
        catch (Exception)
        {
            // Process suspension is best-effort
        }
    }

    private static void ResumeProcess(Process process)
    {
        if (!OperatingSystem.IsWindows()) return;

        try
        {
            var handle = NativeMethods.OpenProcess(NativeConstants.PROCESS_SUSPEND_RESUME, false, process.Id);
            if (handle == IntPtr.Zero) return;

            try
            {
                _ = NativeMethods.NtResumeProcess(handle);
            }
            finally
            {
                _ = NativeMethods.CloseHandle(handle);
            }
        }
        catch (Exception)
        {
            // Process resumption is best-effort
        }
    }
}

/// <summary>
/// Native constants for Windows API.
/// </summary>
internal static class NativeConstants
{
    public const uint PROCESS_SUSPEND_RESUME = 0x0800;
}

/// <summary>
/// Native methods for system operations.
/// </summary>
internal static class NativeMethods
{
    [DllImport("psapi.dll")]
    public static extern bool EmptyWorkingSet(IntPtr hProcess);

    [DllImport("ntdll.dll")]
    public static extern int NtSuspendProcess(IntPtr processHandle);

    [DllImport("ntdll.dll")]
    public static extern int NtResumeProcess(IntPtr processHandle);

    [DllImport("kernel32.dll")]
    public static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

    [DllImport("kernel32.dll")]
    public static extern bool CloseHandle(IntPtr hObject);
}
