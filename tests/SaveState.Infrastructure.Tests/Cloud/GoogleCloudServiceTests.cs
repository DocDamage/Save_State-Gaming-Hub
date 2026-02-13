using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Infrastructure.Cloud;
using Xunit;

namespace SaveState.Infrastructure.Tests.Cloud;

/// <summary>
/// Unit tests for GoogleCloudService using offline JSON snapshots.
/// PHASE 1: Core Services - Google Cloud Integration Tests.
/// </summary>
public class GoogleCloudServiceTests : IDisposable
{
    private readonly Mock<ILogger<GoogleCloudService>> _loggerMock;
    private readonly Mock<IRateLimiter> _rateLimiterMock;
    private readonly IMemoryCache _cache;
    private readonly Mock<HttpMessageHandler> _httpHandlerMock;
    private readonly HttpClient _httpClient;
    private readonly GoogleCloudService _service;

    private const string TestApiKey = "test-api-key";
    private const string TestProjectId = "test-project-id";

    public GoogleCloudServiceTests()
    {
        _loggerMock = new Mock<ILogger<GoogleCloudService>>();
        _rateLimiterMock = new Mock<IRateLimiter>();
        _cache = new MemoryCache(new MemoryCacheOptions());
        _httpHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpHandlerMock.Object);

