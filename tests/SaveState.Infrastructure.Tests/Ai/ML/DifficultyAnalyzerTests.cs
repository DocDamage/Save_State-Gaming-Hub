using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SaveState.Core.AI.Assistant;
using SaveState.Core.Common;
using SaveState.Infrastructure.AI.ML;
using SaveState.Tests.Infrastructure;

namespace SaveState.Infrastructure.Tests.AI.ML;

public class DifficultyAnalyzerTests : IDisposable
{
    private readonly TestTimeProvider _timeProvider;
    private readonly DifficultyAnalyzer _sut;

    public DifficultyAnalyzerTests()
    {
        _timeProvider = new TestTimeProvider(new DateTime(2026, 2, 13, 12, 0, 0, DateTimeKind.Utc));
        _sut = new DifficultyAnalyzer(
            NullLogger<DifficultyAnalyzer>.Instance,
            _timeProvider,
            modelPath: null); // Use in-memory model
    }

    public void Dispose()
    {
        _sut.Dispose();
    }

    [Fact]
    public async Task AnalyzeAsync_WithFrustratedPlayerMetrics_SuggestsDecrease()
    {
        // Arrange
        var metrics = CreateFrustratedPlayerMetrics();

        // Act
        var result = await _sut.AnalyzeAsync(metrics);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.SuggestedDifficulty.Should().Be(SuggestedDifficulty.Decrease);
        result.Value.Confidence.Should().BeGreaterThan(0.6f);
        result.Value.ContributingFactors.Should().NotBeEmpty();
    }

    [Fact]
    public async Task AnalyzeAsync_WithSkilledPlayerMetrics_SuggestsIncrease()
    {
        // Arrange
        var metrics = CreateSkilledPlayerMetrics();

        // Act
        var result = await _sut.AnalyzeAsync(metrics);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.SuggestedDifficulty.Should().Be(SuggestedDifficulty.Increase);
        result.Value.Confidence.Should().BeGreaterThan(0.6f);
    }

    [Fact]
    public async Task AnalyzeAsync_WithAveragePlayerMetrics_SuggestsMaintain()
    {
        // Arrange
        var metrics = CreateAveragePlayerMetrics();

        // Act
        var result = await _sut.AnalyzeAsync(metrics);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.SuggestedDifficulty.Should().Be(SuggestedDifficulty.Maintain);
    }

