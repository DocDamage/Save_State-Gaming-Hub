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

public class GlmProvider : ILlmProvider
{
    private readonly HttpClient _httpClient;
    private readonly GlmOptions _options;
    private readonly ILogger<GlmProvider> _logger;
    private readonly AsyncPolicyWrap _resiliencePolicy;
    private readonly SaveState.Core.Common.Services.IUserPreferencesService _preferencesService;

    public string ProviderName => "GLM";
    public bool IsAvailable => !string.IsNullOrEmpty(_options.ApiKey);
    public IReadOnlyDictionary<string, ModelInfo> AvailableModels { get; }

    public GlmProvider(
        HttpClient httpClient,
        IOptions<GlmOptions> options,
        IAiResiliencePolicy resiliencePolicy,
        SaveState.Core.Common.Services.IUserPreferencesService preferencesService,
        ILogger<GlmProvider> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _preferencesService = preferencesService;
        _logger = logger;
        _resiliencePolicy = resiliencePolicy.GetPipelinePolicy("GLM");

        _httpClient.BaseAddress = new Uri(_options.BaseUrl);

        AvailableModels = new Dictionary<string, ModelInfo>
        {
            ["glm-4"] = new("GLM-4", 128000, 0.000014m),
            ["glm-4v"] = new("GLM-4V (Multimodal)", 8192, 0.00005m),
            ["glm-3-turbo"] = new("GLM-3-Turbo", 128000, 0.000005m)
        };
    }

    private async Task<string> GetApiKeyAsync(CancellationToken ct)
    {
        var key = await _preferencesService.GetAiApiKeyAsync("GLM", ct);
        return !string.IsNullOrEmpty(key) ? key : _options.ApiKey;
    }

    public async Task<Result<CompletionResult>> CompleteAsync(CompletionRequest request, CancellationToken ct)
    {
        try
        {
            return await _resiliencePolicy.ExecuteAsync(async (token) =>
            {
                // GLM API uses chat/completions for all text generation
                var messages = new[] { new { role = "user", content = request.Prompt } };
                var payload = new
                {
                    model = request.Model,
                    messages = messages,
                    max_tokens = request.MaxTokens,
                    temperature = request.Temperature
                };

                var apiKey = await GetApiKeyAsync(token);
                var requestMsg = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
                {
                    Content = JsonContent.Create(payload)
                };
                requestMsg.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

                var response = await _httpClient.SendAsync(requestMsg, token).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<GlmChatResponse>(ct).ConfigureAwait(false);
                if (result?.Choices is null || result.Choices.Length == 0 || result.Choices[0].Message?.Content is null)
                {
                    return Result.Failure<CompletionResult>("Invalid response from GLM API: empty choices or missing content");
                }

                var completionResult = new CompletionResult(
                    result.Choices[0].Message.Content ?? string.Empty,
                    result.Choices[0].FinishReason ?? string.Empty,
                    new TokenUsage(result.Usage?.PromptTokens ?? 0, result.Usage?.CompletionTokens ?? 0, result.Usage?.TotalTokens ?? 0),
                    result.Model ?? "unknown");
                return Result.Success<CompletionResult>(completionResult);
            }, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GLM API request failed");
            return Result.Failure<CompletionResult>($"GLM API request failed: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<ChatResult>> ChatAsync(ChatRequest request, CancellationToken ct)
    {
        try
        {
            return await _resiliencePolicy.ExecuteAsync(async (token) =>
            {
                var payload = new
                {
                    model = request.Model,
                    messages = request.Messages.Select(m => new { role = m.Role, content = m.Content }),
                    max_tokens = request.MaxTokens
                };

                var apiKey = await GetApiKeyAsync(token);
                var requestMsg = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
                {
                    Content = JsonContent.Create(payload)
                };
                requestMsg.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

                var response = await _httpClient.SendAsync(requestMsg, token).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<GlmChatResponse>(ct).ConfigureAwait(false);
                if (result?.Choices is null || result.Choices.Length == 0 || result.Choices[0].Message?.Content is null)
                {
                    return Result.Failure<ChatResult>("Invalid response from GLM API: empty choices or missing content");
                }

                var chatResult = new ChatResult(
                    result.Choices[0].Message.Content ?? string.Empty,
                    result.Choices[0].FinishReason ?? string.Empty,
                    new TokenUsage(result.Usage?.PromptTokens ?? 0, result.Usage?.CompletionTokens ?? 0, result.Usage?.TotalTokens ?? 0),
                    result.Model ?? "unknown");
                return Result.Success<ChatResult>(chatResult);
            }, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GLM chat request failed");
            return Result.Failure<ChatResult>($"GLM API request failed: {ex.Message}", ErrorType.Internal);
        }
    }

    public Task<Result<EmbeddingResult>> GenerateEmbeddingsAsync(EmbeddingRequest request, CancellationToken ct)
        => Task.FromResult(Result.Failure<EmbeddingResult>("Embeddings not yet implemented", ErrorType.Internal));
}

// Response DTOs
internal record GlmChatResponse(
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("choices")] GlmChatChoice[] Choices,
    [property: JsonPropertyName("usage")] GlmUsage Usage);

internal record GlmChatChoice(
    [property: JsonPropertyName("message")] GlmMessage Message,
    [property: JsonPropertyName("finish_reason")] string FinishReason);

internal record GlmMessage(
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("content")] string Content);

internal record GlmUsage(
    [property: JsonPropertyName("prompt_tokens")] int PromptTokens,
    [property: JsonPropertyName("completion_tokens")] int CompletionTokens,
    [property: JsonPropertyName("total_tokens")] int TotalTokens);
