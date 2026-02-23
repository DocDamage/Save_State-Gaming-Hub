using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.InputRecording.Services;
using SaveState.Infrastructure.Persistence;
using InputRecordingEntity = SaveState.Core.InputRecording.InputRecording;
using RecordingSession = SaveState.Core.InputRecording.RecordingSession;
using PlaybackSession = SaveState.Core.InputRecording.PlaybackSession;
using InputFrame = SaveState.Core.InputRecording.InputFrame;
using RecordingType = SaveState.Core.InputRecording.RecordingType;
using RecordingStatus = SaveState.Core.InputRecording.RecordingStatus;
using PlaybackSpeed = SaveState.Core.InputRecording.PlaybackSpeed;
using StartRecordingRequest = SaveState.Core.InputRecording.StartRecordingRequest;
using StartPlaybackRequest = SaveState.Core.InputRecording.StartPlaybackRequest;
using ExportRecordingRequest = SaveState.Core.InputRecording.ExportRecordingRequest;
using ImportRecordingRequest = SaveState.Core.InputRecording.ImportRecordingRequest;
using InputRecordingFilter = SaveState.Core.InputRecording.InputRecordingFilter;
using InputRecordingStatistics = SaveState.Core.InputRecording.InputRecordingStatistics;
using RecordingExportFormat = SaveState.Core.InputRecording.RecordingExportFormat;
using RecordingBookmark = SaveState.Core.InputRecording.RecordingBookmark;

namespace SaveState.Infrastructure.InputRecording.Services;

/// <summary>
/// Service for managing input recording and TAS functionality.
/// </summary>
internal partial class InputRecordingServiceOperations : IInputRecordingService
{
    private readonly SaveStateDbContext _dbContext;
    private readonly ILogger<InputRecordingService> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly string _recordingsBasePath;
    private readonly Dictionary<Guid, RecordingSession> _activeRecordings = new();
    private readonly Dictionary<Guid, PlaybackSession> _activePlaybacks = new();
    private readonly Dictionary<Guid, List<InputFrame>> _frameBuffers = new();

    public InputRecordingServiceOperations(
        SaveStateDbContext dbContext,
        ILogger<InputRecordingService> logger,
        ITimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _logger = logger;
        _timeProvider = timeProvider;
        _recordingsBasePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SaveStateReborn",
            "InputRecordings");
        
