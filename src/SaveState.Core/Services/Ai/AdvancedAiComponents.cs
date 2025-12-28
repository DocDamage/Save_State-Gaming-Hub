using System.Collections.Generic;
using System.Threading.Tasks;
using SaveState.Core.Services.Ai.Events;
using SaveState.Core.Services.Ai.Memory;
using SaveState.Core.Services.Ai.Orchestration;
using SaveState.Core.Services.Ai.Prompts;
using SaveState.Core.Services.Ai.Validation;
using SaveState.Core.Services.GameState;
using SaveState.Core.Services.Player;
using SaveState.Core.Services.Rules;
using SaveState.Core.Services.Timeline;

namespace SaveState.Core.Services.Ai
{
    /// <summary>
    /// Component interfaces for the AdvancedAiService subsystem.
    /// Phase 5: Advanced AI Service Splitting
    /// </summary>

    public interface IAiRequestProcessor
    {
        Task<AiResponse> ProcessAsync(string input, AiRequestContext? context = null);
    }

    public interface IAiMemoryCoordinator
    {
        Task RecordInteractionAsync(string input, string output, string? context = null);
        Task<string> GetContextualMemoryAsync(string query);
        Task<ConsolidatedContext> BuildMemoryContextAsync(string input, List<string>? relevantCharacters);
    }

    public interface IAiWorldStateCoordinator
    {
        void UpdateWorldState(string key, object value, string? source = null);
        WorldState GetCurrentWorldState();
        Task UpdatePlayerModelAsync(PlayerAction action);
        Task<PlayerProfile> GetPlayerProfileAsync(string playerId);
    }

    public interface IAiValidationCoordinator
    {
        Task<(string Content, bool WasValidated, float Confidence, Dictionary<string, object> Metadata)> ValidateAndScoreAsync(
            string content,
            AiRequestContext context,
            AdvancedAiConfig config);
        Task<ActionValidationResult> ValidateActionAsync(ProposedAction action);
    }

    public interface IAiNarrativeGenerator
    {
        Task<string> GenerateNarrativeAsync(string prompt, NarrativeContext? context = null);
        Task<string> GenerateCommentaryAsync(string gameEvent, CommentaryContext? context = null);
    }

    public interface IAiTimelineCoordinator
    {
        void CreateSavePoint(string name, string? description = null);
        Task<WhatIfResult> SimulateWhatIfAsync(string scenario);
    }

    public interface IAiEventCoordinator
    {
        void SubscribeToEvent(string eventType, Events.EventHandler handler);
        Task PublishEventAsync(AiEvent evt);
        Task PublishResponseEventAsync(AiResponse response, IntentCategory intent, string emotion);
    }
}
