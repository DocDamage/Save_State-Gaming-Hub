using SaveState.Core.Common;

namespace SaveState.Core.InputRecording.Services;

/// <summary>
/// Service for managing input recording and TAS functionality.
/// </summary>
public interface IInputRecordingService
{
    /// <summary>
    /// Starts a new input recording session.
    /// </summary>
    Task<Result<RecordingSession>> StartRecordingAsync(StartRecordingRequest request, CancellationToken ct = default);
    
    /// <summary>
    /// Pauses the current recording.
    /// </summary>
    Task<Result> PauseRecordingAsync(Guid sessionId, CancellationToken ct = default);
    
    /// <summary>
    /// Resumes a paused recording.
    /// </summary>
    Task<Result> ResumeRecordingAsync(Guid sessionId, CancellationToken ct = default);
    
    /// <summary>
    /// Stops recording and saves the recording.
    /// </summary>
    Task<Result<InputRecording>> StopRecordingAsync(Guid sessionId, CancellationToken ct = default);
    
    /// <summary>
    /// Records a single frame of input.
    /// </summary>
    Task<Result> RecordFrameAsync(Guid sessionId, InputFrame frame, CancellationToken ct = default);
    
    /// <summary>
    /// Gets the active recording session.
    /// </summary>
    Task<Result<RecordingSession>> GetActiveRecordingAsync(Guid gameId, CancellationToken ct = default);
    
    /// <summary>
    /// Starts playback of a recording.
    /// </summary>
    Task<Result<PlaybackSession>> StartPlaybackAsync(StartPlaybackRequest request, CancellationToken ct = default);
    
    /// <summary>
    /// Pauses playback.
    /// </summary>
    Task<Result> PausePlaybackAsync(Guid sessionId, CancellationToken ct = default);
    
    /// <summary>
    /// Resumes playback.
    /// </summary>
    Task<Result> ResumePlaybackAsync(Guid sessionId, CancellationToken ct = default);
    
    /// <summary>
    /// Stops playback.
    /// </summary>
    Task<Result> StopPlaybackAsync(Guid sessionId, CancellationToken ct = default);
    
    /// <summary>
    /// Advances playback by one frame (frame-stepping).
    /// </summary>
    Task<Result<InputFrame>> AdvanceFrameAsync(Guid sessionId, CancellationToken ct = default);
    
    /// <summary>
    /// Rewinds playback by specified number of frames.
    /// </summary>
    Task<Result> RewindAsync(Guid sessionId, long frameCount, CancellationToken ct = default);
    
    /// <summary>
    /// Sets playback speed.
    /// </summary>
    Task<Result> SetPlaybackSpeedAsync(Guid sessionId, PlaybackSpeed speed, CancellationToken ct = default);
    
    /// <summary>
    /// Seeks to a specific frame.
    /// </summary>
    Task<Result> SeekToFrameAsync(Guid sessionId, long frameNumber, CancellationToken ct = default);
    
    /// <summary>
    /// Gets the next frame for playback.
    /// </summary>
    Task<Result<InputFrame>> GetNextFrameAsync(Guid sessionId, CancellationToken ct = default);
    
    /// <summary>
    /// Gets all recordings with optional filtering.
    /// </summary>
    Task<Result<List<InputRecording>>> GetRecordingsAsync(InputRecordingFilter? filter = null, CancellationToken ct = default);
    
    /// <summary>
    /// Gets a specific recording by ID.
    /// </summary>
    Task<Result<InputRecording>> GetRecordingAsync(Guid recordingId, CancellationToken ct = default);
    
    /// <summary>
    /// Updates recording metadata.
    /// </summary>
    Task<Result<InputRecording>> UpdateRecordingAsync(Guid recordingId, string name, string? description, List<string>? tags, CancellationToken ct = default);
    
    /// <summary>
    /// Deletes a recording.
    /// </summary>
    Task<Result> DeleteRecordingAsync(Guid recordingId, CancellationToken ct = default);
    
    /// <summary>
    /// Adds a bookmark at a specific frame.
    /// </summary>
    Task<Result> AddBookmarkAsync(Guid recordingId, long frameNumber, string label, CancellationToken ct = default);
    
    /// <summary>
    /// Removes a bookmark.
    /// </summary>
    Task<Result> RemoveBookmarkAsync(Guid recordingId, long frameNumber, CancellationToken ct = default);
    
    /// <summary>
    /// Toggles bookmark status for a recording.
    /// </summary>
    Task<Result> ToggleBookmarkAsync(Guid recordingId, bool isBookmarked, CancellationToken ct = default);
    
    /// <summary>
    /// Exports a recording to a file.
    /// </summary>
    Task<Result<string>> ExportRecordingAsync(ExportRecordingRequest request, CancellationToken ct = default);
    
    /// <summary>
    /// Imports a recording from a file.
    /// </summary>
    Task<Result<InputRecording>> ImportRecordingAsync(ImportRecordingRequest request, CancellationToken ct = default);
    
    /// <summary>
    /// Gets the raw frame data for a recording.
    /// </summary>
    Task<Result<List<InputFrame>>> GetFrameDataAsync(Guid recordingId, CancellationToken ct = default);
    
    /// <summary>
    /// Gets frame data for a specific range.
    /// </summary>
    Task<Result<List<InputFrame>>> GetFrameRangeAsync(Guid recordingId, long startFrame, long endFrame, CancellationToken ct = default);
    
    /// <summary>
    /// Gets input histogram (button press frequency).
    /// </summary>
    Task<Result<Dictionary<string, int>>> GetInputHistogramAsync(Guid recordingId, CancellationToken ct = default);
    
    /// <summary>
    /// Trims a recording to a frame range.
    /// </summary>
    Task<Result<InputRecording>> TrimRecordingAsync(Guid recordingId, long startFrame, long endFrame, CancellationToken ct = default);
    
    /// <summary>
    /// Concatenates multiple recordings into one.
    /// </summary>
    Task<Result<InputRecording>> ConcatenateRecordingsAsync(List<Guid> recordingIds, string newName, CancellationToken ct = default);
    
    /// <summary>
    /// Gets statistics for recordings.
    /// </summary>
    Task<Result<InputRecordingStatistics>> GetStatisticsAsync(Guid? gameId = null, CancellationToken ct = default);
    
    /// <summary>
    /// Validates a recording file integrity.
    /// </summary>
    Task<Result<bool>> ValidateRecordingAsync(Guid recordingId, CancellationToken ct = default);
    
    /// <summary>
    /// Repairs a corrupted recording if possible.
    /// </summary>
    Task<Result<InputRecording>> RepairRecordingAsync(Guid recordingId, CancellationToken ct = default);
    
    /// <summary>
    /// Gets supported import formats.
    /// </summary>
    Task<List<RecordingExportFormat>> GetSupportedImportFormatsAsync();
    
    /// <summary>
    /// Gets supported export formats.
    /// </summary>
    Task<List<RecordingExportFormat>> GetSupportedExportFormatsAsync();
}
