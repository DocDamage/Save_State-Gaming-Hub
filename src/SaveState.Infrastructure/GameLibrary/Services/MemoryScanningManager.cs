using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Services;

/// <summary>
/// Manages memory scanning operations for discovering game values.
/// </summary>
public sealed class MemoryScanningManager
{
    private readonly ILogger<MemoryScanningManager> _logger;

    [DllImport("kernel32.dll")]
    private static extern void GetSystemInfo(out SYSTEM_INFO lpSystemInfo);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int dwSize, out uint lpNumberOfBytesRead);

    [StructLayout(LayoutKind.Sequential)]
    private struct SYSTEM_INFO
    {
        public ushort wProcessorArchitecture;
        public ushort wReserved;
        public uint dwPageSize;
        public IntPtr lpMinimumApplicationAddress;
        public IntPtr lpMaximumApplicationAddress;
        public IntPtr dwActiveProcessorMask;
        public uint dwNumberOfProcessors;
        public uint dwProcessorType;
        public uint dwAllocationGranularity;
        public ushort wProcessorLevel;
        public ushort wProcessorRevision;
    }

    public MemoryScanningManager(ILogger<MemoryScanningManager> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Scans a memory range for integer values.
    /// </summary>
    public Task ScanRangeForIntegersAsync(IntPtr processHandle, nuint startAddress, nuint size, List<DiscoveredValue> candidates, CancellationToken ct)
    {
        return Task.Run(() =>
        {
            const int bufferSize = 4096; // Read 4KB at a time
            var buffer = new byte[bufferSize];

            for (nuint offset = 0; offset < size; offset += (nuint)bufferSize)
            {
                ct.ThrowIfCancellationRequested();

                var address = (IntPtr)(startAddress + offset);

                if (!ReadProcessMemory(processHandle, address, buffer, bufferSize, out var bytesRead) || bytesRead == 0)
                    continue;

                // Scan for integers in common ranges
                for (int i = 0; i < bytesRead - 4; i += 4)
                {
                    var value = BitConverter.ToInt32(buffer, i);

                    if (IsCommonIntegerValue(value))
                    {
                        var valueAddress = IntPtr.Add(address, i);
                        candidates.Add(new DiscoveredValue
                        {
                            Address = valueAddress,
                            ValueType = "Int32",
                            CurrentValue = value
                        });
                    }
                }

                // Limit candidates to prevent memory issues
                if (candidates.Count >= 50000)
                    break;
            }
        }, ct);
    }

    /// <summary>
    /// Scans a memory range for float values.
    /// </summary>
    public Task ScanRangeForFloatsAsync(IntPtr processHandle, nuint startAddress, nuint size, List<DiscoveredValue> candidates, CancellationToken ct)
    {
        return Task.Run(() =>
        {
            const int bufferSize = 4096;
            var buffer = new byte[bufferSize];

            for (nuint offset = 0; offset < size; offset += (nuint)bufferSize)
            {
                ct.ThrowIfCancellationRequested();

                var address = (IntPtr)(startAddress + offset);

                if (!ReadProcessMemory(processHandle, address, buffer, bufferSize, out var bytesRead) || bytesRead == 0)
                    continue;

                // Scan for floats in common ranges
                for (int i = 0; i < bytesRead - 4; i += 4)
                {
                    var value = BitConverter.ToSingle(buffer, i);

                    if (IsCommonFloatValue(value))
                    {
                        var valueAddress = IntPtr.Add(address, i);
                        candidates.Add(new DiscoveredValue
                        {
                            Address = valueAddress,
                            ValueType = "Float",
                            CurrentValue = value
                        });
                    }
                }

                if (candidates.Count >= 50000)
                    break;
            }
        }, ct);
    }

    /// <summary>
    /// Reads a value at the specified address.
    /// </summary>
    public object? ReadValueAtAddress(IntPtr processHandle, IntPtr address, string valueType)
    {
        var normalizedType = valueType.ToLowerInvariant();

        return normalizedType switch
        {
            "int32" or "int" => ReadInt32(processHandle, address),
            "float" or "single" => ReadFloat(processHandle, address),
            "int64" or "long" => ReadInt64(processHandle, address),
            "double" => ReadDouble(processHandle, address),
            "int16" or "short" => ReadInt16(processHandle, address),
            "byte" => ReadByte(processHandle, address),
            _ => ReadInt32(processHandle, address)
        };
    }

    /// <summary>
    /// Gets memory scan ranges based on options.
    /// </summary>
    public static List<MemoryRange> GetScanRanges(DiscoveryOptions options)
    {
        var ranges = new List<MemoryRange>();

        // Get system info for memory bounds
        GetSystemInfo(out var sysInfo);

        // Add common game memory ranges
        ranges.Add(new MemoryRange(options.ScanStartAddress, Math.Min(options.ScanSize, 0x01000000))); // First 16MB

        // Additional ranges for 32-bit games
        if (options.ScanSize > 0x01000000)
        {
            ranges.Add(new MemoryRange(0x10000000, Math.Min(options.ScanSize - 0x01000000, 0x10000000))); // 256MB-512MB
        }

        return ranges;
    }

    private int? ReadInt32(IntPtr processHandle, IntPtr address)
    {
        var buffer = new byte[4];
        return ReadProcessMemory(processHandle, address, buffer, 4, out var bytesRead) && bytesRead == 4
            ? BitConverter.ToInt32(buffer, 0)
            : null;
    }

    private float? ReadFloat(IntPtr processHandle, IntPtr address)
    {
        var buffer = new byte[4];
        return ReadProcessMemory(processHandle, address, buffer, 4, out var bytesRead) && bytesRead == 4
            ? BitConverter.ToSingle(buffer, 0)
            : null;
    }

    private long? ReadInt64(IntPtr processHandle, IntPtr address)
    {
        var buffer = new byte[8];
        return ReadProcessMemory(processHandle, address, buffer, 8, out var bytesRead) && bytesRead == 8
            ? BitConverter.ToInt64(buffer, 0)
            : null;
    }

    private double? ReadDouble(IntPtr processHandle, IntPtr address)
    {
        var buffer = new byte[8];
        return ReadProcessMemory(processHandle, address, buffer, 8, out var bytesRead) && bytesRead == 8
            ? BitConverter.ToDouble(buffer, 0)
            : null;
    }

    private short? ReadInt16(IntPtr processHandle, IntPtr address)
    {
        var buffer = new byte[2];
        return ReadProcessMemory(processHandle, address, buffer, 2, out var bytesRead) && bytesRead == 2
            ? BitConverter.ToInt16(buffer, 0)
            : null;
    }

    private byte? ReadByte(IntPtr processHandle, IntPtr address)
    {
        var buffer = new byte[1];
        return ReadProcessMemory(processHandle, address, buffer, 1, out var bytesRead) && bytesRead == 1
            ? buffer[0]
            : null;
    }

    private static bool IsCommonIntegerValue(int value)
    {
        // Health ranges: 1-10000
        if (value >= 1 && value <= 10000)
            return true;

        // Ammo ranges: 0-999
        if (value >= 0 && value <= 999)
            return true;

        // Currency ranges: 0-999999
        if (value >= 0 && value <= 999999)
            return true;

        // XP/Score ranges: 0-99999999
        if (value >= 0 && value <= 99999999)
            return true;

        return false;
    }

    private static bool IsCommonFloatValue(float value)
    {
        // Position coordinates: -100000 to +100000
        if (value >= -100000 && value <= 100000 && value != 0)
            return true;

        // Health as float: 0.0-1000.0
        if (value >= 0 && value <= 1000)
            return true;

        // Timers: 0.0-86400.0 (24 hours in seconds)
        if (value >= 0 && value <= 86400)
            return true;

        return false;
    }
}

/// <summary>
/// Represents a memory range for scanning.
/// </summary>
public readonly struct MemoryRange(nuint start, nuint size)
{
    public nuint Start { get; } = start;
    public nuint Size { get; } = size;
}
