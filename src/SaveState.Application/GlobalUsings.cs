global using SaveState.Core.Common;

// AiCoach type aliases (forwarded from Core) - avoiding conflicts with Mugen types
// Note: GameStateSnapshot and SkillLevel are NOT aliased here to avoid conflicts with Mugen types
global using CoachingPreferences = SaveState.Core.GameLibrary.Models.AiCoach.CoachingPreferences;
global using CoachingSession = SaveState.Core.GameLibrary.Models.AiCoach.CoachingSession;
global using GameAction = SaveState.Core.GameLibrary.Models.AiCoach.GameAction;
global using CoachingFeedback = SaveState.Core.GameLibrary.Models.AiCoach.CoachingFeedback;
global using CoachingReport = SaveState.Core.GameLibrary.Models.AiCoach.CoachingReport;
global using CoachingStyle = SaveState.Core.GameLibrary.Models.AiCoach.CoachingStyle;
global using CoachingFocus = SaveState.Core.GameLibrary.Models.AiCoach.CoachingFocus;
