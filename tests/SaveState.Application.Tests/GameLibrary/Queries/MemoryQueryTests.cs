using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SaveState.Application.GameLibrary.Queries;
using SaveState.Core.GameLibrary.Services;
using Xunit;

namespace SaveState.Application.Tests.GameLibrary.Queries;

public class MemoryQueryTests
{
    private readonly Mock<IGameMemoryReader> _memoryReaderMock;

    public MemoryQueryTests()
    {
        _memoryReaderMock = new Mock<IGameMemoryReader>();
    }

    [Fact]
    public async Task GetMemoryPatternsQueryHandler_AlreadyAttached_ReturnsPatterns()
    {
        // Arrange
        const int processId = 5678;
        var expectedPatterns = new List<MemoryPattern>
        {
            new("Health", new IntPtr(0x12345678), "int32", 100),
            new("Score", new IntPtr(0x87654321), "int32", 5000)
        };

        _memoryReaderMock.Setup(x => x.IsAttached).Returns(true);
        _memoryReaderMock
            .Setup(x => x.DetectPatternsAsync(default))
            .ReturnsAsync(Result<IReadOnlyList<MemoryPattern>>.Success(expectedPatterns));

        var handler = new GetMemoryPatternsQueryHandler(_memoryReaderMock.Object);
        var query = new GetMemoryPatternsQuery { ProcessId = processId };

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(expectedPatterns);
        _memoryReaderMock.Verify(x => x.AttachToProcessAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _memoryReaderMock.Verify(x => x.DetectPatternsAsync(default), Times.Once);
    }

    [Fact]
    public async Task GetMemoryPatternsQueryHandler_NotAttached_AttachesThenReturnsPatterns()
    {
        // Arrange
        const int processId = 5678;
        var expectedPatterns = new List<MemoryPattern>
        {
            new("Health", new IntPtr(0x12345678), "int32", 100)
        };

        _memoryReaderMock.Setup(x => x.IsAttached).Returns(false);
        _memoryReaderMock
            .Setup(x => x.AttachToProcessAsync(processId, default))
            .ReturnsAsync(Result.Success());
        _memoryReaderMock
            .Setup(x => x.DetectPatternsAsync(default))
            .ReturnsAsync(Result<IReadOnlyList<MemoryPattern>>.Success(expectedPatterns));

        var handler = new GetMemoryPatternsQueryHandler(_memoryReaderMock.Object);
        var query = new GetMemoryPatternsQuery { ProcessId = processId };

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(expectedPatterns);
        _memoryReaderMock.Verify(x => x.AttachToProcessAsync(processId, default), Times.Once);
        _memoryReaderMock.Verify(x => x.DetectPatternsAsync(default), Times.Once);
    }

    [Fact]
    public async Task GetMemoryPatternsQueryHandler_AttachFails_ReturnsFailure()
    {
        // Arrange
        const int processId = 9999;
        const string attachError = "Process not found";

        _memoryReaderMock.Setup(x => x.IsAttached).Returns(false);
        _memoryReaderMock
            .Setup(x => x.AttachToProcessAsync(processId, default))
            .ReturnsAsync(Result.Failure(attachError));

        var handler = new GetMemoryPatternsQueryHandler(_memoryReaderMock.Object);
        var query = new GetMemoryPatternsQuery { ProcessId = processId };

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(attachError);
        _memoryReaderMock.Verify(x => x.DetectPatternsAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetMemoryReaderStatusQueryHandler_ReturnsStatus()
    {
        // Arrange
        _memoryReaderMock.Setup(x => x.IsAttached).Returns(true);

        var handler = new GetMemoryReaderStatusQueryHandler(_memoryReaderMock.Object);
        var query = new GetMemoryReaderStatusQuery();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.IsAttached.Should().BeTrue();
        result.Value.Platform.Should().Be(Environment.OSVersion.Platform.ToString());
        result.Value.IsSupported.Should().BeTrue();
    }
}