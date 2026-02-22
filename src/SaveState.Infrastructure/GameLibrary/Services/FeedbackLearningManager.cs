using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Services;

/// <summary>
/// Manages user feedback processing and learning for heuristic improvement.
/// </summary>
public sealed class FeedbackLearningManager
{
    private readonly ILogger<FeedbackLearningManager> _logger;
    private readonly Dictionary<string, HeuristicFeedbackData> _feedbackHistory = new();

    public FeedbackLearningManager(ILogger<FeedbackLearningManager> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Submits feedback for a discovered value.
    /// </summary>
    public Task<Result> SubmitFeedbackAsync(DiscoveryFeedback feedback, CancellationToken ct = default)
    {
        try
        {
            if (feedback == null)
                return Task.FromResult(Result.Failure("Feedback cannot be null", ErrorType.Validation));

            // Store feedback for learning
            var key = $"{feedback.Address:X}_{feedback.CorrectCategory ?? "Unknown"}";
            
            if (!_feedbackHistory.TryGetValue(key, out var history))
            {
                history = new HeuristicFeedbackData();
                _feedbackHistory[key] = history;
            }

            history.TotalSubmissions++;
            if (feedback.WasCorrect)
            {
                history.CorrectIdentifications++;
            }

            if (!string.IsNullOrEmpty(feedback.CorrectName))
            {
                history.UserProvidedNames[feedback.CorrectName] =
                    history.UserProvidedNames.GetValueOrDefault(feedback.CorrectName) + 1;
            }

            if (!string.IsNullOrEmpty(feedback.CorrectCategory))
            {
                history.UserProvidedCategories[feedback.CorrectCategory] =
                    history.UserProvidedCategories.GetValueOrDefault(feedback.CorrectCategory) + 1;
            }

            _logger.LogInformation(
                "Feedback submitted for address {Address}: WasCorrect={WasCorrect}, Category={Category}, Name={Name}",
                feedback.Address, 
                feedback.WasCorrect, 
                feedback.CorrectCategory,
                feedback.CorrectName ?? "(not provided)");

            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting feedback");
            return Task.FromResult(Result.Failure($"Failed to submit feedback: {ex.Message}", ErrorType.Internal));
        }
    }

    /// <summary>
    /// Gets feedback history for an address.
    /// </summary>
    public HeuristicFeedbackData? GetFeedbackHistory(IntPtr address, string? category = null)
    {
        var key = $"{address:X}_{category ?? "Unknown"}";
        _feedbackHistory.TryGetValue(key, out var history);
        return history;
    }

    /// <summary>
    /// Gets the most common user-provided name for an address.
    /// </summary>
    public string? GetMostCommonName(IntPtr address, string? category = null)
    {
        var history = GetFeedbackHistory(address, category);
        if (history?.UserProvidedNames.Count > 0)
        {
            return history.UserProvidedNames
                .OrderByDescending(kvp => kvp.Value)
                .First().Key;
        }
        return null;
    }

    /// <summary>
    /// Calculates the accuracy rate for an address.
    /// </summary>
    public double GetAccuracyRate(IntPtr address, string? category = null)
    {
        var history = GetFeedbackHistory(address, category);
        if (history == null || history.TotalSubmissions == 0)
            return 0.0;
        
        return (double)history.CorrectIdentifications / history.TotalSubmissions;
    }

    /// <summary>
    /// Clears all feedback history.
    /// </summary>
    public void ClearHistory()
    {
        _feedbackHistory.Clear();
        _logger.LogInformation("Feedback history cleared");
    }
}

/// <summary>
/// Stores feedback data for learning.
/// </summary>
public sealed class HeuristicFeedbackData
{
    public int TotalSubmissions { get; set; }
    public int CorrectIdentifications { get; set; }
    public Dictionary<string, int> UserProvidedNames { get; } = new();
    public Dictionary<string, int> UserProvidedCategories { get; } = new();
}
