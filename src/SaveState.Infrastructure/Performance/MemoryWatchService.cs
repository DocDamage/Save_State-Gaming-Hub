using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Performance.Entities;
using SaveState.Core.Performance.Services;
using SaveState.Core.Performance.ValueObjects;
using SaveState.Infrastructure.Persistence;
using System.Globalization;
using System.Text.RegularExpressions;

namespace SaveState.Infrastructure.Performance;

/// <summary>
/// Service for managing memory watches with real-time updates.
/// </summary>
public sealed class MemoryWatchService : IMemoryWatchService
{
    private readonly SaveStateDbContext _context;
    private readonly IMemoryReader _memoryReader;
    private readonly ILogger<MemoryWatchService> _logger;

    public MemoryWatchService(
        SaveStateDbContext context,
        IMemoryReader memoryReader,
        ILogger<MemoryWatchService> logger)
    {
        _context = context;
        _memoryReader = memoryReader;
        _logger = logger;
    }

    public async Task<Result<MemoryWatch>> CreateWatchAsync(
        Guid gameId,
        string label,
        MemoryAddress address,
        MemoryDataType dataType,
        string? description = null,
        CancellationToken ct = default)
    {
        try
        {
            var watch = MemoryWatch.Create(gameId, label, address, dataType, description);
            _context.Set<MemoryWatch>().Add(watch);
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("Created memory watch '{Label}' at {Address} for game {GameId}",
                label, address.ToHexString(), gameId);

            return Result.Success(watch);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create memory watch '{Label}'", label);
            return Result.Failure<MemoryWatch>($"Failed to create watch: {ex.Message}", ErrorType.Database);
        }
    }

    public async Task<Result<IReadOnlyList<MemoryWatch>>> GetWatchesAsync(
        Guid gameId,
        CancellationToken ct = default)
    {
        try
        {
            var watches = await _context.Set<MemoryWatch>()
                .Where(w => w.GameId == gameId && w.IsActive)
                .OrderBy(w => w.CreatedAt)
                .ToListAsync(ct);

            return Result.Success<IReadOnlyList<MemoryWatch>>(watches);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get memory watches for game {GameId}", gameId);
            return Result.Failure<IReadOnlyList<MemoryWatch>>($"Failed to retrieve watches: {ex.Message}", ErrorType.Database);
        }
    }

    public async Task<Result> UpdateWatchValueAsync(
        Guid watchId,
        int processId,
        CancellationToken ct = default)
    {
        try
        {
            var watch = await _context.Set<MemoryWatch>().FindAsync(new object[] { watchId }, ct);
            if (watch == null)
            {
                return Result.Failure("Memory watch not found.", ErrorType.NotFound);
            }

            if (!watch.IsActive)
            {
                return Result.Success();
            }

            if (watch.IsFrozen && watch.CurrentValue != null)
            {
                // If frozen, write the stored value back to memory
                var valueToFreeze = JsonSerializer.Deserialize(watch.CurrentValue, typeof(object));
                if (valueToFreeze != null)
                {
                    // Using a dynamic approach for the structural write
                    // This is simplified for MVP; real implementation would use specific types
                    var rawBytes = watch.DataType.ToBytes(valueToFreeze);
                    await _memoryReader.WriteMemoryAsync(processId, watch.Address, rawBytes, ct);
                }
                return Result.Success();
            }

            // Normal read
            var size = watch.DataType.GetSize();
            if (size <= 0) size = 4; // Default safe read

            var bytesResult = await _memoryReader.ReadMemoryAsync(processId, watch.Address, size, ct);
            if (!bytesResult.IsSuccess)
            {
                return Result.Failure(bytesResult.Error!, bytesResult.ErrorType);
            }

            var value = watch.DataType.ParseValue(bytesResult.Value);
            var valueJson = JsonSerializer.Serialize(value);

            watch.UpdateValue(valueJson);
            await _context.SaveChangesAsync(ct);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update watch value for {WatchId}", watchId);
            return Result.Failure($"Update failed: {ex.Message}", ErrorType.External);
        }
    }

    public async Task<Result> WriteWatchValueAsync(
        Guid watchId,
        int processId,
        string newValue,
        CancellationToken ct = default)
    {
        try
        {
            var watch = await _context.Set<MemoryWatch>().FindAsync(new object[] { watchId }, ct);
            if (watch == null)
            {
                return Result.Failure("Memory watch not found.", ErrorType.NotFound);
            }

            var parsedValue = watch.DataType.ParseValueFromString(newValue);
            if (parsedValue == null)
            {
                return Result.Failure($"Value '{newValue}' is not valid for type {watch.DataType}.", ErrorType.Validation);
            }

            var rawBytes = watch.DataType.ToBytes(parsedValue);
            var writeResult = await _memoryReader.WriteMemoryAsync(processId, watch.Address, rawBytes, ct);

            if (writeResult.IsSuccess)
            {
                // Update local storage too
                watch.UpdateValue(JsonSerializer.Serialize(parsedValue));
                await _context.SaveChangesAsync(ct);
            }

            return writeResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write watch value for {WatchId}", watchId);
            return Result.Failure($"Write failed: {ex.Message}", ErrorType.External);
        }
    }

