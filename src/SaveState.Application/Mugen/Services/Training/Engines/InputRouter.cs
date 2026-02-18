namespace SaveState.Application.Mugen.Services.Training.Engines;

using Microsoft.Extensions.Logging;
using SaveState.Core.Common.Services;

/// <summary>
/// Routes training inputs to the appropriate engine based on session type.
/// </summary>
public class InputRouter
{
    private readonly ILogger<InputRouter> _logger;
    private readonly ReflexTrainer _reflexTrainer;
    private readonly TrainingPatternEngine _patternEngine;
    private readonly ComboLabEngine _comboLab;
    private readonly ITimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="InputRouter"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="reflexTrainer">The reflex trainer engine.</param>
    /// <param name="patternEngine">The pattern engine.</param>
    /// <param name="comboLab">The combo lab engine.</param>
    /// <param name="timeProvider">The time provider.</param>
    public InputRouter(
        ILogger<InputRouter> logger,
        ReflexTrainer reflexTrainer,
        TrainingPatternEngine patternEngine,
        ComboLabEngine comboLab,
        ITimeProvider timeProvider)
    {
        _logger = logger;
        _reflexTrainer = reflexTrainer;
        _patternEngine = patternEngine;
        _comboLab = comboLab;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Processes training input for a session.
    /// </summary>
    /// <param name="session">The training session.</param>
    /// <param name="input">The input data.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The training response.</returns>
    public async Task<TrainingTypes.TrainingResponse> ProcessInputAsync(
        TrainingSession session,
        TrainingInput input,
        CancellationToken ct = default)
    {
        _logger.LogDebug("Processing input for session {SessionId}, type {TrainingType}",
            session.SessionId, session.TrainingType);

        session.LastActivity = _timeProvider.UtcNow;

        return session.TrainingType switch
        {
            TrainingType.Reflex => await ProcessReflexInputAsync(session, input, ct),
            TrainingType.PatternRecognition => await ProcessPatternInputAsync(session, input, ct),
            TrainingType.ComboLab => await ProcessComboInputAsync(session, input, ct),
            _ => CreateErrorResponse(session.SessionId, "Unknown training type")
        };
    }

    private async Task<TrainingTypes.TrainingResponse> ProcessReflexInputAsync(
        TrainingSession session,
        TrainingInput input,
        CancellationToken ct)
    {
        var startTime = input.Timestamp;
        var responseTime = _timeProvider.UtcNow - startTime;
        var isCorrect = ValidateReflexInput(session, input);

        if (session.ReflexData != null)
        {
            session.ReflexData.StimuliPresented++;
            UpdateReactionTimeStats(session.ReflexData, responseTime, isCorrect);
        }

        await _reflexTrainer.GenerateNextStimulusAsync(session, ct);

        return new TrainingTypes.TrainingResponse(
            session.SessionId,
            isCorrect,
            responseTime,
            isCorrect ? "Good reaction!" : "Missed!",
            CreateProgressUpdate(session));
    }

    private async Task<TrainingTypes.TrainingResponse> ProcessPatternInputAsync(
        TrainingSession session,
        TrainingInput input,
        CancellationToken ct)
    {
        if (session.PatternData == null)
        {
            return CreateErrorResponse(session.SessionId, "Pattern data not initialized");
        }

        var inputValue = input.InputData?.ToString() ?? string.Empty;
        var currentSequence = session.PatternData.CurrentSequence.ToList();
        var playerSequence = session.PatternData.PlayerSequence.ToList();

        playerSequence.Add(inputValue);
        session.PatternData.PlayerSequence = playerSequence;

        var isCorrect = playerSequence.Count <= currentSequence.Count &&
                       currentSequence[playerSequence.Count - 1] == inputValue;

        if (!isCorrect)
        {
            session.Progress = session.Progress with
            {
                IncorrectResponses = session.Progress.IncorrectResponses + 1
            };
        }
        else
        {
            session.Progress = session.Progress with
            {
                CorrectResponses = session.Progress.CorrectResponses + 1
            };
        }

        if (playerSequence.Count >= currentSequence.Count)
        {
            if (isCorrect)
            {
                session.PatternData.SequencesCompleted++;
            }

            await _patternEngine.GenerateNextPatternAsync(session, ct);
            session.PatternData.PlayerSequence = Array.Empty<string>();
        }

        UpdateAccuracyRate(session.PatternData);

        return new TrainingTypes.TrainingResponse(
            session.SessionId,
            isCorrect,
            TimeSpan.Zero,
            isCorrect ? "Correct!" : "Wrong pattern!",
            CreateProgressUpdate(session));
    }

    private Task<TrainingTypes.TrainingResponse> ProcessComboInputAsync(
        TrainingSession session,
        TrainingInput input,
        CancellationToken ct)
    {
        if (session.ComboData == null)
        {
            return Task.FromResult(CreateErrorResponse(session.SessionId, "Combo data not initialized"));
        }

        var inputValue = input.InputData?.ToString() ?? string.Empty;
        var targetCombo = session.ComboData.TargetCombo.ToList();
        var currentInput = session.ComboData.CurrentInput.ToList();

        currentInput.Add(inputValue);
        session.ComboData.CurrentInput = currentInput;

        var isCorrect = currentInput.Count <= targetCombo.Count &&
                       targetCombo[currentInput.Count - 1] == inputValue;

        if (!isCorrect)
        {
            session.ComboData.Mistakes++;
            session.ComboData.Attempts++;
            session.ComboData.CurrentInput = Array.Empty<string>();
            session.Progress = session.Progress with
            {
                IncorrectResponses = session.Progress.IncorrectResponses + 1
            };

            return Task.FromResult(new TrainingTypes.TrainingResponse(
                session.SessionId,
                false,
                TimeSpan.Zero,
                "Combo dropped! Try again.",
                CreateProgressUpdate(session)));
        }

        session.ComboData.ComboProgress = currentInput.Count;
        session.Progress = session.Progress with
        {
            CorrectResponses = session.Progress.CorrectResponses + 1
        };

        var comboComplete = currentInput.Count >= targetCombo.Count;
        var feedback = comboComplete ? "Combo complete!" : "Keep going!";

        if (comboComplete)
        {
            session.ComboData.Attempts++;
            session.ComboData.CurrentInput = Array.Empty<string>();
        }

        return Task.FromResult(new TrainingTypes.TrainingResponse(
            session.SessionId,
            true,
            TimeSpan.Zero,
            feedback,
            CreateProgressUpdate(session)));
    }

    private static bool ValidateReflexInput(TrainingSession session, TrainingInput input)
    {
        return input.InputData?.ToString() is "correct" or "true" or "1";
    }

    private static void UpdateReactionTimeStats(ReflexTrainingData data, TimeSpan reactionTime, bool isCorrect)
    {
        if (reactionTime < data.FastestReaction)
        {
            data.FastestReaction = reactionTime;
        }

        if (reactionTime > data.SlowestReaction)
        {
            data.SlowestReaction = reactionTime;
        }

        var totalResponses = data.StimuliPresented;
        var totalTime = data.AverageReactionTime.TotalMilliseconds * (totalResponses - 1) + reactionTime.TotalMilliseconds;
        data.AverageReactionTime = TimeSpan.FromMilliseconds(totalTime / totalResponses);

        var correctResponses = (int)(data.ResponseAccuracy * (totalResponses - 1) / 100) + (isCorrect ? 1 : 0);
        data.ResponseAccuracy = (double)correctResponses / totalResponses * 100;
    }

    private static void UpdateAccuracyRate(PatternRecognitionData data)
    {
        var totalAttempts = data.SequencesCompleted + (data.PlayerSequence.Count > 0 ? 1 : 0);
        if (totalAttempts > 0)
        {
            data.AccuracyRate = (double)data.SequencesCompleted / totalAttempts * 100;
        }
    }

    private static TrainingTypes.ProgressUpdate CreateProgressUpdate(TrainingSession session)
    {
        return new TrainingTypes.ProgressUpdate(
            session.Progress.CurrentRound,
            CalculateAccuracy(session),
            CalculateAverageTime(session),
            session.PatternData?.SequencesCompleted);
    }

    private static double CalculateAccuracy(TrainingSession session)
    {
        var total = session.Progress.CorrectResponses + session.Progress.IncorrectResponses;
        return total > 0 ? (double)session.Progress.CorrectResponses / total * 100 : 0;
    }

    private static TimeSpan? CalculateAverageTime(TrainingSession session)
    {
        return session.ReflexData?.AverageReactionTime;
    }

    private static TrainingTypes.TrainingResponse CreateErrorResponse(string sessionId, string message)
    {
        return new TrainingTypes.TrainingResponse(
            sessionId,
            false,
            TimeSpan.Zero,
            message,
            new TrainingTypes.ProgressUpdate(0, 0, null, 0));
    }
}
