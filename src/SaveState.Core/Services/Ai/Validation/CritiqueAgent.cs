using System;
using System.Threading.Tasks;

namespace SaveState.Core.Services.Ai.Validation
{
    /// <summary>
    /// Specialized agent for output review.
    /// - Returns: Approve, Revise, Reject
    /// - Provides revision suggestions
    /// </summary>
    public enum CritiqueDecision
    {
        Approve,
        Revise,
        Reject
    }

    public class AgentCritiqueResult
    {
        public CritiqueDecision Decision { get; set; }
        public string Reasoning { get; set; } = string.Empty;
        public string? RevisionGuidance { get; set; }
        public float QualityScore { get; set; }
    }

    public interface ICritiqueAgent
    {
        Task<AgentCritiqueResult> ReviewAsync(string output, string originalRequest, CritiqueContext context);
    }

    public class CritiqueAgent : ICritiqueAgent
    {
        private readonly ILlmService? _llmService;
        private readonly IOutputCritiquer _critiquer;

        public CritiqueAgent(ILlmService? llmService = null, IOutputCritiquer? critiquer = null)
        {
            _llmService = llmService;
            _critiquer = critiquer ?? new OutputCritiquer();
        }

        public async Task<AgentCritiqueResult> ReviewAsync(string output, string originalRequest, CritiqueContext context)
        {
            // Run automated critique first
            var critique = await _critiquer.CritiqueAsync(output, context);

            var result = new AgentCritiqueResult
            {
                QualityScore = critique.OverallScore
            };

            if (!critique.IsApproved)
            {
                // Check severity
                var hasCriticalFailure = critique.PassResults.Any(r => 
                    !r.Passed && (r.Pass == ValidationPass.SafetyCheck || r.Pass == ValidationPass.RuleCompliance));

                if (hasCriticalFailure)
                {
                    result.Decision = CritiqueDecision.Reject;
                    result.Reasoning = "Critical validation failure: " + critique.Summary;
                }
                else
                {
                    result.Decision = CritiqueDecision.Revise;
                    result.Reasoning = critique.Summary;
                    result.RevisionGuidance = critique.RevisionRequired;
                }
            }
            else if (critique.OverallScore < context.MinConfidence)
            {
                result.Decision = CritiqueDecision.Revise;
                result.Reasoning = $"Quality score ({critique.OverallScore:P0}) below threshold ({context.MinConfidence:P0})";
                result.RevisionGuidance = "Improve response clarity and relevance";
            }
            else
            {
                result.Decision = CritiqueDecision.Approve;
                result.Reasoning = "All validation passes successful with adequate quality";
            }

            return result;
        }
    }
}
