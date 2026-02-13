global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Threading;
global using System.Threading.Tasks;
global using Ardalis.GuardClauses;

// AiCoach type aliases for backward compatibility
global using CoachingPreferences = SaveState.Core.GameLibrary.Models.AiCoach.CoachingPreferences;
global using CoachingSession = SaveState.Core.GameLibrary.Models.AiCoach.CoachingSession;
global using GameStateSnapshot = SaveState.Core.GameLibrary.Models.AiCoach.GameStateSnapshot;
global using GameAction = SaveState.Core.GameLibrary.Models.AiCoach.GameAction;
global using CoachingFeedback = SaveState.Core.GameLibrary.Models.AiCoach.CoachingFeedback;
global using StrategyStrength = SaveState.Core.GameLibrary.Models.AiCoach.StrategyStrength;
global using StrategyWeakness = SaveState.Core.GameLibrary.Models.AiCoach.StrategyWeakness;
global using StrategyRecommendation = SaveState.Core.GameLibrary.Models.AiCoach.StrategyRecommendation;
global using StrategyAnalysis = SaveState.Core.GameLibrary.Models.AiCoach.StrategyAnalysis;
global using OpponentPattern = SaveState.Core.GameLibrary.Models.AiCoach.OpponentPattern;
global using CounterStrategy = SaveState.Core.GameLibrary.Models.AiCoach.CounterStrategy;
global using OpponentAnalysis = SaveState.Core.GameLibrary.Models.AiCoach.OpponentAnalysis;
global using SkillMilestone = SaveState.Core.GameLibrary.Models.AiCoach.SkillMilestone;
global using SkillAssessment = SaveState.Core.GameLibrary.Models.AiCoach.SkillAssessment;
global using ImprovementGoal = SaveState.Core.GameLibrary.Models.AiCoach.ImprovementGoal;
global using TrainingExercise = SaveState.Core.GameLibrary.Models.AiCoach.TrainingExercise;
global using Milestone = SaveState.Core.GameLibrary.Models.AiCoach.Milestone;
global using ImprovementPlan = SaveState.Core.GameLibrary.Models.AiCoach.ImprovementPlan;
global using CoachingTip = SaveState.Core.GameLibrary.Models.AiCoach.CoachingTip;
global using CoachingReport = SaveState.Core.GameLibrary.Models.AiCoach.CoachingReport;
global using SessionMetrics = SaveState.Core.GameLibrary.Models.AiCoach.SessionMetrics;

// AiCoach enums
global using CoachingStyle = SaveState.Core.GameLibrary.Models.AiCoach.CoachingStyle;
global using SkillLevel = SaveState.Core.GameLibrary.Models.AiCoach.SkillLevel;
global using CoachingFocus = SaveState.Core.GameLibrary.Models.AiCoach.CoachingFocus;
global using CoachingPhase = SaveState.Core.GameLibrary.Models.AiCoach.CoachingPhase;
global using FeedbackType = SaveState.Core.GameLibrary.Models.AiCoach.FeedbackType;
global using FeedbackPriority = SaveState.Core.GameLibrary.Models.AiCoach.FeedbackPriority;
global using StrategyRating = SaveState.Core.GameLibrary.Models.AiCoach.StrategyRating;
global using ActionOutcome = SaveState.Core.GameLibrary.Models.AiCoach.ActionOutcome;
global using OpponentType = SaveState.Core.GameLibrary.Models.AiCoach.OpponentType;
global using OpponentSkillLevel = SaveState.Core.GameLibrary.Models.AiCoach.OpponentSkillLevel;
global using SkillArea = SaveState.Core.GameLibrary.Models.AiCoach.SkillArea;
global using SkillRating = SaveState.Core.GameLibrary.Models.AiCoach.SkillRating;
global using TipCategory = SaveState.Core.GameLibrary.Models.AiCoach.TipCategory;
global using TipDifficulty = SaveState.Core.GameLibrary.Models.AiCoach.TipDifficulty;
global using Difficulty = SaveState.Core.GameLibrary.Models.AiCoach.Difficulty;
global using AnalysisType = SaveState.Core.GameLibrary.Models.AiCoach.AnalysisType;