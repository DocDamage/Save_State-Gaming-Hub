using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly.Wrap;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using SaveState.Core.Ai.Services;
using SaveState.Core.Common;
using SaveState.Core.Configuration;
using SaveState.Infrastructure.Ai.Resilience;

namespace SaveState.Infrastructure.Ai.Providers;

public class GeminiProvider : ILlmProvider
{
    private readonly HttpClient _httpClient;
    private readonly GeminiOptions _options;
    private readonly ILogger<GeminiProvider> _logger;
    private readonly AsyncPolicyWrap _resiliencePolicy;
    private readonly SaveState.Core.Common.Services.IUserPreferencesService _preferencesService;

    public string ProviderName => "Gemini";
    public bool IsAvailable => !string.IsNullOrEmpty(_options.ApiKey);
    public IReadOnlyDictionary<string, ModelInfo> AvailableModels { get; }

    public GeminiProvider(
        HttpClient httpClient,
        IOptions<GeminiOptions> options,
        IAiResiliencePolicy resiliencePolicy,
        SaveState.Core.Common.Services.IUserPreferencesService preferencesService,
        ILogger<GeminiProvider> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _preferencesService = preferencesService;
        _logger = logger;
        _resiliencePolicy = resiliencePolicy.GetPipelinePolicy("Gemini");

        AvailableModels = new Dictionary<string, ModelInfo>
        {
            ["gemini-pro"] = new("Gemini Pro", 32768, 0.00025m),
            ["gemini-pro-vision"] = new("Gemini Pro Vision", 16384, 0.00025m)
        };
    }

    private async Task<string> GetApiKeyAsync(CancellationToken ct)
    {
        var key = await _preferencesService.GetAiApiKeyAsync("Gemini", ct);
        return !string.IsNullOrEmpty(key) ? key : _options.ApiKey;
    }

    public async Task<Result<CompletionResult>> CompleteAsync(CompletionRequest request, CancellationToken ct)
    {
        // Gemini doesn't have a separate completion endpoint, so we use chat
        var messages = new[] { new ChatMessage("user", request.Prompt) };
        var chatRequest = new ChatRequest(messages, request.Model, request.MaxTokens);
        var chatResult = await ChatAsync(chatRequest, ct);

        if (chatResult.IsFailure)
        {
            return Result<CompletionResult>.Failure(chatResult.Error!, chatResult.ErrorType);
        }

        var result = chatResult.Value;
        return Result.Success(new CompletionResult(
            result.Content,
            result.FinishReason,
            result.Usage,
            result.Model));
    }

    public async Task<Result<ChatResult>> ChatAsync(ChatRequest request, CancellationToken ct)
    {
        try
        {
            return await _resiliencePolicy.ExecuteAsync(async (token) =>
            {
                // Convert messages to Gemini format
                var contents = request.Messages.Select(m => new GeminiContent
                {
                    Role = MapRoleToGemini(m.Role),
                    Parts = new[] { new GeminiPart { Text = m.Content } }
                }).ToList();

                var payload = new GeminiRequest
                {
                    Contents = contents,
                    GenerationConfig = new GeminiGenerationConfig
                    {
                        MaxOutputTokens = request.MaxTokens
                    }
                };

                var apiKey = await GetApiKeyAsync(token);
                var endpoint = $"models/{request.Model}:generateContent?key={apiKey}";

                var response = await _httpClient.PostAsJsonAsync(endpoint, payload, token).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<GeminiResponse>(ct).ConfigureAwait(false);
                if (result?.Candidates is null || result.Candidates.Length == 0)
                {
                    return Result.Failure<ChatResult>("Invalid response from Gemini API: empty candidates");
                }

                var candidate = result.Candidates[0];
                if (candidate.Content?.Parts is null || candidate.Content.Parts.Length == 0)
                {
                    return Result.Failure<ChatResult>("Invalid response from Gemini API: missing content or parts");
                }

                var content = string.Join("", candidate.Content.Parts.Select(p => p.Text));
                var finishReason = candidate.FinishReason ?? "stop";

                // Gemini doesn't return token usage in the same format, estimate based on content
                var usage = new TokenUsage(
                    request.Messages.Sum(m => m.Content.Length / 4), // Rough estimate
                    content.Length / 4,
                    (request.Messages.Sum(m => m.Content.Length) + content.Length) / 4);

                var chatResult = new ChatResult(
                    content,
                    finishReason,
                    usage,
                    request.Model);
                return Result.Success<ChatResult>(chatResult);
            }, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gemini API request failed");
            return Result.Failure<ChatResult>($"Gemini API request failed: {ex.Message}", ErrorType.Internal);
        }
    }

    public Task<Result<EmbeddingResult>> GenerateEmbeddingsAsync(EmbeddingRequest request, CancellationToken ct)
        => Task.FromResult(Result.Failure<EmbeddingResult>("Embeddings not yet implemented for Gemini", ErrorType.Internal));

    private static string MapRoleToGemini(string role) => role.ToLowerInvariant() switch
    {
        "system" => "user", // Gemini doesn't support system role, map to user
        "assistant" => "model",
        _ => "user"
    };
}

// Gemini API Request/Response DTOs
internal class GeminiRequest
{
    [JsonPropertyName("contents")]
    public List<GeminiContent> Contents { get; set; } = new();

    [JsonPropertyName("generationConfig")]
    public GeminiGenerationConfig GenerationConfig { get; set; } = new();
}

internal class GeminiContent
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = "user";

    [JsonPropertyName("parts")]
    public GeminiPart[] Parts { get; set; } = Array.Empty<GeminiPart>();
}

internal class GeminiPart
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
}

internal class GeminiGenerationConfig
{
    [JsonPropertyName("maxOutputTokens")]
    public int MaxOutputTokens { get; set; }
}

internal class GeminiResponse
{
    [JsonPropertyName("candidates")]
    public GeminiCandidate[] Candidates { get; set; } = Array.Empty<GeminiCandidate>();

    [JsonPropertyName("usageMetadata")]
    public GeminiUsage? Usage { get; set; }
}

internal class GeminiCandidate
{
    [JsonPropertyName("content")]
    public GeminiContent? Content { get; set; }

    [JsonPropertyName("finishReason")]
    public string? FinishReason { get; set; }

    [JsonPropertyName("index")]
    public int Index { get; set; }
}

internal class GeminiUsage
{
    [JsonPropertyName("promptTokenCount")]
    public int PromptTokens { get; set; }

    [JsonPropertyName("candidatesTokenCount")]
    public int CompletionTokens { get; set; }

    [JsonPropertyName("totalTokenCount")]
    public int TotalTokens { get; set; }
}
