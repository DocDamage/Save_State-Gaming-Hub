using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SaveState.Core.Ai.Voice;
using SaveState.Core.Common;
using SaveState.Core.Configuration;

namespace SaveState.Infrastructure.Ai.Voice;

public sealed class WhisperVoiceProcessor : IVoiceProcessor
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WhisperVoiceProcessor> _logger;
    private readonly OpenAiOptions _options;

    private static readonly string[] _supportedFormats =
        { "mp3", "mp4", "mpeg", "mpga", "m4a", "wav", "webm" };

    public IReadOnlyList<string> SupportedFormats => _supportedFormats;

    public WhisperVoiceProcessor(
        IHttpClientFactory httpClientFactory,
        IOptions<OpenAiOptions> options,
        ILogger<WhisperVoiceProcessor> logger)
    {
        _httpClient = httpClientFactory.CreateClient("OpenAI");
        _options = options.Value;
        _logger = logger;
    }

    public async Task<Result<VoiceTranscription>> TranscribeAsync(
        byte[] audioData,
        string? filename = null,
        VoiceProcessingOptions? options = null,
        CancellationToken ct = default)
    {
        using var stream = new MemoryStream(audioData);
        return await TranscribeAsync(stream, filename ?? "audio.wav", options, ct);
    }

    public async Task<Result<VoiceTranscription>> TranscribeAsync(
        Stream audioStream,
        string filename,
        VoiceProcessingOptions? options = null,
        CancellationToken ct = default)
    {
        options ??= new VoiceProcessingOptions();

        try
        {
            using var content = new MultipartFormDataContent();

            var streamContent = new StreamContent(audioStream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");
            content.Add(streamContent, "file", filename);
            content.Add(new StringContent(options.Model), "model");
            content.Add(new StringContent(options.ResponseFormat), "response_format");
            content.Add(new StringContent(options.Temperature.ToString()), "temperature");

            if (!string.IsNullOrEmpty(options.Language))
            {
                content.Add(new StringContent(options.Language), "language");
            }

            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _options.ApiKey);

            var response = await _httpClient.PostAsync(
                $"{_options.BaseUrl}/audio/transcriptions",
                content,
                ct);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError("Whisper API error: {StatusCode} - {Body}",
                    response.StatusCode, errorBody);
                return Result<VoiceTranscription>.Failure(
                    $"Transcription failed: {response.StatusCode}");
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            var result = JsonSerializer.Deserialize<WhisperResponse>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (result == null)
                return Result<VoiceTranscription>.Failure("Failed to parse response");

            return Result<VoiceTranscription>.Success(new VoiceTranscription(
                result.Text ?? "",
                result.Language ?? "unknown",
                result.Duration,
                1.0f));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Voice transcription failed");
            return Result<VoiceTranscription>.Failure(ex.Message);
        }
    }

    private sealed record WhisperResponse(
        string? Text,
        string? Language,
        float Duration);
}
