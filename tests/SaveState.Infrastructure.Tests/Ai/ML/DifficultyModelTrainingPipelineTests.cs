using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SaveState.Core.AI.Assistant;
using SaveState.Infrastructure.AI.ML;
using SaveState.Tests.Infrastructure;
using SuggestedDifficulty = SaveState.Core.Assistant.Services.SuggestedDifficulty;

namespace SaveState.Infrastructure.Tests.AI.ML;

public class DifficultyModelTrainingPipelineTests : IDisposable
{
    private readonly TestTimeProvider _timeProvider;
    private readonly DifficultyModelTrainingPipeline _sut;
    private readonly string _tempPath;

    public DifficultyModelTrainingPipelineTests()
    {
        _timeProvider = new TestTimeProvider(new DateTime(2026, 2, 13, 12, 0, 0, DateTimeKind.Utc));
        _tempPath = Path.Combine(Path.GetTempPath(), $"test_model_{Guid.NewGuid()}.zip");
        _sut = new DifficultyModelTrainingPipeline(
            NullLogger<DifficultyModelTrainingPipeline>.Instance,
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
    public void RecordSession_WithValidSession_ReturnsSuccess()
    {
        // Arrange
        var session = CreateGameplaySession();

        // Act
        var result = _sut.RecordSession(session);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void RecordSession_WithNullSession_ReturnsValidationFailure()
    {
        // Act
        var result = _sut.RecordSession(null!);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void RecordSession_WithIncompleteSession_ReturnsValidationFailure()
    {
        // Arrange
        var session = CreateGameplaySession() with { EndTimeUtc = default };

        // Act
        var result = _sut.RecordSession(session);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void RecordSessions_WithMultipleSessions_RecordsAll()
    {
        // Arrange
        var sessions = new[]
        {
            CreateGameplaySession(),
            CreateGameplaySession(),
            CreateGameplaySession()
        };

        // Act
        var result = _sut.RecordSessions(sessions);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var stats = _sut.GetStatistics();
        stats.TotalSessions.Should().Be(3);
    }

    [Fact]
    public void GenerateSyntheticTrainingData_WithValidCount_GeneratesSamples()
    {
        // Arrange
        const int sampleCount = 500;

        // Act
        var result = _sut.GenerateSyntheticTrainingData(sampleCount);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var stats = _sut.GetStatistics();
        stats.LabeledSamples.Should().Be(sampleCount);
    }

    [Fact]
    public void GenerateSyntheticTrainingData_WithInsufficientCount_ReturnsValidationFailure()
    {
        // Act
        var result = _sut.GenerateSyntheticTrainingData(50);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void TrainModel_WithInsufficientData_ReturnsValidationFailure()
    {
        // Act
        var result = _sut.TrainModel();

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void ExportTrainingData_WhenDataExists_CreatesFile()
    {
        // Arrange
        _sut.GenerateSyntheticTrainingData(100);
        var exportPath = Path.Combine(Path.GetTempPath(), $"export_{Guid.NewGuid()}.csv");

        try
        {
            // Act
            var result = _sut.ExportTrainingData(exportPath);

            // Assert
            result.IsSuccess.Should().BeTrue();
            File.Exists(exportPath).Should().BeTrue();
        }
        finally
        {
            if (File.Exists(exportPath))
            {
                File.Delete(exportPath);
            }
        }
    }

    [Fact]
    public void GetStatistics_AfterRecordingSessions_ReturnsCorrectCounts()
    {
        // Arrange
        _sut.GenerateSyntheticTrainingData(300);

        // Act
        var stats = _sut.GetStatistics();

        // Assert
        stats.LabeledSamples.Should().Be(300);
        stats.DecreaseSamples.Should().BeGreaterThan(0);
        stats.MaintainSamples.Should().BeGreaterThan(0);
        stats.IncreaseSamples.Should().BeGreaterThan(0);
        stats.TotalSessions.Should().Be(0); // Synthetic data doesn't create sessions
    }

    [Fact]
    public void ClearTrainingData_RemovesAllData()
    {
        // Arrange
        _sut.GenerateSyntheticTrainingData(100);
        _sut.GetStatistics().LabeledSamples.Should().Be(100);

        // Act
        _sut.ClearTrainingData();

        // Assert
        var stats = _sut.GetStatistics();
        stats.LabeledSamples.Should().Be(0);
        stats.TotalSessions.Should().Be(0);
    }

    [Fact]
    public void ImportTrainingData_WithValidFile_ImportsSuccessfully()
    {
        // Arrange
        var exportPath = Path.Combine(Path.GetTempPath(), $"export_{Guid.NewGuid()}.csv");
        _sut.GenerateSyntheticTrainingData(100);
        _sut.ExportTrainingData(exportPath);

        try
        {
            var newPipeline = new DifficultyModelTrainingPipeline(
                NullLogger<DifficultyModelTrainingPipeline>.Instance,
                _timeProvider);

            // Act
            var result = newPipeline.ImportTrainingData(exportPath);

            // Assert
            result.IsSuccess.Should().BeTrue();
            newPipeline.GetStatistics().LabeledSamples.Should().Be(100);
        }
        finally
        {
            if (File.Exists(exportPath))
            {
                File.Delete(exportPath);
            }
        }
    }

    [Fact]
    public void ImportTrainingData_WithMissingFile_ReturnsNotFound()
    {
        // Act
        var result = _sut.ImportTrainingData("nonexistent_file.csv");

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    private GameplaySessionRecord CreateGameplaySession()
    {
        var startTime = _timeProvider.UtcNow.AddHours(-1);
        return new GameplaySessionRecord
        {
            SessionId = Guid.NewGuid(),
            StartTimeUtc = startTime,
            EndTimeUtc = _timeProvider.UtcNow,
            DeathCount = 5,
            RetryCount = 3,
            TimeInCurrentSection = TimeSpan.FromMinutes(15),
            AverageActionsPerMinute = 50f,
            InputErrorRate = 0.15f,
            HasRapidInputBursts = false,
            HasIdleSpikes = false,
            PauseCount = 2,
            TotalPausedTime = TimeSpan.FromMinutes(5),
            ActualDifficultyAdjustment = SuggestedDifficulty.Maintain
        };
    }
}
