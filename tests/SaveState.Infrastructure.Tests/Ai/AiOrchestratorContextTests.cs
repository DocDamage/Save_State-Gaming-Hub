using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using SaveState.Core.Ai.Context;
using SaveState.Core.Ai.Services;
using SaveState.Core.Common;
using SaveState.Core.Configuration;
using SaveState.Core.Monitoring;
using SaveState.Infrastructure.Ai;
using SaveState.Infrastructure.Ai.Context;
using Xunit;

namespace SaveState.Infrastructure.Tests.Ai;

public class AiOrchestratorContextTests
{
    private readonly Mock<ILlmProvider> _providerMock;
    private readonly Mock<ICacheService> _cacheMock;
    private readonly Mock<IApplicationMetrics> _metricsMock;
    private readonly Mock<ICachePerformanceMonitor> _cacheMonitorMock;
    private readonly InMemoryConversationContextService _contextService;
    private readonly AiOrchestrator _sut;

    public AiOrchestratorContextTests()
    {
        _providerMock = new Mock<ILlmProvider>();
        _providerMock.Setup(p => p.IsAvailable).Returns(true);
        _providerMock.Setup(p => p.ProviderName).Returns("TestProvider");

        _cacheMock = new Mock<ICacheService>();
        _metricsMock = new Mock<IApplicationMetrics>();
        _cacheMonitorMock = new Mock<ICachePerformanceMonitor>();

        var cache = new MemoryCache(new MemoryCacheOptions());
        var aiOptions = Options.Create(new AiOptions { SessionTimeoutMinutes = 30 });
        _contextService = new InMemoryConversationContextService(
            cache, aiOptions, NullLogger<InMemoryConversationContextService>.Instance);

        _sut = new AiOrchestrator(
            new[] { _providerMock.Object },
            _cacheMock.Object,
            aiOptions,
            NullLogger<AiOrchestrator>.Instance,
            _metricsMock.Object,
            _cacheMonitorMock.Object,
            _contextService);
    }

    [Fact]
    public async Task ProcessRequestWithContextAsync_MaintainsHistory()
    {
        // Arrange
        var sessionId = "test-session";
        _providerMock
            .Setup(p => p.ChatAsync(It.IsAny<ChatRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ChatResult>.Success(
                new ChatResult("Hello!", "stop", new TokenUsage(10, 5, 15), "gpt-4")));

        var request1 = new AiRequest(AiRequestType.Chat, "Hi there!");
        var request2 = new AiRequest(AiRequestType.Chat, "What did I just say?");

        // Act
        var response1 = await _sut.ProcessRequestWithContextAsync(sessionId, request1);
        var response2 = await _sut.ProcessRequestWithContextAsync(sessionId, request2);

        // Assert
        response1.IsSuccessful.Should().BeTrue();
        response2.IsSuccessful.Should().BeTrue();

        var history = await _contextService.GetHistoryAsync(sessionId);
        history.Value.Should().HaveCount(4); // 2 user + 2 assistant messages
    }

    [Fact]
    public async Task ClearConversationAsync_RemovesHistory()
    {
        // Arrange
        var sessionId = "clear-test";
        await _contextService.AddMessageAsync(sessionId, new ChatMessage("user", "Test"));

        // Act
        var cleared = await _sut.ClearConversationAsync(sessionId);
        var history = await _contextService.GetHistoryAsync(sessionId);

        // Assert
        cleared.Should().BeTrue();
        history.Value.Should().BeEmpty();
    }
}
