using SaveState.Core.Mugen.ValueObjects;

namespace SaveState.Application.Mugen.Models.Educational;

/// <summary>
/// Tutorial data model.
/// </summary>
public class Tutorial
{
    public string TutorialId { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;
    public string Category { get; set; } = default!;
    public DifficultyLevel Difficulty { get; set; } = default!;
    public TimeSpan EstimatedDuration { get; set; } = default!;
    public IReadOnlyList<string> Tags { get; set; } = default!;
    public IReadOnlyList<string> Prerequisites { get; set; } = default!;
    public IReadOnlyList<TutorialStep> Steps { get; set; } = default!;
    public string AuthorId { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = default!;
    public DateTime UpdatedAt { get; set; } = default!;
    public int ViewCount { get; set; } = default!;
    public int CompletionCount { get; set; } = default!;
    public double AverageRating { get; set; } = default!;
    public int TotalRatings { get; set; } = default!;
}

/// <summary>
/// Tutorial query parameters.
/// </summary>
public class TutorialQuery
{
    public DifficultyLevel? Difficulty { get; set; } = default!;
    public string? Category { get; set; } = default!;
    public IReadOnlyList<string> Tags { get; set; } = default!;
    public TutorialSort SortBy { get; set; } = default!;
    public int Offset { get; set; } = default!;
    public int Limit { get; set; } = default!;
}

/// <summary>
/// Tutorial session data.
/// </summary>
public class TutorialSession
{
    public string SessionId { get; set; } = default!;
    public string UserId { get; set; } = default!;
    public string TutorialId { get; set; } = default!;
    public int CurrentStep { get; set; } = default!;
    public int TotalSteps { get; set; } = default!;
    public TutorialStatus Status { get; set; } = default!;
    public DateTime StartedAt { get; set; } = default!;
    public DateTime? CompletedAt { get; set; } = default!;
    public TutorialProgress Progress { get; set; } = default!;
    public List<UserAction> UserActions { get; set; } = default!;
}

/// <summary>
/// Tutorial step data.
/// </summary>
public class TutorialStep
{
    public int StepNumber { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string Instruction { get; set; } = default!;
    public string ExpectedAction { get; set; } = default!;
    public IReadOnlyList<string> Hints { get; set; } = default!;
    public string SuccessCriteria { get; set; } = default!;
    public IReadOnlyList<string>? VisualAids { get; set; } = default!;
    public TimeSpan? TimeLimit { get; set; } = default!;
}

/// <summary>
/// Tutorial action data.
/// </summary>
public class TutorialAction
{
    public string ActionId { get; set; } = default!;
    public EducationalActionType ActionType { get; set; } = default!;
    public object ActionData { get; set; } = default!;
    public bool RequestHint { get; set; } = default!;
    public DateTime Timestamp { get; set; } = default!;
}

/// <summary>
/// Tutorial response data.
/// </summary>
public class TutorialResponse
{
    public string SessionId { get; set; } = default!;
    public bool IsCorrect { get; set; } = default!;
    public string Feedback { get; set; } = default!;
    public string? Hint { get; set; } = default!;
    public ProgressUpdate ProgressUpdate { get; set; } = default!;
}

/// <summary>
/// Tutorial progress update.
/// </summary>
public class ProgressUpdate
{
    public int CurrentStep { get; set; } = default!;
    public int TotalSteps { get; set; } = default!;
    public double CompletionPercentage { get; set; } = default!;
}

/// <summary>
/// Tutorial progress data.
/// </summary>
public class TutorialProgress
{
    public int StepsCompleted { get; set; } = default!;
    public int CorrectActions { get; set; } = default!;
    public int IncorrectActions { get; set; } = default!;
    public TimeSpan TimeSpent { get; set; } = default!;
    public int HintsUsed { get; set; } = default!;
}

/// <summary>
/// User action record.
/// </summary>
public class UserAction
{
    public int StepIndex { get; set; } = default!;
    public TutorialAction Action { get; set; } = default!;
    public bool IsCorrect { get; set; } = default!;
    public DateTime Timestamp { get; set; } = default!;
}

/// <summary>
/// Tutorial creation request.
/// </summary>
public class TutorialCreationRequest
{
    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;
    public string Category { get; set; } = default!;
    public DifficultyLevel Difficulty { get; set; } = default!;
    public TimeSpan EstimatedDuration { get; set; } = default!;
    public IReadOnlyList<string> Tags { get; set; } = default!;
    public IReadOnlyList<string> Prerequisites { get; set; } = default!;
    public IReadOnlyList<TutorialStep> Steps { get; set; } = default!;
    public string AuthorId { get; set; } = default!;
}
