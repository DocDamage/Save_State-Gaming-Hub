using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SaveState.Core.Common.Services;
using SaveState.Core.InputRecording;
using SaveState.Infrastructure.InputRecording.Services;
using SaveState.Infrastructure.Persistence;

namespace SaveState.Infrastructure.Tests.Input;

public class InputRecordingServiceFacadeTests : IDisposable
{
    private readonly List<string> _createdFiles = new();

    [Fact]
    public async Task StartRecordingAsync_ThenGetActiveRecordingAsync_ReturnsSession()
    {
        await using var dbContext = CreateDbContext();
        var now = new DateTime(2026, 2, 19, 12, 0, 0, DateTimeKind.Utc);
        var timeProviderMock = new Mock<ITimeProvider>();
        timeProviderMock.SetupGet(tp => tp.UtcNow).Returns(now);

        var sut = new InputRecordingService(
            dbContext,
            NullLogger<InputRecordingService>.Instance,
            timeProviderMock.Object);
        var gameId = Guid.NewGuid();

        var startResult = await sut.StartRecordingAsync(new StartRecordingRequest { GameId = gameId });
        var activeResult = await sut.GetActiveRecordingAsync(gameId);

        startResult.IsSuccess.Should().BeTrue();
        activeResult.IsSuccess.Should().BeTrue();
        activeResult.Value!.Id.Should().Be(startResult.Value!.Id);
        startResult.Value.StartedAt.Should().Be(now);
    }

    [Fact]
    public async Task StopRecordingAsync_AfterSingleFrame_PersistsReadyRecording()
    {
        await using var dbContext = CreateDbContext();
        var startedAt = new DateTime(2026, 2, 19, 12, 0, 0, DateTimeKind.Utc);
        var stoppedAt = startedAt.AddSeconds(90);
        var timeProviderMock = new Mock<ITimeProvider>();
        timeProviderMock.SetupSequence(tp => tp.UtcNow)
            .Returns(startedAt)
            .Returns(stoppedAt);

        var sut = new InputRecordingService(
            dbContext,
            NullLogger<InputRecordingService>.Instance,
            timeProviderMock.Object);
        var startResult = await sut.StartRecordingAsync(new StartRecordingRequest { GameId = Guid.NewGuid() });
        var sessionId = startResult.Value!.Id;

        await sut.RecordFrameAsync(sessionId, new InputFrame
        {
            PressedInputs = new List<string> { "A", "B" }
        });

        var stopResult = await sut.StopRecordingAsync(sessionId);

        stopResult.IsSuccess.Should().BeTrue();
        stopResult.Value.Should().NotBeNull();
        stopResult.Value!.Status.Should().Be(RecordingStatus.Ready);
        stopResult.Value.TotalFrames.Should().Be(1);
        stopResult.Value.Duration.Should().Be(stoppedAt - startedAt);
        stopResult.Value.Name.Should().Be($"Recording_{stoppedAt:yyyyMMdd_HHmmss}");
        File.Exists(stopResult.Value.FilePath).Should().BeTrue();
        _createdFiles.Add(stopResult.Value.FilePath);
    }

    public void Dispose()
    {
        foreach (var file in _createdFiles.Where(File.Exists))
        {
            File.Delete(file);
        }
    }

    private static SaveStateDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<SaveStateDbContext>()
            .UseInMemoryDatabase($"input-recording-tests-{Guid.NewGuid():N}")
            .Options;

        return new SaveStateDbContext(options);
    }
}
