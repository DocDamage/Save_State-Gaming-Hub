namespace SaveState.Core.GameLibrary.Models.AiCoach;

/// <summary>
/// A coaching tip provided to the player.
/// </summary>
public sealed record CoachingTip(
    string Title,
    string Description,
    TipCategory Category,
    TipDifficulty Difficulty,
    IReadOnlyList<string> Prerequisites);

/// <summary>
/// A contextual hint for the current game situation.
/// </summary>
public sealed record Hint(
    Guid Id,
    string Content,
    HintType Type,
    double RelevanceScore,
    IReadOnlyList<string> Tags,
    DateTime GeneratedAt);

/// <summary>
/// A step-by-step walkthrough hint for complex situations.
/// </summary>
public sealed record WalkthroughHint(
    Guid Id,
    string Title,
    IReadOnlyList<WalkthroughStep> Steps,
    string Summary,
    Difficulty Difficulty);

/// <summary>
/// A single step in a walkthrough.
/// </summary>
public sealed record WalkthroughStep(
    int StepNumber,
    string Instruction,
    string? ScreenshotHint,
    IReadOnlyList<string> KeyPoints);

/// <summary>
/// Type of hint provided.
/// </summary>
public enum HintType
{
    General,
    Contextual,
    Proactive,
    Reactive,
    Tutorial
}
