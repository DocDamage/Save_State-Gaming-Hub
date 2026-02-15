// Type aliases for backward compatibility after refactoring
// These aliases allow existing code to continue using the old type names
// while the new code uses the cleaner, shorter names

namespace SaveState.Application.Mugen.Services;

// Interface alias
using INarrativeMemoryService = NarrativeMemory.INarrativeMemoryService;

// Model type aliases
using NarrativeMemoryServiceMemoryCrystal = Models.NarrativeMemory.MemoryCrystal;
using NarrativeMemoryServiceAlternatePossibility = Models.NarrativeMemory.AlternatePossibility;
using NarrativeMemoryServiceEmotionalContext = Models.NarrativeMemory.EmotionalContext;
using NarrativeMemoryServiceCrystalGenerationRequest = Models.NarrativeMemory.CrystalGenerationRequest;
using NarrativeMemoryServiceAlternateTimeline = Models.NarrativeMemory.AlternateTimeline;
using NarrativeMemoryServiceTimelineBranchRequest = Models.NarrativeMemory.TimelineBranchRequest;
using NarrativeMemoryServiceSynthesizedMove = Models.NarrativeMemory.SynthesizedMove;
using NarrativeMemoryServiceCrystalSynthesisRequest = Models.NarrativeMemory.CrystalSynthesisRequest;
using NarrativeMemoryServiceButterflyEffect = Models.NarrativeMemory.ButterflyEffect;
using NarrativeMemoryServiceButterflyEffectRequest = Models.NarrativeMemory.ButterflyEffectRequest;
using NarrativeMemoryServiceCrystalCollection = Models.NarrativeMemory.CrystalCollection;
using NarrativeMemoryServiceTimelineReplay = Models.NarrativeMemory.TimelineReplay;
using NarrativeMemoryServiceReplayRequest = Models.NarrativeMemory.ReplayRequest;
using NarrativeMemoryServiceCrystalEconomy = Models.NarrativeMemory.CrystalEconomy;
using NarrativeMemoryServiceTradeOpportunity = Models.NarrativeMemory.TradeOpportunity;
using NarrativeMemoryServiceSynthesisPotential = Models.NarrativeMemory.SynthesisPotential;
using NarrativeMemoryServiceCrystalEnhancementRequest = Models.NarrativeMemory.CrystalEnhancementRequest;
using NarrativeMemoryServiceCrystalTrade = Models.NarrativeMemory.CrystalTrade;
using NarrativeMemoryServiceCrystalTradeRequest = Models.NarrativeMemory.CrystalTradeRequest;
using NarrativeMemoryServiceNarrativeAnalytics = Models.NarrativeMemory.NarrativeAnalytics;
using NarrativeMemoryServiceNarrativeMatchResult = Models.NarrativeMemory.NarrativeMatchResult;

// Additional model aliases
using NarrativeMemoryServiceEnhancementRequest = Models.NarrativeMemory.EnhancementRequest;
using NarrativeMemoryServiceMatchMemory = Models.NarrativeMemory.MatchMemory;
using NarrativeMemoryServiceTimelineForkRequest = Models.NarrativeMemory.TimelineForkRequest;
using NarrativeMemoryServiceReplayOptions = Models.NarrativeMemory.ReplayOptions;
using NarrativeMemoryServiceMoveSynthesisRequest = Models.NarrativeMemory.MoveSynthesisRequest;
using NarrativeMemoryServiceButterflyEffectResult = Models.NarrativeMemory.ButterflyEffectResult;

// Enum aliases
using NarrativeMemoryServiceCrystalRarity = Models.NarrativeMemory.CrystalRarity;
using NarrativeMemoryServiceMatchOutcome = Models.NarrativeMemory.MatchOutcome;
using NarrativeMemoryServiceEnhancementType = Models.NarrativeMemory.EnhancementType;
using NarrativeMemoryServiceTradeStatus = Models.NarrativeMemory.TradeStatus;
