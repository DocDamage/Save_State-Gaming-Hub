using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.ContentGeneration.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace SaveState.Infrastructure.ContentGeneration.Services;

/// <summary>
/// OpenAI DALL-E implementation of the image generation client.
/// </summary>
public class OpenAiImageClient : IOpenAiImageClient
{
    private readonly HttpClient _httpClient;
    private readonly OpenAiImageOptions _options;
    private readonly ILogger<OpenAiImageClient> _logger;
    private readonly IUserPreferencesService _preferencesService;

    public OpenAiImageClient(
        HttpClient httpClient,
        IOptions<OpenAiImageOptions> options,
        IUserPreferencesService preferencesService,
        ILogger<OpenAiImageClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _preferencesService = preferencesService;
        _logger = logger;

        _httpClient.BaseAddress = new Uri(_options.BaseUrl);
    }

    public async Task<Result<string>> GenerateImageAsync(
        string prompt,
        int width,
        int height,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogDebug("Generating image with prompt: {Prompt}", prompt);

            // Map dimensions to DALL-E supported sizes
            var size = MapToSupportedSize(width, height);

            var apiKey = await GetApiKeyAsync(ct);
            var requestMsg = new HttpRequestMessage(HttpMethod.Post, "images/generations")
            {
                Content = JsonContent.Create(new
                {
                    model = _options.Model,
                    prompt = prompt,
                    n = 1,
                    size = size,
                    response_format = "b64_json"
                })
            };
            requestMsg.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

            var response = await _httpClient.SendAsync(requestMsg, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("Image generation failed: {StatusCode} - {Error}",
                    response.StatusCode, errorContent);
                return Result<string>.Failure($"Image generation failed: {response.StatusCode}",
                    ErrorType.External);
            }

            var result = await response.Content.ReadFromJsonAsync<DalleResponse>(ct).ConfigureAwait(false);

            if (result?.Data is null || result.Data.Length == 0 || string.IsNullOrEmpty(result.Data[0].B64Json))
            {
                return Result<string>.Failure("No image data received from API", ErrorType.External);
            }

            return Result<string>.Success(result.Data[0].B64Json);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate image");
            return Result<string>.Failure($"Image generation failed: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<string>> GenerateImageVariationAsync(
        string baseImageData,
        string prompt,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogDebug("Generating image variation with prompt: {Prompt}", prompt);

            var apiKey = await GetApiKeyAsync(ct);

            // Convert base64 to form content
            var imageBytes = Convert.FromBase64String(baseImageData);
            var content = new MultipartFormDataContent
            {
                { new StringContent(prompt), "prompt" },
                { new StringContent("1"), "n" },
                { new StringContent("1024x1024"), "size" },
                { new StringContent("b64_json"), "response_format" }
            };

            var imageContent = new ByteArrayContent(imageBytes);
            imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
            content.Add(imageContent, "image", "image.png");

            var requestMsg = new HttpRequestMessage(HttpMethod.Post, "images/variations")
            {
                Content = content
            };
            requestMsg.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

            var response = await _httpClient.SendAsync(requestMsg, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("Image variation failed: {StatusCode} - {Error}",
                    response.StatusCode, errorContent);
                return Result<string>.Failure($"Image variation failed: {response.StatusCode}",
                    ErrorType.External);
            }

            var result = await response.Content.ReadFromJsonAsync<DalleResponse>(ct).ConfigureAwait(false);

            if (result?.Data is null || result.Data.Length == 0 || string.IsNullOrEmpty(result.Data[0].B64Json))
            {
                return Result<string>.Failure("No image data received from API", ErrorType.External);
            }

            return Result<string>.Success(result.Data[0].B64Json);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate image variation");
            return Result<string>.Failure($"Image variation failed: {ex.Message}", ErrorType.Internal);
        }
    }

    private async Task<string> GetApiKeyAsync(CancellationToken ct)
    {
        var key = await _preferencesService.GetAiApiKeyAsync("OpenAI", ct);
        return !string.IsNullOrEmpty(key) ? key : _options.ApiKey;
    }

    private static string MapToSupportedSize(int width, int height)
    {
        // DALL-E 3 supports: 1024x1024, 1792x1024, 1024x1792
        // DALL-E 2 supports: 256x256, 512x512, 1024x1024
        var aspectRatio = (double)width / height;

        return aspectRatio switch
        {
            > 1.5 => "1792x1024",  // Landscape
            < 0.7 => "1024x1792",  // Portrait
            _ => "1024x1024"       // Square
        };
    }
}

/// <summary>
/// Configuration options for OpenAI image generation.
/// </summary>
public class OpenAiImageOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.openai.com/v1/";
    public string Model { get; set; } = "dall-e-3";
}

// Response DTOs
internal record DalleResponse(
    [property: JsonPropertyName("data")] DalleImageData[] Data,
    [property: JsonPropertyName("created")] long Created);

internal record DalleImageData(
    [property: JsonPropertyName("b64_json")] string B64Json,
    [property: JsonPropertyName("url")] string? Url,
    [property: JsonPropertyName("revised_prompt")] string? RevisedPrompt);
