using System.Diagnostics;

namespace SaveState.Core.Interfaces;

public interface IMemoryScannerService
{
    /// <summary>
    /// Attaches to the specified process for memory operations.
    /// </summary>
    bool Attach(Process process);

    /// <summary>
    /// Detaches from the current process.
    /// </summary>
    void Detach();

    /// <summary>
    /// Scans memory for a specific integer value. Returns list of addresses.
    /// </summary>
    Task<List<long>> ScanInt32Async(int value);

    /// <summary>
    /// Filters previous scan results for addresses that now contain the new value.
    /// </summary>
    Task<List<long>> NextScanInt32Async(int newValue);

    /// <summary>
    /// Helper to read an integer from an address.
    /// </summary>
    int ReadInt32(long address);

    /// <summary>
    /// Helper to write an integer to an address.
    /// </summary>
    bool WriteInt32(long address, int value);

    /// <summary>
    /// Scans memory for a specific single-precision floating point value.
    /// </summary>
    Task<List<long>> ScanFloatAsync(float value);

    /// <summary>
    /// Filters previous float scan results for addresses that now contain the new value.
    /// </summary>
    Task<List<long>> NextScanFloatAsync(float newValue);

    /// <summary>
    /// Writes a float value to the specified address.
    /// </summary>
    bool WriteFloat(long address, float value);

    /// <summary>
    /// Scans memory to find the pointer path to a specific address.
    /// </summary>
    Task<string?> ScanForPointerAsync(long targetAddress);

    /// <summary>
    /// Resolves a pointer string (e.g. "game.exe"+0x123) to an absolute address.
    /// </summary>
    Task<long> ResolvePointerAsync(string pointerExpression);

    /// <summary>
    /// Gets the currently attached process ID.
    /// </summary>
    int? CurrentProcessId { get; }

    /// <summary>
    /// Gets the results from the last scan operation.
    /// </summary>
    List<long> LastScanResults { get; }
}

