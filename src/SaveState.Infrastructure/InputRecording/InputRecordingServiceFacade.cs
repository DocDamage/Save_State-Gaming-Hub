using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.InputRecording;
using SaveState.Core.InputRecording.Services;
using SaveState.Infrastructure.Persistence;
using InputRecordingEntity = SaveState.Core.InputRecording.InputRecording;

namespace SaveState.Infrastructure.InputRecording.Services;

/// <summary>
/// Thin facade for input recording operations.
/// </summary>
public class InputRecordingService : IInputRecordingService
{
    private readonly InputRecordingServiceOperations _operations;

    public InputRecordingService(
        SaveStateDbContext dbContext,
        ILogger<InputRecordingService> logger)
    {
        _operations = new InputRecordingServiceOperations(dbContext, logger);
    }

    public Task<Result<RecordingSession>> StartRecordingAsync(StartRecordingRequest request, CancellationToken ct = default)
        => _operations.StartRecordingAsync(request, ct);

    public Task<Result> PauseRecordingAsync(Guid sessionId, CancellationToken ct = default)
        => _operations.PauseRecordingAsync(sessionId, ct);

    public Task<Result> ResumeRecordingAsync(Guid sessionId, CancellationToken ct = default)
        => _operations.ResumeRecordingAsync(sessionId, ct);

    public Task<Result<InputRecordingEntity>> StopRecordingAsync(Guid sessionId, CancellationToken ct = default)
        => _operations.StopRecordingAsync(sessionId, ct);

    public Task<Result> RecordFrameAsync(Guid sessionId, InputFrame frame, CancellationToken ct = default)
        => _operations.RecordFrameAsync(sessionId, frame, ct);

    public Task<Result<RecordingSession>> GetActiveRecordingAsync(Guid gameId, CancellationToken ct = default)
        => _operations.GetActiveRecordingAsync(gameId, ct);

    public Task<Result<PlaybackSession>> StartPlaybackAsync(StartPlaybackRequest request, CancellationToken ct = default)
        => _operations.StartPlaybackAsync(request, ct);

    public Task<Result> PausePlaybackAsync(Guid sessionId, CancellationToken ct = default)
        => _operations.PausePlaybackAsync(sessionId, ct);

    public Task<Result> ResumePlaybackAsync(Guid sessionId, CancellationToken ct = default)
        => _operations.ResumePlaybackAsync(sessionId, ct);

    public Task<Result> StopPlaybackAsync(Guid sessionId, CancellationToken ct = default)
        => _operations.StopPlaybackAsync(sessionId, ct);

    public Task<Result<InputFrame>> AdvanceFrameAsync(Guid sessionId, CancellationToken ct = default)
        => _operations.AdvanceFrameAsync(sessionId, ct);

    public Task<Result> RewindAsync(Guid sessionId, long frameCount, CancellationToken ct = default)
        => _operations.RewindAsync(sessionId, frameCount, ct);

    public Task<Result> SetPlaybackSpeedAsync(Guid sessionId, PlaybackSpeed speed, CancellationToken ct = default)
        => _operations.SetPlaybackSpeedAsync(sessionId, speed, ct);

    public Task<Result> SeekToFrameAsync(Guid sessionId, long frameNumber, CancellationToken ct = default)
        => _operations.SeekToFrameAsync(sessionId, frameNumber, ct);

    public Task<Result<InputFrame>> GetNextFrameAsync(Guid sessionId, CancellationToken ct = default)
        => _operations.GetNextFrameAsync(sessionId, ct);

    public Task<Result<List<InputRecordingEntity>>> GetRecordingsAsync(InputRecordingFilter? filter = null, CancellationToken ct = default)
        => _operations.GetRecordingsAsync(filter, ct);

    public Task<Result<InputRecordingEntity>> GetRecordingAsync(Guid recordingId, CancellationToken ct = default)
        => _operations.GetRecordingAsync(recordingId, ct);

    public Task<Result<InputRecordingEntity>> UpdateRecordingAsync(Guid recordingId, string name, string? description, List<string>? tags, CancellationToken ct = default)
        => _operations.UpdateRecordingAsync(recordingId, name, description, tags, ct);

    public Task<Result> DeleteRecordingAsync(Guid recordingId, CancellationToken ct = default)
        => _operations.DeleteRecordingAsync(recordingId, ct);

    public Task<Result> AddBookmarkAsync(Guid recordingId, long frameNumber, string label, CancellationToken ct = default)
        => _operations.AddBookmarkAsync(recordingId, frameNumber, label, ct);

    public Task<Result> RemoveBookmarkAsync(Guid recordingId, long frameNumber, CancellationToken ct = default)
        => _operations.RemoveBookmarkAsync(recordingId, frameNumber, ct);

    public Task<Result> ToggleBookmarkAsync(Guid recordingId, bool isBookmarked, CancellationToken ct = default)
        => _operations.ToggleBookmarkAsync(recordingId, isBookmarked, ct);

    public Task<Result<string>> ExportRecordingAsync(ExportRecordingRequest request, CancellationToken ct = default)
        => _operations.ExportRecordingAsync(request, ct);

    public Task<Result<InputRecordingEntity>> ImportRecordingAsync(ImportRecordingRequest request, CancellationToken ct = default)
        => _operations.ImportRecordingAsync(request, ct);

    public Task<Result<List<InputFrame>>> GetFrameDataAsync(Guid recordingId, CancellationToken ct = default)
        => _operations.GetFrameDataAsync(recordingId, ct);

    public Task<Result<List<InputFrame>>> GetFrameRangeAsync(Guid recordingId, long startFrame, long endFrame, CancellationToken ct = default)
        => _operations.GetFrameRangeAsync(recordingId, startFrame, endFrame, ct);

    public Task<Result<Dictionary<string, int>>> GetInputHistogramAsync(Guid recordingId, CancellationToken ct = default)
        => _operations.GetInputHistogramAsync(recordingId, ct);

    public Task<Result<InputRecordingEntity>> TrimRecordingAsync(Guid recordingId, long startFrame, long endFrame, CancellationToken ct = default)
        => _operations.TrimRecordingAsync(recordingId, startFrame, endFrame, ct);

    public Task<Result<InputRecordingEntity>> ConcatenateRecordingsAsync(List<Guid> recordingIds, string newName, CancellationToken ct = default)
        => _operations.ConcatenateRecordingsAsync(recordingIds, newName, ct);

    public Task<Result<InputRecordingStatistics>> GetStatisticsAsync(Guid? gameId = null, CancellationToken ct = default)
        => _operations.GetStatisticsAsync(gameId, ct);

    public Task<Result<bool>> ValidateRecordingAsync(Guid recordingId, CancellationToken ct = default)
        => _operations.ValidateRecordingAsync(recordingId, ct);

    public Task<Result<InputRecordingEntity>> RepairRecordingAsync(Guid recordingId, CancellationToken ct = default)
        => _operations.RepairRecordingAsync(recordingId, ct);

    public Task<Result<List<RecordingExportFormat>>> GetSupportedImportFormatsAsync()
        => _operations.GetSupportedImportFormatsAsync();

    public Task<Result<List<RecordingExportFormat>>> GetSupportedExportFormatsAsync()
        => _operations.GetSupportedExportFormatsAsync();
}