        Directory.CreateDirectory(_recordingsBasePath);
    }

    public async Task<Result<RecordingSession>> StartRecordingAsync(StartRecordingRequest request, CancellationToken ct = default)
    {
        try
        {
            var session = new RecordingSession
            {
                GameId = request.GameId,
                StartedAt = _timeProvider.UtcNow,
                IsRecording = true,
                IsPaused = false
            };

            _activeRecordings[session.Id] = session;
            _frameBuffers[session.Id] = new List<InputFrame>();

            _logger.LogInformation("Started recording session {SessionId} for game {GameId:B}", session.Id, request.GameId);
            return Result<RecordingSession>.Success(session);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start recording for game {GameId:B}", request.GameId);
            return Result<RecordingSession>.Failure($"Failed to start recording: {ex.Message}", ErrorType.Internal);
        }
    }

    public Task<Result> PauseRecordingAsync(Guid sessionId, CancellationToken ct = default)
    {
        if (!_activeRecordings.TryGetValue(sessionId, out var session))
            return Task.FromResult(Result.Failure("Recording session not found", ErrorType.NotFound));

        session.IsPaused = true;
        return Task.FromResult(Result.Success());
    }

    public Task<Result> ResumeRecordingAsync(Guid sessionId, CancellationToken ct = default)
    {
        if (!_activeRecordings.TryGetValue(sessionId, out var session))
            return Task.FromResult(Result.Failure("Recording session not found", ErrorType.NotFound));

        session.IsPaused = false;
        return Task.FromResult(Result.Success());
    }

    public async Task<Result<InputRecordingEntity>> StopRecordingAsync(Guid sessionId, CancellationToken ct = default)
    {
        try
        {
            if (!_activeRecordings.TryGetValue(sessionId, out var session))
                return Result<InputRecordingEntity>.Failure("Recording session not found", ErrorType.NotFound);

            if (!_frameBuffers.TryGetValue(sessionId, out var frames))
                frames = new List<InputFrame>();

            var now = _timeProvider.UtcNow;
            var recording = new InputRecordingEntity
            {
                GameId = session.GameId,
                Name = $"Recording_{now:yyyyMMdd_HHmmss}",
                Status = RecordingStatus.Processing,
                TotalFrames = session.CurrentFrame,
                Duration = now - session.StartedAt,
                Fps = 60,
                Tags = new List<string>()
            };

            _dbContext.InputRecordings.Add(recording);
            await _dbContext.SaveChangesAsync(ct);

            // Save frame data to file
            var filePath = await SaveFrameDataAsync(recording.Id, frames, ct);
            recording.FilePath = filePath;
            recording.FileSize = new FileInfo(filePath).Length;
            recording.Status = RecordingStatus.Ready;

            await _dbContext.SaveChangesAsync(ct);

            // Cleanup
            _activeRecordings.Remove(sessionId);
            _frameBuffers.Remove(sessionId);

            _logger.LogInformation("Stopped recording {RecordingId} with {FrameCount} frames", recording.Id, frames.Count);
            return Result<InputRecordingEntity>.Success(recording);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop recording session {SessionId}", sessionId);
            return Result<InputRecordingEntity>.Failure($"Failed to stop recording: {ex.Message}", ErrorType.Internal);
        }
    }

    public Task<Result> RecordFrameAsync(Guid sessionId, InputFrame frame, CancellationToken ct = default)
    {
        if (!_activeRecordings.TryGetValue(sessionId, out var session))
            return Task.FromResult(Result.Failure("Recording session not found", ErrorType.NotFound));

        if (session.IsPaused)
            return Task.FromResult(Result.Success());

        if (!_frameBuffers.TryGetValue(sessionId, out var frames))
        {
            frames = new List<InputFrame>();
            _frameBuffers[sessionId] = frames;
        }

        frame.FrameNumber = session.CurrentFrame++;
        frames.Add(frame);

        return Task.FromResult(Result.Success());
    }

    public Task<Result<RecordingSession>> GetActiveRecordingAsync(Guid gameId, CancellationToken ct = default)
    {
        var session = _activeRecordings.Values.FirstOrDefault(s => s.GameId == gameId);
        
        if (session == null)
            return Task.FromResult(Result<RecordingSession>.Failure("No active recording found", ErrorType.NotFound));

        return Task.FromResult(Result<RecordingSession>.Success(session));
    }

    public Task<Result<PlaybackSession>> StartPlaybackAsync(StartPlaybackRequest request, CancellationToken ct = default)
    {
        try
        {
            var recording = _dbContext.InputRecordings.Find(request.RecordingId);
            if (recording == null)
                return Task.FromResult(Result<PlaybackSession>.Failure("Recording not found", ErrorType.NotFound));

            var session = new PlaybackSession
            {
                RecordingId = request.RecordingId,
                CurrentFrame = request.StartFrame,
                TotalFrames = recording.TotalFrames,
                Speed = request.Speed,
                IsPlaying = true,
                IsPaused = false
            };

            _activePlaybacks[session.Id] = session;
            recording.PlayCount++;
            recording.LastPlayedAt = _timeProvider.UtcNow;
            _dbContext.SaveChanges();

            return Task.FromResult(Result<PlaybackSession>.Success(session));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start playback for recording {RecordingId:B}", request.RecordingId);
            return Task.FromResult(Result<PlaybackSession>.Failure($"Failed to start playback: {ex.Message}", ErrorType.Internal));
        }
    }

    public Task<Result> PausePlaybackAsync(Guid sessionId, CancellationToken ct = default)
    {
        if (!_activePlaybacks.TryGetValue(sessionId, out var session))
            return Task.FromResult(Result.Failure("Playback session not found", ErrorType.NotFound));

        session.IsPaused = true;
        return Task.FromResult(Result.Success());
    }

    public Task<Result> ResumePlaybackAsync(Guid sessionId, CancellationToken ct = default)
    {
        if (!_activePlaybacks.TryGetValue(sessionId, out var session))
            return Task.FromResult(Result.Failure("Playback session not found", ErrorType.NotFound));

        session.IsPaused = false;
        return Task.FromResult(Result.Success());
    }

    public Task<Result> StopPlaybackAsync(Guid sessionId, CancellationToken ct = default)
    {
        if (!_activePlaybacks.Remove(sessionId))
            return Task.FromResult(Result.Failure("Playback session not found", ErrorType.NotFound));

        return Task.FromResult(Result.Success());
    }

    public async Task<Result<InputFrame>> AdvanceFrameAsync(Guid sessionId, CancellationToken ct = default)
    {
        if (!_activePlaybacks.TryGetValue(sessionId, out var session))
            return Result<InputFrame>.Failure("Playback session not found", ErrorType.NotFound);

        var frames = await LoadFrameDataAsync(session.RecordingId, ct);
        if (session.CurrentFrame >= frames.Count)
            return Result<InputFrame>.Failure("End of recording reached", ErrorType.Validation);

        var frame = frames[(int)session.CurrentFrame++];
        session.CurrentInput = frame;
        return Result<InputFrame>.Success(frame);
    }

    public Task<Result> RewindAsync(Guid sessionId, long frameCount, CancellationToken ct = default)
    {
        if (!_activePlaybacks.TryGetValue(sessionId, out var session))
            return Task.FromResult(Result.Failure("Playback session not found", ErrorType.NotFound));

        session.CurrentFrame = Math.Max(0, session.CurrentFrame - frameCount);
        return Task.FromResult(Result.Success());
    }

    public Task<Result> SetPlaybackSpeedAsync(Guid sessionId, PlaybackSpeed speed, CancellationToken ct = default)
    {
        if (!_activePlaybacks.TryGetValue(sessionId, out var session))
            return Task.FromResult(Result.Failure("Playback session not found", ErrorType.NotFound));

        session.Speed = speed;
        return Task.FromResult(Result.Success());
    }

    public Task<Result> SeekToFrameAsync(Guid sessionId, long frameNumber, CancellationToken ct = default)
    {
        if (!_activePlaybacks.TryGetValue(sessionId, out var session))
            return Task.FromResult(Result.Failure("Playback session not found", ErrorType.NotFound));

        session.CurrentFrame = Math.Max(0, Math.Min(frameNumber, session.TotalFrames - 1));
        return Task.FromResult(Result.Success());
    }

    public async Task<Result<InputFrame>> GetNextFrameAsync(Guid sessionId, CancellationToken ct = default)
    {
        return await AdvanceFrameAsync(sessionId, ct);
    }

    public async Task<Result<List<InputRecordingEntity>>> GetRecordingsAsync(InputRecordingFilter? filter = null, CancellationToken ct = default)
    {
        try
        {
            var query = _dbContext.InputRecordings.AsNoTracking().AsQueryable();

            if (filter?.GameId.HasValue == true)
                query = query.Where(r => r.GameId == filter.GameId.Value);

            if (filter?.Type.HasValue == true)
                query = query.Where(r => r.Type == filter.Type.Value);

            if (filter?.Status.HasValue == true)
                query = query.Where(r => r.Status == filter.Status.Value);

            if (filter?.DeviceType.HasValue == true)
                query = query.Where(r => r.DeviceType == filter.DeviceType.Value);

            if (filter?.FromDate.HasValue == true)
                query = query.Where(r => r.RecordedAt >= filter.FromDate.Value);

            if (filter?.ToDate.HasValue == true)
                query = query.Where(r => r.RecordedAt <= filter.ToDate.Value);

            if (filter?.OnlyBookmarked == true)
                query = query.Where(r => r.IsBookmarked);

            if (filter?.OnlyVerifiedTAS == true)
                query = query.Where(r => r.IsVerifiedTAS);

            if (!string.IsNullOrWhiteSpace(filter?.SearchQuery))
            {
                var search = filter.SearchQuery.ToLower();
                query = query.Where(r => r.Name.ToLower().Contains(search) || 
                                        (r.Description != null && r.Description.ToLower().Contains(search)));
            }

            if (filter?.Tags?.Any() == true)
            {
                foreach (var tag in filter.Tags)
                {
                    var tagLower = tag.ToLower();
                    query = query.Where(r => r.Tags.Any(t => t.ToLower().Contains(tagLower)));
                }
            }

            var recordings = await query
                .OrderByDescending(r => r.RecordedAt)
                .ToListAsync(ct);

            return Result<List<InputRecordingEntity>>.Success(recordings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get recordings");
            return Result<List<InputRecordingEntity>>.Failure($"Query failed: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<InputRecordingEntity>> GetRecordingAsync(Guid recordingId, CancellationToken ct = default)
    {
        try
        {
            var recording = await _dbContext.InputRecordings
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == recordingId, ct);

            if (recording == null)
                return Result<InputRecordingEntity>.Failure("Recording not found", ErrorType.NotFound);

            return Result<InputRecordingEntity>.Success(recording);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get recording {RecordingId:B}", recordingId);
            return Result<InputRecordingEntity>.Failure($"Query failed: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<InputRecordingEntity>> UpdateRecordingAsync(Guid recordingId, string name, string? description, List<string>? tags, CancellationToken ct = default)
    {
        try
        {
            var recording = await _dbContext.InputRecordings.FindAsync(new object[] { recordingId }, ct);
            if (recording == null)
                return Result<InputRecordingEntity>.Failure("Recording not found", ErrorType.NotFound);

            recording.Name = name;
            if (description != null) recording.Description = description;
            if (tags != null) recording.Tags = tags;
            recording.UpdatedAt = _timeProvider.UtcNow;

            await _dbContext.SaveChangesAsync(ct);
            return Result<InputRecordingEntity>.Success(recording);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update recording {RecordingId:B}", recordingId);
            return Result<InputRecordingEntity>.Failure($"Update failed: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result> DeleteRecordingAsync(Guid recordingId, CancellationToken ct = default)
    {
        try
        {
            var recording = await _dbContext.InputRecordings.FindAsync(new object[] { recordingId }, ct);
            if (recording == null)
                return Result.Failure("Recording not found", ErrorType.NotFound);

            // Delete file if exists
            if (File.Exists(recording.FilePath))
                File.Delete(recording.FilePath);

            _dbContext.InputRecordings.Remove(recording);
            await _dbContext.SaveChangesAsync(ct);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete recording {RecordingId:B}", recordingId);
            return Result.Failure($"Delete failed: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result> AddBookmarkAsync(Guid recordingId, long frameNumber, string label, CancellationToken ct = default)
    {
        try
        {
            var recording = await _dbContext.InputRecordings.FindAsync(new object[] { recordingId }, ct);
            if (recording == null)
                return Result.Failure("Recording not found", ErrorType.NotFound);

            recording.Bookmarks ??= new List<RecordingBookmark>();
            
            // Remove existing bookmark at same frame if exists
            recording.Bookmarks.RemoveAll(b => b.FrameNumber == frameNumber);
            
            recording.Bookmarks.Add(new RecordingBookmark
            {
                FrameNumber = frameNumber,
                Label = label
            });

            await _dbContext.SaveChangesAsync(ct);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add bookmark to recording {RecordingId:B}", recordingId);
            return Result.Failure($"Failed to add bookmark: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result> RemoveBookmarkAsync(Guid recordingId, long frameNumber, CancellationToken ct = default)
    {
        try
        {
            var recording = await _dbContext.InputRecordings.FindAsync(new object[] { recordingId }, ct);
            if (recording == null)
                return Result.Failure("Recording not found", ErrorType.NotFound);

            recording.Bookmarks ??= new List<RecordingBookmark>();
            recording.Bookmarks.RemoveAll(b => b.FrameNumber == frameNumber);

            await _dbContext.SaveChangesAsync(ct);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove bookmark from recording {RecordingId:B}", recordingId);
            return Result.Failure($"Failed to remove bookmark: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result> ToggleBookmarkAsync(Guid recordingId, bool isBookmarked, CancellationToken ct = default)
    {
        try
        {
            var recording = await _dbContext.InputRecordings.FindAsync(new object[] { recordingId }, ct);
            if (recording == null)
                return Result.Failure("Recording not found", ErrorType.NotFound);

            recording.IsBookmarked = isBookmarked;
            await _dbContext.SaveChangesAsync(ct);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to toggle bookmark for recording {RecordingId:B}", recordingId);
            return Result.Failure($"Failed to toggle bookmark: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<string>> ExportRecordingAsync(ExportRecordingRequest request, CancellationToken ct = default)
    {
        try
        {
            var recording = await _dbContext.InputRecordings.FindAsync(new object[] { request.RecordingId }, ct);
            if (recording == null)
                return Result<string>.Failure("Recording not found", ErrorType.NotFound);

            var frames = await LoadFrameDataAsync(request.RecordingId, ct);
            var outputPath = request.OutputPath;

            switch (request.Format)
            {
                case RecordingExportFormat.Native:
                    outputPath = await ExportNativeFormatAsync(recording, frames, outputPath, request.IncludeMetadata, ct);
                    break;
                case RecordingExportFormat.FM2:
                    outputPath = await ExportFM2FormatAsync(recording, frames, outputPath, ct);
                    break;
                default:
                    return Result<string>.Failure($"Export format {request.Format} not yet implemented", ErrorType.NotImplemented);
            }

            _logger.LogInformation("Exported recording {RecordingId:B} to {OutputPath}", request.RecordingId, outputPath);
            return Result<string>.Success(outputPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export recording {RecordingId:B}", request.RecordingId);
            return Result<string>.Failure($"Export failed: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<InputRecordingEntity>> ImportRecordingAsync(ImportRecordingRequest request, CancellationToken ct = default)
    {
        try
        {
            if (!File.Exists(request.FilePath))
                return Result<InputRecordingEntity>.Failure("File not found", ErrorType.NotFound);

            var extension = Path.GetExtension(request.FilePath).ToLower();
            InputRecordingEntity? recording;
            List<InputFrame>? frames;

            switch (extension)
            {
                case ".json":
                    (recording, frames) = await ImportNativeFormatAsync(request.FilePath, ct);
                    break;
                case ".fm2":
                    (recording, frames) = await ImportFM2FormatAsync(request.FilePath, ct);
                    break;
                default:
                    return Result<InputRecordingEntity>.Failure($"Import format {extension} not supported", ErrorType.NotImplemented);
            }

            if (recording == null)
                return Result<InputRecordingEntity>.Failure("Failed to parse recording file", ErrorType.Validation);

            recording.GameId = request.GameId;
            recording.Name = request.CustomName ?? recording.Name ?? Path.GetFileNameWithoutExtension(request.FilePath);
            if (request.Tags != null) recording.Tags = request.Tags;

            _dbContext.InputRecordings.Add(recording);
            await _dbContext.SaveChangesAsync(ct);

            // Save frame data
            recording.FilePath = await SaveFrameDataAsync(recording.Id, frames ?? new List<InputFrame>(), ct);
            recording.FileSize = new FileInfo(recording.FilePath).Length;
            recording.Status = RecordingStatus.Ready;
            await _dbContext.SaveChangesAsync(ct);

            _logger.LogInformation("Imported recording {RecordingId:B} from {FilePath}", recording.Id, request.FilePath);
            return Result<InputRecordingEntity>.Success(recording);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import recording from {FilePath}", request.FilePath);
            return Result<InputRecordingEntity>.Failure($"Import failed: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<List<InputFrame>>> GetFrameDataAsync(Guid recordingId, CancellationToken ct = default)
    {
        try
        {
            var frames = await LoadFrameDataAsync(recordingId, ct);
            return Result<List<InputFrame>>.Success(frames);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load frame data for recording {RecordingId:B}", recordingId);
            return Result<List<InputFrame>>.Failure($"Failed to load frame data: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<List<InputFrame>>> GetFrameRangeAsync(Guid recordingId, long startFrame, long endFrame, CancellationToken ct = default)
    {
        try
        {
            var allFrames = await LoadFrameDataAsync(recordingId, ct);
            var rangeFrames = allFrames
                .Where(f => f.FrameNumber >= startFrame && f.FrameNumber <= endFrame)
                .ToList();
            return Result<List<InputFrame>>.Success(rangeFrames);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load frame range for recording {RecordingId:B}", recordingId);
            return Result<List<InputFrame>>.Failure($"Failed to load frame range: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<Dictionary<string, int>>> GetInputHistogramAsync(Guid recordingId, CancellationToken ct = default)
    {
        try
        {
            var frames = await LoadFrameDataAsync(recordingId, ct);
            var histogram = new Dictionary<string, int>();

            foreach (var frame in frames)
            {
                foreach (var input in frame.PressedInputs)
                {
                    if (!histogram.ContainsKey(input))
                        histogram[input] = 0;
                    histogram[input]++;
                }
            }

            return Result<Dictionary<string, int>>.Success(histogram);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get input histogram for recording {RecordingId:B}", recordingId);
            return Result<Dictionary<string, int>>.Failure($"Failed to get histogram: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<InputRecordingEntity>> TrimRecordingAsync(Guid recordingId, long startFrame, long endFrame, CancellationToken ct = default)
    {
        try
        {
            var recording = await _dbContext.InputRecordings.FindAsync(new object[] { recordingId }, ct);
            if (recording == null)
                return Result<InputRecordingEntity>.Failure("Recording not found", ErrorType.NotFound);

            var frames = await LoadFrameDataAsync(recordingId, ct);
            var trimmedFrames = frames
                .Where(f => f.FrameNumber >= startFrame && f.FrameNumber <= endFrame)
                .ToList();

            // Renumber frames
            for (int i = 0; i < trimmedFrames.Count; i++)
                trimmedFrames[i].FrameNumber = i;

            // Save trimmed data
            recording.FilePath = await SaveFrameDataAsync(recordingId, trimmedFrames, ct);
            recording.TotalFrames = trimmedFrames.Count;
            recording.FileSize = new FileInfo(recording.FilePath).Length;
            await _dbContext.SaveChangesAsync(ct);

            return Result<InputRecordingEntity>.Success(recording);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to trim recording {RecordingId:B}", recordingId);
            return Result<InputRecordingEntity>.Failure($"Trim failed: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<InputRecordingEntity>> ConcatenateRecordingsAsync(List<Guid> recordingIds, string newName, CancellationToken ct = default)
    {
        try
        {
            if (recordingIds.Count < 2)
                return Result<InputRecordingEntity>.Failure("At least 2 recordings required for concatenation", ErrorType.Validation);

            var allFrames = new List<InputFrame>();
            long frameOffset = 0;
            Guid? gameId = null;

            foreach (var id in recordingIds)
            {
                var recording = await _dbContext.InputRecordings.FindAsync(new object[] { id }, ct);
                if (recording == null)
                    return Result<InputRecordingEntity>.Failure($"Recording {id:B} not found", ErrorType.NotFound);

                gameId ??= recording.GameId;
                if (recording.GameId != gameId)
                    return Result<InputRecordingEntity>.Failure("All recordings must be for the same game", ErrorType.Validation);

                var frames = await LoadFrameDataAsync(id, ct);
                foreach (var frame in frames)
                {
                    frame.FrameNumber += frameOffset;
                    allFrames.Add(frame);
                }
                frameOffset = allFrames.Count;
            }

            var newRecording = new InputRecordingEntity
            {
                GameId = gameId.Value,
                Name = newName,
                Type = RecordingType.Gameplay,
                TotalFrames = allFrames.Count,
                Tags = new List<string> { "concatenated" }
            };

            _dbContext.InputRecordings.Add(newRecording);
            await _dbContext.SaveChangesAsync(ct);

            newRecording.FilePath = await SaveFrameDataAsync(newRecording.Id, allFrames, ct);
            newRecording.FileSize = new FileInfo(newRecording.FilePath).Length;
            newRecording.Status = RecordingStatus.Ready;
            await _dbContext.SaveChangesAsync(ct);

            return Result<InputRecordingEntity>.Success(newRecording);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to concatenate recordings");
            return Result<InputRecordingEntity>.Failure($"Concatenation failed: {ex.Message}", ErrorType.Internal);
        }
    }
}
