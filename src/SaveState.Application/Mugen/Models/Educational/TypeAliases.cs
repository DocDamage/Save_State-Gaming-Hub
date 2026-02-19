// Type aliases for backward compatibility
// These allow existing code to continue using the prefixed names while new code uses cleaner names

namespace SaveState.Application.Mugen.Services.Educational;

using SaveState.Application.Mugen.Models.Educational;
using SaveState.Application.Mugen.Services.Educational.Engines;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;

// Model type aliases
public class EducationalContentServiceTutorial : Tutorial { }
public class EducationalContentServiceTutorialQuery : TutorialQuery { }
public class EducationalContentServiceTutorialSession : TutorialSession { }
public class EducationalContentServiceTutorialStep : TutorialStep { }
public class EducationalContentServiceTutorialAction : TutorialAction { }
public class EducationalContentServiceTutorialResponse : TutorialResponse { }
public class EducationalContentServiceProgressUpdate : ProgressUpdate { }
public class EducationalContentServiceTutorialProgress : TutorialProgress { }
public class EducationalContentServiceUserAction : UserAction { }
public class EducationalContentServiceTutorialCreationRequest : TutorialCreationRequest { }
public class EducationalContentServiceStrategyGuide : StrategyGuide { }
public class EducationalContentServiceStrategyGuideQuery : StrategyGuideQuery { }
public class EducationalContentServiceGuideSection : GuideSection { }
public class EducationalContentServiceMechanicsGuide : MechanicsGuide { }
public class EducationalContentServiceGuideContent : GuideContent { }
public class EducationalContentServiceLearningPath : LearningPath { }
public class EducationalContentServiceLearningModule : LearningModule { }
public class EducationalContentServiceLearningProgress : LearningProgress { }
public class EducationalContentServicePracticeRequest : PracticeRequest { }
public class EducationalContentServicePracticeSession : PracticeSession { }
public class EducationalContentServicePracticeExercise : PracticeExercise { }
public class EducationalContentServiceContentAnalytics : ContentAnalytics { }
public class EducationalContentServiceUserEngagement : UserEngagement { }
public class EducationalContentServiceContentQuality : ContentQuality { }
public class EducationalContentServiceUserDashboard : UserDashboard { }
public class EducationalContentServiceRecommendedContent : RecommendedContent { }

// Engine type aliases
public class EducationalContentServiceContentEngine : ContentEngine
{
    public EducationalContentServiceContentEngine(
        Microsoft.Extensions.Logging.ILogger<ContentEngine> logger,
        ITimeProvider timeProvider,
        Dictionary<string, Tutorial> tutorials,
        Dictionary<string, StrategyGuide> strategyGuides,
        Dictionary<string, MechanicsGuide> mechanicsGuides)
        : base(logger, timeProvider, tutorials, strategyGuides, mechanicsGuides) { }
}

public class EducationalContentServiceLearningPathEngine : LearningPathEngine
{
    public EducationalContentServiceLearningPathEngine(
        Microsoft.Extensions.Logging.ILogger<LearningPathEngine> logger,
        ITimeProvider timeProvider,
        Dictionary<string, LearningPath> learningPaths)
        : base(logger, timeProvider, learningPaths) { }
}

public class EducationalContentServiceProgressEngine : ProgressEngine
{
    public EducationalContentServiceProgressEngine(
        Microsoft.Extensions.Logging.ILogger<ProgressEngine> logger)
        : base(logger) { }
}

public class EducationalContentServiceAssessmentEngine : AssessmentEngine
{
    public EducationalContentServiceAssessmentEngine(
        Microsoft.Extensions.Logging.ILogger<AssessmentEngine> logger,
        ITimeProvider timeProvider)
        : base(logger, timeProvider) { }
}

public class EducationalContentServiceRecommendationEngine : RecommendationEngine
{
    public EducationalContentServiceRecommendationEngine(
        Microsoft.Extensions.Logging.ILogger<RecommendationEngine> logger,
        ICacheService cacheService)
        : base(logger, cacheService) { }
}

// Enum aliases
public enum EducationalContentServiceTutorialSort { Title = TutorialSort.Title, Popularity = TutorialSort.Popularity, Recent = TutorialSort.Recent, Rating = TutorialSort.Rating, Difficulty = TutorialSort.Difficulty }
public enum EducationalContentServiceTutorialStatus { NotStarted = TutorialStatus.NotStarted, InProgress = TutorialStatus.InProgress, Paused = TutorialStatus.Paused, Completed = TutorialStatus.Completed, Failed = TutorialStatus.Failed }
public enum EducationalContentServiceEducationalActionType { ButtonPress = EducationalActionType.ButtonPress, Movement = EducationalActionType.Movement, Combo = EducationalActionType.Combo, Timing = EducationalActionType.Timing, Other = EducationalActionType.Other }
public enum EducationalContentServiceGameMode { Versus = GameMode.Versus, Tournament = GameMode.Tournament, Training = GameMode.Training, Story = GameMode.Story, Survival = GameMode.Survival }
public enum EducationalContentServiceSkillLevel { Beginner = SkillLevel.Beginner, Intermediate = SkillLevel.Intermediate, Advanced = SkillLevel.Advanced, Expert = SkillLevel.Expert }
public enum EducationalContentServicePracticeSessionStatus { InProgress = PracticeSessionStatus.InProgress, Completed = PracticeSessionStatus.Completed, Abandoned = PracticeSessionStatus.Abandoned }