    [Fact]
    public async Task GetModelMetricsAsync_WhenModelNotLoaded_ReturnsNotFound()
    {
        // Act
        var result = await _sut.GetModelMetricsAsync();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task RecordFeedbackAsync_WithValidSession_ReturnsSuccess()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var metrics = CreateFrustratedPlayerMetrics();
        metrics = metrics with { SessionId = sessionId };
        
        // First analyze to create a suggestion record
        await _sut.AnalyzeAsync(metrics);

        // Act
        var result = await _sut.RecordFeedbackAsync(sessionId, true);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task RecordFeedbackAsync_WithUnknownSession_ReturnsNotFound()
    {
        // Act
        var result = await _sut.RecordFeedbackAsync(Guid.NewGuid(), true);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Act & Assert
        _sut.Dispose();
        var act = () => _sut.Dispose();
        act.Should().NotThrow();
    }

    [Fact]
    public async Task AnalyzeAsync_AfterDisposed_ReturnsFailure()
    {
        // Arrange
        _sut.Dispose();
        var metrics = CreateAveragePlayerMetrics();

        // Act
        var result = await _sut.AnalyzeAsync(metrics);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task AnalyzeAsync_HighDeathCount_ContributesToDecreaseSuggestion()
    {
        // Arrange
        var metrics = CreateAveragePlayerMetrics() with { DeathCount = 15 };

        // Act
        var result = await _sut.AnalyzeAsync(metrics);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.FrustrationProbability.Should().BeGreaterThan(0.3f);
        result.Value.ContributingFactors.Should().Contain(f => f.Contains("deaths"));
    }

    [Fact]
    public async Task AnalyzeAsync_HighRetryCount_ContributesToDecreaseSuggestion()
    {
        // Arrange
        var metrics = CreateAveragePlayerMetrics() with { RetryCount = 12 };

        // Act
        var result = await _sut.AnalyzeAsync(metrics);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.FrustrationProbability.Should().BeGreaterThan(0.2f);
        result.Value.ContributingFactors.Should().Contain(f => f.Contains("retries"));
    }

    [Fact]
    public async Task AnalyzeAsync_HighErrorRate_ContributesToDecreaseSuggestion()
    {
        // Arrange
        var metrics = CreateAveragePlayerMetrics() with { InputErrorRate = 0.45f };

        // Act
        var result = await _sut.AnalyzeAsync(metrics);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.FrustrationProbability.Should().BeGreaterThan(0.15f);
    }

    [Fact]
    public async Task AnalyzeAsync_LowDeathsAndRetries_SuggestsIncreaseOrMaintain()
    {
        // Arrange
        var metrics = CreateAveragePlayerMetrics() with 
        { 
            DeathCount = 0,
            RetryCount = 0,
            InputErrorRate = 0.02f
        };

        // Act
        var result = await _sut.AnalyzeAsync(metrics);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.MasteryProbability.Should().BeGreaterThan(0.5f);
    }

    private PlayerBehaviorMetrics CreateFrustratedPlayerMetrics()
    {
        return new PlayerBehaviorMetrics
        {
            SessionId = Guid.NewGuid(),
            GameId = Guid.NewGuid(),
            SessionStartTimeUtc = _timeProvider.UtcNow.AddHours(-1),
            TimestampUtc = _timeProvider.UtcNow,
            DeathCount = 12,
            RetryCount = 10,
            TimeInCurrentSection = TimeSpan.FromMinutes(25),
            TotalSessionDuration = TimeSpan.FromMinutes(45),
            ActionsPerMinute = 35f,
            InputErrorRate = 0.35f,
            HasRapidInputBursts = true,
            HasIdleSpikes = true,
            PauseCount = 5,
            TotalPausedTime = TimeSpan.FromMinutes(10),
            CurrentDifficultyLevel = null
        };
    }

    private PlayerBehaviorMetrics CreateSkilledPlayerMetrics()
    {
        return new PlayerBehaviorMetrics
        {
            SessionId = Guid.NewGuid(),
            GameId = Guid.NewGuid(),
            SessionStartTimeUtc = _timeProvider.UtcNow.AddMinutes(-30),
            TimestampUtc = _timeProvider.UtcNow,
            DeathCount = 1,
            RetryCount = 1,
            TimeInCurrentSection = TimeSpan.FromMinutes(5),
            TotalSessionDuration = TimeSpan.FromMinutes(30),
            ActionsPerMinute = 75f,
            InputErrorRate = 0.05f,
            HasRapidInputBursts = false,
            HasIdleSpikes = false,
            PauseCount = 1,
            TotalPausedTime = TimeSpan.FromMinutes(2),
            CurrentDifficultyLevel = null
        };
    }

    private PlayerBehaviorMetrics CreateAveragePlayerMetrics()
    {
        return new PlayerBehaviorMetrics
        {
            SessionId = Guid.NewGuid(),
            GameId = Guid.NewGuid(),
            SessionStartTimeUtc = _timeProvider.UtcNow.AddMinutes(-45),
            TimestampUtc = _timeProvider.UtcNow,
            DeathCount = 4,
            RetryCount = 3,
            TimeInCurrentSection = TimeSpan.FromMinutes(10),
            TotalSessionDuration = TimeSpan.FromMinutes(40),
            ActionsPerMinute = 50f,
            InputErrorRate = 0.15f,
            HasRapidInputBursts = false,
            HasIdleSpikes = false,
            PauseCount = 2,
            TotalPausedTime = TimeSpan.FromMinutes(5),
            CurrentDifficultyLevel = null
        };
    }
}
