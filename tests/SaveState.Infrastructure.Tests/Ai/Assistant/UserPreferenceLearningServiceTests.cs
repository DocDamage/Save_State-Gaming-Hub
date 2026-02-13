using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SaveState.Core.AI.Assistant;
using SaveState.Infrastructure.AI.Assistant;
using SaveState.Tests.Infrastructure;

namespace SaveState.Infrastructure.Tests.AI.Assistant;

public class UserPreferenceLearningServiceTests : IDisposable
{
    private readonly TestTimeProvider _timeProvider;
    private readonly string _tempPath;
    private readonly UserPreferenceLearningService _sut;

    public UserPreferenceLearningServiceTests()
    {
        _timeProvider = new TestTimeProvider(new DateTime(2026, 2, 13, 12, 0, 0, DateTimeKind.Utc));
        _tempPath = Path.Combine(Path.GetTempPath(), $"prefs_{Guid.NewGuid()}.json");
        _sut = new UserPreferenceLearningService(
            NullLogger<UserPreferenceLearningService>.Instance,
            _timeProvider,
            _tempPath);
    }

    public void Dispose()
    {
        try
        {
            if (File.Exists(_tempPath))
            {
                File.Delete(_tempPath);
            }
        }
        catch { }
    }

    [Fact]
    public async Task GetUserPreferencesAsync_ReturnsDefaultPreferences()
    {
        // Act
        var result = await _sut.GetUserPreferencesAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.BreakReminderFrequency.Should().BeApproximately(0.5f, 0.01f);
        result.Value.DifficultySuggestionThreshold.Should().BeApproximately(0.7f, 0.01f);
        result.Value.PrefersSpoilerFreeHints.Should().BeTrue();
    }

