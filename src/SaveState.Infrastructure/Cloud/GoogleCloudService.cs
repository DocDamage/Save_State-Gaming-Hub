using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;

namespace SaveState.Infrastructure.Cloud;

/// <summary>
/// Google Cloud Services integration for AI and speech capabilities.
/// PHASE 1: Production-ready implementation with real JSON parsing, rate limiting, and caching.
/// </summary>
public class GoogleCloudService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GoogleCloudService> _logger;
    private readonly IRateLimiter _rateLimiter;
    private readonly IMemoryCache _cache;
    private readonly string _apiKey;
    private readonly string _projectId;

    private const string RateLimitOperationSpeech = "GoogleCloudSpeech";
    private const string RateLimitOperationVision = "GoogleCloudVision";
    private const string RateLimitOperationTranslation = "GoogleCloudTranslation";
    private const string RateLimitOperationStorage = "GoogleCloudStorage";
    private const int MaxRetries = 3;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

    public GoogleCloudService(
        HttpClient httpClient,
        ILogger<GoogleCloudService> logger,
        IRateLimiter rateLimiter,
        IMemoryCache cache,
        string apiKey,
        string projectId)
    {
        _httpClient = httpClient;
        _logger = logger;
        _rateLimiter = rateLimiter;
        _cache = cache;
        _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
        _projectId = projectId ?? throw new ArgumentNullException(nameof(projectId));
    }

    /// <summary>
    /// Recognizes speech using Google Cloud Speech-to-Text with real JSON parsing.
    /// Includes rate limiting, retry logic, and quality-aware confidence handling.
    /// </summary>
    public async Task<Result<SpeechRecognitionResult>> RecognizeSpeechAsync(
        Stream audioStream,
        string languageCode = "en-US",
        CancellationToken ct = default)
    {
        try
        {
            // Check rate limit
            if (!await _rateLimiter.IsAllowedAsync(_projectId, RateLimitOperationSpeech, ct))
            {
                var resetTime = await _rateLimiter.GetResetTimeAsync(_projectId, RateLimitOperationSpeech, ct);
                var message = resetTime.IsSuccess
                    ? $"Rate limit exceeded. Resets at {resetTime.Value:HH:mm:ss}"
                    : "Rate limit exceeded for speech recognition";
                _logger.LogWarning(message);
                return Result.Failure<SpeechRecognitionResult>(message, ErrorType.RateLimited);
            }

            _logger.LogInformation("Starting Google Cloud speech recognition for language: {Language}", languageCode);

            var uri = $"https://speech.googleapis.com/v1/speech:recognize?key={_apiKey}";

            // Read audio data
            using var memoryStream = new MemoryStream();
            await audioStream.CopyToAsync(memoryStream, ct);
            var audioBytes = memoryStream.ToArray();
            var base64Audio = Convert.ToBase64String(audioBytes);

            // Determine encoding based on audio quality indicators
            var encoding = DetermineAudioEncoding(audioBytes);

            var requestBody = new
            {
                config = new
                {
                    encoding = encoding,
                    languageCode = languageCode,
                    enableAutomaticPunctuation = true,
                    model = "default",
                    useEnhanced = true
                },
                audio = new
                {
                    content = base64Audio
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            // Execute with retry logic
            HttpResponseMessage? response = null;
            for (int attempt = 0; attempt < MaxRetries; attempt++)
            {
                try
                {
                    response = await _httpClient.PostAsync(uri, content, ct);
                    if (response.IsSuccessStatusCode) break;

                    if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                    {
                        var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt + 1));
                        _logger.LogWarning("Rate limited by Google Cloud, retrying in {Delay}s", delay.TotalSeconds);
                        await Task.Delay(delay, ct);
                        continue;
                    }
                    break; // Non-retryable error
                }
                catch (HttpRequestException) when (attempt < MaxRetries - 1)
                {
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt + 1));
                    _logger.LogWarning("HTTP error, retrying in {Delay}s", delay.TotalSeconds);
                    await Task.Delay(delay, ct);
                }
            }

            if (response == null || !response.IsSuccessStatusCode)
            {
                var error = response != null ? await response.Content.ReadAsStringAsync(ct) : "No response";
                _logger.LogError("Google Cloud speech recognition failed: {Error}", error);
                return Result.Failure<SpeechRecognitionResult>(
                    $"Speech recognition failed: {error}",
                    ErrorType.External);
            }

            // Record successful operation for rate limiting
            await _rateLimiter.RecordOperationAsync(_projectId, RateLimitOperationSpeech, ct);

            // Parse the real JSON response
            var resultContent = await response.Content.ReadAsStringAsync(ct);
            var speechResponse = JsonSerializer.Deserialize<GoogleSpeechResponse>(resultContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (speechResponse?.Results == null || speechResponse.Results.Length == 0)
            {
                _logger.LogWarning("No speech results returned from Google Cloud");
                return Result.Success(new SpeechRecognitionResult(
                    RecognizedText: string.Empty,
                    Confidence: 0f,
                    Duration: TimeSpan.Zero,
                    LanguageCode: languageCode,
                    IsFinal: true));
            }

            // Combine all alternatives from all results
            var allText = string.Join(" ", speechResponse.Results
                .Where(r => r.Alternatives != null && r.Alternatives.Length > 0)
                .Select(r => r.Alternatives![0].Transcript));

            var avgConfidence = speechResponse.Results
                .Where(r => r.Alternatives != null && r.Alternatives.Length > 0)
                .Average(r => r.Alternatives![0].Confidence);

            // Estimate duration from audio length (rough calculation: 16kHz, 16-bit mono)
            var estimatedDuration = TimeSpan.FromSeconds(audioBytes.Length / 32000.0);

            _logger.LogInformation("Google Cloud speech recognition completed: {TextLength} chars, {Confidence:P1} confidence",
                allText.Length, avgConfidence);

            return Result.Success(new SpeechRecognitionResult(
                RecognizedText: allText,
                Confidence: (float)avgConfidence,
                Duration: estimatedDuration,
                LanguageCode: languageCode,
                IsFinal: true));
        }
        catch (OperationCanceledException)
        {
            return Result.Failure<SpeechRecognitionResult>("Speech recognition was cancelled", ErrorType.Cancelled);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Google Cloud speech recognition error");
            return Result.Failure<SpeechRecognitionResult>(
                $"Speech recognition failed: {ex.Message}",
                ErrorType.External);
        }
    }

    /// <summary>
    /// Translates text using Google Cloud Translation API.
    /// </summary>
    public async Task<Result<string>> TranslateTextAsync(
        string text,
        string targetLanguage,
        string sourceLanguage = "en",
        CancellationToken ct = default)
    {
        try
        {
            // Check rate limit
            if (!await _rateLimiter.IsAllowedAsync(_projectId, RateLimitOperationTranslation, ct))
            {
                return Result.Failure<string>("Rate limit exceeded for translation", ErrorType.RateLimited);
            }

            // Check cache first
            var cacheKey = $"translate:{sourceLanguage}:{targetLanguage}:{text.GetHashCode()}";
            if (_cache.TryGetValue(cacheKey, out string? cachedTranslation))
            {
                _logger.LogDebug("Translation cache hit for {CacheKey}", cacheKey);
                return Result.Success(cachedTranslation!);
            }

            _logger.LogInformation(
                "Translating text using Google Cloud from {Source} to {Target}",
                sourceLanguage,
                targetLanguage);

            var uri = $"https://translation.googleapis.com/language/translate/v2?key={_apiKey}";

            var requestBody = new
            {
                q = text,
                target = targetLanguage,
                source = sourceLanguage,
                format = "text"
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(uri, content, ct);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError("Google Cloud translation failed: {Error}", error);
                return Result.Failure<string>($"Translation failed: {error}", ErrorType.External);
            }

            await _rateLimiter.RecordOperationAsync(_projectId, RateLimitOperationTranslation, ct);

            var resultContent = await response.Content.ReadAsStringAsync(ct);
            var translationResponse = JsonSerializer.Deserialize<GoogleTranslationResponse>(resultContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            var translatedText = translationResponse?.Data?.Translations?.FirstOrDefault()?.TranslatedText ?? text;

            // Cache the result
            _cache.Set(cacheKey, translatedText, CacheDuration);

            _logger.LogInformation("Google Cloud translation completed");
            return Result.Success(translatedText);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Google Cloud translation error");
            return Result.Failure<string>($"Translation failed: {ex.Message}", ErrorType.External);
        }
    }

    /// <summary>
    /// Uploads file to Google Cloud Storage.
    /// </summary>
    public async Task<Result<string>> UploadFileAsync(
        string bucketName,
        string fileName,
        Stream fileContent,
        CancellationToken ct = default)
    {
        try
        {
            // Check rate limit
            if (!await _rateLimiter.IsAllowedAsync(_projectId, RateLimitOperationStorage, ct))
            {
                return Result.Failure<string>("Rate limit exceeded for storage operations", ErrorType.RateLimited);
            }

            _logger.LogInformation(
                "Uploading file {FileName} to bucket {BucketName}",
                fileName,
                bucketName);

            var uri = $"https://storage.googleapis.com/upload/storage/v1/b/{bucketName}/o?uploadType=media&name={Uri.EscapeDataString(fileName)}&key={_apiKey}";

            using var content = new StreamContent(fileContent);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

            var response = await _httpClient.PostAsync(uri, content, ct);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError("File upload failed: {Error}", error);
                return Result.Failure<string>($"Upload failed: {error}", ErrorType.External);
            }

            await _rateLimiter.RecordOperationAsync(_projectId, RateLimitOperationStorage, ct);

            _logger.LogInformation("File upload completed to Google Cloud Storage");
            return Result.Success($"gs://{bucketName}/{fileName}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "File upload error");
            return Result.Failure<string>($"Upload failed: {ex.Message}", ErrorType.External);
        }
    }

    /// <summary>
    /// Analyzes content using Google Cloud Vision API with real JSON parsing.
    /// Includes rate limiting and response caching.
    /// </summary>
    public async Task<Result<ContentAnalysisResult>> AnalyzeImageAsync(
        string imageUri,
        CancellationToken ct = default)
    {
        try
        {
            // Check rate limit
            if (!await _rateLimiter.IsAllowedAsync(_projectId, RateLimitOperationVision, ct))
            {
                return Result.Failure<ContentAnalysisResult>("Rate limit exceeded for image analysis", ErrorType.RateLimited);
            }

            // Check cache first
            var cacheKey = $"vision:{imageUri.GetHashCode()}";
            if (_cache.TryGetValue(cacheKey, out ContentAnalysisResult? cachedResult))
            {
                _logger.LogDebug("Vision analysis cache hit for {ImageUri}", imageUri);
                return Result.Success(cachedResult!);
            }

            _logger.LogInformation("Analyzing image using Google Cloud Vision: {ImageUri}", imageUri);

            var uri = $"https://vision.googleapis.com/v1/images:annotate?key={_apiKey}";

            var requestBody = new
            {
                requests = new[]
                {
                    new
                    {
                        image = new { source = new { imageUri = imageUri } },
                        features = new object[]
                        {
                            new { type = "LABEL_DETECTION", maxResults = 20 },
                            new { type = "TEXT_DETECTION", maxResults = 50 },
                            new { type = "OBJECT_LOCALIZATION", maxResults = 10 }
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(uri, content, ct);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError("Image analysis failed: {Error}", error);
                return Result.Failure<ContentAnalysisResult>(
                    $"Analysis failed: {error}",
                    ErrorType.External);
            }

            await _rateLimiter.RecordOperationAsync(_projectId, RateLimitOperationVision, ct);

            var resultContent = await response.Content.ReadAsStringAsync(ct);
            var visionResponse = JsonSerializer.Deserialize<GoogleVisionResponse>(resultContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            var firstResult = visionResponse?.Responses?.FirstOrDefault();

            // Extract labels
            var labels = firstResult?.LabelAnnotations?
                .OrderByDescending(l => l.Score)
                .Select(l => l.Description)
                .ToArray() ?? Array.Empty<string>();

            // Extract text (full text annotation or combined text annotations)
            var detectedText = firstResult?.FullTextAnnotation?.Text
                ?? string.Join(" ", firstResult?.TextAnnotations?.Select(t => t.Description) ?? Array.Empty<string>());

            // Extract objects
            var objects = firstResult?.LocalizedObjectAnnotations?
                .OrderByDescending(o => o.Score)
                .Select(o => o.Name)
                .Distinct()
                .ToArray() ?? Array.Empty<string>();

            var result = new ContentAnalysisResult(
                Labels: labels,
                DetectedText: detectedText,
                Objects: objects);

            // Cache the result
            _cache.Set(cacheKey, result, CacheDuration);

            _logger.LogInformation("Image analysis completed: {LabelCount} labels, {ObjectCount} objects detected",
                labels.Length, objects.Length);

            return Result.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Image analysis error");
            return Result.Failure<ContentAnalysisResult>(
                $"Analysis failed: {ex.Message}",
                ErrorType.External);
        }
    }

    /// <summary>
    /// Determines audio encoding based on file signature/magic bytes.
    /// </summary>
    private static string DetermineAudioEncoding(byte[] audioBytes)
    {
        if (audioBytes.Length < 4)
            return "LINEAR16";

        // Check for common audio format signatures
        if (audioBytes[0] == 0x52 && audioBytes[1] == 0x49 && audioBytes[2] == 0x46 && audioBytes[3] == 0x46) // RIFF
            return "LINEAR16"; // WAV
        if (audioBytes[0] == 0xFF && (audioBytes[1] & 0xE0) == 0xE0) // MP3
            return "MP3";
        if (audioBytes[0] == 0x4F && audioBytes[1] == 0x67 && audioBytes[2] == 0x67 && audioBytes[3] == 0x53) // OggS
            return "OGG_OPUS";
        if (audioBytes[0] == 0x66 && audioBytes[1] == 0x4C && audioBytes[2] == 0x61 && audioBytes[3] == 0x43) // fLaC
            return "FLAC";

        return "LINEAR16"; // Default
    }
}

#region Google Cloud API Response Models

internal class GoogleSpeechResponse
{
    public GoogleSpeechResult[]? Results { get; set; }
}

internal class GoogleSpeechResult
{
    public GoogleSpeechAlternative[]? Alternatives { get; set; }
    public bool IsFinal { get; set; }
}

internal class GoogleSpeechAlternative
{
    public string Transcript { get; set; } = string.Empty;
    public float Confidence { get; set; }
}

internal class GoogleTranslationResponse
{
    public GoogleTranslationData? Data { get; set; }
}

internal class GoogleTranslationData
{
    public GoogleTranslation[]? Translations { get; set; }
}

internal class GoogleTranslation
{
    public string TranslatedText { get; set; } = string.Empty;
    public string DetectedSourceLanguage { get; set; } = string.Empty;
}

internal class GoogleVisionResponse
{
    public GoogleVisionAnnotateResponse[]? Responses { get; set; }
}

internal class GoogleVisionAnnotateResponse
{
    public GoogleVisionLabel[]? LabelAnnotations { get; set; }
    public GoogleVisionText[]? TextAnnotations { get; set; }
    public GoogleVisionFullText? FullTextAnnotation { get; set; }
    public GoogleVisionObject[]? LocalizedObjectAnnotations { get; set; }
}

internal class GoogleVisionLabel
{
    public string Description { get; set; } = string.Empty;
    public float Score { get; set; }
}

internal class GoogleVisionText
{
    public string Description { get; set; } = string.Empty;
}

internal class GoogleVisionFullText
{
    public string Text { get; set; } = string.Empty;
}

internal class GoogleVisionObject
{
    public string Name { get; set; } = string.Empty;
    public float Score { get; set; }
}

#endregion
