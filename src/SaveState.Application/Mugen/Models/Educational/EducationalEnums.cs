namespace SaveState.Application.Mugen.Models.Educational;

/// <summary>
/// Tutorial sorting options.
/// </summary>
public enum TutorialSort { Title, Popularity, Recent, Rating, Difficulty }

/// <summary>
/// Tutorial session status.
/// </summary>
public enum TutorialStatus { NotStarted, InProgress, Paused, Completed, Failed }

/// <summary>
/// Educational action types.
/// </summary>
public enum EducationalActionType { ButtonPress, Movement, Combo, Timing, Other }

/// <summary>
/// Game mode types.
/// </summary>
public enum GameMode { Versus, Tournament, Training, Story, Survival }

/// <summary>
/// Skill level classifications.
/// </summary>
public enum SkillLevel { Beginner, Intermediate, Advanced, Expert }

/// <summary>
/// Practice session status.
/// </summary>
public enum PracticeSessionStatus { InProgress, Completed, Abandoned }

/// <summary>
/// Content difficulty levels.
/// </summary>
public enum DifficultyLevel { Beginner, Intermediate, Easy, Medium, Hard, Advanced, Expert, Master }
