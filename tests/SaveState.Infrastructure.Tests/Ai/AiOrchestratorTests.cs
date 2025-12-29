using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SaveState.Core.Ai.Services;
using SaveState.Core.Common;
using SaveState.Core.Configuration;
using SaveState.Infrastructure.Ai;
using Xunit;

namespace SaveState.Infrastructure.Tests.Ai;

public class AiOrchestratorTests
{
    private readonly Mock<IMemoryCache> _cacheMock = new();
    private readonly Mock<IOptions<AiOptions>> _optionsMock = new();
    private readonly Mock<ILogger<AiOrchestrator>> _loggerMock = new();
    private readonly List<ILlmProvider> _providers;
    private readonly AiOrchestrator _sut;

    public AiOrchestratorTests()
    {
        _optionsMock.Setup(o => o.Value).Returns(new AiOptions());

        _providers = new List<ILlmProvider>
        {
            Mock.Of<ILlmProvider>(p => p.ProviderName == "OpenAI" && p.IsAvailable),
            Mock.Of<ILlmProvider>(p => p.ProviderName == "Groq" && p.IsAvailable)
        };

        _sut = new AiOrchestrator(
            _providers,
            _cacheMock.Object,
            _optionsMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task ProcessRequestAsync_WithCacheHit_ReturnsCachedResponse()
    {
        // Arrange
        var request = new AiRequest(AiRequestType.Completion, Prompt: "Test prompt");
        var cachedResponse = new AiResponse("Cached response", "stop", new TokenUsage(10, 5, 15), "gpt-4", "OpenAI", true);

        _cacheMock.Setup(c => c.TryGetValue(It.IsAny<string>(), out cachedResponse))
            .Returns(true);

        // Act
        var result = await _sut.ProcessRequestAsync(request);

        // Assert
        result.Should().Be(cachedResponse);
        _cacheMock.Verify(c => c.TryGetValue(It.IsAny<string>(), out It.Ref<AiResponse>.IsAny), Times.Once);
    }

    [Fact]
    public async Task ProcessRequestAsync_WithCacheDisabled_SkipsCache()
    {
        // Arrange
        var request = new AiRequest(AiRequestType.Completion, Prompt: "Test prompt", AllowCache: false);
        var mockProvider = Mock.Get(_providers[0]);
        mockProvider.Setup(p => p.CompleteAsync(It.IsAny<CompletionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<CompletionResult>.Success(new CompletionResult("Response", "stop", new TokenUsage(10, 5, 15), "gpt-4")));

        // Act
        var result = await _sut.ProcessRequestAsync(request);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        _cacheMock.Verify(c => c.TryGetValue(It.IsAny<string>(), out It.Ref<AiResponse>.IsAny), Times.Never);
    }

    [Fact]
    public async Task ProcessRequestAsync_WithNoProvidersAvailable_ReturnsFailure()
    {
        // Arrange
        var orchestrator = new AiOrchestrator(
            Array.Empty<ILlmProvider>(),
            _cacheMock.Object,
            _optionsMock.Object,
            _loggerMock.Object);

        var request = new AiRequest(AiRequestType.Completion, Prompt: "Test prompt", AllowCache: false);

        // Act
        var result = await orchestrator.ProcessRequestAsync(request);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().Be("No AI providers available");
    }

    [Fact]
    public async Task ProcessRequestAsync_WithPreferredProvider_UsesCorrectProvider()
    {
        // Arrange
        var request = new AiRequest(AiRequestType.Completion, Prompt: "Test prompt", PreferredProvider: "Groq", AllowCache: false);
        var mockGroqProvider = Mock.Get(_providers[1]);
        mockGroqProvider.Setup(p => p.CompleteAsync(It.IsAny<CompletionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<CompletionResult>.Success(new CompletionResult("Groq response", "stop", new TokenUsage(8, 4, 12), "mixtral-8x7b")));

        // Act
        var result = await _sut.ProcessRequestAsync(request);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Provider.Should().Be("Groq");
        mockGroqProvider.Verify(p => p.CompleteAsync(It.IsAny<CompletionRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessRequestAsync_WithProviderFailure_FallsBackToNextProvider()
    {
        // Arrange
        var request = new AiRequest(AiRequestType.Completion, Prompt: "Test prompt", AllowCache: false);
        var mockOpenAiProvider = Mock.Get(_providers[0]);
        var mockGroqProvider = Mock.Get(_providers[1]);

        mockOpenAiProvider.Setup(p => p.CompleteAsync(It.IsAny<CompletionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<CompletionResult>.Failure("Provider failed", ErrorType.Internal));

        mockGroqProvider.Setup(p => p.CompleteAsync(It.IsAny<CompletionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<CompletionResult>.Success(new CompletionResult("Fallback response", "stop", new TokenUsage(8, 4, 12), "mixtral-8x7b")));

        // Act
        var result = await _sut.ProcessRequestAsync(request);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Content.Should().Be("Fallback response");
        result.Provider.Should().Be("Groq");
    }

    [Fact]
    public void GetAvailableProviders_ReturnsProviderNames()
    {
        // Act
        var providers = _sut.GetAvailableProviders();

        // Assert
        providers.Should().HaveCount(2);
        providers.Should().Contain("OpenAI");
        providers.Should().Contain("Groq");
    }

    [Fact]
    public async Task IsProviderHealthyAsync_WithAvailableProvider_ReturnsTrue()
    {
        // Arrange
        var mockProvider = Mock.Get(_providers[0]);
        mockProvider.Setup(p => p.IsAvailable).Returns(true);

        // Act
        var result = await _sut.IsProviderHealthyAsync("OpenAI", default);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsProviderHealthyAsync_WithUnavailableProvider_ReturnsFalse()
    {
        // Arrange
        var mockProvider = Mock.Get(_providers[0]);
        mockProvider.Setup(p => p.IsAvailable).Returns(false);

        // Act
        var result = await _sut.IsProviderHealthyAsync("OpenAI", default);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsProviderHealthyAsync_WithUnknownProvider_ReturnsFalse()
    {
        // Act
        var result = await _sut.IsProviderHealthyAsync("UnknownProvider", default);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GetCacheStatistics_ReturnsCorrectStats()
    {
        // Act
        var stats = _sut.GetCacheStatistics();

        // Assert
        stats.Requests.Should().BeGreaterThanOrEqualTo(0);
        stats.Hits.Should().BeGreaterThanOrEqualTo(0);
        stats.HitRate.Should().BeInRange(0, 100);
    }

    [Fact]
    public async Task ProcessRequestAsync_WithChatRequest_UsesChatProvider()
    {
        // Arrange
        var messages = new[] { new ChatMessage("user", "Hello") };
        var request = new AiRequest(AiRequestType.Chat, Messages: messages, AllowCache: false);
        var mockProvider = Mock.Get(_providers[0]);
        mockProvider.Setup(p => p.ChatAsync(It.IsAny<ChatRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ChatResult>.Success(new ChatResult("Chat response", "stop", new TokenUsage(10, 5, 15), "gpt-4")));

        // Act
        var result = await _sut.ProcessRequestAsync(request);

        // Assert
        result.IsSuccessful.Should().BeTrue();
        result.Content.Should().Be("Chat response");
        result.Provider.Should().Be("OpenAI");
        mockProvider.Verify(p => p.ChatAsync(It.IsAny<ChatRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessRequestAsync_WithEmbeddingRequest_ThrowsNotImplemented()
    {
        // Arrange
        var request = new AiRequest(AiRequestType.Embedding, Prompt: "Test text", AllowCache: false);

        // Act & Assert
        await Assert.ThrowsAsync<NotImplementedException>(() =>
            _sut.ProcessRequestAsync(request));
    }

    [Fact]
    public async Task ProcessRequestAsync_WithNullRequest_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _sut.ProcessRequestAsync(null!));
    }

    [Fact]
    public async Task ProcessRequestAsync_WhenProviderFailsAfterRetries_ReturnsFailure()
    {
        // Arrange - Both providers fail
        var request = new AiRequest(AiRequestType.Completion, Prompt: "Test prompt", AllowCache: false);
        var mockOpenAiProvider = Mock.Get(_providers[0]);
        var mockGroqProvider = Mock.Get(_providers[1]);

        mockOpenAiProvider.Setup(p => p.CompleteAsync(It.IsAny<CompletionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<CompletionResult>.Failure("OpenAI failed", ErrorType.Internal));
        mockGroqProvider.Setup(p => p.CompleteAsync(It.IsAny<CompletionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<CompletionResult>.Failure("Groq failed", ErrorType.Internal));

        // Act
        var result = await _sut.ProcessRequestAsync(request);

        // Assert
        result.IsSuccessful.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ProcessRequestAsync_WithCustomModel_UsesSpecifiedModel()
    {
        // Arrange
        var request = new AiRequest(AiRequestType.Completion, Prompt: "Test prompt", Model: "custom-model", AllowCache: false);
        var mockProvider = Mock.Get(_providers[0]);
        mockProvider.Setup(p => p.CompleteAsync(It.IsAny<CompletionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<CompletionResult>.Success(new CompletionResult("Response", "stop", new TokenUsage(10, 5, 15), "custom-model")))
            .Callback<CompletionRequest, CancellationToken>((req, ct) =>
            {
                req.Model.Should().Be("custom-model");
            });

        // Act
        await _sut.ProcessRequestAsync(request);

        // Assert - Callback verifies the model was passed correctly
        mockProvider.Verify(p => p.CompleteAsync(It.IsAny<CompletionRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
