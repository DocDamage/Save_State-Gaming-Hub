using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Performance.Services;
using SaveState.Core.Performance.ValueObjects;

namespace SaveState.Infrastructure.Performance;

/// <summary>
/// Windows implementation of memory scanning.
/// </summary>
public sealed class WindowsMemoryScanner : IMemoryScanner, IDisposable
{
    private readonly IMemoryReader _memoryReader;
    private readonly ILogger<WindowsMemoryScanner> _logger;

    private List<long> _results = new();
    private Dictionary<long, object> _storedValues = new();
    private MemoryDataType? _currentDataType;
    private bool _disposed;

    public int TotalResults => _results.Count;
    public MemoryDataType? CurrentDataType => _currentDataType;

    public WindowsMemoryScanner(IMemoryReader memoryReader, ILogger<WindowsMemoryScanner> logger)
    {
        _memoryReader = memoryReader;
        _logger = logger;
    }

    public async Task<Result<int>> StartNewScanAsync(
        int processId,
        MemoryDataType dataType,
        ScanType scanType,
        object? value = null,
        CancellationToken ct = default)
    {
        ClearScan();
        _currentDataType = dataType;

        var baseAddressResult = await _memoryReader.GetModuleBaseAddressAsync(processId, null, ct);
        if (!baseAddressResult.IsSuccess) return baseAddressResult.ToResult<int>();

        long start = baseAddressResult.Value;
        long size = 0x1000000; // 16MB limit for MVP

        int valueSize = dataType.GetSize();
        var bufferResult = await _memoryReader.ReadMemoryAsync(processId, MemoryAddress.Create(start), (int)size, ct);
        if (!bufferResult.IsSuccess) return bufferResult.ToResult<int>();

        var buffer = bufferResult.Value;

        for (int i = 0; i <= buffer.Length - valueSize; i += 1)
        {
            var currentValue = dataType.ParseValue(buffer.AsSpan(i, valueSize).ToArray());
            if (currentValue == null) continue;

            if (Matches(currentValue, value, scanType, null))
            {
                long address = start + i;
                _results.Add(address);
                _storedValues[address] = currentValue;
            }
        }

        return Result.Success(_results.Count);
    }

    public async Task<Result<int>> NextScanAsync(
        int processId,
        ScanType scanType,
        object? value = null,
        CancellationToken ct = default)
    {
        if (_results.Count == 0 || _currentDataType == null)
            return Result.Failure<int>("No active scan to narrow down.", ErrorType.Validation);

        var newResults = new List<long>();
        var newStoredValues = new Dictionary<long, object>();
        int valueSize = _currentDataType.Value.GetSize();

        foreach (var address in _results)
        {
            var readResult = await _memoryReader.ReadMemoryAsync(processId, MemoryAddress.Create(address), valueSize, ct);
            if (!readResult.IsSuccess) continue;

            var currentValue = _currentDataType.Value.ParseValue(readResult.Value);
            if (currentValue == null) continue;

            _storedValues.TryGetValue(address, out var previousValue);

            if (Matches(currentValue, value, scanType, previousValue))
            {
                newResults.Add(address);
                newStoredValues[address] = currentValue;
            }
        }

        _results = newResults;
        _storedValues = newStoredValues;
        return Result.Success(_results.Count);
    }

    private bool Matches(object current, object? target, ScanType scanType, object? previous)
    {
        try
        {
            return scanType switch
            {
                ScanType.ExactValue => Equals(current, target),
                ScanType.GreaterThan => Compare(current, target) > 0,
                ScanType.LessThan => Compare(current, target) < 0,
                ScanType.Increased => previous != null && Compare(current, previous) > 0,
                ScanType.Decreased => previous != null && Compare(current, previous) < 0,
                ScanType.Changed => previous != null && !Equals(current, previous),
                ScanType.Unchanged => previous != null && Equals(current, previous),
                _ => false
            };
        }
        catch
        {
            return false;
        }
    }

    private int Compare(object a, object? b)
    {
        if (b == null) return 0;
        if (a is IComparable comp)
        {
            return comp.CompareTo(Convert.ChangeType(b, a.GetType()));
        }
        return 0;
    }

    public Task<Result<IReadOnlyList<long>>> GetResultsAsync(int offset, int limit, CancellationToken ct = default)
    {
        var paged = (IReadOnlyList<long>)_results.Skip(offset).Take(limit).ToList();
        return Task.FromResult(Result.Success(paged));
    }

    public void ClearScan()
    {
        _results.Clear();
        _storedValues.Clear();
        _currentDataType = null;
    }

    public void Dispose()
    {
        if (_disposed) return;
        ClearScan();
        _disposed = true;
    }
}
