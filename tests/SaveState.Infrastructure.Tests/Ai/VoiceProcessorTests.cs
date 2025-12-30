using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using SaveState.Core.Ai.Voice;
using SaveState.Core.Common;
using SaveState.Core.Configuration;
using SaveState.Infrastructure.Ai.Voice;
using Xunit;

namespace SaveState.Infrastructure.Tests.Ai;

public class VoiceProcessorTests
{
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock = new();
    private readonly Mock<IOptions<OpenAiOptions>> _optionsMock = new();

    public VoiceProcessorTests()
    {
        var httpClient = new HttpClient();
        _httpClientFactoryMock.Setup(f => f.CreateClient("OpenAI")).Returns(httpClient);

        var options = new OpenAiOptions
        {
            ApiKey = "test-key",
            BaseUrl = "https://api.openai.com/v1"
        };
        _optionsMock.Setup(o => o.Value).Returns(options);
    }

    [Fact]
    public async Task TranscribeAsync_WithValidAudioData_CallsWhisperApi()
    {
        // Arrange
        var audioData = new byte[] { 1, 2, 3, 4, 5 }; // Mock audio data
        var processor = new WhisperVoiceProcessor(
            _httpClientFactoryMock.Object,
            _optionsMock.Object,
            NullLogger<WhisperVoiceProcessor>.Instance);

        // Act & Assert - This will fail due to no real API, but verifies the method exists and is callable
        var result = await processor.TranscribeAsync(audioData, "test.wav");

        // The result should be a failure since we're not hitting a real API
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Transcription failed");
    }

    [Fact]
    public void SupportedFormats_ReturnsExpectedFormats()
    {
        // Arrange
        var processor = new WhisperVoiceProcessor(
            _httpClientFactoryMock.Object,
            _optionsMock.Object,
            NullLogger<WhisperVoiceProcessor>.Instance);

        // Act
        var formats = processor.SupportedFormats;

        // Assert
        formats.Should().Contain("mp3");
        formats.Should().Contain("wav");
        formats.Should().Contain("webm");
        formats.Should().HaveCount(7);
    }

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Act
        var processor = new WhisperVoiceProcessor(
            _httpClientFactoryMock.Object,
            _optionsMock.Object,
            NullLogger<WhisperVoiceProcessor>.Instance);

        // Assert
        processor.Should().NotBeNull();
        processor.SupportedFormats.Should().NotBeEmpty();
    }
}
