using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Services;

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
        try
        {
            if (_isAttached)
                return Task.FromResult(Result.Failure("Already attached to a process. Detach first."));

            if (!Directory.Exists($"/proc/{processId}"))
                return Task.FromResult(Result.Failure($"Process {processId} not found"));

            _attachedProcess = Process.GetProcessById(processId);
            if (_attachedProcess == null)
                return Task.FromResult(Result.Failure($"Process {processId} not found"));

            // Attach via ptrace
            var result = PtraceAttach(processId);
            if (!result.IsSuccess)
                return Task.FromResult(result);

            // Open /proc/{pid}/mem
            var memPath = $"/proc/{processId}/mem";
            try
            {
                _memStream = new FileStream(memPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            }
            catch (Exception ex)
            {
                PtraceDetach(processId);
                return Task.FromResult(Result.Failure($"Failed to open memory: {ex.Message}"));
            }

            _processId = processId;
            _isAttached = true;

            _logger.LogInformation("Attached to process {ProcessId} on Linux", processId);
            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result.Failure($"Attach failed: {ex.Message}"));
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
            return Result.Failure($"Detach failed: {ex.Message}");
        }
    }

    public Task<Result<IReadOnlyList<MemoryPattern>>> DetectPatternsAsync(CancellationToken ct = default)
    {
        if (!_isAttached)
            return Task.FromResult(Result.Failure<IReadOnlyList<MemoryPattern>>("Not attached to any process"));

        // Simplified implementation - would scan for common patterns
        var patterns = new List<MemoryPattern>();
        return Task.FromResult(Result.Success<IReadOnlyList<MemoryPattern>>(patterns));
    }

    public Task<Result<byte[]>> ReadMemoryBytesAsync(IntPtr address, int length, CancellationToken ct = default)
    {
        if (!_isAttached || _memStream == null)
            return Task.FromResult(Result.Failure<byte[]>("Not attached to any process"));

        try
        {
            lock (_lock)
            {
                _memStream.Seek(address.ToInt64(), SeekOrigin.Begin);
                var buffer = new byte[length];
                var bytesRead = _memStream.Read(buffer, 0, length);
                if (bytesRead < length)
                    Array.Resize(ref buffer, bytesRead);
                return Task.FromResult(Result.Success(buffer));
            }
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result.Failure<byte[]>($"Read failed: {ex.Message}"));
        }
    }

    public Task<Result> WriteMemoryAsync(IntPtr address, int value, CancellationToken ct = default)
    {
        return Task.FromResult(Result.Failure("Write not implemented on Linux"));
    }

    public Task<Result> WriteMemoryAsync(IntPtr address, float value, CancellationToken ct = default)
    {
        return Task.FromResult(Result.Failure("Write not implemented on Linux"));
    }

    public Task<Result> FreezeValueAsync(IntPtr address, object value, CancellationToken ct = default)
    {
        _logger.LogInformation("Freeze value requested for address {Address} with value {Value} (not implemented on Linux)", address, value);
        return Task.FromResult(Result.Failure("Freeze not implemented on Linux"));
    }

    public Task<Result> UnfreezeValueAsync(IntPtr address, CancellationToken ct = default)
    {
        _logger.LogInformation("Unfreeze value requested for address {Address} (not implemented on Linux)", address);
        return Task.FromResult(Result.Failure("Unfreeze not implemented on Linux"));
    }

    public async Task<Result<long>> GetModuleBaseAddressAsync(string? moduleName = null, CancellationToken ct = default)
    {
        if (!_isAttached || _attachedProcess == null)
            return Result.Failure<long>("Not attached to any process");

        return await Task.Run(() =>
        {
            try
            {
                // Parse /proc/{pid}/maps to find module base address
                var mapsPath = $"/proc/{_processId}/maps";
                if (!File.Exists(mapsPath))
                    return Result.Failure<long>("Cannot access process memory maps");

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
                                return Result.Success(baseAddr);
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
                                return Result.Success(baseAddr);
                        }
                    }
                }

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
        var result = ptrace(PTRACE_ATTACH, pid, IntPtr.Zero, IntPtr.Zero);
        if (result == -1)
        {
            var error = Marshal.GetLastWin32Error();
            return Result.Failure($"ptrace attach failed (errno: {error}). Try sudo.");
        }
        Thread.Sleep(100); // Wait for stop
        return Result.Success();
    }

    private void PtraceDetach(int pid)
    {
        ptrace(PTRACE_DETACH, pid, IntPtr.Zero, IntPtr.Zero);
    }

    public void Dispose()
    {
        DetachInternal();
    }
}
