using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SaveState.Application.GameLibrary.Commands;
using SaveState.Core.GameLibrary.Services;
using Xunit;

namespace SaveState.Application.Tests.GameLibrary.Commands;

public class MemoryCommandTests
{
    private readonly Mock<IGameMemoryReader> _memoryReaderMock;
    private readonly Mock<ILogger<AttachMemoryReaderCommandHandler>> _attachLoggerMock;
    private readonly Mock<ILogger<DetachMemoryReaderCommandHandler>> _detachLoggerMock;

    public MemoryCommandTests()
    {
        _memoryReaderMock = new Mock<IGameMemoryReader>();
        _attachLoggerMock = new Mock<ILogger<AttachMemoryReaderCommandHandler>>();
        _detachLoggerMock = new Mock<ILogger<DetachMemoryReaderCommandHandler>>();
    }

    [Fact]
    public async Task AttachMemoryReaderCommandHandler_ValidProcessId_Succeeds()
    {
        // Arrange
        const int processId = 1234;
        _memoryReaderMock
            .Setup(x => x.AttachToProcessAsync(processId, default))
            .ReturnsAsync(Result.Success());

        var handler = new AttachMemoryReaderCommandHandler(_memoryReaderMock.Object, _attachLoggerMock.Object);
        var command = new AttachMemoryReaderCommand { ProcessId = processId };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _memoryReaderMock.Verify(x => x.AttachToProcessAsync(processId, default), Times.Once);
    }

    [Fact]
    public async Task AttachMemoryReaderCommandHandler_ProcessNotFound_Fails()
    {
        // Arrange
        const int processId = 9999;
        const string errorMessage = "Process 9999 not found";
        _memoryReaderMock
            .Setup(x => x.AttachToProcessAsync(processId, default))
            .ReturnsAsync(Result.Failure(errorMessage));

        var handler = new AttachMemoryReaderCommandHandler(_memoryReaderMock.Object, _attachLoggerMock.Object);
        var command = new AttachMemoryReaderCommand { ProcessId = processId };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(errorMessage);
    }

    [Fact]
    public async Task DetachMemoryReaderCommandHandler_WhenAttached_Succeeds()
    {
        // Arrange
        _memoryReaderMock
            .Setup(x => x.DetachAsync(default))
            .ReturnsAsync(Result.Success());

        var handler = new DetachMemoryReaderCommandHandler(_memoryReaderMock.Object, _detachLoggerMock.Object);
        var command = new DetachMemoryReaderCommand();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _memoryReaderMock.Verify(x => x.DetachAsync(default), Times.Once);
    }

    [Fact]
    public async Task DetachMemoryReaderCommandHandler_ErrorDuringDetach_Fails()
    {
        // Arrange
        const string errorMessage = "Failed to detach from process";
        _memoryReaderMock
            .Setup(x => x.DetachAsync(default))
            .ReturnsAsync(Result.Failure(errorMessage));

        var handler = new DetachMemoryReaderCommandHandler(_memoryReaderMock.Object, _detachLoggerMock.Object);
        var command = new DetachMemoryReaderCommand();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(errorMessage);
    }
}