        // Default: Allow all rate limit operations
        _rateLimiterMock.Setup(r => r.IsAllowedAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _rateLimiterMock.Setup(r => r.RecordOperationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _service = new GoogleCloudService(
            _httpClient,
            _loggerMock.Object,
            _rateLimiterMock.Object,
            _cache,
            TestApiKey,
            TestProjectId);
    }

    public void Dispose()
    {
        _httpClient.Dispose();
        _cache.Dispose();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullApiKey_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new GoogleCloudService(
            _httpClient,
            _loggerMock.Object,
            _rateLimiterMock.Object,
            _cache,
            null!,
            TestProjectId);

        act.Should().Throw<ArgumentNullException>().WithParameterName("apiKey");
    }

    [Fact]
    public void Constructor_WithNullProjectId_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new GoogleCloudService(
            _httpClient,
            _loggerMock.Object,
            _rateLimiterMock.Object,
            _cache,
            TestApiKey,
            null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("projectId");
    }

    #endregion

    #region Speech Recognition Tests

    [Fact]
    public async Task RecognizeSpeechAsync_WithValidResponse_ParsesTranscriptAndConfidence()
    {
        // Arrange
        var speechResponseJson = await LoadTestDataAsync("google_speech_response.json");
        SetupHttpResponse(HttpStatusCode.OK, speechResponseJson);

        using var audioStream = new MemoryStream(Encoding.UTF8.GetBytes("fake audio data"));

        // Act
        var result = await _service.RecognizeSpeechAsync(audioStream, "en-US");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.RecognizedText.Should().Be("Welcome to the SaveState cloud catalog");
        result.Value.Confidence.Should().BeApproximately(0.87f, 0.01f);
        result.Value.LanguageCode.Should().Be("en-US");
        result.Value.IsFinal.Should().BeTrue();
    }

    [Fact]
    public async Task RecognizeSpeechAsync_WithEmptyResults_ReturnsEmptyTranscript()
    {
        // Arrange
        var emptyResponse = """{ "results": [] }""";
        SetupHttpResponse(HttpStatusCode.OK, emptyResponse);

        using var audioStream = new MemoryStream(Encoding.UTF8.GetBytes("fake audio data"));

        // Act
        var result = await _service.RecognizeSpeechAsync(audioStream, "en-US");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.RecognizedText.Should().BeEmpty();
        result.Value.Confidence.Should().Be(0f);
    }

    [Fact]
    public async Task RecognizeSpeechAsync_WithNullResults_ReturnsEmptyTranscript()
    {
        // Arrange
        var nullResultsResponse = """{ "results": null }""";
        SetupHttpResponse(HttpStatusCode.OK, nullResultsResponse);

        using var audioStream = new MemoryStream(Encoding.UTF8.GetBytes("fake audio data"));

        // Act
        var result = await _service.RecognizeSpeechAsync(audioStream, "en-US");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.RecognizedText.Should().BeEmpty();
    }

    [Fact]
    public async Task RecognizeSpeechAsync_WhenRateLimited_ReturnsRateLimitedError()
    {
        // Arrange
        _rateLimiterMock.Setup(r => r.IsAllowedAsync(TestProjectId, "GoogleCloudSpeech", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _rateLimiterMock.Setup(r => r.GetResetTimeAsync(TestProjectId, "GoogleCloudSpeech", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(DateTimeOffset.UtcNow.AddMinutes(5)));

        using var audioStream = new MemoryStream(Encoding.UTF8.GetBytes("fake audio data"));

        // Act
        var result = await _service.RecognizeSpeechAsync(audioStream, "en-US");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Rate limit exceeded");
    }

    [Fact]
    public async Task RecognizeSpeechAsync_WhenCancelled_ReturnsCancelledError()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        using var audioStream = new MemoryStream(Encoding.UTF8.GetBytes("fake audio data"));

        // Act
        var result = await _service.RecognizeSpeechAsync(audioStream, "en-US", cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("cancelled");
    }

    [Fact]
    public async Task RecognizeSpeechAsync_WithHttpError_ReturnsExternalError()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.InternalServerError, """{"error": "Internal Server Error"}""");

        using var audioStream = new MemoryStream(Encoding.UTF8.GetBytes("fake audio data"));

        // Act
        var result = await _service.RecognizeSpeechAsync(audioStream, "en-US");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Speech recognition failed");
    }

    [Fact]
    public async Task RecognizeSpeechAsync_RecordsOperationOnSuccess()
    {
        // Arrange
        var speechResponseJson = await LoadTestDataAsync("google_speech_response.json");
        SetupHttpResponse(HttpStatusCode.OK, speechResponseJson);

        using var audioStream = new MemoryStream(Encoding.UTF8.GetBytes("fake audio data"));

        // Act
        await _service.RecognizeSpeechAsync(audioStream, "en-US");

        // Assert
        _rateLimiterMock.Verify(r => r.RecordOperationAsync(
            TestProjectId,
            "GoogleCloudSpeech",
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Vision Analysis Tests

    [Fact]
    public async Task AnalyzeImageAsync_WithValidResponse_ParsesLabelsAndText()
    {
        // Arrange
        var visionResponseJson = await LoadTestDataAsync("google_vision_response.json");
        SetupHttpResponse(HttpStatusCode.OK, visionResponseJson);

        // Act
        var result = await _service.AnalyzeImageAsync("https://example.com/screenshot.png");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Labels.Should().Contain("Video game");
        result.Value.Labels.Should().Contain("Action game");
        result.Value.Labels.Should().Contain("Fighting game");
        result.Value.DetectedText.Should().Contain("ROUND 1");
        result.Value.DetectedText.Should().Contain("FIGHT!");
        result.Value.Objects.Should().Contain("Person");
    }

    [Fact]
    public async Task AnalyzeImageAsync_WithCachedResult_ReturnsCachedValue()
    {
        // Arrange
        var visionResponseJson = await LoadTestDataAsync("google_vision_response.json");
        SetupHttpResponse(HttpStatusCode.OK, visionResponseJson);

        var imageUri = "https://example.com/cached-image.png";

        // Act - First call
        var result1 = await _service.AnalyzeImageAsync(imageUri);
        // Act - Second call (should be cached)
        var result2 = await _service.AnalyzeImageAsync(imageUri);

        // Assert
        result1.IsSuccess.Should().BeTrue();
        result2.IsSuccess.Should().BeTrue();
        result1.Value.Should().BeEquivalentTo(result2.Value);

        // Verify HTTP was only called once (second request was cached)
        _httpHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task AnalyzeImageAsync_WhenRateLimited_ReturnsRateLimitedError()
    {
        // Arrange
        _rateLimiterMock.Setup(r => r.IsAllowedAsync(TestProjectId, "GoogleCloudVision", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _service.AnalyzeImageAsync("https://example.com/image.png");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Rate limit exceeded");
    }

    [Fact]
    public async Task AnalyzeImageAsync_WithEmptyResponse_ReturnsEmptyArrays()
    {
        // Arrange
        var emptyResponse = """{ "responses": [{}] }""";
        SetupHttpResponse(HttpStatusCode.OK, emptyResponse);

        // Act
        var result = await _service.AnalyzeImageAsync("https://example.com/blank.png");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Labels.Should().BeEmpty();
        result.Value.DetectedText.Should().BeEmpty();
        result.Value.Objects.Should().BeEmpty();
    }

    #endregion

    #region Translation Tests

    [Fact]
    public async Task TranslateTextAsync_WithValidResponse_ReturnsTranslatedText()
    {
        // Arrange
        var translationResponseJson = await LoadTestDataAsync("google_translation_response.json");
        SetupHttpResponse(HttpStatusCode.OK, translationResponseJson);

        // Act
        var result = await _service.TranslateTextAsync(
            "Welcome to the SaveState cloud catalog!",
            "es",
            "en");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("¡Bienvenido al catálogo de la nube de SaveState!");
    }

    [Fact]
    public async Task TranslateTextAsync_WithCachedResult_ReturnsCachedValue()
    {
        // Arrange
        var translationResponseJson = await LoadTestDataAsync("google_translation_response.json");
        SetupHttpResponse(HttpStatusCode.OK, translationResponseJson);

        var text = "Hello cached world";

        // Act - First call
        var result1 = await _service.TranslateTextAsync(text, "es", "en");
        // Act - Second call (should be cached)
        var result2 = await _service.TranslateTextAsync(text, "es", "en");

        // Assert
        result1.IsSuccess.Should().BeTrue();
        result2.IsSuccess.Should().BeTrue();

        // Verify HTTP was only called once
        _httpHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task TranslateTextAsync_WhenRateLimited_ReturnsRateLimitedError()
    {
        // Arrange
        _rateLimiterMock.Setup(r => r.IsAllowedAsync(TestProjectId, "GoogleCloudTranslation", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _service.TranslateTextAsync("Hello", "es", "en");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Rate limit exceeded");
    }

    [Fact]
    public async Task TranslateTextAsync_WithHttpError_ReturnsExternalError()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.BadRequest, """{"error": "Invalid request"}""");

        // Act
        var result = await _service.TranslateTextAsync("Hello", "invalid", "en");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Translation failed");
    }

    #endregion

    #region Storage Tests

    [Fact]
    public async Task UploadFileAsync_WithSuccessfulUpload_ReturnsGsUri()
    {
        // Arrange
        var uploadResponse = """{ "name": "test-file.txt", "bucket": "test-bucket" }""";
        SetupHttpResponse(HttpStatusCode.OK, uploadResponse);

        using var fileStream = new MemoryStream(Encoding.UTF8.GetBytes("file content"));

        // Act
        var result = await _service.UploadFileAsync("test-bucket", "test-file.txt", fileStream);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("gs://test-bucket/test-file.txt");
    }

    [Fact]
    public async Task UploadFileAsync_WhenRateLimited_ReturnsRateLimitedError()
    {
        // Arrange
        _rateLimiterMock.Setup(r => r.IsAllowedAsync(TestProjectId, "GoogleCloudStorage", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        using var fileStream = new MemoryStream(Encoding.UTF8.GetBytes("file content"));

        // Act
        var result = await _service.UploadFileAsync("test-bucket", "test.txt", fileStream);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Rate limit exceeded");
    }

    #endregion

    #region Audio Format Detection Tests

    [Fact]
    public async Task RecognizeSpeechAsync_WithWavAudio_UsesLinear16Encoding()
    {
        // Arrange - WAV file has RIFF header
        var wavBytes = new byte[] { 0x52, 0x49, 0x46, 0x46 }; // "RIFF"
        var speechResponseJson = await LoadTestDataAsync("google_speech_response.json");

        HttpRequestMessage? capturedRequest = null;
        _httpHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(speechResponseJson, Encoding.UTF8, "application/json")
            });

        using var audioStream = new MemoryStream(wavBytes);

        // Act
        await _service.RecognizeSpeechAsync(audioStream, "en-US");

        // Assert
        capturedRequest.Should().NotBeNull();
        var content = await capturedRequest!.Content!.ReadAsStringAsync();
        content.Should().Contain("\"encoding\":\"LINEAR16\"");
    }

    [Fact]
    public async Task RecognizeSpeechAsync_WithMp3Audio_UsesMp3Encoding()
    {
        // Arrange - MP3 file has 0xFF 0xFB or 0xFF 0xE0+ header
        var mp3Bytes = new byte[] { 0xFF, 0xFB, 0x00, 0x00 };
        var speechResponseJson = await LoadTestDataAsync("google_speech_response.json");

        HttpRequestMessage? capturedRequest = null;
        _httpHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(speechResponseJson, Encoding.UTF8, "application/json")
            });

        using var audioStream = new MemoryStream(mp3Bytes);

        // Act
        await _service.RecognizeSpeechAsync(audioStream, "en-US");

        // Assert
        capturedRequest.Should().NotBeNull();
        var content = await capturedRequest!.Content!.ReadAsStringAsync();
        content.Should().Contain("\"encoding\":\"MP3\"");
    }

    [Fact]
    public async Task RecognizeSpeechAsync_WithFlacAudio_UsesFlacEncoding()
    {
        // Arrange - FLAC file has "fLaC" header
        var flacBytes = new byte[] { 0x66, 0x4C, 0x61, 0x43 };
        var speechResponseJson = await LoadTestDataAsync("google_speech_response.json");

        HttpRequestMessage? capturedRequest = null;
        _httpHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(speechResponseJson, Encoding.UTF8, "application/json")
            });

        using var audioStream = new MemoryStream(flacBytes);

        // Act
        await _service.RecognizeSpeechAsync(audioStream, "en-US");

        // Assert
        capturedRequest.Should().NotBeNull();
        var content = await capturedRequest!.Content!.ReadAsStringAsync();
        content.Should().Contain("\"encoding\":\"FLAC\"");
    }

    [Fact]
    public async Task RecognizeSpeechAsync_WithOggAudio_UsesOggOpusEncoding()
    {
        // Arrange - OGG file has "OggS" header
        var oggBytes = new byte[] { 0x4F, 0x67, 0x67, 0x53 };
        var speechResponseJson = await LoadTestDataAsync("google_speech_response.json");

        HttpRequestMessage? capturedRequest = null;
        _httpHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(speechResponseJson, Encoding.UTF8, "application/json")
            });

        using var audioStream = new MemoryStream(oggBytes);

        // Act
        await _service.RecognizeSpeechAsync(audioStream, "en-US");

        // Assert
        capturedRequest.Should().NotBeNull();
        var content = await capturedRequest!.Content!.ReadAsStringAsync();
        content.Should().Contain("\"encoding\":\"OGG_OPUS\"");
    }

    #endregion

    #region Retry Logic Tests

    [Fact]
    public async Task RecognizeSpeechAsync_WithTransientFailure_RetriesAndSucceeds()
    {
        // Arrange
        var speechResponseJson = await LoadTestDataAsync("google_speech_response.json");
        var callCount = 0;

        _httpHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                callCount++;
                if (callCount < 2)
                {
                    return new HttpResponseMessage(HttpStatusCode.TooManyRequests);
                }
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(speechResponseJson, Encoding.UTF8, "application/json")
                };
            });

        using var audioStream = new MemoryStream(Encoding.UTF8.GetBytes("fake audio data"));

        // Act
        var result = await _service.RecognizeSpeechAsync(audioStream, "en-US");

        // Assert
        result.IsSuccess.Should().BeTrue();
        callCount.Should().Be(2); // First attempt failed, second succeeded
    }

