using Microsoft.Extensions.Logging;
using SaveState.Core.AI.Assistant;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using System.Collections.Concurrent;

namespace SaveState.Infrastructure.AI.Assistant;

/// <summary>
/// Learns user preferences over time to personalize assistant behavior.
/// Uses exponential moving averages and decay factors for adaptive learning.
/// </summary>
public sealed class UserPreferenceLearningService : IUserPreferenceLearningService
{
    private readonly ILogger<UserPreferenceLearningService> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly string _preferencesPath;
    
    // In-memory storage for feedback and actions
    private readonly ConcurrentQueue<SuggestionFeedback> _feedbackHistory = new();
    private readonly ConcurrentDictionary<Guid, UserActionRecord> _actionHistory = new();
    private readonly ConcurrentDictionary<string, PreferenceWeight> _preferenceWeights = new();
    
    // Learning parameters
    private const float LearningRate = 0.1f;
    private const float DecayFactor = 0.95f;
    private const int MaxFeedbackHistory = 1000;
    private const int MinSamplesForUpdate = 10;

    // Current learned preferences
    private UserPreferences _currentPreferences;

    public UserPreferenceLearningService(
        ILogger<UserPreferenceLearningService> logger,
        ITimeProvider timeProvider,
        string? preferencesPath = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _preferencesPath = preferencesPath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SaveStateReborn",
            "UserPreferences.json");
        
        // Initialize with default preferences
        _currentPreferences = CreateDefaultPreferences(_timeProvider.UtcNow);
        
