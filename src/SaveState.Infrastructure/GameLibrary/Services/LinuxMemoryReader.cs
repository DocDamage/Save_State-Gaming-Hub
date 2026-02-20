using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Services;
using SaveState.Application.Common;

namespace SaveState.Infrastructure.GameLibrary.Services;

/// <summary>
/// Linux implementation of game memory reading using ptrace and /proc/{pid}/mem.
/// </summary>
public sealed class LinuxMemoryReader : IGameMemoryReader, IDisposable
{
    private readonly ILogger<LinuxMemoryReader> _logger;
    private int _processId = -1;
    private bool _isAttached;
    private FileStream? _memStream;
    private Process? _attachedProcess;
    private readonly object _lock = new();

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
            _logger.LogInformation("Detaching from process {ProcessId}", _processId);
            
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

            _logger.LogInformation("Successfully detached from process");
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detaching from process");
            return Result.Failure($"Detach failed: {ex.Message}");
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

    public Task<Result> WriteMemoryAsync(IntPtr address, int value, CancellationToken ct = default)
    {
        _logger.LogWarning("Write not implemented on Linux (address: {Address:X}, value: {Value})", address.ToInt64(), value);
        return Task.FromResult(Result.Failure("Write not implemented on Linux"));
    }

    public Task<Result> WriteMemoryAsync(IntPtr address, float value, CancellationToken ct = default)
    {
        _logger.LogWarning("Write not implemented on Linux (address: {Address:X}, value: {Value})", address.ToInt64(), value);
        return Task.FromResult(Result.Failure("Write not implemented on Linux"));
    }

    public Task<Result> FreezeValueAsync(IntPtr address, object value, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Freeze value requested for address {Address} with value {Value} (not implemented on Linux)", 
            address, 
            value);
        return Task.FromResult(Result.Failure("Freeze not implemented on Linux"));
    }

    public Task<Result> UnfreezeValueAsync(IntPtr address, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Unfreeze value requested for address {Address} (not implemented on Linux)", 
            address);
        return Task.FromResult(Result.Failure("Unfreeze not implemented on Linux"));
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
