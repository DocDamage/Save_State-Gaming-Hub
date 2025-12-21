using System;
using System.Threading.Tasks;

namespace SaveState.Core.Services.Ai.Validation
{
    /// <summary>
    /// Iterative improvement until approval.
    /// - Max 3 iterations
    /// - Fallback to safe response
    /// </summary>
    public class RevisionResult
    {
        public string FinalOutput { get; set; } = string.Empty;
        public int IterationsUsed { get; set; }
        public bool WasApproved { get; set; }
        public bool UsedFallback { get; set; }
        public string? FinalDecisionReason { get; set; }
    }

    public interface IRevisionLoop
    {
        Task<RevisionResult> ProcessWithRevision(string prompt, string systemPrompt, CritiqueContext context);
        string GetSafetyFallback(string requestType);
    }

    public class RevisionLoop : IRevisionLoop
    {
        private readonly ILlmService _llmService;
        private readonly ICritiqueAgent _critiqueAgent;
        private readonly int _maxIterations = 3;

        public RevisionLoop(ILlmService llmService, ICritiqueAgent? critiqueAgent = null)
        {
            _llmService = llmService;
            _critiqueAgent = critiqueAgent ?? new CritiqueAgent(llmService);
        }

        public async Task<RevisionResult> ProcessWithRevision(string prompt, string systemPrompt, CritiqueContext context)
        {
            var result = new RevisionResult();
            string currentOutput = "";
            string currentPrompt = prompt;

            for (int i = 0; i < _maxIterations; i++)
            {
                result.IterationsUsed = i + 1;

                // Generate response
                currentOutput = await _llmService.CompleteAsync(currentPrompt, systemPrompt);

                // Review response
                var critique = await _critiqueAgent.ReviewAsync(currentOutput, prompt, context);

                if (critique.Decision == CritiqueDecision.Approve)
                {
                    result.FinalOutput = currentOutput;
                    result.WasApproved = true;
                    result.FinalDecisionReason = critique.Reasoning;
                    return result;
                }

                if (critique.Decision == CritiqueDecision.Reject)
                {
                    // Cannot recover, use fallback
                    result.FinalOutput = GetSafetyFallback(context.RequestType ?? "general");
                    result.UsedFallback = true;
                    result.FinalDecisionReason = $"Rejected: {critique.Reasoning}";
                    return result;
                }

                // Revise - modify prompt with guidance
                if (!string.IsNullOrEmpty(critique.RevisionGuidance))
                {
                    currentPrompt = $@"{prompt}

REVISION REQUIRED:
Previous attempt had issues: {critique.Reasoning}
Please revise with the following guidance: {critique.RevisionGuidance}

Generate an improved response:";
                }
            }

            // Max iterations reached
            result.FinalOutput = currentOutput;
            result.WasApproved = false;
            result.FinalDecisionReason = "Max revision iterations reached";
            return result;
        }

        public string GetSafetyFallback(string requestType)
        {
            return requestType.ToLowerInvariant() switch
            {
                "narrative" => "The scene continues, though the details remain shrouded in uncertainty...",
                "combat" => "The clash continues with neither side gaining clear advantage.",
                "dialogue" => "The character pauses thoughtfully before speaking again.",
                "lore" => "The ancient texts remain silent on this particular matter.",
                "quest" => "Your journey continues. Seek guidance from those who know the way.",
                _ => "Processing complete. Please try again or rephrase your request."
            };
        }
    }
}
