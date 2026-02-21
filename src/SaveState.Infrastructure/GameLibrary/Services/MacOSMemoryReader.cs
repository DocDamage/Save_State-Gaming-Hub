using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Services;
using SaveState.Application.Common;

namespace SaveState.Infrastructure.GameLibrary.Services;

/// <summary>
/// macOS implementation of game memory reading using Mach kernel APIs.
/// Requires code signing with specific entitlements for memory access.
/// </summary>
public sealed class MacOSMemoryReader : IGameMemoryReader, IDisposable
{
    private readonly ILogger<MacOSMemoryReader> _logger;
    private int _processId = -1;
    private IntPtr _task = IntPtr.Zero;  // Mach task port
    private bool _isAttached;
    private readonly Timer _monitoringTimer;
    public event EventHandler<GameStateChangedEventArgs>? StateChanged;
    public bool IsAttached => _isAttached;

    // Mach API Constants
    private const int KERN_SUCCESS = 0;
    private const int TASK_DYLD_INFO = 17;
    
    // vm_prot_t values
    private const int VM_PROT_NONE = 0x00;
    private const int VM_PROT_READ = 0x01;
    private const int VM_PROT_WRITE = 0x02;
    private const int VM_PROT_COPY = 0x08;
    private const int VM_PROT_EXECUTE = 0x04;

    public MacOSMemoryReader(ILogger<MacOSMemoryReader> logger)
    {
        _logger = logger;
        _monitoringTimer = new Timer(MonitorGameState, null, Timeout.Infinite, Timeout.Infinite);
    }

