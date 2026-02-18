namespace SaveState.Application.Mugen.Services.Training.Engines;

using Microsoft.Extensions.Logging;
using SaveState.Core.Common.Services;

/// <summary>
/// Handles reflex training stimulus generation and tracking.
/// </summary>
public class ReflexTrainer
{
    private readonly ILogger<ReflexTrainer> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly Random _random = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ReflexTrainer"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="timeProvider">The time provider.</param>
    public ReflexTrainer(ILogger<ReflexTrainer> logger, ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Calculates the total number of rounds for a reflex training session.
    /// </summary>
    /// <param name="duration">The session duration.</param>
    /// <returns>The total number of rounds.</returns>
    public int CalculateTotalRounds(TimeSpan duration)
    {
        var roundsPerMinute = 20;
        var totalMinutes = duration.TotalMinutes;
        var totalRounds = (int)(totalMinutes * roundsPerMinute);
        return Math.Max(5, totalRounds);
    }

    /// <summary>
    /// Generates the next stimulus for reflex training.
    /// </summary>
    /// <param name="session">The training session.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task GenerateNextStimulusAsync(TrainingSession session, CancellationToken ct = default)
    {
        if (session.ReflexData == null)
        {
            _logger.LogWarning("Reflex data not initialized for session {SessionId}", session.SessionId);
            return Task.CompletedTask;
        }

        var stimulus = GenerateStimulusForMode(session.ReflexData.TrainingMode);
        _logger.LogDebug("Generated {Mode} stimulus for session {SessionId}: {Stimulus}",
            session.ReflexData.TrainingMode, session.SessionId, stimulus);

        session.ReflexData.StimuliPresented++;
        UpdateProgress(session);

        return Task.CompletedTask;
    }

    private string GenerateStimulusForMode(ReflexTrainingMode mode)
    {
        return mode switch
        {
            ReflexTrainingMode.VisualStimuli => GenerateVisualStimulus(),
            ReflexTrainingMode.AudioStimuli => GenerateAudioStimulus(),
            ReflexTrainingMode.MixedStimuli => _random.Next(2) == 0 ? GenerateVisualStimulus() : GenerateAudioStimulus(),
            ReflexTrainingMode.Predictive => GeneratePredictiveStimulus(),
            _ => GenerateVisualStimulus()
        };
    }

    private string GenerateVisualStimulus()
    {
        var visualStimuli = new[]
        {
            "circle_red", "circle_blue", "circle_green",
            "square_red", "square_blue", "square_green",
            "triangle_up", "triangle_down", "triangle_left", "triangle_right",
            "arrow_up", "arrow_down", "arrow_left", "arrow_right",
            "flash_center", "flash_left", "flash_right"
        };
        return visualStimuli[_random.Next(visualStimuli.Length)];
    }

    private string GenerateAudioStimulus()
    {
        var audioStimuli = new[]
        {
            "beep_high", "beep_low", "beep_double",
            "tone_1000hz", "tone_500hz", "tone_200hz",
            "voice_left", "voice_right", "voice_center",
            "click_fast", "click_slow"
        };
        return audioStimuli[_random.Next(audioStimuli.Length)];
    }

    private string GeneratePredictiveStimulus()
    {
        var predictiveStimuli = new[]
        {
            "incoming_high", "incoming_low", "incoming_mid",
            "dodge_left", "dodge_right", "dodge_jump",
            "counter_opportunity", "guard_break_warning"
        };
        return predictiveStimuli[_random.Next(predictiveStimuli.Length)];
    }

    private void UpdateProgress(TrainingSession session)
    {
        if (session.Duration.HasValue)
        {
            var elapsed = _timeProvider.UtcNow - session.StartedAt;
            var totalRounds = (int)(session.Duration.Value.TotalMinutes * 20);
            var currentRound = Math.Min(session.ReflexData?.StimuliPresented ?? 0, totalRounds);

            session.Progress = session.Progress with
            {
                CurrentRound = currentRound,
                TotalRounds = totalRounds
            };
        }
    }
}
