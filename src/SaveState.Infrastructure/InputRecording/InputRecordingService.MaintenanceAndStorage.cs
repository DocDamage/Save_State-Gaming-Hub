using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.InputRecording.Services;
using InputRecordingEntity = SaveState.Core.InputRecording.InputRecording;
using RecordingStatus = SaveState.Core.InputRecording.RecordingStatus;
using RecordingType = SaveState.Core.InputRecording.RecordingType;
using InputFrame = SaveState.Core.InputRecording.InputFrame;
using InputRecordingStatistics = SaveState.Core.InputRecording.InputRecordingStatistics;
using RecordingExportFormat = SaveState.Core.InputRecording.RecordingExportFormat;

namespace SaveState.Infrastructure.InputRecording.Services;

internal partial class InputRecordingServiceOperations
{
    public async Task<Result<InputRecordingStatistics>> GetStatisticsAsync(Guid? gameId = null, CancellationToken ct = default)
    {
        try
        {
            var query = _dbContext.InputRecordings.AsNoTracking().AsQueryable();
            if (gameId.HasValue)
                query = query.Where(r => r.GameId == gameId.Value);

            var recordings = await query.ToListAsync(ct);

            var stats = new InputRecordingStatistics
            {
                TotalRecordings = recordings.Count,
                TotalDuration = TimeSpan.FromTicks(recordings.Sum(r => r.Duration.Ticks)),
                TotalStorageBytes = recordings.Sum(r => r.FileSize),
                RecordingsByType = recordings
                    .GroupBy(r => r.Type)
                    .ToDictionary(g => g.Key, g => g.Count()),
                AverageDuration = recordings.Any() 
                    ? TimeSpan.FromTicks(recordings.Sum(r => r.Duration.Ticks) / recordings.Count) 
                    : TimeSpan.Zero,
                LongestRecording = recordings.Any() 
                    ? recordings.Max(r => r.Duration) 
                    : TimeSpan.Zero
            };

            if (recordings.Any())
            {
                var mostActiveDay = recordings
                    .GroupBy(r => r.RecordedAt.Date)
                    .OrderByDescending(g => g.Count())
                    .FirstOrDefault();
                stats.MostActiveDay = mostActiveDay?.Key;
            }

            return Result<InputRecordingStatistics>.Success(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get recording statistics");
            return Result<InputRecordingStatistics>.Failure($"Statistics query failed: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<bool>> ValidateRecordingAsync(Guid recordingId, CancellationToken ct = default)
    {
        try
        {
            var recording = await _dbContext.InputRecordings.FindAsync(new object[] { recordingId }, ct);
            if (recording == null)
                return Result<bool>.Failure("Recording not found", ErrorType.NotFound);

            if (!File.Exists(recording.FilePath))
            {
                recording.Status = RecordingStatus.Corrupted;
                await _dbContext.SaveChangesAsync(ct);
                return Result<bool>.Success(false);
            }

            try
            {
                var frames = await LoadFrameDataAsync(recordingId, ct);
                var isValid = frames.Count == recording.TotalFrames;
                recording.Status = isValid ? RecordingStatus.Ready : RecordingStatus.Corrupted;
                await _dbContext.SaveChangesAsync(ct);
                return Result<bool>.Success(isValid);
            }
            catch
            {
                recording.Status = RecordingStatus.Corrupted;
                await _dbContext.SaveChangesAsync(ct);
                return Result<bool>.Success(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate recording {RecordingId:B}", recordingId);
            return Result<bool>.Failure($"Validation failed: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<InputRecordingEntity>> RepairRecordingAsync(Guid recordingId, CancellationToken ct = default)
    {
        try
        {
            var recording = await _dbContext.InputRecordings.FindAsync(new object[] { recordingId }, ct);
            if (recording == null)
                return Result<InputRecordingEntity>.Failure("Recording not found", ErrorType.NotFound);

            if (!File.Exists(recording.FilePath))
                return Result<InputRecordingEntity>.Failure("Recording file is missing and cannot be repaired", ErrorType.NotFound);

            try
            {
                var frames = await LoadFrameDataAsync(recordingId, ct);
                recording.TotalFrames = frames.Count;
                recording.Status = RecordingStatus.Ready;
                await _dbContext.SaveChangesAsync(ct);
                return Result<InputRecordingEntity>.Success(recording);
            }
            catch (Exception ex)
            {
                return Result<InputRecordingEntity>.Failure($"Recording is corrupted beyond repair: {ex.Message}", ErrorType.Internal);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to repair recording {RecordingId:B}", recordingId);
            return Result<InputRecordingEntity>.Failure($"Repair failed: {ex.Message}", ErrorType.Internal);
        }
    }

    public Task<Result<List<RecordingExportFormat>>> GetSupportedImportFormatsAsync()
    {
        return Task.FromResult(Result.Success(new List<RecordingExportFormat>
        {
            RecordingExportFormat.Native,
            RecordingExportFormat.FM2
        }));
    }

    public Task<Result<List<RecordingExportFormat>>> GetSupportedExportFormatsAsync()
    {
        return Task.FromResult(Result.Success(new List<RecordingExportFormat>
        {
            RecordingExportFormat.Native,
            RecordingExportFormat.FM2
        }));
    }

    // Private helper methods

    private async Task<string> SaveFrameDataAsync(Guid recordingId, List<InputFrame> frames, CancellationToken ct)
    {
        var filePath = Path.Combine(_recordingsBasePath, $"{recordingId:B}.json.gz");
        var json = JsonSerializer.Serialize(frames, new JsonSerializerOptions { WriteIndented = false });
        var bytes = Encoding.UTF8.GetBytes(json);

        await using var fileStream = File.Create(filePath);
        await using var gzipStream = new GZipStream(fileStream, CompressionLevel.Optimal);
        await gzipStream.WriteAsync(bytes, ct);

        return filePath;
    }

    private async Task<List<InputFrame>> LoadFrameDataAsync(Guid recordingId, CancellationToken ct)
    {
        var filePath = Path.Combine(_recordingsBasePath, $"{recordingId:B}.json.gz");
        
        if (!File.Exists(filePath))
            return new List<InputFrame>();

        await using var fileStream = File.OpenRead(filePath);
        await using var gzipStream = new GZipStream(fileStream, CompressionMode.Decompress);
        using var reader = new StreamReader(gzipStream, Encoding.UTF8);
        var json = await reader.ReadToEndAsync(ct);
        
        return JsonSerializer.Deserialize<List<InputFrame>>(json) ?? new List<InputFrame>();
    }

    private async Task<string> ExportNativeFormatAsync(InputRecordingEntity recording, List<InputFrame> frames, string outputPath, bool includeMetadata, CancellationToken ct)
    {
        var export = new
        {
            Metadata = includeMetadata ? recording : null,
            Frames = frames,
            ExportedAt = _timeProvider.UtcNow
        };

        var json = JsonSerializer.Serialize(export, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(outputPath, json, ct);
        return outputPath;
    }

    private async Task<string> ExportFM2FormatAsync(InputRecordingEntity recording, List<InputFrame> frames, string outputPath, CancellationToken ct)
    {
        // FM2 format is text-based, one frame per line
        var sb = new StringBuilder();
        sb.AppendLine("version 3");
        sb.AppendLine($"romFilename {recording.RomHash ?? "unknown"}");
        sb.AppendLine($"guid {recording.Id:B}");
        sb.AppendLine($"frameCount {frames.Count}");
        sb.AppendLine();

        foreach (var frame in frames)
        {
            var buttons = string.Join("|", frame.PressedInputs.Select(MapToNESButton));
            sb.AppendLine($"{buttons}");
        }

        await File.WriteAllTextAsync(outputPath, sb.ToString(), ct);
        return outputPath;
    }

    private string MapToNESButton(string input)
    {
        return input.ToUpper() switch
        {
            "UP" => "U",
            "DOWN" => "D",
            "LEFT" => "L",
            "RIGHT" => "R",
            "A" => "A",
            "B" => "B",
            "SELECT" => "S",
            "START" => "T",
            _ => ""
        };
    }

    private async Task<(InputRecordingEntity? recording, List<InputFrame>? frames)> ImportNativeFormatAsync(string filePath, CancellationToken ct)
    {
        var json = await File.ReadAllTextAsync(filePath, ct);
        var export = JsonSerializer.Deserialize<ExportData>(json);
        return (export?.Metadata, export?.Frames);
    }

    private async Task<(InputRecordingEntity? recording, List<InputFrame>? frames)> ImportFM2FormatAsync(string filePath, CancellationToken ct)
    {
        var lines = await File.ReadAllLinesAsync(filePath, ct);
        var recording = new InputRecordingEntity
        {
            Type = RecordingType.TAS,
            EmulatorCore = "FCEUX",
            Tags = new List<string> { "imported", "fm2" }
        };

        var frames = new List<InputFrame>();
        long frameNumber = 0;

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("version") || line.StartsWith("romFilename") ||
                line.StartsWith("guid") || line.StartsWith("frameCount"))
            {
                if (line.StartsWith("romFilename "))
                    recording.RomHash = line.Substring(12).Trim();
                continue;
            }

            var frame = new InputFrame
            {
                FrameNumber = frameNumber++,
                PressedInputs = ParseFM2Frame(line)
            };
            frames.Add(frame);
        }

        recording.TotalFrames = frames.Count;
        return (recording, frames);
    }

    private List<string> ParseFM2Frame(string line)
    {
        var inputs = new List<string>();
        var parts = line.Split('|');

        foreach (var part in parts)
        {
            switch (part)
            {
                case "U": inputs.Add("UP"); break;
                case "D": inputs.Add("DOWN"); break;
                case "L": inputs.Add("LEFT"); break;
                case "R": inputs.Add("RIGHT"); break;
                case "A": inputs.Add("A"); break;
                case "B": inputs.Add("B"); break;
                case "S": inputs.Add("SELECT"); break;
                case "T": inputs.Add("START"); break;
            }
        }

        return inputs;
    }

    private class ExportData
    {
        public InputRecordingEntity? Metadata { get; set; }
        public List<InputFrame>? Frames { get; set; }
    }
}
