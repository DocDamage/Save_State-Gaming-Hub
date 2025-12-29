namespace SaveState.Infrastructure.Ai.Learning;

using Microsoft.Extensions.Logging;
using SaveState.Core.Ai.Knowledge;
using SaveState.Core.Ai.Learning;

public class LocalLearningService : IFeedbackLoop
{
    private readonly IKnowledgeStore _store;
    private readonly ILogger<LocalLearningService> _logger;

    public LocalLearningService(
        IKnowledgeStore store,
        ILogger<LocalLearningService> logger)
    {
        _store = store;
        _logger = logger;
    }

    public async Task SubmitFeedbackAsync(string messageId, FeedbackType type, string comment, CancellationToken ct = default)
    {
        try
        {
            switch (type)
            {
                case FeedbackType.Helpful:
                    // Boost the relevance of the context used for this message
                    await _store.BoostAsync(messageId, 1.2f, ct).ConfigureAwait(false);
                    _logger.LogInformation("Boosted relevance for helpful message {MessageId}", messageId);
                    break;

                case FeedbackType.Inaccurate:
                    // Penalize by reducing relevance score
                    await _store.BoostAsync(messageId, 0.8f, ct).ConfigureAwait(false);
                    _logger.LogWarning("Reduced relevance for inaccurate message {MessageId}: {Comment}", messageId, comment);
                    break;

                case FeedbackType.Harmful:
                case FeedbackType.Hallucination:
                    // Flag for immediate review/removal
                    await _store.FlagAsync(messageId, ct).ConfigureAwait(false);
                    _logger.LogError("Flagged harmful/hallucinated message {MessageId}: {Comment}", messageId, comment);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process feedback for message {MessageId}", messageId);
            throw;
        }
    }

    public async Task MaintainContextQualityAsync(CancellationToken ct = default)
    {
        try
        {
            // Periodic pruning of low-relevance or highly-penalized context
            await _store.PruneLowQualityAsync(0.3f, ct).ConfigureAwait(false);
            _logger.LogInformation("Completed context quality maintenance");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to maintain context quality");
            throw;
        }
    }
}
