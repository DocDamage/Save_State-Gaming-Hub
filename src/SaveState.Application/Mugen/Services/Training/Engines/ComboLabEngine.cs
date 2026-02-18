namespace SaveState.Application.Mugen.Services.Training.Engines;

using Microsoft.Extensions.Logging;
using SaveState.Core.Mugen.ValueObjects;

/// <summary>
/// Manages combo lab training exercises.
/// </summary>
public class ComboLabEngine
{
    private readonly ILogger<ComboLabEngine> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ComboLabEngine"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public ComboLabEngine(ILogger<ComboLabEngine> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Generates a combo sequence based on lab type and difficulty.
    /// </summary>
    /// <param name="labType">The type of combo lab.</param>
    /// <param name="difficulty">The difficulty level.</param>
    /// <returns>A list of inputs for the combo.</returns>
    public IReadOnlyList<string> GenerateComboSequence(ComboLabType labType, DifficultyLevel difficulty)
    {
        return labType switch
        {
            ComboLabType.BasicCombos => GenerateBasicCombo(difficulty),
            ComboLabType.AdvancedCombos => GenerateAdvancedCombo(difficulty),
            ComboLabType.ChallengeCombos => GenerateChallengeCombo(difficulty),
            ComboLabType.CustomCombos => GenerateBasicCombo(difficulty),
            _ => GenerateBasicCombo(difficulty)
        };
    }

    /// <summary>
    /// Validates a combo attempt against the target combo.
    /// </summary>
    /// <param name="targetCombo">The target combo sequence.</param>
    /// <param name="attempt">The player's attempt.</param>
    /// <returns>True if the combo is completed correctly.</returns>
    public bool ValidateCombo(IReadOnlyList<string> targetCombo, IReadOnlyList<string> attempt)
    {
        if (attempt.Count < targetCombo.Count)
        {
            return false;
        }

        for (var i = 0; i < targetCombo.Count; i++)
        {
            if (!string.Equals(targetCombo[i], attempt[i], StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Calculates the difficulty rating for a combo.
    /// </summary>
    /// <param name="combo">The combo sequence.</param>
    /// <returns>A difficulty score from 1 to 10.</returns>
    public int CalculateComboDifficulty(IReadOnlyList<string> combo)
    {
        var difficulty = 1;

        foreach (var input in combo)
        {
            var inputLower = input.ToLowerInvariant();

            if (inputLower.Contains("qcf") || inputLower.Contains("qcb"))
            {
                difficulty += 2;
            }
            else if (inputLower.Contains("dp") || inputLower.Contains("rdp"))
            {
                difficulty += 3;
            }
            else if (inputLower.Contains("360") || inputLower.Contains("hcf") || inputLower.Contains("hcb"))
            {
                difficulty += 4;
            }
            else if (inputLower.Contains("lp") || inputLower.Contains("lk"))
            {
                difficulty += 1;
            }
            else if (inputLower.Contains("hp") || inputLower.Contains("hk"))
            {
                difficulty += 1;
            }
            else if (inputLower.Contains("cancel") || inputLower.Contains("link"))
            {
                difficulty += 2;
            }
        }

        difficulty += combo.Count / 3;

        return Math.Min(10, Math.Max(1, difficulty));
    }

    /// <summary>
    /// Provides feedback on a combo attempt.
    /// </summary>
    /// <param name="targetCombo">The target combo.</param>
    /// <param name="attempt">The player's attempt.</param>
    /// <param name="completionTime">The time taken to complete.</param>
    /// <returns>Feedback message.</returns>
    public string GetComboFeedback(IReadOnlyList<string> targetCombo, IReadOnlyList<string> attempt, TimeSpan completionTime)
    {
        if (attempt.Count == 0)
        {
            return "Start the combo!";
        }

        for (var i = 0; i < Math.Min(attempt.Count, targetCombo.Count); i++)
        {
            if (!string.Equals(attempt[i], targetCombo[i], StringComparison.OrdinalIgnoreCase))
            {
                return $"Wrong input at position {i + 1}. Expected: {targetCombo[i]}";
            }
        }

        if (attempt.Count < targetCombo.Count)
        {
            return $"Good so far! Next: {targetCombo[attempt.Count]}";
        }

        var speedRating = completionTime.TotalSeconds switch
        {
            < 2 => "Lightning fast!",
            < 4 => "Great speed!",
            < 6 => "Good timing.",
            _ => "Combo complete! Try to speed it up."
        };

        return $"Combo complete! {speedRating}";
    }

    private static IReadOnlyList<string> GenerateBasicCombo(DifficultyLevel difficulty)
    {
        return difficulty switch
        {
            DifficultyLevel.VeryEasy => new[] { "lp", "lp", "lk" },
            DifficultyLevel.Easy => new[] { "lp", "lk", "mp", "hp" },
            DifficultyLevel.Medium => new[] { "crouch", "lk", "lp", "qcf", "lp" },
            DifficultyLevel.Hard => new[] { "jump", "hk", "crouch", "lk", "lp", "qcf", "hp" },
            DifficultyLevel.VeryHard or DifficultyLevel.Expert => new[] { "jump", "hk", "crouch", "lk", "lp", "qcf", "lp", "qcf", "hp" },
            _ => new[] { "lp", "lp", "lk" }
        };
    }

    private static IReadOnlyList<string> GenerateAdvancedCombo(DifficultyLevel difficulty)
    {
        return difficulty switch
        {
            DifficultyLevel.VeryEasy => new[] { "qcf", "lp", "qcf", "lp" },
            DifficultyLevel.Easy => new[] { "dp", "lp", "cancel", "qcf", "hp" },
            DifficultyLevel.Medium => new[] { "jump", "hk", "crouch", "lk", "link", "crouch", "lp", "qcf", "hp", "cancel", "qcf", "hp" },
            DifficultyLevel.Hard => new[] { "crouch", "lk", "crouch", "lp", "cancel", "qcf", "lk", "cancel", "dp", "hp" },
            DifficultyLevel.VeryHard or DifficultyLevel.Expert => new[] { "jump", "hk", "crouch", "lk", "crouch", "lp", "cancel", "qcf", "lk", "cancel", "qcf", "hp", "fadc", "ultra" },
            _ => new[] { "qcf", "lp", "qcf", "lp" }
        };
    }

    private static IReadOnlyList<string> GenerateChallengeCombo(DifficultyLevel difficulty)
    {
        return difficulty switch
        {
            DifficultyLevel.VeryEasy => new[] { "360", "lp", "360", "lp" },
            DifficultyLevel.Easy => new[] { "jump", "hk", "360", "hp" },
            DifficultyLevel.Medium => new[] { "walk_forward", "360", "lk", "crouch", "lp", "qcf", "hp" },
            DifficultyLevel.Hard => new[] { "hcb", "f", "hp", "cancel", "qcf", "qcf", "hp" },
            DifficultyLevel.VeryHard or DifficultyLevel.Expert => new[] { "jump", "hk", "crouch", "lk", "crouch", "lp", "cancel", "dp", "lp", "fadc", "jump", "hk", "crouch", "lk", "ultra" },
            _ => new[] { "360", "lp", "360", "lp" }
        };
    }
}
