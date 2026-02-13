using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Mugen.ValueObjects;
using SaveState.Application.Mugen.Services.Training;
using SaveState.Application.Mugen.Services.Training.Engines;
using Microsoft.Extensions.Logging;

namespace SaveState.Application.Mugen.Services;

/// <summary>
/// Advanced training modes service providing reflex training, pattern recognition,
/// combo labs, and comprehensive skill development tools for MUGEN players.
/// This service acts as a coordinator, delegating specific logic to specialized engines.
/// </summary>
public class TrainingModeService : ITrainingModeService
{
    private readonly ILogger<TrainingModeService> _logger;
    private readonly ICacheService _cache;

    // Core engines
    private readonly SessionManager _sessionManager;
    private readonly InputRouter _inputRouter;
    private readonly ReflexTrainer _reflexTrainer;
    private readonly TrainingPatternEngine _patternEngine;
    private readonly ComboLabEngine _comboLab;
    private readonly SkillAssessor _skillAssessor;
    private readonly ChallengeEngine _challengeEngine;
    private readonly RecordingEngine _recordingEngine;
    private readonly AiDummyEngine _dummyEngine;

    public TrainingModeService(
        ILogger<TrainingModeService> logger,
        ILoggerFactory loggerFactory,
        ICacheService cache)
    {
        _logger = logger;
        _cache = cache;

        // Initialize engines
        _reflexTrainer = new ReflexTrainer(loggerFactory.CreateLogger<ReflexTrainer>());
        _patternEngine = new TrainingPatternEngine(loggerFactory.CreateLogger<TrainingPatternEngine>());
        _comboLab = new ComboLabEngine(loggerFactory.CreateLogger<ComboLabEngine>());
        _sessionManager = new SessionManager(loggerFactory.CreateLogger<SessionManager>());
        _inputRouter = new InputRouter(
            loggerFactory.CreateLogger<InputRouter>(),
            _reflexTrainer,
            _patternEngine,
            _comboLab);
        _skillAssessor = new SkillAssessor(loggerFactory.CreateLogger<SkillAssessor>());
        _challengeEngine = new ChallengeEngine(loggerFactory.CreateLogger<ChallengeEngine>());
        _recordingEngine = new RecordingEngine(loggerFactory.CreateLogger<RecordingEngine>());
        _dummyEngine = new AiDummyEngine(loggerFactory.CreateLogger<AiDummyEngine>());
    }

    #region Session Management

    /// <inheritdoc />
    public async Task<Result<TrainingSession>> StartReflexTrainingAsync(
        string userId,
        ReflexTrainingRequest request,
        CancellationToken ct = default)
    {
        return await StartSessionAsync(userId, TrainingType.Reflex, request.Difficulty, request.Duration,
            session =>
            {
                session.TrainingMode = request.TrainingMode;
                session.ReflexData = CreateReflexData(request.TrainingMode, _reflexTrainer.CalculateTotalRounds(request.Duration));
            },
            async session => await _reflexTrainer.GenerateNextStimulusAsync(session, ct),
            ct);
    }

    /// <inheritdoc />
    public async Task<Result<TrainingSession>> StartPatternRecognitionAsync(
        string userId,
        PatternRecognitionRequest request,
        CancellationToken ct = default)
    {
        return await StartSessionAsync(userId, TrainingType.PatternRecognition, request.Difficulty, request.Duration,
            session =>
            {
                session.PatternData = CreatePatternData(request);
            },
            async session => await _patternEngine.GenerateNextPatternAsync(session, ct),
            ct);
    }

    /// <inheritdoc />
    public async Task<Result<TrainingSession>> StartComboLabAsync(
        string userId,
        ComboLabRequest request,
        CancellationToken ct = default)
    {
        return await StartSessionAsync(userId, TrainingType.ComboLab, request.Difficulty, request.Duration,
            session =>
            {
                session.ComboData = CreateComboData(request);
            },
            _ => Task.CompletedTask,
            ct);
    }

    /// <inheritdoc />
    public Task<Result<TrainingSession>> GetTrainingSessionAsync(string sessionId, CancellationToken ct = default)
    {
        if (_sessionManager.TryGetSession(sessionId, out var session))
        {
            return Task.FromResult(Result.Success(session!));
        }
        return Task.FromResult(Result.Failure<TrainingSession>("Training session not found"));
    }

    /// <inheritdoc />
    public async Task<Result> EndTrainingSessionAsync(string sessionId, CancellationToken ct = default)
    {
        if (!_sessionManager.TryGetSession(sessionId, out var session) || session is null)
        {
            return Result.Failure("Training session not found");
        }

        _logger.LogInformation("Ending training session {SessionId}", sessionId);

        _sessionManager.CompleteSession(session);
        await SaveTrainingDataAsync(session, ct);

        _sessionManager.RemoveSession(sessionId);

        _logger.LogInformation("Training session ended successfully");
        return Result.Success();
    }

    #endregion

    #region Input Processing