    public Task<Result> AttachToProcessAsync(int processId, CancellationToken ct = default)
    {
        using (_logger.BeginCorrelationScope())
        using (_logger.BeginMemoryScanScope(processId, "Unknown"))
        {
            try
            {
                if (_isAttached)
                {
                    _logger.LogWarning("Already attached to a process. Detach first.");
                    return Task.FromResult(Result.Failure("Already attached to a process. Detach first."));
                }

                _logger.LogInformation("Attempting to attach to process {ProcessId} on macOS", processId);

                // On macOS, we need to use task_for_pid() to get the Mach task port
                // This requires either:
                // 1. Running as root
                // 2. Code signing with specific entitlements
                // 3. The user to approve in System Preferences > Security > Privacy

                var result = task_for_pid(mach_task_self(), processId, out _task);
                
                if (result != KERN_SUCCESS)
                {
                    var error = GetMachErrorString(result);
                    _logger.LogError(
                        "task_for_pid failed with error {ErrorCode}: {Error}. " +
                        "Ensure the app has appropriate entitlements or run with sudo.",
                        result, error);
                    
                    return Task.FromResult(Result.Failure(
                        $"Failed to attach to process: {error}. " +
                        "On macOS, this requires code signing entitlements or running with sudo."));
                }

                _processId = processId;
                _isAttached = true;

                _logger.LogInformation(
                    "Successfully attached to process {ProcessId} on macOS (task: {Task})",
                    processId, _task);

                // Start monitoring
                _monitoringTimer.Change(TimeSpan.Zero, TimeSpan.FromSeconds(2));

                return Task.FromResult(Result.Success());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error attaching to process {ProcessId}", processId);
                return Task.FromResult(Result.Failure($"Failed to attach: {ex.Message}"));
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
            if (!_isAttached)
            {
                return Result.Success();
            }

            _logger.LogInformation("Detaching from process on macOS");

            _monitoringTimer.Change(Timeout.Infinite, Timeout.Infinite);

            if (_task != IntPtr.Zero)
            {
                var deallocResult = mach_port_deallocate(mach_task_self(), _task);
                if (deallocResult != KERN_SUCCESS)
                {
                    _logger.LogWarning("Failed to deallocate Mach port: {Error}", GetMachErrorString(deallocResult));
                }
                _task = IntPtr.Zero;
            }

            _processId = -1;
            _isAttached = false;
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detaching from process");
            return Result.Failure($"Failed to detach: {ex.Message}");
        }
    }

    public Task<Result<IReadOnlyList<MemoryPattern>>> DetectPatternsAsync(CancellationToken ct = default)
    {
        using (_logger.BeginCorrelationScope())
        {
            _logger.LogInformation("Starting pattern detection on macOS");
            
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            if (!_isAttached || _task == IntPtr.Zero)
            {
                stopwatch.Stop();
                _logger.LogWarning("Pattern detection attempted while not attached");
                return Task.FromResult(Result.Failure<IReadOnlyList<MemoryPattern>>("Not attached to any process"));
            }

            try
            {
                var patterns = new List<MemoryPattern>();

                // Get process info
                var processName = GetProcessName(_processId);
                _logger.LogInformation(
                    "Detecting patterns for process {ProcessId} ({ProcessName})",
                    _processId, processName);

                // Get memory regions
                var regions = GetMemoryRegions();
                _logger.LogDebug("Found {RegionCount} memory regions", regions.Count);
                
                foreach (var region in regions)
                {
                    ct.ThrowIfCancellationRequested();
                    
                    // Only scan readable regions
                    if ((region.Protection & VM_PROT_READ) == 0)
                        continue;

                    // Skip kernel and system regions
                    if (region.Address.ToInt64() < 0x100000000)
                        continue;

                    // Scan this region for patterns
                    var regionPatterns = ScanRegionForPatterns(region);
                    patterns.AddRange(regionPatterns);
                }

                stopwatch.Stop();
                _logger.LogInformation(
                    "Pattern detection completed. Found {Count} patterns in {ElapsedMs}ms on macOS", 
                    patterns.Count,
                    stopwatch.ElapsedMilliseconds);
                    
                return Task.FromResult(Result.Success<IReadOnlyList<MemoryPattern>>(patterns));
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Error detecting memory patterns on macOS");
                return Task.FromResult(Result.Failure<IReadOnlyList<MemoryPattern>>($"Detection failed: {ex.Message}"));
            }
        }
    }

    public Task<Result<byte[]>> ReadMemoryBytesAsync(IntPtr address, int length, CancellationToken ct = default)
    {
        if (!_isAttached || _task == IntPtr.Zero)
        {
            _logger.LogWarning("Read attempted while not attached");
            return Task.FromResult(Result.Failure<byte[]>("Not attached to any process"));
        }

        try
        {
            var buffer = new byte[length];
            var data = IntPtr.Zero;
            var dataCount = 0;

            // Use vm_read to read process memory
            var result = vm_read(
                _task,
                (ulong)address.ToInt64(),
                (ulong)length,
                out data,
                out dataCount);

            if (result != KERN_SUCCESS)
            {
                var error = GetMachErrorString(result);
                _logger.LogError("vm_read failed at address {Address:X} with error {Error}", address.ToInt64(), error);
                return Task.FromResult(Result.Failure<byte[]>($"vm_read failed: {error}"));
            }

            // Copy data from Mach buffer to managed buffer
            Marshal.Copy(data, buffer, 0, (int)dataCount);
            
            // Deallocate the Mach buffer
            var deallocResult = vm_deallocate(mach_task_self(), data, (uint)dataCount);
            if (deallocResult != KERN_SUCCESS)
            {
                _logger.LogWarning("Failed to deallocate Mach buffer: {Error}", GetMachErrorString(deallocResult));
            }

            _logger.LogDebug("Read {BytesRead} bytes from address {Address:X}", dataCount, address.ToInt64());
            return Task.FromResult(Result.Success(buffer));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading memory at address {Address:X}", address.ToInt64());
            return Task.FromResult(Result.Failure<byte[]>($"Read failed: {ex.Message}"));
        }
    }

    public Task<Result> WriteMemoryAsync(IntPtr address, int value, CancellationToken ct = default)
    {
        _logger.LogWarning(
            "Write not implemented on macOS (address: {Address:X}, value: {Value})", 
            address.ToInt64(), 
            value);
        // Memory writing on macOS requires VM_PROT_WRITE permission
        // and is more restricted than reading
        return Task.FromResult(Result.Failure(
            "Memory writing on macOS is restricted. " +
            "Consider using vm_protect to change protection first."));
    }

    public Task<Result> WriteMemoryAsync(IntPtr address, float value, CancellationToken ct = default)
    {
        _logger.LogWarning(
            "Write not implemented on macOS (address: {Address:X}, value: {Value})", 
            address.ToInt64(), 
            value);
        return Task.FromResult(Result.Failure(
            "Memory writing on macOS is restricted. " +
            "Consider using vm_protect to change protection first."));
    }

    public Task<Result> FreezeValueAsync(IntPtr address, object value, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Freeze value requested for address {Address} with value {Value} (not implemented on macOS)", 
            address, 
            value);
        return Task.FromResult(Result.Failure("Value freezing not yet implemented on macOS"));
    }