    public async Task<Result<int>> UpdateAllWatchesAsync(
        Guid gameId,
        int processId,
        CancellationToken ct = default)
    {
        try
        {
            var watches = await _context.Set<MemoryWatch>()
                .Where(w => w.GameId == gameId && w.IsActive)
                .ToListAsync(ct);

            var updateCount = 0;
            foreach (var watch in watches)
            {
                var result = await UpdateWatchValueAsync(watch.Id, processId, ct);
                if (result.IsSuccess)
                {
                    updateCount++;
                }
            }

            _logger.LogDebug("Updated {Count}/{Total} watches for game {GameId}",
                updateCount, watches.Count, gameId);

            return Result.Success(updateCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update all watches for game {GameId}", gameId);
            return Result.Failure<int>(ex.Message, ErrorType.Database);
        }
    }

    public async Task<Result> DeleteWatchAsync(Guid watchId, CancellationToken ct = default)
    {
        try
        {
            var watch = await _context.Set<MemoryWatch>().FindAsync(new object[] { watchId }, ct);
            if (watch == null)
            {
                return Result.Failure("Memory watch not found.", ErrorType.NotFound);
            }

            _context.Set<MemoryWatch>().Remove(watch);
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("Deleted memory watch '{Label}'", watch.Label);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete watch {WatchId}", watchId);
            return Result.Failure(ex.Message, ErrorType.Database);
        }
    }

    public async Task<Result> ToggleFreezeAsync(Guid watchId, CancellationToken ct = default)
    {
        try
        {
            var watch = await _context.Set<MemoryWatch>().FindAsync(new object[] { watchId }, ct);
            if (watch == null)
            {
                return Result.Failure("Memory watch not found.", ErrorType.NotFound);
            }

            watch.ToggleFreeze();
            await _context.SaveChangesAsync(ct);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to toggle freeze for watch {WatchId}", watchId);
            return Result.Failure(ex.Message, ErrorType.Database);
        }
    }

    public async Task<Result<string>> ExportWatchesAsync(Guid gameId, CancellationToken ct = default)
    {
        try
        {
            var watches = await GetWatchesAsync(gameId, ct);
            if (!watches.IsSuccess)
            {
                return Result.Failure<string>(watches.Error!, watches.ErrorType);
            }

            if (watches.Value is null)
                return Result.Failure<string>("No watches to export", ErrorType.NotFound);

            var exportData = watches.Value.Select(w => new
            {
                w.Label,
                Address = w.Address.ToHexString(),
                DataType = w.DataType.ToString(),
                w.Description
            });

            var json = JsonSerializer.Serialize(exportData, new JsonSerializerOptions { WriteIndented = true });
            return Result.Success(json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export watches for game {GameId}", gameId);
            return Result.Failure<string>(ex.Message, ErrorType.Validation);
        }
    }

    public async Task<Result<int>> ImportWatchesAsync(Guid gameId, string json, CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return Result.Failure<int>("Import data is empty.", ErrorType.Validation);
            }

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var importData = JsonSerializer.Deserialize<List<ImportWatchData>>(json, options);

            if (importData == null || importData.Count == 0)
            {
                return Result.Failure<int>("No watch entries found in import data.", ErrorType.Validation);
            }

            var importedCount = 0;
            foreach (var entry in importData)
            {
                if (string.IsNullOrWhiteSpace(entry.Label) ||
                    string.IsNullOrWhiteSpace(entry.Address) ||
                    string.IsNullOrWhiteSpace(entry.DataType))
                {
                    _logger.LogWarning("Skipping invalid memory watch import entry with missing fields.");
                    continue;
                }

                if (!TryParseAddress(entry.Address, out var address))
                {
                    _logger.LogWarning("Skipping memory watch import entry with invalid address: {Address}", entry.Address);
                    continue;
                }

                if (!Enum.TryParse(entry.DataType, ignoreCase: true, out MemoryDataType dataType))
                {
                    _logger.LogWarning("Skipping memory watch import entry with invalid data type: {DataType}", entry.DataType);
                    continue;
                }

                var watch = MemoryWatch.Create(gameId, entry.Label, address!, dataType, entry.Description);
                _context.Set<MemoryWatch>().Add(watch);
                importedCount++;
            }

            if (importedCount == 0)
            {
                return Result.Failure<int>("No valid watch entries were imported.", ErrorType.Validation);
            }

            await _context.SaveChangesAsync(ct);
            return Result.Success(importedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import watches for game {GameId}", gameId);
            return Result.Failure<int>(ex.Message, ErrorType.Validation);
        }
    }

    private static bool TryParseAddress(string addressText, out MemoryAddress? address)
    {
        address = null;
        if (string.IsNullOrWhiteSpace(addressText))
        {
            return false;
        }

        var matches = Regex.Matches(addressText, "0x[0-9a-fA-F]+");
        if (matches.Count == 0)
        {
            return false;
        }

        var values = new List<long>();
        foreach (Match match in matches)
        {
            if (long.TryParse(match.Value.AsSpan(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value))
            {
                values.Add(value);
            }
        }

        if (values.Count == 0)
        {
            return false;
        }

        var baseAddress = values[0];
        if (values.Count == 1)
        {
            address = MemoryAddress.Create(baseAddress);
            return true;
        }

        var offsets = values.Skip(1)
            .Where(value => value <= int.MaxValue)
            .Select(value => (int)value)
            .ToArray();

        address = MemoryAddress.CreatePointerChain(baseAddress, offsets);
        return true;
    }

    private sealed class ImportWatchData
    {
        public string? Label { get; set; }
        public string? Address { get; set; }
        public string? DataType { get; set; }
        public string? Description { get; set; }
    }
}
