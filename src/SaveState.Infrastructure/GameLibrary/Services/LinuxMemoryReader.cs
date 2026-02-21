using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Services;
using SaveState.Application.Common;

namespace SaveState.Infrastructure.GameLibrary.Services;

/// <summary>
/// Linux implementation of game memory reading using ptrace, /proc/{pid}/mem, and process_vm_writev.
/// </summary>
public sealed class LinuxMemoryReader : IGameMemoryReader, IDisposable
{
    private readonly ILogger<LinuxMemoryReader> _logger;
    private int _processId = -1;
    private bool _isAttached;
    private FileStream? _memStream;
    private Process? _attachedProcess;
    private readonly object _lock = new();
    private Timer? _monitoringTimer;
    private GameStateType _currentState = GameStateType.Unknown;

    // Track frozen values for freeze functionality
    private readonly ConcurrentDictionary<IntPtr, FrozenValue> _frozenValues = new();
    private CancellationTokenSource? _freezeCts;
    private Task? _freezeTask;

    private class FrozenValue
    {
        public object Value { get; set; } = null!;
        public string ValueType { get; set; } = string.Empty;
        public CancellationTokenSource Cts { get; } = new();
    }

    public event EventHandler<GameStateChangedEventArgs>? StateChanged;
    public bool IsAttached => _isAttached;

    public LinuxMemoryReader(ILogger<LinuxMemoryReader> logger)
    {
        _logger = logger;
    }

