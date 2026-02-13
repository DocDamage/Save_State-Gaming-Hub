using SaveState.Core.Common;
using SaveState.Core.Performance.ValueObjects;

namespace SaveState.Core.Performance.Services;

/// <summary>
/// Interface for scanning process memory to find and track values.
/// </summary>
public interface IMemoryScanner
{
    /// <summary>
    /// Starts a new scan on the process.
    /// </summary>
    Task<Result<int>> StartNewScanAsync(
        int processId,
        MemoryDataType dataType,
        ScanType scanType,
        object? value = null,
        CancellationToken ct = default);

    /// <summary>
    /// Performs a next-step scan on existing results to narrow them down.
    /// </summary>
    Task<Result<int>> NextScanAsync(
        int processId,
        ScanType scanType,
        object? value = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets a page of current scan results.
    /// </summary>
    Task<Result<IReadOnlyList<long>>> GetResultsAsync(int offset, int limit, CancellationToken ct = default);

    /// <summary>
    /// Clears the current scan and releases associated memory.
    /// </summary>
    void ClearScan();

    /// <summary>
    /// Gets the total number of results found in the current scan.
    /// </summary>
    int TotalResults { get; }

    /// <summary>
    /// Gets the current data type being scanned.
    /// </summary>
    MemoryDataType? CurrentDataType { get; }
}
