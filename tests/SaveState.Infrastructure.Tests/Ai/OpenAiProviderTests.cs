using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Polly;
using System.Net;
using System.Text.Json;
using Xunit;
using SaveState.Core.Ai.Services;
using SaveState.Core.Common;
using SaveState.Core.Configuration;
using SaveState.Infrastructure.Ai.Providers;
using SaveState.Infrastructure.Ai.Resilience;
using System.Threading.Tasks;

namespace SaveState.Infrastructure.Tests.Ai;

public class OpenAiProviderTests
{
    private readonly Mock<HttpMessageHandler> _httpMessageHandler = new();
    private readonly Mock<IOptions<OpenAiOptions>> _optionsMock = new();
    private readonly Mock<IAiResiliencePolicy> _resiliencePolicyMock = new();
    private readonly Mock<SaveState.Core.Common.Services.IUserPreferencesService> _preferencesMock = new();
    private readonly Mock<ILogger<OpenAiProvider>> _loggerMock = new();
    private OpenAiProvider _sut;

    public OpenAiProviderTests()
    {
        var options = new OpenAiOptions
        {
            ApiKey = "test-api-key",
            BaseUrl = "https://api.openai.com/v1/"
        };
        _optionsMock.Setup(o => o.Value).Returns(options);

        var httpClient = new HttpClient(_httpMessageHandler.Object);
        var resiliencePolicy = Policy.WrapAsync(Policy.NoOpAsync(), Policy.NoOpAsync());
        _resiliencePolicyMock.Setup(r => r.GetPipelinePolicy("OpenAI")).Returns(resiliencePolicy);

        _sut = new OpenAiProvider(
            httpClient,
            _optionsMock.Object,
            _resiliencePolicyMock.Object,
            _preferencesMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public void Constructor_WithValidOptions_ConfiguresHttpClientCorrectly()
    {
        // Assert
        _sut.ProviderName.Should().Be("OpenAI");
        _sut.IsAvailable.Should().BeTrue();
        _sut.AvailableModels.Should().ContainKey("gpt-4");
        _sut.AvailableModels.Should().ContainKey("gpt-3.5-turbo");
    }

    [Fact]
    public void Constructor_WithMissingApiKey_IsNotAvailable()
    {
        // Arrange
        var options = new OpenAiOptions { BaseUrl = "https://api.openai.com/v1/" };
        _optionsMock.Setup(o => o.Value).Returns(options);

        var httpClient = new HttpClient(_httpMessageHandler.Object);
        var resiliencePolicy = Policy.WrapAsync(Policy.NoOpAsync(), Policy.NoOpAsync());

        // Act
        var provider = new OpenAiProvider(
            httpClient,
            _optionsMock.Object,
            _resiliencePolicyMock.Object,
            _preferencesMock.Object,
            _loggerMock.Object);

        // Assert
        provider.IsAvailable.Should().BeFalse();
    }

    [Fact]
    public async Task CompleteAsync_WithValidRequest_ReturnsCompletionResult()
    {
        // Arrange
        var responseContent = @"{
            ""choices"": [
                {
                    ""text"": ""Test response"",
                    ""finish_reason"": ""stop""
                }
            ],
            ""usage"": {
                ""prompt_tokens"": 10,
                ""completion_tokens"": 5,
                ""total_tokens"": 15
            },
            ""model"": ""gpt-3.5-turbo""
        }";

        _httpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseContent)
            });

        var request = new CompletionRequest("Test prompt", "gpt-3.5-turbo", 100);

        // Act
        var result = await _sut.CompleteAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Text.Should().Be("Test response");
        result.Value.FinishReason.Should().Be("stop");
        result.Value.Model.Should().Be("gpt-3.5-turbo");
        result.Value.Usage.PromptTokens.Should().Be(10);
        result.Value.Usage.CompletionTokens.Should().Be(5);
        result.Value.Usage.TotalTokens.Should().Be(15);
    }

    [Fact]
    public async Task CompleteAsync_WithApiError_ReturnsFailureResult()
    {
        // Arrange
        _httpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Unauthorized,
                Content = new StringContent("Invalid API key")
            });

        var request = new CompletionRequest("Test prompt", "gpt-3.5-turbo", 100);

        // Act
        var result = await _sut.CompleteAsync(request, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("API request failed");
    }

    [Fact]
    public async Task CompleteAsync_WithRateLimitError_RetriesAndSucceeds()
    {
        // Arrange - First call fails with rate limit, second succeeds
        var callCount = 0;
        var retryPolicy = Policy.Handle<HttpRequestException>(ex => ex.StatusCode == HttpStatusCode.TooManyRequests)
            .RetryAsync(1);
        var resiliencePolicy = Policy.WrapAsync(Policy.NoOpAsync(), retryPolicy);
        _resiliencePolicyMock.Setup(r => r.GetPipelinePolicy("OpenAI")).Returns(resiliencePolicy);

        // Re-create SUT with real retry policy
        var httpClient = new HttpClient(_httpMessageHandler.Object);
        _sut = new OpenAiProvider(httpClient, _optionsMock.Object, _resiliencePolicyMock.Object, _preferencesMock.Object, _loggerMock.Object);

        var responseContent = @"{
            ""model"": ""gpt-4"",
            ""choices"": [
                {
                    ""text"": ""Success after retry"",
                    ""finish_reason"": ""stop""
                }
            ],
            ""usage"": {
                ""prompt_tokens"": 5,
                ""completion_tokens"": 3,
                ""total_tokens"": 8
            }
        }";

        _httpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                callCount++;
                if (callCount == 1)
                {
                    return new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.TooManyRequests,
                        Content = new StringContent("Rate limit exceeded")
                    };
                }
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(responseContent)
                };
            });

        var request = new CompletionRequest("Test prompt", "gpt-4", 50);

        // Act
        var result = await _sut.CompleteAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue($"Result should be success. Call count: {callCount}. Error: {result.Error}");
        result.Value!.Text.Should().Be("Success after retry");
        callCount.Should().Be(2); // Should have made 2 calls due to retry
    }

    [Fact]
    public async Task CompleteAsync_WithInvalidJson_ReturnsFailure()
    {
        // Arrange
        _httpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("Invalid JSON")
            });

        var request = new CompletionRequest("Test prompt", "gpt-4", 100);

        // Act
        var result = await _sut.CompleteAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("OpenAI API request failed");
    }

    [Fact]
    public async Task CompleteAsync_WithNetworkError_ReturnsFailure()
    {
        // Arrange
        _httpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        var request = new CompletionRequest("Test prompt", "gpt-3.5-turbo", 100);

        // Act
        var result = await _sut.CompleteAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("OpenAI API request failed");
    }

    [Fact]
    public async Task CompleteAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        // Arrange
        _httpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.Is<CancellationToken>(t => t.IsCancellationRequested))
            .ThrowsAsync(new TaskCanceledException());

        var request = new CompletionRequest("Test prompt", "gpt-4", 100);

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(() =>
            _sut.CompleteAsync(request, new CancellationToken(true)));
    }

    [Fact]
    public void AvailableModels_ContainsExpectedModelsWithCorrectCosts()
    {
        // Assert
        _sut.AvailableModels.Should().HaveCount(2);

        _sut.AvailableModels.Should().ContainKey("gpt-4")
            .WhoseValue.Should().BeEquivalentTo(new
            {
                Name = "GPT-4",
                MaxTokens = 8192,
                CostPerToken = 0.00003m
            });

        _sut.AvailableModels.Should().ContainKey("gpt-3.5-turbo")
            .WhoseValue.Should().BeEquivalentTo(new
            {
                Name = "GPT-3.5 Turbo",
                MaxTokens = 4096,
                CostPerToken = 0.000002m
            });
    }

    [Fact]
    public async Task ChatAsync_WithValidRequest_ReturnsChatResult()
    {
        // Arrange
        var chatResponseContent = @"{
            ""choices"": [
                {
                    ""message"": {
                        ""content"": ""Chat response"",
                        ""role"": ""assistant""
                    },
                    ""finish_reason"": ""stop""
                }
            ],
            ""usage"": {
                ""prompt_tokens"": 8,
                ""completion_tokens"": 4,
                ""total_tokens"": 12
            }
        }";

        _httpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(chatResponseContent)
            });

        var messages = new[] { new ChatMessage("user", "Hello") };
        var request = new ChatRequest(messages, "gpt-4", 100);

        // Act
        var result = await _sut.ChatAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Content.Should().Be("Chat response");
        result.Value.FinishReason.Should().Be("stop");
        result.Value.Usage.TotalTokens.Should().Be(12);
    }
}
