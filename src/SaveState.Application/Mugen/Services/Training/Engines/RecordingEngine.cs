namespace SaveState.Application.Mugen.Services.Training.Engines;

using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;

/// <summary>
/// Manages recording and playback of training sessions.
/// </summary>
public class RecordingEngine
{
    private readonly ILogger<RecordingEngine> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly Dictionary<string, Recording> _recordings = new();
    private readonly Dictionary<string, PlaybackSession> _playbackSessions = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="RecordingEngine"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="timeProvider">The time provider.</param>
    public RecordingEngine(ILogger<RecordingEngine> logger, ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Starts a new recording.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="characterId">Optional character ID.</param>
    /// <param name="stageId">Optional stage ID.</param>
    /// <returns>The new recording.</returns>
    public Recording StartRecording(string userId, string? characterId = null, string? stageId = null)
    {
        var recording = new Recording
        {
            RecordingId = Guid.NewGuid().ToString(),
            UserId = userId,
            CharacterId = characterId,
            StageId = stageId,
            Name = $"Recording {_timeProvider.UtcNow:yyyy-MM-dd HH:mm:ss}",
            RecordedAt = _timeProvider.UtcNow,
            Frames = new List<RecordedFrame>(),
            Metadata = new RecordingMetadata(),
            Tags = new List<string>()
        };

        _recordings[recording.RecordingId] = recording;
        _logger.LogInformation("Started recording {RecordingId} for user {UserId}", recording.RecordingId, userId);

        return recording;
    }

    /// <summary>
    /// Records a frame.
    /// </summary>
    /// <param name="recordingId">The recording ID.</param>
    /// <param name="frame">The frame data.</param>
    public void RecordFrame(string recordingId, RecordedFrame frame)
    {
        if (_recordings.TryGetValue(recordingId, out var recording))
        {
            var frames = recording.Frames.ToList();
            frames.Add(frame);
            recording.Frames = frames;
            recording.FrameCount = frames.Count;
        }
    }

    /// <summary>
    /// Stops a recording.
    /// </summary>
    /// <param name="recordingId">The recording ID.</param>
    /// <returns>The completed recording.</returns>
    public Result<Recording> StopRecording(string recordingId)
    {
        if (_recordings.TryGetValue(recordingId, out var recording))
        {
            recording.Duration = _timeProvider.UtcNow - recording.RecordedAt;
            _logger.LogInformation("Stopped recording {RecordingId} with {FrameCount} frames",
                recordingId, recording.FrameCount);
            return Result.Success(recording);
        }

        return Result.Failure<Recording>($"Recording {recordingId} not found", ErrorType.NotFound);
    }

    /// <summary>
    /// Gets a recording by ID.
    /// </summary>
    /// <param name="recordingId">The recording ID.</param>
    /// <returns>The recording if found.</returns>
    public Result<Recording> GetRecording(string recordingId)
    {
        if (_recordings.TryGetValue(recordingId, out var recording))
        {
            return Result.Success(recording);
        }

        return Result.Failure<Recording>($"Recording {recordingId} not found", ErrorType.NotFound);
    }

    /// <summary>
    /// Gets all recordings for a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>List of user recordings.</returns>
    public IReadOnlyList<Recording> GetUserRecordings(string userId)
    {
        return _recordings.Values.Where(r => r.UserId == userId).ToList();
    }

    /// <summary>
    /// Deletes a recording.
    /// </summary>
    /// <param name="recordingId">The recording ID.</param>
    /// <returns>True if deleted.</returns>
    public bool DeleteRecording(string recordingId)
    {
        return _recordings.Remove(recordingId);
    }

    /// <summary>
    /// Starts playback of a recording.
    /// </summary>
    /// <param name="recordingId">The recording ID.</param>
    /// <param name="options">Playback options.</param>
    /// <returns>The playback session.</returns>
    public Result<PlaybackSession> StartPlayback(string recordingId, PlaybackOptions? options = null)
    {
        if (!_recordings.TryGetValue(recordingId, out var recording))
        {
            return Result.Failure<PlaybackSession>($"Recording {recordingId} not found", ErrorType.NotFound);
        }

        options ??= new PlaybackOptions();

        var session = new PlaybackSession
        {
            SessionId = Guid.NewGuid().ToString(),
            RecordingId = recordingId,
            Options = options,
            Status = PlaybackStatus.Playing,
            CurrentFrame = options.StartFrame,
            CurrentLoop = 1,
            StartedAt = _timeProvider.UtcNow
        };

        _playbackSessions[session.SessionId] = session;
        _logger.LogInformation("Started playback session {SessionId} for recording {RecordingId}",
            session.SessionId, recordingId);

        return Result.Success(session);
    }

    /// <summary>
    /// Gets the next frame during playback.
    /// </summary>
    /// <param name="sessionId">The playback session ID.</param>
    /// <returns>The next frame, or failure if playback ended.</returns>
    public Result<RecordedFrame> GetNextFrame(string sessionId)
    {
        if (!_playbackSessions.TryGetValue(sessionId, out var session))
        {
            return Result.Failure<RecordedFrame>($"Session {sessionId} not found", ErrorType.NotFound);
        }

        if (!_recordings.TryGetValue(session.RecordingId, out var recording))
        {
            return Result.Failure<RecordedFrame>($"Recording {session.RecordingId} not found", ErrorType.NotFound);
        }

        if (session.Status != PlaybackStatus.Playing)
        {
            return Result.Failure<RecordedFrame>($"Session is not playing (status: {session.Status})", ErrorType.Validation);
        }

        var frames = recording.Frames.ToList();
        var endFrame = session.Options.EndFrame ?? frames.Count - 1;

        if (session.CurrentFrame > endFrame)
        {
            HandlePlaybackEnd(session, recording);
            return Result.Failure<RecordedFrame>("Playback reached end", ErrorType.NotFound);
        }

        var frame = frames[session.CurrentFrame];
        session.CurrentFrame++;
        session.ElapsedTime = _timeProvider.UtcNow - session.StartedAt;

        return Result.Success(frame);
    }

    /// <summary>
    /// Pauses playback.
    /// </summary>
    /// <param name="sessionId">The playback session ID.</param>
    /// <returns>True if paused.</returns>
    public bool PausePlayback(string sessionId)
    {
        if (_playbackSessions.TryGetValue(sessionId, out var session))
        {
            if (session.Status == PlaybackStatus.Playing)
            {
                session.Status = PlaybackStatus.Paused;
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Resumes playback.
    /// </summary>
    /// <param name="sessionId">The playback session ID.</param>
    /// <returns>True if resumed.</returns>
    public bool ResumePlayback(string sessionId)
    {
        if (_playbackSessions.TryGetValue(sessionId, out var session))
        {
            if (session.Status == PlaybackStatus.Paused)
            {
                session.Status = PlaybackStatus.Playing;
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Stops playback.
    /// </summary>
    /// <param name="sessionId">The playback session ID.</param>
    /// <returns>True if stopped.</returns>
    public bool StopPlayback(string sessionId)
    {
        if (_playbackSessions.TryGetValue(sessionId, out var session))
        {
            session.Status = PlaybackStatus.Stopped;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Seeks to a specific frame.
    /// </summary>
    /// <param name="sessionId">The playback session ID.</param>
    /// <param name="frameNumber">The frame number.</param>
    /// <returns>True if seek was successful.</returns>
    public bool SeekToFrame(string sessionId, int frameNumber)
    {
        if (_playbackSessions.TryGetValue(sessionId, out var session))
        {
            if (_recordings.TryGetValue(session.RecordingId, out var recording))
            {
                var maxFrame = recording.FrameCount - 1;
                session.CurrentFrame = Math.Max(0, Math.Min(frameNumber, maxFrame));
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Gets the current playback session.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <returns>The playback session if found.</returns>
    public Result<PlaybackSession> GetPlaybackSession(string sessionId)
    {
        if (_playbackSessions.TryGetValue(sessionId, out var session))
        {
            return Result.Success(session);
        }

        return Result.Failure<PlaybackSession>($"Session {sessionId} not found", ErrorType.NotFound);
    }

    private void HandlePlaybackEnd(PlaybackSession session, Recording recording)
    {
        var endFrame = session.Options.EndFrame ?? recording.FrameCount - 1;
        var startFrame = session.Options.StartFrame;

        switch (session.Options.Mode)
        {
            case PlaybackMode.Once:
                session.Status = PlaybackStatus.Finished;
                break;

            case PlaybackMode.Loop:
                if (session.CurrentLoop < session.Options.LoopCount)
                {
                    session.CurrentLoop++;
                    session.CurrentFrame = startFrame;
                }
                else
                {
                    session.Status = PlaybackStatus.Finished;
                }
                break;

            case PlaybackMode.Random:
                session.CurrentFrame = Random.Shared.Next(startFrame, endFrame + 1);
                break;

            case PlaybackMode.PingPong:
                session.CurrentFrame = endFrame;
                break;
        }
    }
}