        // Try to load saved preferences
        LoadPreferences();
    }

    /// <inheritdoc />
    public Task<Result> RecordSuggestionFeedbackAsync(SuggestionFeedback feedback, CancellationToken ct = default)
    {
        if (feedback == null)
        {
            return Task.FromResult(Result.Failure("Feedback cannot be null.", ErrorType.Validation));
        }

        // Add to history
        _feedbackHistory.Enqueue(feedback);
        
        // Trim history if too large
        while (_feedbackHistory.Count > MaxFeedbackHistory && _feedbackHistory.TryDequeue(out _)) { }

        // Update preference weights immediately based on feedback
        UpdateWeightsFromFeedback(feedback);

        _logger.LogDebug(
            "Recorded feedback for {SuggestionType}: {WasHelpful}",
            feedback.SuggestionType,
            feedback.WasHelpful);

        return Task.FromResult(Result.Success());
    }

    /// <inheritdoc />
    public Task<Result> RecordUserActionAsync(Guid sessionId, UserActionType action, CancellationToken ct = default)
    {
        var record = new UserActionRecord
        {
            SessionId = sessionId,
            ActionType = action,
            TimestampUtc = _timeProvider.UtcNow
        };

        _actionHistory[sessionId] = record;

        _logger.LogDebug(
            "Recorded user action {ActionType} for session {SessionId}",
            action,
            sessionId);

        return Task.FromResult(Result.Success());
    }

    /// <inheritdoc />
    public Task<Result<UserPreferences>> GetUserPreferencesAsync(CancellationToken ct = default)
    {
        return Task.FromResult(Result.Success(_currentPreferences));
    }

    /// <inheritdoc />
    public Task<Result> UpdatePreferencesAsync(CancellationToken ct = default)
    {
        try
        {
            var feedbackList = _feedbackHistory.ToList();
            
            if (feedbackList.Count < MinSamplesForUpdate)
            {
                if (_actionHistory.Count < MinSamplesForUpdate)
                {
                    _logger.LogDebug(
                        "Insufficient feedback/action samples ({FeedbackCount}/{ActionCount}) for preference update. Need {MinSamples}.",
                        feedbackList.Count,
                        _actionHistory.Count,
                        MinSamplesForUpdate);

                    // Persist current state for deterministic reload behavior.
                    SavePreferences();
                    return Task.FromResult(Result.Success());
                }
            }

            // Calculate new preference values based on historical data
            var newPreferences = CalculatePreferences(feedbackList);
            
            // Apply exponential moving average
            _currentPreferences = BlendPreferences(_currentPreferences, newPreferences, LearningRate);
            
            // Update timestamp
            var updateTimestamp = _timeProvider.UtcNow;
            if (updateTimestamp <= _currentPreferences.LastUpdatedAtUtc)
            {
                updateTimestamp = _currentPreferences.LastUpdatedAtUtc.AddTicks(1);
            }
            _currentPreferences = _currentPreferences with { LastUpdatedAtUtc = updateTimestamp };

            // Persist preferences
            SavePreferences();

            _logger.LogInformation(
                "Updated user preferences. Break reminder: {BreakReminder:P0}, Difficulty threshold: {DifficultyThreshold:P0}",
                _currentPreferences.BreakReminderFrequency,
                _currentPreferences.DifficultySuggestionThreshold);

            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update preferences");
            return Task.FromResult(Result.Failure(
                $"Failed to update preferences: {ex.Message}",
                ErrorType.Internal));
        }
    }

    /// <summary>
    /// Gets the learned weight for a specific preference dimension.
    /// </summary>
    public float GetPreferenceWeight(string dimension)
    {
        return _preferenceWeights.TryGetValue(dimension, out var weight)
            ? weight.CurrentValue
            : 0.5f;
    }

    /// <summary>
    /// Resets all learned preferences to defaults.
    /// </summary>
    public void ResetPreferences()
    {
        _currentPreferences = CreateDefaultPreferences(_timeProvider.UtcNow);
        _feedbackHistory.Clear();
        _actionHistory.Clear();
        _preferenceWeights.Clear();
        
        SavePreferences();
        
        _logger.LogInformation("User preferences reset to defaults");
    }

    /// <summary>
    /// Gets statistics about the learning data.
    /// </summary>
    public LearningStatistics GetLearningStatistics()
    {
        var feedbackList = _feedbackHistory.ToList();
        
        return new LearningStatistics(
            TotalFeedbackEntries: feedbackList.Count,
            TotalActionsRecorded: _actionHistory.Count,
            HelpfulSuggestions: feedbackList.Count(f => f.WasHelpful),
            IgnoredSuggestions: feedbackList.Count(f => !f.WasHelpful),
            LastUpdatedAtUtc: _currentPreferences.LastUpdatedAtUtc,
            PreferenceDimensions: _preferenceWeights.Keys.ToList().AsReadOnly());
    }

    #region Private Methods

    private void UpdateWeightsFromFeedback(SuggestionFeedback feedback)
    {
        var weightKey = feedback.SuggestionType.ToString();
        
        if (!_preferenceWeights.TryGetValue(weightKey, out var weight))
        {
            weight = new PreferenceWeight
            {
                Dimension = weightKey,
                CurrentValue = 0.5f,
                SampleCount = 0
            };
        }

        // Update using exponential moving average
        var targetValue = feedback.WasHelpful ? 1.0f : 0.0f;
        var newValue = (weight.CurrentValue * weight.SampleCount + targetValue) / (weight.SampleCount + 1);
        
        // Apply decay to older samples
        if (weight.SampleCount > 100)
        {
            newValue = (newValue * DecayFactor) + (0.5f * (1 - DecayFactor));
        }

        _preferenceWeights[weightKey] = new PreferenceWeight
        {
            Dimension = weightKey,
            CurrentValue = Math.Clamp(newValue, 0.0f, 1.0f),
            SampleCount = weight.SampleCount + 1,
            LastUpdatedAtUtc = _timeProvider.UtcNow
        };
    }

    private UserPreferences CalculatePreferences(List<SuggestionFeedback> feedbackList)
    {
        // Group feedback by suggestion type
        var byType = feedbackList.GroupBy(f => f.SuggestionType).ToList();
        
        // Calculate break reminder frequency preference
        var breakFeedback = byType.FirstOrDefault(g => g.Key == SuggestionType.BreakReminder);
        var breakFrequency = breakFeedback != null
            ? breakFeedback.Average(f => f.WasHelpful ? 1.0f : 0.0f)
            : 0.5f;

        // Calculate difficulty suggestion threshold
        var difficultyFeedback = byType.FirstOrDefault(g => g.Key == SuggestionType.DifficultyAdjustment);
        var difficultyThreshold = difficultyFeedback != null
            ? 1.0f - difficultyFeedback.Average(f => f.WasHelpful ? 1.0f : 0.0f) // Inverse: lower threshold if feedback is positive
            : 0.7f;

        // Calculate coaching tip frequency
        var coachingFeedback = byType.FirstOrDefault(g => g.Key == SuggestionType.CoachingTip);
        var coachingFrequency = coachingFeedback != null
            ? coachingFeedback.Average(f => f.WasHelpful ? 1.0f : 0.0f)
            : 0.5f;

        // Analyze action patterns
        var actions = _actionHistory.Values.ToList();
        var acceptedRate = actions.Any()
            ? actions.Count(a => a.ActionType == UserActionType.AcceptedSuggestion) / (float)actions.Count
            : 0.5f;

        return new UserPreferences(
            BreakReminderFrequency: Math.Clamp(breakFrequency, 0.2f, 1.0f),
            DifficultySuggestionThreshold: Math.Clamp(difficultyThreshold, 0.5f, 0.95f),
            CoachingTipFrequency: Math.Clamp(coachingFrequency, 0.2f, 1.0f),
            PrefersSpoilerFreeHints: InferSpoilerPreference(feedbackList),
            AutoAcceptHighConfidenceSuggestions: acceptedRate > 0.7f,
            PreferredSessionDuration: InferPreferredSessionDuration(feedbackList),
            PreferredGameGenres: _currentPreferences.PreferredGameGenres, // Preserve existing
            LastUpdatedAtUtc: _timeProvider.UtcNow);
    }

    private UserPreferences BlendPreferences(UserPreferences current, UserPreferences update, float alpha)
    {
        return new UserPreferences(
            BreakReminderFrequency: Lerp(current.BreakReminderFrequency, update.BreakReminderFrequency, alpha),
            DifficultySuggestionThreshold: Lerp(current.DifficultySuggestionThreshold, update.DifficultySuggestionThreshold, alpha),
            CoachingTipFrequency: Lerp(current.CoachingTipFrequency, update.CoachingTipFrequency, alpha),
            PrefersSpoilerFreeHints: update.PrefersSpoilerFreeHints, // Binary, no interpolation
            AutoAcceptHighConfidenceSuggestions: update.AutoAcceptHighConfidenceSuggestions, // Binary decision
            PreferredSessionDuration: TimeSpan.FromMinutes(
                Lerp((float)current.PreferredSessionDuration.TotalMinutes, 
                     (float)update.PreferredSessionDuration.TotalMinutes, alpha)),
            PreferredGameGenres: current.PreferredGameGenres, // Preserve existing
            LastUpdatedAtUtc: _timeProvider.UtcNow);
    }

    private static float Lerp(float a, float b, float t)
    {
        return a + (b - a) * t;
    }

    private bool InferSpoilerPreference(List<SuggestionFeedback> feedbackList)
    {
        // Check if user typically dismisses spoiler-containing suggestions
        var spoilerFeedback = feedbackList.Where(f => 
            !string.IsNullOrEmpty(f.UserComment) && 
            (f.UserComment.Contains("spoiler", StringComparison.OrdinalIgnoreCase) ||
             f.UserComment.Contains("ruin", StringComparison.OrdinalIgnoreCase)));
        
        return spoilerFeedback.Any() && spoilerFeedback.Average(f => f.WasHelpful ? 1.0f : 0.0f) < 0.3f;
    }

    private TimeSpan InferPreferredSessionDuration(List<SuggestionFeedback> feedbackList)
    {
        // Analyze break reminder feedback to infer preferred session length
        var breakReminders = feedbackList
            .Where(f => f.SuggestionType == SuggestionType.BreakReminder)
            .ToList();

        if (!breakReminders.Any())
        {
            return TimeSpan.FromHours(1.5);
        }

        var helpfulRate = breakReminders.Average(f => f.WasHelpful ? 1.0f : 0.0f);
        
        // If user finds break reminders helpful, they prefer shorter sessions
        // If user ignores them, they prefer longer sessions
        var preferredMinutes = helpfulRate > 0.5f 
            ? 60f  // 1 hour
            : 120f; // 2 hours

        return TimeSpan.FromMinutes(preferredMinutes);
    }

    private void SavePreferences()
    {
        try
        {
            var directory = Path.GetDirectoryName(_preferencesPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = System.Text.Json.JsonSerializer.Serialize(_currentPreferences, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });
            
            File.WriteAllText(_preferencesPath, json);
            _logger.LogDebug("User preferences saved to {Path}", _preferencesPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save user preferences to {Path}", _preferencesPath);
        }
    }

    private void LoadPreferences()
    {
        try
        {
            if (!File.Exists(_preferencesPath))
            {
                _logger.LogDebug("No saved preferences found at {Path}", _preferencesPath);
                return;
            }

            var json = File.ReadAllText(_preferencesPath);
            var loaded = System.Text.Json.JsonSerializer.Deserialize<UserPreferences>(json);
            
            if (loaded != null)
            {
                _currentPreferences = loaded;
                _logger.LogInformation("Loaded user preferences from {Path}", _preferencesPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load user preferences from {Path}", _preferencesPath);
        }
    }

    private static UserPreferences CreateDefaultPreferences(DateTime nowUtc)
    {
        return new UserPreferences(
            BreakReminderFrequency: 0.5f,
            DifficultySuggestionThreshold: 0.7f,
            CoachingTipFrequency: 0.6f,
            PrefersSpoilerFreeHints: true,
            AutoAcceptHighConfidenceSuggestions: false,
            PreferredSessionDuration: TimeSpan.FromHours(1.5),
            PreferredGameGenres: new List<string>().AsReadOnly(),
            LastUpdatedAtUtc: nowUtc);
    }

    #endregion

    #region Internal Types

    private class PreferenceWeight
    {
        public string Dimension { get; set; } = string.Empty;
        public float CurrentValue { get; set; }
        public int SampleCount { get; set; }
        public DateTime? LastUpdatedAtUtc { get; set; }
    }

    private class UserActionRecord
    {
        public Guid SessionId { get; set; }
        public UserActionType ActionType { get; set; }
        public DateTime TimestampUtc { get; set; }
    }

    #endregion
}

/// <summary>
/// Statistics about the learning process.
/// </summary>
public sealed record LearningStatistics(
    int TotalFeedbackEntries,
    int TotalActionsRecorded,
    int HelpfulSuggestions,
    int IgnoredSuggestions,
    DateTime LastUpdatedAtUtc,
    IReadOnlyList<string> PreferenceDimensions);
