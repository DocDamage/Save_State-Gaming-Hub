using SaveState.Core.Mugen.ValueObjects;

namespace SaveState.Application.Mugen.Models.Educational;

/// <summary>
/// Learning path data model.
/// </summary>
public class LearningPath
{
    public string PathId { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;
    public DifficultyLevel Difficulty { get; set; } = default!;
    public TimeSpan EstimatedDuration { get; set; } = default!;
    public IReadOnlyList<LearningModule> Modules { get; set; } = default!;
    public IReadOnlyList<string> Prerequisites { get; set; } = default!;
    public IReadOnlyList<string> SkillsCovered { get; set; } = default!;
    public string AuthorId { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = default!;
    public DateTime UpdatedAt { get; set; } = default!;
    public int EnrollmentCount { get; set; } = default!;
    public double CompletionRate { get; set; } = default!;
}

/// <summary>
/// Learning module data.
/// </summary>
public class LearningModule
{
    public string ModuleId { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;
    public int Order { get; set; } = default!;
    public IReadOnlyList<string> ContentItems { get; set; } = default!;
    public TimeSpan EstimatedDuration { get; set; } = default!;
    public IReadOnlyList<string> SkillsTaught { get; set; } = default!;
}

/// <summary>
/// Learning progress data.
/// </summary>
public class LearningProgress
{
    public string UserId { get; set; } = default!;
    public int TutorialsCompleted { get; set; } = default!;
    public int TutorialsInProgress { get; set; } = default!;
    public TimeSpan TotalTimeSpent { get; set; } = default!;
    public IReadOnlyList<string> SkillsMastered { get; set; } = default!;
    public int CurrentStreak { get; set; } = default!;
    public int LongestStreak { get; set; } = default!;
    public double AverageScore { get; set; } = default!;
    public IReadOnlyList<string> WeakAreas { get; set; } = default!;
    public string RecommendedNext { get; set; } = default!;
}

/// <summary>
/// Practice request data.
/// </summary>
public class PracticeRequest
{
    public string UserId { get; set; } = default!;
    public string Topic { get; set; } = default!;
    public DifficultyLevel Difficulty { get; set; } = default!;
}

/// <summary>
/// Practice session data.
/// </summary>
public class PracticeSession
{
    public string SessionId { get; set; } = default!;
    public string UserId { get; set; } = default!;
    public string Topic { get; set; } = default!;
    public DifficultyLevel Difficulty { get; set; } = default!;
    public DateTime StartedAt { get; set; } = default!;
    public PracticeSessionStatus Status { get; set; } = default!;
    public IReadOnlyList<PracticeExercise> Exercises { get; set; } = default!;
}

/// <summary>
/// Practice exercise data.
/// </summary>
public class PracticeExercise
{
    public string ExerciseId { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string Goal { get; set; } = default!;
    public bool Completed { get; set; } = default!;
}
