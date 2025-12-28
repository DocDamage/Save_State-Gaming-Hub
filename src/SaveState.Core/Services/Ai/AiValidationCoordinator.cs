using System.Collections.Generic;
using System.Threading.Tasks;
using SaveState.Core.Services.Ai.Uncertainty;
using SaveState.Core.Services.Ai.Validation;
using SaveState.Core.Services.GameState;
using SaveState.Core.Services.Rules;

namespace SaveState.Core.Services.Ai
{
    /// <summary>
    /// Coordinates output validation, confidence scoring, and action validation.
    /// </summary>
    public class AiValidationCoordinator : IAiValidationCoordinator
    {
        private readonly IOutputCritiquer _outputCritiquer;
        private readonly IConfidenceScorer _confidenceScorer;
        private readonly IUncertaintyWrapper _uncertaintyWrapper;
        private readonly IActionValidator _actionValidator;
        private readonly IWorldStateService _worldStateService;

        public AiValidationCoordinator(
            IOutputCritiquer outputCritiquer,
            IConfidenceScorer confidenceScorer,
            IUncertaintyWrapper uncertaintyWrapper,
            IActionValidator actionValidator,
            IWorldStateService worldStateService)
        {
            _outputCritiquer = outputCritiquer;
            _confidenceScorer = confidenceScorer;
            _uncertaintyWrapper = uncertaintyWrapper;
            _actionValidator = actionValidator;
            _worldStateService = worldStateService;
        }

        public async Task<(string Content, bool WasValidated, float Confidence, Dictionary<string, object> Metadata)> ValidateAndScoreAsync(
            string content,
            AiRequestContext context,
            AdvancedAiConfig config)
        {
            var metadata = new Dictionary<string, object>();
            var wasValidated = false;
            var confidence = 1.0f;
            var finalContent = content;

            // Validate response if required
            if (config.EnableValidation && context.RequireValidation)
            {
                var critiqueContext = new CritiqueContext
                {
                    ExpectedTone = context.RequestType,
                    ActiveFlags = _worldStateService.CurrentState.Flags,
                    MinConfidence = config.MinConfidenceThreshold
                };

                var critique = await _outputCritiquer.CritiqueAsync(content, critiqueContext);
                wasValidated = critique.IsApproved;

                if (!critique.IsApproved && !string.IsNullOrEmpty(critique.RevisionRequired))
                {
                    metadata["revision_note"] = critique.RevisionRequired;
                }
            }

            // Score confidence and wrap if needed
            if (config.EnableConfidenceScoring)
            {
                var confidenceContext = new ConfidenceContext
                {
                    OriginalQuery = context.SessionId ?? "",
                    KnowledgeBaseHits = new List<string>()
                };

                var confidenceResult = _confidenceScorer.Score(content, confidenceContext);
                confidence = confidenceResult.OverallConfidence;

                if (confidenceResult.ConfidenceLevel != "high")
                {
                    var wrapped = _uncertaintyWrapper.Wrap(content, confidenceResult);
                    if (wrapped.WasWrapped)
                    {
                        finalContent = wrapped.FinalOutput;
                        metadata["was_hedged"] = true;
                    }
                }
            }

            return (finalContent, wasValidated, confidence, metadata);
        }

        public Task<ActionValidationResult> ValidateActionAsync(ProposedAction action)
        {
            var gameContext = new GameContext
            {
                Flags = _worldStateService.CurrentState.Flags,
                Counters = _worldStateService.CurrentState.Counters,
                CurrentAction = action.ActionType,
                Actor = action.Actor,
                Target = action.Target
            };

            var result = _actionValidator.Validate(action, gameContext);
            return Task.FromResult(result);
        }
    }
}