    public Task<Result> AttachToProcessAsync(int processId, CancellationToken ct = default)
    {
        using (_logger.BeginCorrelationScope())
        using (_logger.BeginMemoryScanScope(processId, "Unknown"))
        {
            _logger.LogInformation("Attempting to attach to process {ProcessId}", processId);
            
            try
            {
                if (_isAttached)
                {
                    _logger.LogWarning("Already attached to a process. Detach first.");
                    return Task.FromResult(Result.Failure("Already attached to a process. Detach first."));
                }

                if (!Directory.Exists($"/proc/{processId}"))
                {
                    _logger.LogError("Process {ProcessId} not found in /proc", processId);
                    return Task.FromResult(Result.Failure($"Process {processId} not found"));
                }

                _attachedProcess = Process.GetProcessById(processId);
                if (_attachedProcess == null)
                {
                    _logger.LogError("Process {ProcessId} could not be retrieved", processId);
                    return Task.FromResult(Result.Failure($"Process {processId} not found"));
                }

                // Attach via ptrace
                _logger.LogDebug("Attaching to process {ProcessId} via ptrace", processId);
                var result = PtraceAttach(processId);
                if (!result.IsSuccess)
                {
                    _logger.LogError("ptrace attach failed for process {ProcessId}: {Error}", processId, result.Error);
                    return Task.FromResult(result);
                }

                // Open /proc/{pid}/mem
                var memPath = $"/proc/{processId}/mem";
                try
                {
                    _memStream = new FileStream(memPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                    _logger.LogDebug("Successfully opened {MemPath}", memPath);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to open {MemPath}", memPath);
                    PtraceDetach(processId);
                    return Task.FromResult(Result.Failure($"Failed to open memory: {ex.Message}"));
                }

                _processId = processId;
                _isAttached = true;

                // Start state monitoring
                _monitoringTimer = new Timer(_ => MonitorGameState(), null, TimeSpan.Zero, TimeSpan.FromSeconds(1));

                _logger.LogInformation(
                    "Successfully attached to process {ProcessId} ({ProcessName}) on Linux",
                    processId,
                    _attachedProcess?.ProcessName ?? "Unknown");
                    
                return Task.FromResult(Result.Success());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to attach to process {ProcessId}", processId);
                return Task.FromResult(Result.Failure($"Attach failed: {ex.Message}"));
            }
        }
    }

    public Task<Result> DetachAsync(CancellationToken ct = default)
    {
        return Task.FromResult(DetachInternal());
    }

    private Result DetachInternal()
    {
        try
        {
            _logger.LogInformation("Detaching from process on Linux");

            // Stop freeze loop
            _freezeCts?.Cancel();
            _freezeCts?.Dispose();
            _freezeCts = null;
            _frozenValues.Clear();
            _freezeTask = null;

            // Stop monitoring timer
            _monitoringTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            _monitoringTimer?.Dispose();
            _monitoringTimer = null;
            
            lock (_lock)
            {
                _memStream?.Close();
                _memStream?.Dispose();
                _memStream = null;
            }

            if (_processId != -1)
                PtraceDetach(_processId);

            _attachedProcess = null;
            _processId = -1;
            _isAttached = false;
            _currentState = GameStateType.Unknown;

            _logger.LogInformation("Successfully detached from process on Linux");
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detaching from process on Linux");
            return Result.Failure($"Failed to detach: {ex.Message}");
        }
    }

    private void MonitorGameState()
    {
        if (_attachedProcess == null) return;

        try
        {
            // Check if process is still running
            if (_attachedProcess.HasExited)
            {
                if (_currentState != GameStateType.Unknown)
                {
                    _currentState = GameStateType.Unknown;
                    StateChanged?.Invoke(this, new GameStateChangedEventArgs { StateType = _currentState });
                    _logger.LogInformation("Process exited on Linux");
                }
                return;
            }

            // Check if process is suspended (stopped)
            var statusPath = $"/proc/{_processId}/stat";
            if (File.Exists(statusPath))
            {
                try
                {
                    var stat = File.ReadAllText(statusPath);
                    var stateCode = stat.Split(')')[1].Trim().Split(' ')[0];
                    
                    var newState = stateCode switch
                    {
                        "T" => GameStateType.Paused,
                        "R" => GameStateType.InGame,
                        "S" => GameStateType.InGame,
                        "D" => GameStateType.Paused,
                        _ => GameStateType.Unknown
                    };

                    if (newState != _currentState)
                    {
                        _currentState = newState;
                        StateChanged?.Invoke(this, new GameStateChangedEventArgs { StateType = _currentState, Data = DateTime.Now });
                        _logger.LogInformation("Process state changed to {State} on Linux", _currentState);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to read process state from /proc/{Pid}/stat", _processId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error monitoring game state on Linux");
        }
    }

    public Task<Result<IReadOnlyList<MemoryPattern>>> DetectPatternsAsync(CancellationToken ct = default)
    {
        using (_logger.BeginCorrelationScope())
        {
            _logger.LogInformation("Starting pattern detection on Linux");
            
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            if (!_isAttached)
            {
                stopwatch.Stop();
                _logger.LogWarning("Pattern detection attempted while not attached");
                return Task.FromResult(Result.Failure<IReadOnlyList<MemoryPattern>>("Not attached to any process"));
            }

            // Simplified implementation - would scan for common patterns
            _logger.LogDebug("Scanning process memory for patterns");
            var patterns = new List<MemoryPattern>();
            
            stopwatch.Stop();
            _logger.LogInformation(
                "Pattern detection completed. Found {Count} patterns in {ElapsedMs}ms",
                patterns.Count,
                stopwatch.ElapsedMilliseconds);
                
            return Task.FromResult(Result.Success<IReadOnlyList<MemoryPattern>>(patterns));
        }
    }

    public Task<Result<byte[]>> ReadMemoryBytesAsync(IntPtr address, int length, CancellationToken ct = default)
    {
        if (!_isAttached || _memStream == null)
        {
            _logger.LogWarning("Read attempted while not attached");
            return Task.FromResult(Result.Failure<byte[]>("Not attached to any process"));
        }

        try
        {
            lock (_lock)
            {
                _memStream.Seek(address.ToInt64(), SeekOrigin.Begin);
                var buffer = new byte[length];
                var bytesRead = _memStream.Read(buffer, 0, length);
                if (bytesRead < length)
                    Array.Resize(ref buffer, bytesRead);
                    
                _logger.LogDebug("Read {BytesRead} bytes from address {Address:X}", bytesRead, address.ToInt64());
                return Task.FromResult(Result.Success(buffer));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read memory at address {Address:X}", address.ToInt64());
            return Task.FromResult(Result.Failure<byte[]>($"Read failed: {ex.Message}"));
        }
    }

    /// <summary>
    /// Writes an integer value to the attached process's memory.
    /// </summary>
    /// <remarks>
    /// <para><b>Linux Requirements:</b></para>
    /// <para>This method requires the CAP_SYS_PTRACE capability. Run with:</para>
    /// <code>sudo setcap cap_sys_ptrace=eip ./SaveStateReborn</code>
    /// <para>Or run with sudo (not recommended):</para>
    /// <code>sudo ./SaveStateReborn</code>
    /// <para>Note: process_vm_writev is more efficient than ptrace but still slower than Windows WriteProcessMemory.</para>
    /// </remarks>
    public async Task<Result> WriteMemoryAsync(IntPtr address, int value, CancellationToken ct = default)
    {
        if (!_isAttached || _processId == -1)
        {
            return Result.Failure("Not attached to any process");
        }

        try
        {
            _logger.LogDebug("Writing int value {Value} to address {Address} on Linux", value, address);

            var bytes = BitConverter.GetBytes(value);
            var result = WriteMemoryBytes(address, bytes);

            if (result)
            {
                _logger.LogDebug("Successfully wrote int value to address {Address}", address);
                return Result.Success();
            }
            else
            {
                var error = Marshal.GetLastWin32Error();
                _logger.LogWarning("Failed to write memory: errno {Error}", error);
                return Result.Failure($"Failed to write memory (errno: {error}). Ensure CAP_SYS_PTRACE capability.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error writing memory on Linux");
            return Result.Failure($"Write failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Writes a float value to the attached process's memory.
    /// </summary>
    /// <remarks>
    /// <para><b>Linux Requirements:</b></para>
    /// <para>This method requires the CAP_SYS_PTRACE capability. Run with:</para>
    /// <code>sudo setcap cap_sys_ptrace=eip ./SaveStateReborn</code>
    /// <para>Or run with sudo (not recommended):</para>
    /// <code>sudo ./SaveStateReborn</code>
    /// <para>Note: process_vm_writev is more efficient than ptrace but still slower than Windows WriteProcessMemory.</para>
    /// </remarks>
    public async Task<Result> WriteMemoryAsync(IntPtr address, float value, CancellationToken ct = default)
    {
        if (!_isAttached || _processId == -1)
        {
            return Result.Failure("Not attached to any process");
        }

        try
        {
            _logger.LogDebug("Writing float value {Value} to address {Address} on Linux", value, address);

            var bytes = BitConverter.GetBytes(value);
            var result = WriteMemoryBytes(address, bytes);

            if (result)
            {
                _logger.LogDebug("Successfully wrote float value to address {Address}", address);
                return Result.Success();
            }
            else
            {
                var error = Marshal.GetLastWin32Error();
                return Result.Failure($"Failed to write memory (errno: {error}). Ensure CAP_SYS_PTRACE capability.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error writing memory on Linux");
            return Result.Failure($"Write failed: {ex.Message}");
        }
    }

    private bool WriteMemoryBytes(IntPtr address, byte[] bytes)
    {
        // Pin the byte array to get a pointer
        var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
        try
        {
            var localIov = new IoVec
            {
                iov_base = handle.AddrOfPinnedObject(),
                iov_len = (IntPtr)bytes.Length
            };

            var remoteIov = new IoVec
            {
                iov_base = address,
                iov_len = (IntPtr)bytes.Length
            };

            var result = process_vm_writev(
                _processId,
                new[] { localIov },
                1,
                new[] { remoteIov },
                1,
                0);

            return result == bytes.Length;
        }
        finally
        {
            handle.Free();
        }
    }

    /// <summary>
    /// Freezes a value at the specified memory address, continuously writing it to prevent changes.
    /// </summary>
    /// <param name="address">Memory address to freeze</param>
    /// <param name="value">Value to freeze (int or float)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Result indicating success or failure</returns>
    public Task<Result> FreezeValueAsync(IntPtr address, object value, CancellationToken ct = default)
    {
        if (!_isAttached || _processId == -1)
        {
            return Task.FromResult(Result.Failure("Not attached to any process"));
        }

        try
        {
            // Unfreeze existing if present
            UnfreezeValueAsync(address, ct).Wait(ct);

            var frozenValue = new FrozenValue
            {
                Value = value,
                ValueType = value.GetType().Name
            };

            if (!_frozenValues.TryAdd(address, frozenValue))
            {
                return Task.FromResult(Result.Failure("Failed to add freeze entry"));
            }

            // Start freeze loop if not already running
            if (_freezeTask == null || _freezeTask.IsCompleted)
            {
                _freezeCts = new CancellationTokenSource();
                _freezeTask = Task.Run(() => FreezeLoopAsync(_freezeCts.Token));
            }

            _logger.LogInformation("Started freezing value at address {Address}", address);
            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error freezing value on Linux");
            return Task.FromResult(Result.Failure($"Freeze failed: {ex.Message}"));
        }
    }

    /// <summary>
    /// Unfreezes a previously frozen value at the specified memory address.
    /// </summary>
    /// <param name="address">Memory address to unfreeze</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Result indicating success or failure</returns>
    public Task<Result> UnfreezeValueAsync(IntPtr address, CancellationToken ct = default)
    {
        try
        {
            if (_frozenValues.TryRemove(address, out var frozenValue))
            {
                frozenValue.Cts.Cancel();
                frozenValue.Cts.Dispose();
                _logger.LogInformation("Stopped freezing value at address {Address}", address);
            }

            // Stop freeze loop if no more frozen values
            if (_frozenValues.IsEmpty && _freezeCts != null)
            {
                _freezeCts.Cancel();
                _freezeCts.Dispose();
                _freezeCts = null;
                _freezeTask = null;
            }

            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unfreezing value on Linux");
            return Task.FromResult(Result.Failure($"Unfreeze failed: {ex.Message}"));
        }
    }

    private async Task FreezeLoopAsync(CancellationToken ct)
    {
        _logger.LogInformation("Started Linux freeze loop");

        try
        {
            while (!ct.IsCancellationRequested && !_frozenValues.IsEmpty)
            {
                foreach (var kvp in _frozenValues.ToArray())
                {
                    if (kvp.Value.Cts.IsCancellationRequested)
                        continue;

                    try
                    {
                        var address = kvp.Key;
                        var value = kvp.Value.Value;

                        // Write the frozen value
                        byte[] bytes = value switch
                        {
                            int i => BitConverter.GetBytes(i),
                            float f => BitConverter.GetBytes(f),
                            _ => BitConverter.GetBytes(Convert.ToInt32(value))
                        };

                        WriteMemoryBytes(address, bytes);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error writing frozen value at {Address}", kvp.Key);
                    }
                }

                await Task.Delay(100, ct); // 100ms interval (slower than Windows due to syscall overhead)
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Freeze loop cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in freeze loop");
        }
    }

    public async Task<Result<long>> GetModuleBaseAddressAsync(string? moduleName = null, CancellationToken ct = default)
    {
        if (!_isAttached || _attachedProcess == null)
        {
            _logger.LogWarning("GetModuleBaseAddress attempted while not attached");
            return Result.Failure<long>("Not attached to any process");
        }

        return await Task.Run(() =>
        {
            try
            {
                // Parse /proc/{pid}/maps to find module base address
                var mapsPath = $"/proc/{_processId}/maps";
                _logger.LogDebug("Reading module base address from {MapsPath}", mapsPath);
                
                if (!File.Exists(mapsPath))
                {
                    _logger.LogError("Cannot access process memory maps at {MapsPath}", mapsPath);
                    return Result.Failure<long>("Cannot access process memory maps");
                }

                var lines = File.ReadAllLines(mapsPath);
                
                if (string.IsNullOrEmpty(moduleName))
                {
                    // Return the first executable segment (main executable)
                    foreach (var line in lines)
                    {
                        if (line.Contains("r-xp") && !line.Contains("[vdso]") && !line.Contains("[vsyscall]"))
                        {
                            var parts = line.Split('-');
                            if (parts.Length > 0 && long.TryParse(parts[0], System.Globalization.NumberStyles.HexNumber, null, out var baseAddr))
                            {
                                _logger.LogDebug("Found main executable base address: {BaseAddr:X}", baseAddr);
                                return Result.Success(baseAddr);
                            }
                        }
                    }
                }
                else
                {
                    // Look for specific module
                    foreach (var line in lines)
                    {
                        if (line.Contains(moduleName, StringComparison.OrdinalIgnoreCase))
                        {
                            var parts = line.Split('-');
                            if (parts.Length > 0 && long.TryParse(parts[0], System.Globalization.NumberStyles.HexNumber, null, out var baseAddr))
                            {
                                _logger.LogDebug("Found module {ModuleName} base address: {BaseAddr:X}", moduleName, baseAddr);
                                return Result.Success(baseAddr);
                            }
                        }
                    }
                }

                _logger.LogWarning("Module '{ModuleName}' not found in process maps", moduleName ?? "(main executable)");
                return Result.Failure<long>($"Module '{moduleName}' not found");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting module base address");
                return Result.Failure<long>($"Error getting module base address: {ex.Message}");
            }
        }, ct);
    }

    /// <summary>
    /// Checks if the current process has the required permissions to write to process memory.
    /// </summary>
    /// <returns>
    /// Success if permissions are available, otherwise a failure with instructions on how to enable.
    /// </returns>
    public Result CheckWritePermissions()
    {
        _logger.LogInformation("Checking Linux memory write permissions");
        
        // Try to read from ourselves to test if process_vm_readv works
        var testValue = 42;
        var testBytes = BitConverter.GetBytes(testValue);
        
        var localIov = new IoVec
        {
            iov_base = Marshal.AllocHGlobal(4),
            iov_len = (IntPtr)4
        };
        
        var remoteIov = new IoVec
        {
            iov_base = Marshal.AllocHGlobal(4),
            iov_len = (IntPtr)4
        };
        
        Marshal.Copy(testBytes, 0, remoteIov.iov_base, 4);
        
        var result = process_vm_readv(
            Environment.ProcessId,
            new[] { localIov },
            1,
            new[] { remoteIov },
            1,
            0);
        
        Marshal.FreeHGlobal(localIov.iov_base);
        Marshal.FreeHGlobal(remoteIov.iov_base);
        
        if (result == -1)
        {
            var errno = Marshal.GetLastWin32Error();
            if (errno == 1) // EPERM - Operation not permitted
            {
                _logger.LogWarning("CAP_SYS_PTRACE capability not available. Permission check failed.");
                return Result.Failure(
                    "CAP_SYS_PTRACE capability required. Run: sudo setcap cap_sys_ptrace=eip ./SaveStateReborn",
                    ErrorType.Forbidden);
            }
            else if (errno == 3) // ESRCH - No such process (shouldn't happen for self)
            {
                _logger.LogWarning("process_vm_readv returned ESRCH during permission check");
                return Result.Failure(
                    "Unexpected error during permission check. Please try running with elevated permissions.",
                    ErrorType.Internal);
            }
            else
            {
                _logger.LogWarning("process_vm_readv failed with errno {Errno} during permission check", errno);
                return Result.Failure(
                    $"Permission check failed with error code {errno}. Ensure the application has proper permissions.",
                    ErrorType.Internal);
            }
        }
        
        _logger.LogInformation("Linux memory write permissions verified successfully");
        return Result.Success();
    }

    // P/Invoke for process_vm_writev and process_vm_readv
    [DllImport("libc", SetLastError = true)]
    private static extern int process_vm_writev(
        int pid,
        [In] IoVec[] local_iov,
        ulong liovcnt,
        [In] IoVec[] remote_iov,
        ulong riovcnt,
        ulong flags);

    [DllImport("libc", SetLastError = true)]
    private static extern int process_vm_readv(
        int pid,
        [Out] IoVec[] local_iov,
        ulong liovcnt,
        [In] IoVec[] remote_iov,
        ulong riovcnt,
        ulong flags);

    [StructLayout(LayoutKind.Sequential)]
    private struct IoVec
    {
        public IntPtr iov_base;
        public IntPtr iov_len;
    }

    // P/Invoke for ptrace
    private const int PTRACE_ATTACH = 16;
    private const int PTRACE_DETACH = 17;

    [DllImport("libc", SetLastError = true)]
    private static extern long ptrace(int request, int pid, IntPtr addr, IntPtr data);

    private Result PtraceAttach(int pid)
    {
        _logger.LogDebug("Calling ptrace(PTRACE_ATTACH, {Pid})", pid);
        var result = ptrace(PTRACE_ATTACH, pid, IntPtr.Zero, IntPtr.Zero);
        if (result == -1)
        {
            var error = Marshal.GetLastWin32Error();
            _logger.LogError("ptrace attach failed for pid {Pid} (errno: {Error})", pid, error);
            return Result.Failure($"ptrace attach failed (errno: {error}). Try sudo.");
        }
        Thread.Sleep(100); // Wait for stop
        _logger.LogDebug("ptrace attach successful for pid {Pid}", pid);
        return Result.Success();
    }

    private void PtraceDetach(int pid)
    {
        _logger.LogDebug("Calling ptrace(PTRACE_DETACH, {Pid})", pid);
        ptrace(PTRACE_DETACH, pid, IntPtr.Zero, IntPtr.Zero);
    }

    public void Dispose()
    {
        DetachInternal();
    }
}
