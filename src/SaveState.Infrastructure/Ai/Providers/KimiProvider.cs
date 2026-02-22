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

public class KimiProvider : ILlmProvider
{
    private readonly HttpClient _httpClient;
    private readonly KimiOptions _options;
    private readonly ILogger<KimiProvider> _logger;
    private readonly AsyncPolicyWrap _resiliencePolicy;
    private readonly SaveState.Core.Common.Services.IUserPreferencesService _preferencesService;

    public string ProviderName => "Kimi";
    public bool IsAvailable => !string.IsNullOrEmpty(_options.ApiKey);
    public IReadOnlyDictionary<string, ModelInfo> AvailableModels { get; }

    public KimiProvider(
        HttpClient httpClient,
        IOptions<KimiOptions> options,
        IAiResiliencePolicy resiliencePolicy,
        SaveState.Core.Common.Services.IUserPreferencesService preferencesService,
        ILogger<KimiProvider> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _preferencesService = preferencesService;
        _logger = logger;
        _resiliencePolicy = resiliencePolicy.GetPipelinePolicy("Kimi");

        _httpClient.BaseAddress = new Uri(_options.BaseUrl);

        AvailableModels = new Dictionary<string, ModelInfo>
        {
            ["moonshot-v1-8k"] = new("Moonshot V1 8K", 8192, 0.000003m),
            ["moonshot-v1-32k"] = new("Moonshot V1 32K", 32768, 0.000006m),
            ["moonshot-v1-128k"] = new("Moonshot V1 128K", 131072, 0.000012m)
        };
    }

    private async Task<string> GetApiKeyAsync(CancellationToken ct)
    {
        var key = await _preferencesService.GetAiApiKeyAsync("Kimi", ct);
        return !string.IsNullOrEmpty(key) ? key : _options.ApiKey;
    }

    public async Task<Result<CompletionResult>> CompleteAsync(CompletionRequest request, CancellationToken ct)
    {
        try
        {
            return await _resiliencePolicy.ExecuteAsync(async (token) =>
            {
                var payload = new
                {
                    model = request.Model,
                    prompt = request.Prompt,
                    max_tokens = request.MaxTokens,
                    temperature = request.Temperature
                };

                var apiKey = await GetApiKeyAsync(token);
                var requestMsg = new HttpRequestMessage(HttpMethod.Post, "completions")
                {
                    Content = JsonContent.Create(payload)
                };
                requestMsg.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

                var response = await _httpClient.SendAsync(requestMsg, token).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<KimiCompletionResponse>(ct).ConfigureAwait(false);
                if (result?.Choices is null || result.Choices.Length == 0)
                {
                    return Result.Failure<CompletionResult>("Invalid response from Kimi API: empty choices");
                }

                var completionResult = new CompletionResult(
                    result.Choices[0].Text ?? string.Empty,
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
            _logger.LogError(ex, "Kimi API request failed");
            return Result.Failure<CompletionResult>($"Kimi API request failed: {ex.Message}", ErrorType.Internal);
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

                var result = await response.Content.ReadFromJsonAsync<KimiChatResponse>(ct).ConfigureAwait(false);
                if (result?.Choices is null || result.Choices.Length == 0 || result.Choices[0].Message?.Content is null)
                {
                    return Result.Failure<ChatResult>("Invalid response from Kimi API: empty choices or missing content");
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
            _logger.LogError(ex, "Kimi chat request failed");
            return Result.Failure<ChatResult>($"Kimi API request failed: {ex.Message}", ErrorType.Internal);
        }
    }

    public Task<Result<EmbeddingResult>> GenerateEmbeddingsAsync(EmbeddingRequest request, CancellationToken ct)
        => Task.FromResult(Result.Failure<EmbeddingResult>("Embeddings not supported by Kimi provider", ErrorType.Internal));
}

// Response DTOs
internal record KimiCompletionResponse(
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("choices")] KimiChoice[] Choices,
    [property: JsonPropertyName("usage")] KimiUsage Usage);

internal record KimiChatResponse(
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("choices")] KimiChatChoice[] Choices,
    [property: JsonPropertyName("usage")] KimiUsage Usage);

internal record KimiChoice(
    [property: JsonPropertyName("text")] string Text,
    [property: JsonPropertyName("finish_reason")] string FinishReason);

internal record KimiChatChoice(
    [property: JsonPropertyName("message")] KimiMessage Message,
    [property: JsonPropertyName("finish_reason")] string FinishReason);

internal record KimiMessage(
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("content")] string Content);

internal record KimiUsage(
    [property: JsonPropertyName("prompt_tokens")] int PromptTokens,
    [property: JsonPropertyName("completion_tokens")] int CompletionTokens,
    [property: JsonPropertyName("total_tokens")] int TotalTokens);
