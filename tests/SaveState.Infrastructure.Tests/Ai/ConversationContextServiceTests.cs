using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SaveState.Core.Ai.Services;
using SaveState.Core.Configuration;
using SaveState.Infrastructure.Ai.Context;
using Xunit;

namespace SaveState.Infrastructure.Tests.Ai;

public class ConversationContextServiceTests
{
    private readonly InMemoryConversationContextService _sut;

    public ConversationContextServiceTests()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var options = Options.Create(new AiOptions { SessionTimeoutMinutes = 30 });
        _sut = new InMemoryConversationContextService(cache, options, NullLogger<InMemoryConversationContextService>.Instance);
    }

    [Fact]
    public async Task GetOrCreateContextAsync_WithNewSession_CreatesContext()
    {
        // Act
        var result = await _sut.GetOrCreateContextAsync("test-session-1");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.SessionId.Should().Be("test-session-1");
        result.Value.Messages.Should().BeEmpty();
    }

    [Fact]
    public async Task AddMessageAsync_WithValidMessage_StoresInHistory()
    {
        // Arrange
        var sessionId = "test-session-2";
        var message = new ChatMessage("user", "Hello, AI!");

        // Act
        var addResult = await _sut.AddMessageAsync(sessionId, message);
        var historyResult = await _sut.GetHistoryAsync(sessionId);

        // Assert
        addResult.IsSuccess.Should().BeTrue();
        historyResult.IsSuccess.Should().BeTrue();
        historyResult.Value.Should().HaveCount(1);
        historyResult.Value![0].Content.Should().Be("Hello, AI!");
    }

    [Fact]
    public async Task ClearSessionAsync_WithExistingSession_RemovesHistory()
    {
        // Arrange
        var sessionId = "clear-test";
        await _sut.AddMessageAsync(sessionId, new ChatMessage("user", "Test"));

        // Act
        var clearResult = await _sut.ClearSessionAsync(sessionId);
        var historyResult = await _sut.GetHistoryAsync(sessionId);

        // Assert
        clearResult.IsSuccess.Should().BeTrue();
        historyResult.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task GetActiveSessionCount_ReturnsCorrectCount()
    {
        // Arrange
        await _sut.GetOrCreateContextAsync("session-a");
        await _sut.GetOrCreateContextAsync("session-b");

        // Act
        var count = _sut.GetActiveSessionCount();

        // Assert
        count.Should().BeGreaterOrEqualTo(2);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task GetOrCreateContextAsync_WithInvalidSessionId_ReturnsFailure(string? sessionId)
    {
        // Act
        var result = await _sut.GetOrCreateContextAsync(sessionId!);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("empty");
    }
}