    #endregion

    #region Helper Methods

    private static async Task<string> LoadTestDataAsync(string fileName)
    {
        // Navigate from the test execution directory to the test data folder
        var basePath = AppContext.BaseDirectory;
        var testDataPath = Path.Combine(basePath, "..", "..", "..", "..", "data", fileName);

        if (!File.Exists(testDataPath))
        {
            // Fallback to looking in common test data locations
            testDataPath = Path.Combine(basePath, "data", fileName);
        }

        if (!File.Exists(testDataPath))
        {
            // Create a default response for CI environments
            return fileName switch
            {
                "google_speech_response.json" => """
                    {
                      "results": [
                        {
                          "alternatives": [
                            { "transcript": "Welcome to the SaveState cloud catalog", "confidence": 0.87 }
                          ],
                          "isFinal": true
                        }
                      ]
                    }
                    """,
                "google_vision_response.json" => """
                    {
                      "responses": [
                        {
                          "labelAnnotations": [
                            { "description": "Video game", "score": 0.95 },
                            { "description": "Action game", "score": 0.89 },
                            { "description": "Fighting game", "score": 0.85 }
                          ],
                          "textAnnotations": [
                            { "description": "ROUND 1\nFIGHT!" }
                          ],
                          "fullTextAnnotation": { "text": "ROUND 1\nFIGHT!\nP1: 100\nP2: 100" },
                          "localizedObjectAnnotations": [
                            { "name": "Person", "score": 0.92 }
                          ]
                        }
                      ]
                    }
                    """,
                "google_translation_response.json" => """
                    {
                      "data": {
                        "translations": [
                          { "translatedText": "¡Bienvenido al catálogo de la nube de SaveState!", "detectedSourceLanguage": "en" }
                        ]
                      }
                    }
                    """,
                _ => throw new FileNotFoundException($"Test data file not found: {fileName}")
            };
        }

        return await File.ReadAllTextAsync(testDataPath);
    }

    private void SetupHttpResponse(HttpStatusCode statusCode, string content)
    {
        _httpHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(content, Encoding.UTF8, "application/json")
            });
    }

    #endregion
}
