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

public class OpenAiProvider : ILlmProvider
{
    private readonly HttpClient _httpClient;
    private readonly OpenAiOptions _options;
    private readonly ILogger<OpenAiProvider> _logger;
    private readonly AsyncPolicyWrap _resiliencePolicy;
    private readonly SaveState.Core.Common.Services.IUserPreferencesService _preferencesService;

    public string ProviderName => "OpenAI";
    public bool IsAvailable => !string.IsNullOrEmpty(_options.ApiKey);
    public IReadOnlyDictionary<string, ModelInfo> AvailableModels { get; }

    public OpenAiProvider(
        HttpClient httpClient,
        IOptions<OpenAiOptions> options,
        IAiResiliencePolicy resiliencePolicy,
        SaveState.Core.Common.Services.IUserPreferencesService preferencesService,
        ILogger<OpenAiProvider> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _preferencesService = preferencesService;
        _logger = logger;
        _resiliencePolicy = resiliencePolicy.GetPipelinePolicy("OpenAI");

        _httpClient.BaseAddress = new Uri(_options.BaseUrl);

        AvailableModels = new Dictionary<string, ModelInfo>
        {
            ["gpt-4"] = new("GPT-4", 8192, 0.00003m),
            ["gpt-3.5-turbo"] = new("GPT-3.5 Turbo", 4096, 0.000002m)
        };
    }

    private async Task<string> GetApiKeyAsync(CancellationToken ct)
    {
        var key = await _preferencesService.GetAiApiKeyAsync("OpenAI", ct);
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

                var result = await response.Content.ReadFromJsonAsync<OpenAiCompletionResponse>(ct).ConfigureAwait(false);
                var completionResult = new CompletionResult(
                    result!.Choices[0].Text,
                    result.Choices[0].FinishReason,
                    new TokenUsage(result.Usage.PromptTokens, result.Usage.CompletionTokens, result.Usage.TotalTokens),
                    result.Model);
                return Result.Success<CompletionResult>(completionResult);
            }, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenAI API request failed");
            return Result.Failure<CompletionResult>($"OpenAI API request failed: {ex.Message}", ErrorType.Internal);
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

                var result = await response.Content.ReadFromJsonAsync<OpenAiChatResponse>(ct).ConfigureAwait(false);
                var chatResult = new ChatResult(
                    result!.Choices[0].Message.Content,
                    result.Choices[0].FinishReason,
                    new TokenUsage(result.Usage.PromptTokens, result.Usage.CompletionTokens, result.Usage.TotalTokens),
                    result.Model);
                return Result.Success<ChatResult>(chatResult);
            }, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenAI chat request failed");
            return Result.Failure<ChatResult>($"OpenAI API request failed: {ex.Message}", ErrorType.Internal);
        }
    }

    public Task<Result<EmbeddingResult>> GenerateEmbeddingsAsync(EmbeddingRequest request, CancellationToken ct)
        => Task.FromResult(Result.Failure<EmbeddingResult>("Embeddings not yet implemented", ErrorType.Internal));
}

// Response DTOs
internal record OpenAiCompletionResponse(
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("choices")] OpenAiChoice[] Choices,
    [property: JsonPropertyName("usage")] OpenAiUsage Usage);

internal record OpenAiChatResponse(
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("choices")] OpenAiChatChoice[] Choices,
    [property: JsonPropertyName("usage")] OpenAiUsage Usage);

internal record OpenAiChoice(
    [property: JsonPropertyName("text")] string Text,
    [property: JsonPropertyName("finish_reason")] string FinishReason);

internal record OpenAiChatChoice(
    [property: JsonPropertyName("message")] OpenAiMessage Message,
    [property: JsonPropertyName("finish_reason")] string FinishReason);

internal record OpenAiMessage(
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("content")] string Content);

internal record OpenAiUsage(
    [property: JsonPropertyName("prompt_tokens")] int PromptTokens,
    [property: JsonPropertyName("completion_tokens")] int CompletionTokens,
    [property: JsonPropertyName("total_tokens")] int TotalTokens);

