using FluentAssertions;
using SaveState.Core.Ai.Assistant;
using SuggestedDifficulty = SaveState.Core.Assistant.Services.SuggestedDifficulty;
using SaveState.Infrastructure.Ai.ML;

namespace SaveState.Infrastructure.Tests.AI.ML;

public class DifficultyPredictionTests
{
    [Fact]
    public void GetConfidence_WithEmptyScores_ReturnsDefault()
    {
        // Arrange
        var prediction = new DifficultyPrediction
        {
            PredictedDifficulty = "Maintain",
            Scores = Array.Empty<float>()
        };

        // Act
        var confidence = prediction.GetConfidence();

        // Assert
        confidence.Should().Be(0.5f);
    }

    [Fact]
    public void GetConfidence_WithScores_ReturnsMaxProbability()
    {
        // Arrange
        var prediction = new DifficultyPrediction
        {
            PredictedDifficulty = "Decrease",
            Scores = new[] { 2.0f, 0.5f, 0.1f } // Decrease should have highest probability
        };

        // Act
        var confidence = prediction.GetConfidence();

        // Assert
        confidence.Should().BeGreaterThan(0.5f);
        confidence.Should().BeLessThan(1.0f);
    }

    [Fact]
    public void GetProbabilities_WithEmptyScores_ReturnsUniformDistribution()
    {
        // Arrange
        var prediction = new DifficultyPrediction
        {
            PredictedDifficulty = "Maintain",
            Scores = Array.Empty<float>()
        };

        // Act
        var probabilities = prediction.GetProbabilities();

        // Assert
        probabilities.Should().HaveCount(3);
        probabilities[SuggestedDifficulty.Decrease].Should().BeApproximately(0.33f, 0.01f);
        probabilities[SuggestedDifficulty.Maintain].Should().BeApproximately(0.34f, 0.01f);
        probabilities[SuggestedDifficulty.Increase].Should().BeApproximately(0.33f, 0.01f);
    }

    [Fact]
    public void GetProbabilities_WithScores_ReturnsProbabilitiesThatSumToOne()
    {
        // Arrange
        var prediction = new DifficultyPrediction
        {
            PredictedDifficulty = "Decrease",
            Scores = new[] { 1.5f, 0.8f, 0.3f }
        };

        // Act
        var probabilities = prediction.GetProbabilities();

        // Assert
        var sum = probabilities.Values.Sum();
        sum.Should().BeApproximately(1.0f, 0.01f);
        probabilities[SuggestedDifficulty.Decrease].Should().BeGreaterThan(probabilities[SuggestedDifficulty.Maintain]);
        probabilities[SuggestedDifficulty.Decrease].Should().BeGreaterThan(probabilities[SuggestedDifficulty.Increase]);
    }

    [Fact]
    public void DifficultyTrainingData_DefaultValues_AreSet()
    {
        // Arrange & Act
        var data = new DifficultyTrainingData();

        // Assert
        data.Label.Should().Be("Maintain");
        data.DeathCount.Should().Be(0);
        data.RetryCount.Should().Be(0);
        data.ActionsPerMinute.Should().Be(0f);
        data.InputErrorRate.Should().Be(0f);
    }

    [Fact]
    public void DifficultyTrainingData_SetValues_AreStored()
    {
        // Arrange & Act
        var data = new DifficultyTrainingData
        {
            Label = "Decrease",
            DeathCount = 10,
            RetryCount = 5,
            ActionsPerMinute = 60f,
            InputErrorRate = 0.25f,
            TotalSessionDurationMinutes = 45f,
            TimeInCurrentSectionMinutes = 20f,
            HasRapidInputBursts = true,
            HasIdleSpikes = false,
            PauseCount = 3,
            TotalPausedTimeMinutes = 10f
        };

        // Assert
        data.Label.Should().Be("Decrease");
        data.DeathCount.Should().Be(10);
        data.RetryCount.Should().Be(5);
        data.ActionsPerMinute.Should().Be(60f);
        data.InputErrorRate.Should().Be(0.25f);
        data.HasRapidInputBursts.Should().BeTrue();
        data.HasIdleSpikes.Should().BeFalse();
    }
}
