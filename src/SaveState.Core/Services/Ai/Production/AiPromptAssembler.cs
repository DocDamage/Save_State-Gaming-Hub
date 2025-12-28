using System;
using System.Collections.Generic;
using System.Linq;
using SaveState.Core.Services.Ai;

namespace SaveState.Core.Services.Ai.Production
{
    public interface IAiPromptAssembler
    {
        string AssemblePrompt(string input, ProductionAiRequest request, string? memoryContext, List<(string Role, string Content)> history);
        string AssembleSystemPrompt(ProductionAiRequest request, string? intent, ProductionAiConfig config);
    }

    public class AiPromptAssembler : IAiPromptAssembler
    {
        public string AssemblePrompt(string input, ProductionAiRequest request, string? memoryContext, List<(string Role, string Content)> history)
        {
            var parts = new List<string>();

            // Add memory context
            if (!string.IsNullOrEmpty(memoryContext))
            {
                parts.Add($"Previous context:\n{memoryContext}\n");
            }

            // Add world state if available
            if (request.Options?.InjectWorldState == true && request.Context?.WorldState != null)
            {
                var state = request.Context.WorldState;
                var stateParts = new List<string>();

                if (!string.IsNullOrEmpty(state.CurrentScene)) stateParts.Add($"Scene: {state.CurrentScene}");
                if (!string.IsNullOrEmpty(state.CurrentLocation)) stateParts.Add($"Location: {state.CurrentLocation}");
                if (state.Flags.Any()) stateParts.Add($"Flags: {string.Join(", ", state.Flags.Take(3).Select(f => f.Key))}");

                if (stateParts.Any())
                {
                    parts.Add($"Current situation: {string.Join(", ", stateParts)}\n");
                }
            }

            // Add conversation history
            if (history != null && history.Any())
            {
                var historyStr = string.Join("\n", history
                    .Select(t => $"{t.Role}: {t.Content}"));
                parts.Add($"Conversation so far:\n{historyStr}\n");
            }

            parts.Add($"User: {input}");

            return string.Join("\n", parts);
        }

        public string AssembleSystemPrompt(ProductionAiRequest request, string? intent, ProductionAiConfig config)
        {
            var basePrompt = request.SystemPrompt ?? config.DefaultSystemPrompt;

            // Add context-specific instructions
            if (request.Context?.InCombat == true)
            {
                basePrompt += "\nThe user is currently in combat. Be concise and action-oriented.";
            }
            else if (request.Context?.InDialogue == true)
            {
                basePrompt += "\nThe user is in a dialogue scene. Stay in character and maintain narrative flow.";
            }

            // Add intent-specific guidance
            if (!string.IsNullOrEmpty(intent))
            {
                basePrompt += $"\nDetected intent: {intent}. Respond appropriately.";
            }

            return basePrompt;
        }
    }
}
