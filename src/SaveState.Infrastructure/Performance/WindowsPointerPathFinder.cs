using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Performance.Services;
using SaveState.Core.Performance.ValueObjects;

namespace SaveState.Infrastructure.Performance;

/// <summary>
/// Windows implementation of pointer path discovery.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WindowsPointerPathFinder : IPointerPathFinder
{
    private readonly IMemoryReader _memoryReader;
    private readonly ILogger<WindowsPointerPathFinder> _logger;

    public WindowsPointerPathFinder(IMemoryReader memoryReader, ILogger<WindowsPointerPathFinder> logger)
    {
        _memoryReader = memoryReader;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<PointerPath>>> FindPathsAsync(
        int processId,
        long targetAddress,
        int maxDepth = 2,
        CancellationToken ct = default)
    {
        var paths = new List<PointerPath>();

        // 1. Get main module info
        var baseAddressResult = await _memoryReader.GetModuleBaseAddressAsync(processId, null, ct);
        if (!baseAddressResult.IsSuccess) return baseAddressResult.ToResult<IReadOnlyList<PointerPath>>();

        long baseAddr = baseAddressResult.Value;
        long scanSize = 0x1000000; // Scan first 16MB of module data for pointers

        // 2. Read module memory
        var bufferResult = await _memoryReader.ReadMemoryAsync(processId, MemoryAddress.Create(baseAddr), (int)scanSize, ct);
        if (!bufferResult.IsSuccess) return bufferResult.ToResult<IReadOnlyList<PointerPath>>();

        var buffer = bufferResult.Value;
        int ptrSize = IntPtr.Size;

        // 3. Scan for pointers pointing CLOSE to targetAddress (max offset 0x1000)
        for (int i = 0; i <= buffer.Length - ptrSize; i += 4) // Scan with 4-byte alignment
        {
            long val = ptrSize == 8 ? BitConverter.ToInt64(buffer, i) : BitConverter.ToInt32(buffer, i);

            if (val > 0 && targetAddress >= val && targetAddress - val < 0x1000)
            {
                long offset = targetAddress - val;
                paths.Add(new PointerPath(
                    "MainModule", // Simplified for MVP
                    i,
                    new List<long> { offset }));

                if (paths.Count > 100) break; // Limit results
            }
        }

        _logger.LogInformation("Found {Count} candidate pointer paths for 0x{Target:X}", paths.Count, targetAddress);
        return Result.Success<IReadOnlyList<PointerPath>>(paths);
    }
}