    public Task<Result> UnfreezeValueAsync(IntPtr address, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Unfreeze value requested for address {Address} (not implemented on macOS)", 
            address);
        return Task.FromResult(Result.Failure("Value freezing not yet implemented on macOS"));
    }

    public async Task<Result<long>> GetModuleBaseAddressAsync(string? moduleName = null, CancellationToken ct = default)
    {
        if (!_isAttached || _task == IntPtr.Zero)
        {
            _logger.LogWarning("GetModuleBaseAddress attempted while not attached");
            return Result.Failure<long>("Not attached to any process");
        }

        return await Task.Run(() =>
        {
            try
            {
                _logger.LogDebug("Getting module base address for '{ModuleName}'", moduleName ?? "(main executable)");

                // Get memory regions and look for the executable
                var regions = GetMemoryRegions();
                
                if (string.IsNullOrEmpty(moduleName))
                {
                    // Find the main executable - typically the first executable region with read+execute
                    foreach (var region in regions)
                    {
                        // Look for readable and executable regions
                        if ((region.Protection & VM_PROT_READ) != 0 && 
                            (region.Protection & VM_PROT_EXECUTE) != 0 &&
                            region.Address.ToInt64() > 0x100000000) // Skip low memory
                        {
                            _logger.LogDebug("Found main executable base address: {BaseAddr:X}", region.Address.ToInt64());
                            return Result.Success(region.Address.ToInt64());
                        }
                    }
                }
                else
                {
                    // Try to find specific module using vmmap
                    var moduleAddress = FindModuleAddress(moduleName);
                    if (moduleAddress != 0)
                    {
                        _logger.LogDebug("Found module {ModuleName} base address: {BaseAddr:X}", moduleName, moduleAddress);
                        return Result.Success(moduleAddress);
                    }
                }

                _logger.LogWarning("Module '{ModuleName}' not found in process memory", moduleName ?? "(main executable)");
                return Result.Failure<long>($"Module '{moduleName}' not found");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting module base address");
                return Result.Failure<long>($"Error getting module base address: {ex.Message}");
            }
        }, ct);
    }

    // Mach API P/Invoke Declarations

    [DllImport("/usr/lib/libSystem.dylib")]
    private static extern int task_for_pid(IntPtr target_tport, int pid, out IntPtr t);

    [DllImport("/usr/lib/libSystem.dylib")]
    private static extern IntPtr mach_task_self();

    [DllImport("/usr/lib/libSystem.dylib")]
    private static extern int mach_port_deallocate(IntPtr task, IntPtr name);

    [DllImport("/usr/lib/libSystem.dylib")]
    private static extern int vm_read(
        IntPtr target_task,
        ulong address,
        ulong size,
        out IntPtr data,
        out int dataCnt);

    [DllImport("/usr/lib/libSystem.dylib")]
    private static extern int vm_deallocate(IntPtr target_task, IntPtr address, uint size);

    [DllImport("/usr/lib/libSystem.dylib")]
    private static extern int vm_region_recurse_64(
        IntPtr target_task,
        ref ulong address,
        ref ulong size,
        ref uint depth,
        ref vm_region_submap_info_64 info,
        ref uint infoCount);

    // Helper methods
    private string GetProcessName(int processId)
    {
        try
        {
            // Use ps command to get process name
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "ps",
                Arguments = $"-p {processId} -o comm=",
                RedirectStandardOutput = true,
                UseShellExecute = false
            };
            
            using var proc = System.Diagnostics.Process.Start(psi);
            return proc?.StandardOutput.ReadLine()?.Trim() ?? "Unknown";
        }
        catch
        {
            return "Unknown";
        }
    }

    private List<MemoryRegionInfo> GetMemoryRegions()
    {
        var regions = new List<MemoryRegionInfo>();
        ulong address = 0;
        
        while (true)
        {
            var info = new vm_region_submap_info_64();
            uint depth = 0;
            uint infoCount = (uint)(Marshal.SizeOf<vm_region_submap_info_64>() / sizeof(int));
            ulong size = 0;

            var result = vm_region_recurse_64(
                _task,
                ref address,
                ref size,
                ref depth,
                ref info,
                ref infoCount);

            if (result != KERN_SUCCESS)
                break;

            regions.Add(new MemoryRegionInfo
            {
                Address = (IntPtr)(long)address,
                Size = (long)size,
                Protection = (int)info.protection,
                IsSubmap = info.is_submap != 0
            });

            address += size;
            
            // Safety limit
            if (regions.Count > 10000)
                break;
        }

        return regions;
    }

    private List<MemoryPattern> ScanRegionForPatterns(MemoryRegionInfo region)
    {
        var patterns = new List<MemoryPattern>();
        
        try
        {
            // Read a sample of the region to look for common patterns
            var sampleSize = (int)Math.Min(region.Size, 4096);
            var result = ReadMemoryBytesAsync(region.Address, sampleSize, default).Result;
            
            if (result.IsFailure)
                return patterns;

            var data = result.Value;
            
            // Look for common game value patterns (simplified)
            // In a real implementation, this would scan for specific signatures
            
            return patterns;
        }
        catch
        {
            return patterns;
        }
    }

    private long FindModuleAddress(string moduleName)
    {
        try
        {
            // Use vmmap to find module addresses
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "vmmap",
                Arguments = $"{_processId}",
                RedirectStandardOutput = true,
                UseShellExecute = false
            };
            
            using var proc = System.Diagnostics.Process.Start(psi);
            if (proc == null)
                return 0;

            var output = proc.StandardOutput.ReadToEnd();
            var lines = output.Split('\n');
            
            foreach (var line in lines)
            {
                if (line.Contains(moduleName, StringComparison.OrdinalIgnoreCase))
                {
                    // Parse address from line like "0000000100000000-00000001000c4000"
                    var parts = line.Split(new[] { ' ', '-' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 0 && long.TryParse(parts[0], System.Globalization.NumberStyles.HexNumber, null, out var addr))
                    {
                        return addr;
                    }
                }
            }
        }
        catch
        {
            // vmmap might not be available or might fail
        }
        
        return 0;
    }

    private string GetMachErrorString(int errorCode)
    {
        return errorCode switch
        {
            1 => "KERN_INVALID_ADDRESS",
            2 => "KERN_PROTECTION_FAILURE",
            3 => "KERN_NO_SPACE",
            4 => "KERN_INVALID_ARGUMENT",
            5 => "KERN_FAILURE",
            6 => "KERN_RESOURCE_SHORTAGE",
            7 => "KERN_NOT_RECEIVER",
            8 => "KERN_NO_ACCESS",
            9 => "KERN_MEMORY_FAILURE",
            10 => "KERN_MEMORY_ERROR",
            11 => "KERN_ALREADY_IN_SET",
            12 => "KERN_NOT_IN_SET",
            13 => "KERN_NAME_EXISTS",
            14 => "KERN_ABORTED",
            15 => "KERN_INVALID_NAME",
            16 => "KERN_INVALID_TASK",
            17 => "KERN_INVALID_RIGHT",
            18 => "KERN_INVALID_VALUE",
            19 => "KERN_UREFS_OVERFLOW",
            20 => "KERN_INVALID_CAPABILITY",
            21 => "KERN_RIGHT_EXISTS",
            22 => "KERN_INVALID_HOST",
            23 => "KERN_MEMORY_PRESENT",
            24 => "KERN_MEMORY_DATA_MOVED",
            25 => "KERN_MEMORY_RESTART_COPY",
            26 => "KERN_INVALID_PROCESSOR_SET",
            27 => "KERN_POLICY_LIMIT",
            28 => "KERN_INVALID_POLICY",
            29 => "KERN_INVALID_OBJECT",
            30 => "KERN_ALREADY_WAITING",
            31 => "KERN_DEFAULT_SET",
            32 => "KERN_EXCEPTION_PROTECTED",
            33 => "KERN_INVALID_LEDGER",
            34 => "KERN_INVALID_MEMORY_CONTROL",
            35 => "KERN_INVALID_SECURITY",
            _ => $"Unknown error ({errorCode})"
        };
    }

    private void MonitorGameState(object? state)
    {
        if (!_isAttached)
            return;

        try
        {
            // Simplified game state detection
            // In a real implementation, this would scan for specific game state indicators
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error monitoring game state");
        }
    }

    public void Dispose()
    {
        _monitoringTimer?.Dispose();
        DetachInternal();
        GC.SuppressFinalize(this);
    }

    // Helper classes
    private class MemoryRegionInfo
    {
        public IntPtr Address { get; set; }
        public long Size { get; set; }
        public int Protection { get; set; }
        public bool IsSubmap { get; set; }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct vm_region_submap_info_64
    {
        public uint protection;
        public uint max_protection;
        public uint inheritance;
        public ulong offset;
        public uint user_tag;
        public uint pages_resident;
        public uint pages_shared_now_private;
        public uint pages_swapped_out;
        public uint pages_dirtied;
        public uint ref_count;
        public short shadow_depth;
        public byte external_pager;
        public byte share_mode;
        public int is_submap;
        public uint behavior;
        public uint object_id;
        public uint user_wired_count;
    }
}
