namespace SaveState.Application.Mugen.Services.Training.Engines;

using Microsoft.Extensions.Logging;
using SaveState.Core.Mugen.ValueObjects;

/// <summary>
/// Generates patterns for pattern recognition training.
/// </summary>
public class TrainingPatternEngine
{
    private readonly ILogger<TrainingPatternEngine> _logger;
    private readonly Random _random = new();

    private static readonly string[] DirectionalInputs = { "up", "down", "left", "right" };
    private static readonly string[] ButtonInputs = { "lp", "mp", "hp", "lk", "mk", "hk" };
    private static readonly string[] QuarterCircleInputs = { "qcf", "qcb", "dp", "rdp" };

    /// <summary>
    /// Initializes a new instance of the <see cref="TrainingPatternEngine"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public TrainingPatternEngine(ILogger<TrainingPatternEngine> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Generates the next pattern for the session.
    /// </summary>
    /// <param name="session">The training session.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task GenerateNextPatternAsync(TrainingSession session, CancellationToken ct = default)
    {
        if (session.PatternData == null)
        {
            _logger.LogWarning("Pattern data not initialized for session {SessionId}", session.SessionId);
            return Task.CompletedTask;
        }

        var sequenceLength = session.PatternData.SequenceLength;
        var patternType = session.PatternData.SequenceType;
        var difficulty = session.Difficulty;

        var sequence = GenerateSequence(patternType, sequenceLength, difficulty);
        session.PatternData.CurrentSequence = sequence;

        _logger.LogDebug("Generated {Type} pattern for session {SessionId}: {Sequence}",
            patternType, session.SessionId, string.Join(", ", sequence));

        UpdateProgress(session);

        return Task.CompletedTask;
    }

    private string[] GenerateSequence(PatternType type, int length, DifficultyLevel difficulty)
    {
        return type switch
        {
            PatternType.InputSequence => GenerateInputSequence(length, difficulty),
            PatternType.MoveSequence => GenerateMoveSequence(length, difficulty),
            PatternType.TimingSequence => GenerateTimingSequence(length, difficulty),
            PatternType.Mixed => GenerateMixedSequence(length, difficulty),
            _ => GenerateInputSequence(length, difficulty)
        };
    }

    private string[] GenerateInputSequence(int length, DifficultyLevel difficulty)
    {
        var sequence = new List<string>();
        var availableInputs = GetInputsForDifficulty(difficulty);

        for (var i = 0; i < length; i++)
        {
            sequence.Add(availableInputs[_random.Next(availableInputs.Length)]);
        }

        return sequence.ToArray();
    }

    private string[] GenerateMoveSequence(int length, DifficultyLevel difficulty)
    {
        var sequence = new List<string>();
        var moves = GetMovesForDifficulty(difficulty);

        for (var i = 0; i < length; i++)
        {
            sequence.Add(moves[_random.Next(moves.Length)]);
        }

        return sequence.ToArray();
    }

    private string[] GenerateTimingSequence(int length, DifficultyLevel difficulty)
    {
        var sequence = new List<string>();
        var timingIntervals = GetTimingIntervalsForDifficulty(difficulty);

        for (var i = 0; i < length; i++)
        {
            var interval = timingIntervals[_random.Next(timingIntervals.Length)];
            sequence.Add($"timing_{interval}");
        }

        return sequence.ToArray();
    }

    private string[] GenerateMixedSequence(int length, DifficultyLevel difficulty)
    {
        var sequence = new List<string>();
        var allTypes = new[] { PatternType.InputSequence, PatternType.MoveSequence, PatternType.TimingSequence };

        for (var i = 0; i < length; i++)
        {
            var type = allTypes[_random.Next(allTypes.Length)];
            var element = type switch
            {
                PatternType.InputSequence => GetInputsForDifficulty(difficulty)[_random.Next(GetInputsForDifficulty(difficulty).Length)],
                PatternType.MoveSequence => GetMovesForDifficulty(difficulty)[_random.Next(GetMovesForDifficulty(difficulty).Length)],
                PatternType.TimingSequence => $"timing_{GetTimingIntervalsForDifficulty(difficulty)[_random.Next(GetTimingIntervalsForDifficulty(difficulty).Length)]}",
                _ => "neutral"
            };
            sequence.Add(element);
        }

        return sequence.ToArray();
    }

    private string[] GetInputsForDifficulty(DifficultyLevel difficulty)
    {
        return difficulty switch
        {
            DifficultyLevel.VeryEasy => DirectionalInputs,
            DifficultyLevel.Easy => DirectionalInputs.Concat(ButtonInputs.Take(2)).ToArray(),
            DifficultyLevel.Medium => DirectionalInputs.Concat(ButtonInputs).ToArray(),
            DifficultyLevel.Hard => DirectionalInputs.Concat(ButtonInputs).Concat(QuarterCircleInputs.Take(2)).ToArray(),
            DifficultyLevel.VeryHard or DifficultyLevel.Expert => DirectionalInputs.Concat(ButtonInputs).Concat(QuarterCircleInputs).ToArray(),
            _ => DirectionalInputs
        };
    }

    private string[] GetMovesForDifficulty(DifficultyLevel difficulty)
    {
        var basicMoves = new[] { "walk_forward", "walk_backward", "crouch", "jump" };
        var intermediateMoves = new[] { "dash_forward", "dash_backward", "super_jump", "roll" };
        var advancedMoves = new[] { " wavedash", "parry", "just_defend", "instant_air_dash" };

        return difficulty switch
        {
            DifficultyLevel.VeryEasy => basicMoves.Take(2).ToArray(),
            DifficultyLevel.Easy => basicMoves,
            DifficultyLevel.Medium => basicMoves.Concat(intermediateMoves.Take(2)).ToArray(),
            DifficultyLevel.Hard => basicMoves.Concat(intermediateMoves).ToArray(),
            DifficultyLevel.VeryHard or DifficultyLevel.Expert => basicMoves.Concat(intermediateMoves).Concat(advancedMoves).ToArray(),
            _ => basicMoves
        };
    }

    private int[] GetTimingIntervalsForDifficulty(DifficultyLevel difficulty)
    {
        return difficulty switch
        {
            DifficultyLevel.VeryEasy => new[] { 30, 60 },
            DifficultyLevel.Easy => new[] { 20, 30, 60 },
            DifficultyLevel.Medium => new[] { 10, 20, 30, 45 },
            DifficultyLevel.Hard => new[] { 5, 10, 15, 20, 30 },
            DifficultyLevel.VeryHard or DifficultyLevel.Expert => new[] { 1, 3, 5, 10, 15, 20 },
            _ => new[] { 30, 60 }
        };
    }

    private static void UpdateProgress(TrainingSession session)
    {
        var completed = session.PatternData?.SequencesCompleted ?? 0;
        session.Progress = session.Progress with
        {
            CurrentRound = completed % 10,
            TotalRounds = 10
        };
    }
}
