namespace SaveState.Application.Mugen.Models.Educational;

/// <summary>
/// User progress data for educational content.
/// </summary>
public class UserProgress
{
    public string UserId { get; set; } = default!;
    public int TutorialsCompleted { get; set; }
    public int TutorialsInProgress { get; set; }
    public TimeSpan TotalTimeSpent { get; set; }
    public IReadOnlyList<string> SkillsMastered { get; set; } = default!;
    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }
    public double AverageScore { get; set; }
    public IReadOnlyList<string> WeakAreas { get; set; } = default!;
    public string RecommendedNext { get; set; } = default!;
    public IReadOnlyList<CategoryProgress> CategoryProgress { get; set; } = default!;
}

/// <summary>
/// Progress data for a specific category.
/// </summary>
public class CategoryProgress
{
    public string Category { get; set; } = default!;
    public int TotalItems { get; set; }
    public int CompletedItems { get; set; }
    public double CompletionPercentage { get; set; }
    public TimeSpan TimeSpent { get; set; }
    public double AverageScore { get; set; }
    public DateTime LastAccessed { get; set; }
}