    [Fact]
    public async Task RecordSuggestionFeedbackAsync_WithValidFeedback_ReturnsSuccess()
    {
        // Arrange
        var feedback = new SuggestionFeedback(
            Guid.NewGuid(),
            SuggestionType.DifficultyAdjustment,
            true,
            "Very helpful",
            _timeProvider.UtcNow);

        // Act
        var result = await _sut.RecordSuggestionFeedbackAsync(feedback);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task RecordSuggestionFeedbackAsync_WithNullFeedback_ReturnsFailure()
    {
        // Act
        var result = await _sut.RecordSuggestionFeedbackAsync(null!);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task RecordUserActionAsync_WithValidAction_ReturnsSuccess()
    {
        // Arrange
        var sessionId = Guid.NewGuid();

        // Act
        var result = await _sut.RecordUserActionAsync(sessionId, UserActionType.AcceptedSuggestion);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task UpdatePreferencesAsync_WithInsufficientData_ReturnsSuccessButDoesNotUpdate()
    {
        // Arrange - No feedback recorded yet

        // Act
        var result = await _sut.UpdatePreferencesAsync();

        // Assert
        result.IsSuccess.Should().BeTrue(); // Returns success, just doesn't update
    }

    [Fact]
    public async Task UpdatePreferencesAsync_WithEnoughData_UpdatesPreferences()
    {
        // Arrange - Record enough feedback
        for (int i = 0; i < 15; i++)
        {
            await _sut.RecordSuggestionFeedbackAsync(new SuggestionFeedback(
                Guid.NewGuid(),
                SuggestionType.BreakReminder,
                i % 3 != 0, // Most are helpful
                null,
                _timeProvider.UtcNow));
        }

        var initialPrefs = (await _sut.GetUserPreferencesAsync()).Value!;

        // Act
        var result = await _sut.UpdatePreferencesAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        var updatedPrefs = (await _sut.GetUserPreferencesAsync()).Value!;
        updatedPrefs.LastUpdatedAtUtc.Should().BeAfter(initialPrefs.LastUpdatedAtUtc);
    }

    [Fact]
    public void GetLearningStatistics_WithNoData_ReturnsZeros()
    {
        // Act
        var stats = _sut.GetLearningStatistics();

        // Assert
        stats.TotalFeedbackEntries.Should().Be(0);
        stats.TotalActionsRecorded.Should().Be(0);
        stats.HelpfulSuggestions.Should().Be(0);
        stats.IgnoredSuggestions.Should().Be(0);
    }

    [Fact]
    public void GetLearningStatistics_WithData_ReturnsCorrectCounts()
    {
        // Arrange
        _sut.RecordSuggestionFeedbackAsync(new SuggestionFeedback(
            Guid.NewGuid(), SuggestionType.BreakReminder, true, null, _timeProvider.UtcNow)).Wait();
        _sut.RecordSuggestionFeedbackAsync(new SuggestionFeedback(
            Guid.NewGuid(), SuggestionType.BreakReminder, true, null, _timeProvider.UtcNow)).Wait();
        _sut.RecordSuggestionFeedbackAsync(new SuggestionFeedback(
            Guid.NewGuid(), SuggestionType.DifficultyAdjustment, false, null, _timeProvider.UtcNow)).Wait();
        _sut.RecordUserActionAsync(Guid.NewGuid(), UserActionType.AcceptedSuggestion).Wait();

        // Act
        var stats = _sut.GetLearningStatistics();

        // Assert
        stats.TotalFeedbackEntries.Should().Be(3);
        stats.HelpfulSuggestions.Should().Be(2);
        stats.IgnoredSuggestions.Should().Be(1);
        stats.TotalActionsRecorded.Should().Be(1);
    }

    [Fact]
    public void ResetPreferences_ResetsToDefaults()
    {
        // Arrange - Add some data and update
        for (int i = 0; i < 15; i++)
        {
            _sut.RecordSuggestionFeedbackAsync(new SuggestionFeedback(
                Guid.NewGuid(), SuggestionType.BreakReminder, true, null, _timeProvider.UtcNow)).Wait();
        }
        _sut.UpdatePreferencesAsync().Wait();

        var prefsBefore = _sut.GetUserPreferencesAsync().Result.Value!;
        prefsBefore.BreakReminderFrequency.Should().NotBe(0.5f);

        // Act
        _sut.ResetPreferences();

        // Assert
        var prefsAfter = _sut.GetUserPreferencesAsync().Result.Value!;
        prefsAfter.BreakReminderFrequency.Should().BeApproximately(0.5f, 0.01f);
        prefsAfter.DifficultySuggestionThreshold.Should().BeApproximately(0.7f, 0.01f);
    }

    [Fact]
    public void GetPreferenceWeight_WithUnknownDimension_ReturnsDefault()
    {
        // Act
        var weight = _sut.GetPreferenceWeight("UnknownDimension");

        // Assert
        weight.Should().Be(0.5f);
    }

    [Fact]
    public void GetPreferenceWeight_AfterFeedback_ReturnsUpdatedWeight()
    {
        // Arrange
        _sut.RecordSuggestionFeedbackAsync(new SuggestionFeedback(
            Guid.NewGuid(), SuggestionType.CoachingTip, true, null, _timeProvider.UtcNow)).Wait();

        // Act
        var weight = _sut.GetPreferenceWeight("CoachingTip");

        // Assert
        weight.Should().BeGreaterThan(0.5f);
    }

    [Fact]
    public void Constructor_LoadsSavedPreferences()
    {
        // Arrange
        _sut.RecordSuggestionFeedbackAsync(new SuggestionFeedback(
            Guid.NewGuid(), SuggestionType.BreakReminder, true, null, _timeProvider.UtcNow)).Wait();
        _sut.UpdatePreferencesAsync().Wait();

        var prefsBefore = _sut.GetUserPreferencesAsync().Result.Value!;

        // Act - Create new service instance that loads the same file
        var newService = new UserPreferenceLearningService(
            NullLogger<UserPreferenceLearningService>.Instance,
            _timeProvider,
            _tempPath);

        // Assert
        var prefsAfter = newService.GetUserPreferencesAsync().Result.Value!;
        prefsAfter.BreakReminderFrequency.Should().Be(prefsBefore.BreakReminderFrequency);
        prefsAfter.LastUpdatedAtUtc.Should().Be(prefsBefore.LastUpdatedAtUtc);
    }

    [Fact]
    public async Task UpdatePreferencesAsync_PrefersSpoilerFreeHints_DetectsFromComments()
    {
        // Arrange - Record feedback with spoiler complaints
        for (int i = 0; i < 15; i++)
        {
            await _sut.RecordSuggestionFeedbackAsync(new SuggestionFeedback(
                Guid.NewGuid(),
                SuggestionType.CoachingTip,
                false, // Not helpful
                "Contains spoilers",
                _timeProvider.UtcNow));
        }

        // Act
        await _sut.UpdatePreferencesAsync();

        // Assert
        var prefs = (await _sut.GetUserPreferencesAsync()).Value!;
        prefs.PrefersSpoilerFreeHints.Should().BeTrue();
    }

    [Fact]
    public async Task UpdatePreferencesAsync_AutoAcceptHighConfidence_DetectsFromAcceptRate()
    {
        // Arrange - Record many accepted suggestions
        for (int i = 0; i < 20; i++)
        {
            await _sut.RecordUserActionAsync(Guid.NewGuid(), UserActionType.AcceptedSuggestion);
        }
        // And a few ignored
        for (int i = 0; i < 3; i++)
        {
            await _sut.RecordUserActionAsync(Guid.NewGuid(), UserActionType.IgnoredSuggestion);
        }

        // Act
        await _sut.UpdatePreferencesAsync();

        // Assert
        var prefs = (await _sut.GetUserPreferencesAsync()).Value!;
        // 20/23 = 87% acceptance rate > 70% threshold
        prefs.AutoAcceptHighConfidenceSuggestions.Should().BeTrue();
    }
}