    /// <inheritdoc />
    public async Task<Result<TrainingTypes.TrainingResponse>> ProcessTrainingInputAsync(
        string sessionId,
        TrainingInput input,
        CancellationToken ct = default)
    {
        try
        {
            if (!_sessionManager.TryGetSession(sessionId, out var session) || session is null)
            {
                return Result.Failure<TrainingTypes.TrainingResponse>("Training session not found");
            }

            _logger.LogInformation("Processing training input for session {SessionId}", sessionId);

            var response = await _inputRouter.ProcessInputAsync(session, input, ct);

            if (_sessionManager.ShouldEndSession(session))
            {
                await EndTrainingSessionAsync(sessionId, ct);
            }

            return Result.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing training input for session {SessionId}", sessionId);
            return Result.Failure<TrainingTypes.TrainingResponse>($"Input processing failed: {ex.Message}");
        }
    }

    #endregion

    #region Statistics and Recommendations

    /// <inheritdoc />
    public async Task<Result<TrainingStatistics>> GetTrainingStatisticsAsync(
        string userId,
        TimeSpan period,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Getting training statistics for user {UserId}", userId);

            var stats = await _skillAssessor.CalculateStatisticsAsync(
                userId, period, _sessionManager.GetUserSessions(userId), ct);

            return Result.Success(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting training statistics for {UserId}", userId);
            return Result.Failure<TrainingStatistics>($"Statistics retrieval failed: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result<TrainingRecommendations>> GetTrainingRecommendationsAsync(
        string userId,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Generating training recommendations for user {UserId}", userId);

            var statsResult = await GetTrainingStatisticsAsync(userId, TimeSpan.FromDays(30), ct);
            var recommendations = await _skillAssessor.GenerateRecommendationsAsync(
                userId, statsResult.IsSuccess ? statsResult.Value : null, ct);

            return Result.Success(recommendations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating training recommendations for {UserId}", userId);
            return Result.Failure<TrainingRecommendations>($"Recommendations failed: {ex.Message}");
        }
    }

    #endregion

    #region Helper Methods

    private async Task<Result<TrainingSession>> StartSessionAsync(
        string userId,
        TrainingType type,
        DifficultyLevel difficulty,
        TimeSpan duration,
        Action<TrainingSession> configure,
        Func<TrainingSession, Task> initialize,
        CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Starting {TrainingType} training for user {UserId}", type, userId);

            var session = new TrainingSession
            {
                SessionId = Guid.NewGuid().ToString(),
                UserId = userId,
                TrainingType = type,
                Difficulty = difficulty,
                Duration = duration,
                Status = SessionStatus.Active,
                StartedAt = DateTime.UtcNow,
                Progress = new TrainingTypes.TrainingProgressData(0, 10, 0, 0, TimeSpan.Zero, TimeSpan.MaxValue)
            };

            configure(session);
            _sessionManager.AddSession(session);
            await initialize(session);

            _logger.LogInformation("Training session started: {SessionId}", session.SessionId);
            return Result.Success(session);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting {TrainingType} training for {UserId}", type, userId);
            return Result.Failure<TrainingSession>($"Training failed: {ex.Message}");
        }
    }

    private static ReflexTrainingData CreateReflexData(ReflexTrainingMode mode, int totalRounds)
    {
        return new ReflexTrainingData
        {
            StimuliPresented = 0,
            ResponseAccuracy = 0.0,
            AverageReactionTime = TimeSpan.Zero,
            FastestReaction = TimeSpan.MaxValue,
            SlowestReaction = TimeSpan.Zero,
            TrainingMode = mode
        };
    }

    private static PatternRecognitionData CreatePatternData(PatternRecognitionRequest request)
    {
        return new PatternRecognitionData
        {
            SequenceLength = request.SequenceLength,
            SequenceType = request.SequenceType,
            CurrentSequence = Array.Empty<string>(),
            PlayerSequence = Array.Empty<string>(),
            SequencesCompleted = 0,
            AccuracyRate = 0.0
        };
    }

    private static ComboLabData CreateComboData(ComboLabRequest request)
    {
        return new ComboLabData
        {
            LabType = request.LabType,
            TargetCombo = request.TargetCombo,
            CurrentInput = Array.Empty<string>(),
            ComboProgress = 0,
            Mistakes = 0,
            TimeToComplete = TimeSpan.Zero,
            BestTime = TimeSpan.MaxValue,
            Attempts = 0
        };
    }

    private Task SaveTrainingDataAsync(TrainingSession session, CancellationToken ct)
    {
        var cacheKey = $"training_session_{session.SessionId}";
        _cache.Set(cacheKey, session, TimeSpan.FromDays(30));
        return Task.CompletedTask;
    }

    #endregion

    #region Engine Accessors

    /// <summary>Gets the challenge engine.</summary>
    public ChallengeEngine ChallengeEngine => _challengeEngine;

    /// <summary>Gets the recording engine.</summary>
    public RecordingEngine RecordingEngine => _recordingEngine;

    /// <summary>Gets the AI dummy engine.</summary>
    public AiDummyEngine DummyEngine => _dummyEngine;

    /// <summary>Gets the session manager.</summary>
    public SessionManager SessionManager => _sessionManager;

    #endregion
}
