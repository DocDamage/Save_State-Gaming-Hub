using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Performance.Services;
using SaveState.Core.Performance.ValueObjects;

namespace SaveState.Infrastructure.Performance;

/// <summary>
/// Windows-specific memory reader using P/Invoke.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WindowsMemoryReader : IMemoryReader, IDisposable
{
    private readonly ILogger<WindowsMemoryReader> _logger;
    private readonly Dictionary<int, IntPtr> _processHandles = new();
    private readonly SemaphoreSlim _rateLimiter = new(60, 60); // 60 reads per second max
    private bool _disposed;

    public WindowsMemoryReader(ILogger<WindowsMemoryReader> logger)
    {
        _logger = logger;
    }

    public async Task<Result<byte[]>> ReadMemoryAsync(
        int processId,
        MemoryAddress address,
        int size,
        CancellationToken ct = default)
    {
        if (size <= 0 || size > 1024 * 1024) // Max 1MB per read
        {
            return Result.Failure<byte[]>("Invalid read size. Must be between 1 and 1,048,576 bytes.", ErrorType.Validation);
        }

        // Rate limiting
        await _rateLimiter.WaitAsync(ct);
        try
        {
            var handleResult = GetProcessHandle(processId);
            if (!handleResult.IsSuccess)
            {
                return handleResult.ToResult<byte[]>();
            }

            var handle = handleResult.Value;
            var targetAddress = address.FinalAddress;

            // Resolve pointer chain if needed
            if (address.IsPointerChain)
            {
                var resolveResult = await ResolvePointerChainAsync(processId, address, ct);
                if (!resolveResult.IsSuccess)
                {
                    return resolveResult.ToResult<byte[]>();
                }
                targetAddress = resolveResult.Value;
            }

            var buffer = new byte[size];
            var success = ReadProcessMemory(
                handle,
                (IntPtr)targetAddress,
                buffer,
                (uint)size,
                out var bytesRead);

            if (!success || bytesRead != size)
            {
                var error = Marshal.GetLastWin32Error();
                _logger.LogWarning("Failed to read memory at 0x{Address:X}. Error: {Error}", targetAddress, error);
                return Result.Failure<byte[]>($"Memory read failed at address 0x{targetAddress:X}. Error code: {error}", ErrorType.External);
            }

            return Result.Success(buffer);
        }
        finally
        {
            _rateLimiter.Release();
        }
    }

    public async Task<Result<T>> ReadValueAsync<T>(
        int processId,
        MemoryAddress address,
        CancellationToken ct = default) where T : struct
    {
        var size = Marshal.SizeOf<T>();
        var bytesResult = await ReadMemoryAsync(processId, address, size, ct);

        if (!bytesResult.IsSuccess)
        {
            return bytesResult.ToResult<T>();
        }

        try
        {
            var handle = GCHandle.Alloc(bytesResult.Value, GCHandleType.Pinned);
            try
            {
                var value = Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject());
                return Result.Success(value);
            }
            finally
            {
                handle.Free();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to convert bytes to type {Type}", typeof(T).Name);
            return Result.Failure<T>($"Type conversion failed: {ex.Message}", ErrorType.Validation);
        }
    }

    public async Task<Result<long>> ResolvePointerChainAsync(
        int processId,
        MemoryAddress address,
        CancellationToken ct = default)
    {
        if (!address.IsPointerChain)
        {
            return Result.Success(address.FinalAddress);
        }

        var currentAddress = address.BaseAddress;

        // Determine pointer size (4 bytes for 32-bit, 8 bytes for 64-bit)
        var pointerSize = IntPtr.Size;

        foreach (var offset in address.Offsets)
        {
            // Read pointer at current address (size depends on process architecture)
            var bytesResult = await ReadMemoryAsync(processId, MemoryAddress.Create(currentAddress), pointerSize, ct);
            if (!bytesResult.IsSuccess)
            {
                return Result.Failure<long>($"Failed to resolve pointer at 0x{currentAddress:X}: {bytesResult.Error}", ErrorType.External);
            }

            // Convert bytes to pointer value
            var pointerValue = pointerSize == 8
                ? BitConverter.ToInt64(bytesResult.Value, 0)
                : BitConverter.ToInt32(bytesResult.Value, 0);

            currentAddress = pointerValue + offset;
        }

        address.UpdateFinalAddress(currentAddress);
        return Result.Success(currentAddress);
    }

    public async Task<Result> WriteMemoryAsync(
        int processId,
        MemoryAddress address,
        byte[] buffer,
        CancellationToken ct = default)
    {
        if (buffer == null || buffer.Length == 0)
        {
            return Result.Failure("Invalid write buffer.", ErrorType.Validation);
        }

        await _rateLimiter.WaitAsync(ct);
        try
        {
            var handleResult = GetProcessHandle(processId);
            if (!handleResult.IsSuccess)
            {
                return handleResult;
            }

            var handle = handleResult.Value;
            var targetAddress = address.FinalAddress;

            if (address.IsPointerChain)
            {
                var resolveResult = await ResolvePointerChainAsync(processId, address, ct);
                if (!resolveResult.IsSuccess)
                {
                    return resolveResult;
                }
                targetAddress = resolveResult.Value;
            }

            var success = WriteProcessMemory(
                handle,
                (IntPtr)targetAddress,
                buffer,
                (uint)buffer.Length,
                out var bytesWritten);

            if (!success || bytesWritten != buffer.Length)
            {
                var error = Marshal.GetLastWin32Error();
                _logger.LogWarning("Failed to write memory at 0x{Address:X}. Error: {Error}", targetAddress, error);
                return Result.Failure($"Memory write failed at address 0x{targetAddress:X}. Error code: {error}", ErrorType.External);
            }

            return Result.Success();
        }
        finally
        {
            _rateLimiter.Release();
        }
    }

    public async Task<Result> WriteValueAsync<T>(
        int processId,
        MemoryAddress address,
        T value,
        CancellationToken ct = default) where T : struct
    {
        var size = Marshal.SizeOf<T>();
        var buffer = new byte[size];

        try
        {
            var ptr = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.StructureToPtr(value, ptr, false);
                Marshal.Copy(ptr, buffer, 0, size);
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to convert type {Type} to bytes", typeof(T).Name);
            return Result.Failure($"Type conversion failed: {ex.Message}", ErrorType.Validation);
        }

        return await WriteMemoryAsync(processId, address, buffer, ct);
    }

    public Result<bool> CanAccessProcess(int processId)
    {
        try
        {
            var process = Process.GetProcessById(processId);
            if (process.HasExited)
            {
                return Result.Failure<bool>("Process has exited.", ErrorType.NotFound);
            }

            // Try to open with read/write permissions
            var handle = OpenProcess(ProcessAccessFlags.VMRead | ProcessAccessFlags.VMWrite | ProcessAccessFlags.VMOperation | ProcessAccessFlags.QueryInformation, false, processId);
            if (handle == IntPtr.Zero)
            {
                var error = Marshal.GetLastWin32Error();
                return Result.Failure<bool>($"Cannot access process. Error code: {error}. Try running as administrator.", ErrorType.Unauthorized);
            }

            CloseHandle(handle);
            return Result.Success(true);
        }
        catch (ArgumentException)
        {
            return Result.Failure<bool>("Process not found.", ErrorType.NotFound);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking process access for PID {ProcessId}", processId);
            return Result.Failure<bool>($"Access check failed: {ex.Message}", ErrorType.External);
        }
    }

    public async Task<Result<long>> GetModuleBaseAddressAsync(
        int processId,
        string moduleName,
        CancellationToken ct = default)
    {
        try
        {
            var process = Process.GetProcessById(processId);
            await Task.Run(() =>
            {
                process.Refresh();
            }, ct);

            // If moduleName is null or empty, use the main module
            var module = string.IsNullOrEmpty(moduleName)
                ? process.MainModule
                : process.Modules.Cast<ProcessModule>()
                    .FirstOrDefault(m => m.ModuleName?.Equals(moduleName, StringComparison.OrdinalIgnoreCase) == true);

            if (module == null)
            {
                return Result.Failure<long>($"Module '{moduleName ?? "MainModule"}' not found in process.", ErrorType.NotFound);
            }

            return Result.Success(module.BaseAddress.ToInt64());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get module base address for {ModuleName}", moduleName);
            return Result.Failure<long>($"Module lookup failed: {ex.Message}", ErrorType.External);
        }
    }

    private Result<IntPtr> GetProcessHandle(int processId)
    {
        if (_processHandles.TryGetValue(processId, out var existingHandle))
        {
            return Result.Success(existingHandle);
        }

        var handle = OpenProcess(ProcessAccessFlags.VMRead | ProcessAccessFlags.VMWrite | ProcessAccessFlags.VMOperation | ProcessAccessFlags.QueryInformation, false, processId);
        if (handle == IntPtr.Zero)
        {
            var error = Marshal.GetLastWin32Error();
            return Result.Failure<IntPtr>($"Failed to open process {processId}. Error: {error}", ErrorType.Unauthorized);
        }

        _processHandles[processId] = handle;
        return Result.Success(handle);
    }

    public void Dispose()
    {
        if (_disposed) return;

        foreach (var handle in _processHandles.Values)
        {
            CloseHandle(handle);
        }
        _processHandles.Clear();
        _rateLimiter.Dispose();

        _disposed = true;
    }

    #region P/Invoke

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(
        ProcessAccessFlags processAccess,
        bool bInheritHandle,
        int processId);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool ReadProcessMemory(
        IntPtr hProcess,
        IntPtr lpBaseAddress,
        [Out] byte[] lpBuffer,
        uint dwSize,
        out uint lpNumberOfBytesRead);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool WriteProcessMemory(
        IntPtr hProcess,
        IntPtr lpBaseAddress,
        byte[] lpBuffer,
        uint dwSize,
        out uint lpNumberOfBytesWritten);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(IntPtr hObject);

    [Flags]
    private enum ProcessAccessFlags : uint
    {
        VMOperation = 0x0008,
        VMRead = 0x0010,
        VMWrite = 0x0020,
        QueryInformation = 0x0400
    }

    #endregion
}
