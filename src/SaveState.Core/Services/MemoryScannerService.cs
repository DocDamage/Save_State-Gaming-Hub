using System.Diagnostics;
using System.Runtime.InteropServices;
using SaveState.Core.Infrastructure;
using SaveState.Core.Interfaces;
using SaveState.Core.Models;
using Serilog;

namespace SaveState.Core.Services;

public class MemoryScannerService : IMemoryScannerService
{
    private readonly ILogger _logger = Log.ForContext<MemoryScannerService>();
    private IntPtr _processHandle = IntPtr.Zero;
    private Process? _attachedProcess;
    
    // MBAD: Counters for anomaly detection
    private int _readCount = 0;
    private int _writeCount = 0;
    private DateTime _lastSnapshotTime = DateTime.UtcNow;
    
    // Cached scan results for NextScan functionality
    private List<long> _lastScanResults = new();
    private bool _isFloatScan = false;
    
    public int? CurrentProcessId => _attachedProcess?.Id;
    public List<long> LastScanResults => _lastScanResults;

    public bool Attach(Process process)
    {
        try
        {
            Detach(); // Detach if already attached

            _attachedProcess = process;
            _processHandle = NativeMethods.OpenProcess(
                NativeMethods.PROCESS_ALL_ACCESS, 
                false, 
                process.Id);

            if (_processHandle == IntPtr.Zero)
            {
                var error = Marshal.GetLastWin32Error();
                _logger.Error("Failed to open process {Id}. Error Code: {Code}", process.Id, error);
                return false;
            }

            _logger.Information("Successfully attached to process {Name} ({Id})", process.ProcessName, process.Id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Exception while attaching to process");
            return false;
        }
    }

    public void Detach()
    {
        if (_processHandle != IntPtr.Zero)
        {
            NativeMethods.CloseHandle(_processHandle);
            _processHandle = IntPtr.Zero;
        }
        _attachedProcess = null;
    }

    public async Task<List<long>> ScanInt32Async(int value)
    {
        var results = await Task.Run(() => ScanInt32(value));
        _lastScanResults = results;
        _isFloatScan = false;
        return results;
    }

    public async Task<List<long>> NextScanInt32Async(int newValue)
    {
        if (_lastScanResults.Count == 0)
        {
            _logger.Warning("No previous scan results to filter. Run initial scan first.");
            return new List<long>();
        }

        if (_isFloatScan)
        {
            _logger.Warning("Previous scan was float type. Use NextScanFloatAsync instead.");
            return new List<long>();
        }

        return await Task.Run(() =>
        {
            var filtered = new List<long>();
            foreach (var addr in _lastScanResults)
            {
                _readCount++;
                var currentValue = ReadInt32(addr);
                if (currentValue == newValue)
                {
                    filtered.Add(addr);
                }
            }

            _lastScanResults = filtered;
            _logger.Information("Next scan complete. Filtered to {Count} results for value {Value}", filtered.Count, newValue);
            return filtered;
        });
    }

    private List<long> ScanInt32(int value)
    {
        var results = new List<long>();

        if (_processHandle == IntPtr.Zero)
        {
            _logger.Warning("Cannot scan: No process attached.");
            return results;
        }

        // Basic memory scan implementation:
        // 1. Iterate over memory regions using VirtualQueryEx
        // 2. If region is committed and readable, read it into a buffer
        // 3. Scan buffer for the value

        IntPtr currentAddress = IntPtr.Zero;
        long maxAddress = 0x7FFFFFFFFFFF; // User mode limit mostly

        while ((long)currentAddress < maxAddress)
        {
            NativeMethods.MEMORY_BASIC_INFORMATION memInfo = new NativeMethods.MEMORY_BASIC_INFORMATION();
            int bytesReturned = NativeMethods.VirtualQueryEx(_processHandle, currentAddress, out memInfo, (uint)Marshal.SizeOf(typeof(NativeMethods.MEMORY_BASIC_INFORMATION)));

            if (bytesReturned == 0)
                break;

            // Check if memory is committed and readable/writable
            if (memInfo.State == NativeMethods.MEM_COMMIT && 
               (memInfo.Protect == NativeMethods.PAGE_READWRITE || memInfo.Protect == NativeMethods.PAGE_EXECUTE_READWRITE))
            {
                // Read the memory chunk
                byte[] buffer = new byte[(int)memInfo.RegionSize];
                IntPtr bytesRead;
                
                if (NativeMethods.ReadProcessMemory(_processHandle, memInfo.BaseAddress, buffer, (int)memInfo.RegionSize, out bytesRead))
                {
                    // Scan the buffer
                    for (int i = 0; i < (int)bytesRead - 4; i += 4)
                    {
                        int val = BitConverter.ToInt32(buffer, i);
                        if (val == value)
                        {
                            results.Add((long)memInfo.BaseAddress + i);
                            
                            // Limit results for performance
                            if (results.Count >= 10000) 
                            {
                                _logger.Information("Scan limit reached (10000 results).");
                                return results;
                            }
                        }
                    }
                }
            }

            // Move to next region
            long nextAddress = (long)memInfo.BaseAddress + (long)memInfo.RegionSize;
            if (nextAddress <= (long)currentAddress) // Overflow check
                break;
                
            currentAddress = (IntPtr)nextAddress;
        }

        _logger.Information("Scan complete. Found {Count} results for value {Value}", results.Count, value);
        return results;
    }

    public int ReadInt32(long address)
    {
        if (_processHandle == IntPtr.Zero) return 0;

        byte[] buffer = new byte[4];
        IntPtr bytesRead;
        if (NativeMethods.ReadProcessMemory(_processHandle, (IntPtr)address, buffer, 4, out bytesRead))
        {
            return BitConverter.ToInt32(buffer, 0);
        }
        return 0;
    }

    public async Task<List<long>> ScanFloatAsync(float value)
    {
        var results = await Task.Run(() => ScanFloat(value));
        _lastScanResults = results;
        _isFloatScan = true;
        return results;
    }

    public async Task<List<long>> NextScanFloatAsync(float newValue)
    {
        if (_lastScanResults.Count == 0)
        {
            _logger.Warning("No previous scan results to filter. Run initial scan first.");
            return new List<long>();
        }

        if (!_isFloatScan)
        {
            _logger.Warning("Previous scan was int type. Use NextScanInt32Async instead.");
            return new List<long>();
        }

        return await Task.Run(() =>
        {
            var filtered = new List<long>();
            foreach (var addr in _lastScanResults)
            {
                _readCount++;
                var currentValue = ReadFloat(addr);
                if (Math.Abs(currentValue - newValue) < 0.001f)
                {
                    filtered.Add(addr);
                }
            }

            _lastScanResults = filtered;
            _logger.Information("Next scan complete. Filtered to {Count} results for value {Value}", filtered.Count, newValue);
            return filtered;
        });
    }

    public float ReadFloat(long address)
    {
        if (_processHandle == IntPtr.Zero) return 0f;

        byte[] buffer = new byte[4];
        IntPtr bytesRead;
        _readCount++;
        if (NativeMethods.ReadProcessMemory(_processHandle, (IntPtr)address, buffer, 4, out bytesRead))
        {
            return BitConverter.ToSingle(buffer, 0);
        }
        return 0f;
    }

    private List<long> ScanFloat(float value)
    {
        var results = new List<long>();
        if (_processHandle == IntPtr.Zero) return results;

        IntPtr currentAddress = IntPtr.Zero;
        long maxAddress = 0x7FFFFFFFFFFF;

        while ((long)currentAddress < maxAddress)
        {
            NativeMethods.MEMORY_BASIC_INFORMATION memInfo = new NativeMethods.MEMORY_BASIC_INFORMATION();
            int bytesReturned = NativeMethods.VirtualQueryEx(_processHandle, currentAddress, out memInfo, (uint)Marshal.SizeOf(typeof(NativeMethods.MEMORY_BASIC_INFORMATION)));

            if (bytesReturned == 0) break;

            if (memInfo.State == NativeMethods.MEM_COMMIT && 
               (memInfo.Protect == NativeMethods.PAGE_READWRITE || memInfo.Protect == NativeMethods.PAGE_EXECUTE_READWRITE))
            {
                byte[] buffer = new byte[(int)memInfo.RegionSize];
                IntPtr bytesRead;
                
                if (NativeMethods.ReadProcessMemory(_processHandle, memInfo.BaseAddress, buffer, (int)memInfo.RegionSize, out bytesRead))
                {
                    for (int i = 0; i < (int)bytesRead - 4; i += 4)
                    {
                        float val = BitConverter.ToSingle(buffer, i);
                        // Float comparison with epsilon
                        if (Math.Abs(val - value) < 0.001f)
                        {
                            results.Add((long)memInfo.BaseAddress + i);
                            if (results.Count >= 10000) return results;
                        }
                    }
                }
            }

            long nextAddress = (long)memInfo.BaseAddress + (long)memInfo.RegionSize;
            if (nextAddress <= (long)currentAddress) break;
            currentAddress = (IntPtr)nextAddress;
        }
        
        _logger.Information("Float scan complete. Found {Count} results for value {Value}", results.Count, value);
        return results;
    }

    public bool WriteFloat(long address, float value)
    {
        if (_processHandle == IntPtr.Zero) return false;
        _writeCount++;
        byte[] buffer = BitConverter.GetBytes(value);
        IntPtr bytesWritten;
        return NativeMethods.WriteProcessMemory(_processHandle, (IntPtr)address, buffer, 4, out bytesWritten);
    }

    public bool WriteInt32(long address, int value)
    {
        if (_processHandle == IntPtr.Zero) return false;
        _writeCount++;
        byte[] buffer = BitConverter.GetBytes(value);
        IntPtr bytesWritten;
        return NativeMethods.WriteProcessMemory(_processHandle, (IntPtr)address, buffer, 4, out bytesWritten);
    }
    public async Task<string?> ScanForPointerAsync(long targetAddress)
    {
        // Simple Single-Level Pointer Scan
        // 1. Scan ANY 4-byte value in memory that equals 'targetAddress'
        // 2. If found, calculate offset from Base Address
        
        if (_attachedProcess == null || _processHandle == IntPtr.Zero) return null;

        var pointers = await Task.Run(() => ScanInt32((int)targetAddress));
        
        if (pointers.Count > 0)
        {
            // Just take the first one for MVP
            var pointerAddr = pointers[0];
            var module = _attachedProcess.MainModule;
            if (module != null)
            {
                var baseAddr = (long)module.BaseAddress;
                var offset = pointerAddr - baseAddr;
                
                // Return in "module+offset" format
                return $"\"{module.ModuleName}\"+0x{offset:X}";
            }
        }
        
        return null;
    }
    public async Task<long> ResolvePointerAsync(string pointerExpression)
    {
        // Parses "moduleName"+0xOffset
        // Example: "ff6.exe"+0x12A4
        
        return await Task.Run(() =>
        {
            if (_attachedProcess == null) return 0;
            
            try 
            {
                var parts = pointerExpression.Split('+');
                if (parts.Length == 2)
                {
                    var modName = parts[0].Trim('"');
                    var offsetStr = parts[1].Trim().ToLower().Replace("0x", "");
                    
                    var module = _attachedProcess.Modules.Cast<ProcessModule>()
                        .FirstOrDefault(m => m.ModuleName.Equals(modName, StringComparison.OrdinalIgnoreCase));
                        
                    if (module != null && long.TryParse(offsetStr, System.Globalization.NumberStyles.HexNumber, null, out long offset))
                    {
                        return (long)module.BaseAddress + offset;
                    }
                }
                // Fallback: Try parse as direct long
                 if (long.TryParse(pointerExpression, out long direct)) return direct;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to resolve pointer {Ptr}", pointerExpression);
            }
            
            return 0;
        });
    }

    /// <summary>
    /// Capture a memory snapshot for MBAD analysis
    /// </summary>
    public MemorySnapshot CaptureSnapshot(List<long> watchAddresses)
    {
        var now = DateTime.UtcNow;
        var deltaMs = (now - _lastSnapshotTime).TotalMilliseconds;
        
        // Read values at watched addresses
        var addressValues = new Dictionary<long, int>();
        foreach (var addr in watchAddresses)
        {
            _readCount++;
            addressValues[addr] = ReadInt32(addr);
        }

        // Get process CPU usage (approximate)
        double cpuUsage = 0;
        try
        {
            if (_attachedProcess != null && !_attachedProcess.HasExited)
            {
                _attachedProcess.Refresh();
                cpuUsage = _attachedProcess.TotalProcessorTime.TotalMilliseconds / deltaMs * 100;
            }
        }
        catch { /* Ignore CPU read errors */ }

        // Get active modules
        var modules = new List<string>();
        try
        {
            if (_attachedProcess != null && !_attachedProcess.HasExited)
            {
                modules = _attachedProcess.Modules
                    .Cast<ProcessModule>()
                    .Select(m => m.ModuleName)
                    .ToList();
            }
        }
        catch { /* Ignore module read errors */ }

        var snapshot = new MemorySnapshot
        {
            Timestamp = now,
            WatchedAddresses = addressValues,
            WriteCount = _writeCount,
            ReadCount = _readCount,
            CpuUsage = cpuUsage,
            ActiveModules = modules,
            DeltaMs = deltaMs
        };

        // Reset counters
        _writeCount = 0;
        _readCount = 0;
        _lastSnapshotTime = now;

        return snapshot;
    }
}
